"""Run unchanged original-client checks with explicit machine path settings.
Example: python Tools/run_original_checks.py --unity-data PATH --check Tools/OriginalWorldChecks/check_training.py
Native PE checks still need the exact original binaries and retain their SHA checks.
"""
import argparse
import ast
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]

def writes_report(path, name):
    """Conservative AST check for report writers, excluding read-only consumers."""
    tree=ast.parse(path.read_text(encoding='utf-8'));aliases=set()
    def contains(node):
        if isinstance(node,ast.Constant):return node.value==name
        if isinstance(node,ast.Name):return node.id in aliases
        if isinstance(node,ast.BinOp)and isinstance(node.op,(ast.Div,ast.Add)):return contains(node.left)or contains(node.right)
        if isinstance(node,ast.Call):
            if isinstance(node.func,ast.Name)and node.func.id in ('Path','PurePath'):return any(contains(arg)for arg in node.args)
            if isinstance(node.func,ast.Attribute)and node.func.attr in ('with_name','joinpath'):return any(contains(arg)for arg in node.args)
            if isinstance(node.func,ast.Attribute)and node.func.attr in ('resolve','absolute')and not node.args:return contains(node.func.value)
        # A read_text/json.loads result is report data, never a report path alias.
        return False
    # A producer may bind OUT/'report.json' to a local path before writing it.
    assignments=[node for node in ast.walk(tree)if isinstance(node,ast.Assign)]
    for _ in range(len(assignments)+1):
        previous=len(aliases)
        for node in assignments:
            if contains(node.value):
                aliases.update(target.id for target in node.targets if isinstance(target,ast.Name))
        if len(aliases)==previous:break
    for node in ast.walk(tree):
        if not isinstance(node,ast.Call)or not isinstance(node.func,ast.Attribute)or not contains(node.func.value):continue
        if node.func.attr in ('write_text','write_bytes'):return True
        if node.func.attr=='open':
            mode=next((kw.value for kw in node.keywords if kw.arg=='mode'),node.args[0]if node.args else None)
            if isinstance(mode,ast.Constant)and isinstance(mode.value,str)and any(c in mode.value for c in 'wax+'):return True
    return False

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-data', type=Path, required=True, help='Unity Editor/Data (Windows) or Editor/Data equivalent')
    parser.add_argument('--work-dir', type=Path, default=Path(tempfile.gettempdir())/'darksaver-checks')
    parser.add_argument('--late-client', type=Path, help='Exact late-version Saver.exe/DLL directory; no version substitution')
    parser.add_argument('--installer-root', type=Path, help='Extracted ds-installers tree matching installer_inventory.json')
    parser.add_argument('--path-map', action='append', default=[], metavar='OLD=NEW', help='Additional explicit prefix mapping')
    parser.add_argument('--normalize-evidence-eol', action='store_true', help='Normalize the generated waiting tutorial analysis JSON only if LF exactly matches its declared SHA256')
    parser.add_argument('--refresh-integration', action='store_true', help='Rerun stale report producers before the final integration audit')
    parser.add_argument('--check', action='append', default=[], help='Repository-relative check_*.py (repeatable, sequential)')
    options = parser.parse_args()
    unity = options.unity_data.resolve()
    if not (unity/'DotNetSdk').is_dir():
        parser.error('--unity-data must contain DotNetSdk')
    work = options.work_dir.resolve(); work.mkdir(parents=True, exist_ok=True)
    rules = [('/workspace/DarkSaverWorld', ROOT.as_posix()),
             ('/workspace/.tools/unity/6000.5.7f1/Editor/Data', unity.as_posix()),
             ('/tmp', work.as_posix()),
             ((ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data').as_posix(), unity.as_posix())]
    if options.late_client:
        late = options.late_client.resolve()
        rules += [('/workspace/.tools/ds-ghidra/installed/saver', late.as_posix()),
                  ('/workspace/DarkSaverWorld/.tools/ds-ghidra/installed/saver', late.as_posix()),
                  ((ROOT/'.tools/ds-ghidra/installed/saver').as_posix(), late.as_posix())]
    if options.installer_root:
        rules.append(('/tmp/ds-installers', options.installer_root.resolve().as_posix()))
    for item in options.path_map:
        if '=' not in item: parser.error('--path-map requires OLD=NEW')
        old, new = item.split('=', 1)
        if not old or not new: parser.error('empty path mapping')
        rules.append((old.replace('\\','/').rstrip('/'), Path(new).resolve().as_posix()))
    env = dict(os.environ, DARKSAVER_CHECK_PATHS=json.dumps(rules), PYTHONDONTWRITEBYTECODE='1',
               DOTNET_CLI_HOME=str(work/'dotnet-home'), PYTHONUTF8='1', PYTHONIOENCODING='utf-8')
    env.update(TMPDIR=str(work), TEMP=str(work), TMP=str(work))
    support = str(ROOT/'Tools/CheckSupport')
    env['PYTHONPATH'] = support + (os.pathsep + env['PYTHONPATH'] if env.get('PYTHONPATH') else '')
    if not options.check and not options.refresh_integration:
        parser.error('specify --check or --refresh-integration')
    normalizations = []
    if options.normalize_evidence_eol:
        name = '분석/Saver_Late/waiting_tutorial_resources.json'
        evidence = ROOT/name
        manifest = json.loads((ROOT/'Tools/OriginalTutorialChecks/tutorial-assets.json').read_text(encoding='utf-8'))
        expected = manifest['sources'][name]
        raw = evidence.read_bytes(); normalized = raw.replace(b'\r\n', b'\n')
        if hashlib.sha256(raw).hexdigest() != expected:
            if hashlib.sha256(normalized).hexdigest() != expected:
                parser.error('Evidence differs beyond CRLF; preserving file: '+name)
            evidence.write_bytes(normalized); normalizations.append(name)
    results = []
    def run_check(name):
        index = len(results)
        path = (ROOT/name).resolve()
        if not path.is_relative_to(ROOT/'Tools') or not path.name.startswith('check_') or path.suffix != '.py' or not path.is_file():
            parser.error('check must be an existing Tools/**/check_*.py: ' + name)
        log = work/(str(index).zfill(3)+'-'+path.parent.name+'-'+path.stem+'.log')
        with log.open('w', encoding='utf-8') as output:
            completed = subprocess.run([sys.executable, str(path)], cwd=ROOT, env=env, stdout=output, stderr=subprocess.STDOUT)
        row = dict(check=path.relative_to(ROOT).as_posix(), exitCode=completed.returncode,
                   status='passed' if completed.returncode == 0 else 'failed', log=str(log))
        results.append(row); print(json.dumps(row, ensure_ascii=False), flush=True)
        return completed.returncode
    for name in options.check:
        run_check(name)
    if options.refresh_integration and not any(r['exitCode'] for r in results):
        folder = ROOT/'Tools/OriginalWorldChecks'
        integration = folder/'check_client_windows_integration.py'
        names = []
        for node in ast.walk(ast.parse(integration.read_text(encoding="utf-8"))):
            if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'names' for t in node.targets):
                names += ast.literal_eval(node.value)
            elif isinstance(node, ast.AugAssign) and isinstance(node.target, ast.Name) and node.target.id == 'names':
                names += ast.literal_eval(node.value)
        spec=importlib.util.spec_from_file_location('validation_resource_inventory',folder/'check_runtime_resource_bindings.py')
        resource_inventory=importlib.util.module_from_spec(spec);spec.loader.exec_module(resource_inventory)
        # Reevaluate after each successful producer; regenerated dependencies can
        # invalidate reports that were previously current. Never rewrite hashes here.
        for attempt in range(128):
            stale = []
            for name in names:
                report = folder/name
                data = json.loads(report.read_text(encoding="utf-8"))
                if any(not (ROOT/source).is_file() or hashlib.sha256((ROOT/source).read_bytes()).hexdigest() != digest
                       for source,digest in data.get('sources', {}).items()):
                    stale.append(report)
            resource_report=folder/'runtime-resource-bindings-check.json'
            if json.loads(resource_report.read_text(encoding='utf-8'))['inventorySha256']!=resource_inventory.inventory()[2] and resource_report not in stale:
                stale.insert(0,resource_report)
            if not stale:
                run_check(integration.relative_to(ROOT).as_posix()); break
            report = stale[0]
            candidates = [p for p in report.parent.glob('check_*.py')
                if p != integration and (repr(report.name) in p.read_text(encoding="utf-8") or '"'+report.name+'"' in p.read_text(encoding="utf-8"))]
            candidates=[p for p in candidates if writes_report(p,report.name)]
            if len(candidates) != 1:
                raise RuntimeError('Cannot identify unique producer for '+str(report)+': '+str(candidates))
            if run_check(candidates[0].relative_to(ROOT).as_posix()): break
        else:
            raise RuntimeError('Report dependencies did not stabilize; inspect logs instead of rewriting hashes')
    (work/'run-results.json').write_text(json.dumps(dict(results=results, normalizations=normalizations, unityPlay=False,
        scope='Original check entrypoints and subprocesses; only paths/compiler argument quoting adapted. No skipped assertions or inferred native file versions.'),ensure_ascii=False,indent=2)+'\n', encoding='utf-8')
    return int(any(r['exitCode'] for r in results))

if __name__ == '__main__':
    raise SystemExit(main())
