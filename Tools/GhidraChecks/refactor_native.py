"""Collect, annotate and snapshot selected original functions through live MCP.

Only Ghidra analysis metadata is mutated. Original PE files are hash-checked.
Run collect, review the pseudocode/plan, then apply for one target at a time.
"""
import argparse
import hashlib
import json
from pathlib import Path
import socket

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '분석/Ghidra'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def data(response):
    result = response.get('result', {})
    if response.get('error') or response.get('bridgeError') or result.get('isError'):
        raise RuntimeError(json.dumps(response, ensure_ascii=False)[:1800])
    if 'structuredContent' in result:
        value = result['structuredContent']
    else:
        value = json.loads(next(c['text'] for c in result.get('content', []) if c['type'] == 'text'))
    if isinstance(value, dict) and ('error' in value or 'exception' in value):
        raise RuntimeError(json.dumps(value, ensure_ascii=False)[:1800])
    return value


def request(tool, arguments, destination):
    payload = {'method': 'tools/call', 'params': {'name': tool, 'arguments': arguments}, 'timeout': 900}
    destination.parent.mkdir(parents=True, exist_ok=True)
    request_path = destination.with_suffix('.request.json')
    request_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n')
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as connection:
        connection.settimeout(930)
        connection.connect('/tmp/ds-rea-ghidra.sock')
        connection.sendall((json.dumps(payload) + '\n').encode())
        with connection.makefile('rb') as stream:
            response = json.loads(stream.readline())
    destination.write_text(json.dumps(response, ensure_ascii=False, indent=2) + '\n')
    value = data(response)
    print(json.dumps({'tool': tool, 'path': str(destination.relative_to(ROOT)),
                      'evidence': value.get('evidence_id') if isinstance(value, dict) else None}), flush=True)
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('phase', choices=['collect', 'apply'])
    parser.add_argument('target')
    parser.add_argument('--reuse-open', action='store_true')
    args = parser.parse_args()
    plan = json.loads((OUT / 'refactor-plan.json').read_text())
    target = next(t for t in plan['targets'] if t['name'] == args.target)
    path = Path(target['path'])
    assert digest(path) == target['sha256']
    folder = OUT / target['name']
    folder.mkdir(parents=True, exist_ok=True)
    if args.phase == 'collect':
        if not args.reuse_open:
            request('open_binary', {'path': str(path), 'provider_id': 'ghidra'}, folder / 'open.json')
        for function in target['functions']:
            if function.get('mcpAvailable') is False:
                continue
            address = function['address']
            destination = folder / ('before-' + address + '.json')
            if destination.exists():
                evidence = data(json.loads(destination.read_text()))
            else:
                evidence = request('procedure_pseudo_code', {'procedure': address}, destination)
            assert evidence['subject']['digest']['sha256'] == target['sha256']
            code = evidence['normalized_result']
            assert isinstance(code, str) and code.strip()
            (folder / ('before-' + address + '.c')).write_text(
                '/* Ghidra decompilation; not recovered original source. Evidence: ' + evidence['evidence_id'] + ' */\n' + code)
            request('procedure_callers', {'procedure': address}, folder / ('callers-' + address + '.json'))
        request('export_evidence_bundle', {'path': str(folder / 'collection-evidence-bundle.json')}, folder / 'collection-export.json')
        snapshot = folder / (target['name'] + '.base-snapshot')
        request('close_binary', {'snapshot_path': str(snapshot)}, folder / 'baseline-close.json')
        assert snapshot.exists()
        return

    snapshot = folder / (target['name'] + '.base-snapshot')
    assert snapshot.exists(), 'Save an unmodified baseline snapshot during collection first'
    if not args.reuse_open:
        request('open_binary', {'path': str(path), 'provider_id': 'ghidra',
                                'snapshot_path': str(snapshot)}, folder / 'open-for-annotations.json')
    for function in target['functions']:
        if function.get('mcpAvailable') is False:
            continue
        address = function['address']
        before_path = folder / ('before-' + address + '.json')
        assert before_path.exists(), 'Collect/review before applying annotations'
        request('annotate_native_function', {'procedure': address, 'name': function['name'],
                                            'comment': function['comment']}, folder / ('annotation-' + address + '.json'))
        evidence = request('procedure_pseudo_code', {'procedure': address}, folder / ('after-' + address + '.json'))
        assert evidence['subject']['digest']['sha256'] == target['sha256']
        code = evidence['normalized_result']
        assert function['name'] in code, (address, function['name'])
        (folder / (function['name'] + '.c')).write_text(
            '/* Annotated Ghidra decompilation, original PE unchanged.\n'
            ' * Entry: ' + address + '\n * Evidence: ' + evidence['evidence_id'] + '\n */\n' + code)
    bundle = folder / 'evidence-bundle.json'
    if bundle.exists():
        bundle = folder / 'evidence-bundle-reviewed.json'
    request('export_evidence_bundle', {'path': str(bundle)}, folder / 'export.json')
    # REA refuses snapshot export after metadata mutation. Preserve the baseline
    # plus replayable plan and post-annotation Evidence instead of bypassing it.
    request('close_binary', {}, folder / 'close.json')
    assert digest(path) == target['sha256']
    (folder / 'verified.json').write_text(json.dumps({'target': target['name'],
        'sha256': target['sha256'], 'originalUnchanged': True,
        'annotatedFunctions': sum(f.get('mcpAvailable') is not False for f in target['functions']),
        'plannedProjectFunctions': len(target['functions']), 'snapshot': str(snapshot.relative_to(ROOT)),
        'evidenceBundle': str(bundle.relative_to(ROOT)),
        'snapshotKind': 'unmodified analysis baseline; annotations retained in plan and evidence',
        'snapshotSha256': digest(snapshot)}, indent=2) + '\n')


if __name__ == '__main__':
    main()
