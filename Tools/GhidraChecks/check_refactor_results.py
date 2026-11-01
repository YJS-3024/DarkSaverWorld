"""Verify live MCP evidence, reopened Ghidra project reports and original hashes."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / '분석/Ghidra'


def read(path):
    return json.loads(path.read_text())


def main():
    plan = read(OUT / 'refactor-plan.json')
    rows, total, mcp_total = [], 0, 0
    for target in plan['targets']:
        source = Path(target['path'])
        assert hashlib.sha256(source.read_bytes()).hexdigest() == target['sha256']
        folder = OUT / target['name']
        project = read(OUT / 'project-verification' / (target['name'] + '.json'))
        typed = read(OUT / 'typed' / target['name'] / 'typed-project-report.json')
        assert project['reopenedProject'] and project['typesVerified'] and project['sourceUnchanged']
        assert project['sourceSha256'] == target['sha256']
        assert project['functionsVerified'] == len(target['functions']) == typed['annotatedFunctions']
        functions = []
        for function in target['functions']:
            address = function['address']
            typed_file = OUT / 'typed' / target['name'] / (function['name'] + '.c')
            assert typed_file.exists() and function['name'] in typed_file.read_text()
            mcp = function.get('mcpAvailable') is not False
            evidence_id = None
            if mcp:
                response = read(folder / ('annotation-' + address + '.json'))
                assert not response['result'].get('isError')
                evidence = response['result']['structuredContent']
                assert evidence['subject']['digest']['sha256'] == target['sha256']
                result = evidence['normalized_result']
                # Ghidra may retain the original C++ class namespace.
                assert result['annotations']['name'].split('::')[-1] == function['name']
                assert result['annotations']['comment'] == function['comment']
                assert result['effects']['scope'] == 'session-analysis-database'
                assert result['effects']['source_bytes_modified'] is False
                after = read(folder / ('after-' + address + '.json'))['result']['structuredContent']
                assert function['name'] in after['normalized_result']
                evidence_id = after['evidence_id']
                mcp_total += 1
            else:
                assert function['recoverBoundary'] and function['requiredCallSite']
                boundary = read(folder / ('resolve-' + function['requiredCallSite'] + '.json'))
                assert boundary['result']['structuredContent']['normalized_result']['found'] is False
            functions.append(dict(address=address, name=function['name'], mcpAnnotated=mcp,
                                  evidenceId=evidence_id, typedCode=str(typed_file.relative_to(ROOT))))
        rows.append(dict(target=target['name'], sourceSha256=target['sha256'], originalUnchanged=True,
                         functionCount=len(functions), functions=functions,
                         recoveredProjectEntries=typed.get('recoveredFunctionEntries', [])))
        total += len(functions)
    native = read(OUT / 'unit-layout-check.json')
    assert len(native['nativeProbes']) == 6
    readiness = read(OUT / 'readiness.json')
    assert readiness['healthy'] is True
    ghidra = next(p for p in readiness['providerInspections'] if p['id'] == 'ghidra')
    assert ghidra['available'] and ghidra['providerVersion'] == '12.1.4'
    result = dict(scope='Selected native analysis refactor, not a complete source recovery or original runtime validation',
                  ghidraVersion='12.1.4', reaVersion='6.8.0', targets=rows,
                  targetCount=len(rows), typedFunctionCount=total, mcpAnnotatedFunctionCount=mcp_total,
                  recoveredHeadlessFunctionCount=total - mcp_total,
                  reopenedProject=True, originalFilesUnchanged=True,
                  nativeSyntheticCases=6, compiledPartialLayouts=True,
                  project='분석/Ghidra/projects/DarkSaverOriginal.gpr',
                  limitations=[
                      'Names/comments and partial struct layouts are analyst annotations, not original source names.',
                      'REA snapshot files contain observations/profile state, not a persistent Ghidra project database.',
                      'MCP did not identify two Saver bodies; the separate project recovered their entries and verified required native call sites.',
                      'Static decompilation and synthetic x86 probes do not establish original Windows playback or Unity Play.',
                      'No additional historical scenario01..25 initial monster roster has been recovered by this refactor.'
                  ])
    (OUT / 'refactor-check.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(dict(targets=len(rows), functions=total, mcpAnnotations=mcp_total,
                          projectOnlyRecovered=total - mcp_total, nativeCases=6,
                          reopenedProject=True, originalsUnchanged=True)))


if __name__ == '__main__':
    main()
