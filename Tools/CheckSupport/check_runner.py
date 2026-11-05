"""Isolated runner controls: LF repair is bounded and assertions stay failures."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
ROOT=Path(__file__).resolve().parents[2]
rules=json.loads(os.environ['DARKSAVER_CHECK_PATHS'])
unity=next(new for old,new in rules if old=='/workspace/.tools/unity/6000.5.7f1/Editor/Data')
checks=0
def check(value):
    global checks
    assert value
    checks+=1
with tempfile.TemporaryDirectory(prefix='ds-runner-control-') as temp:
    root=Path(temp);tools=root/'Tools';tools.mkdir()
    shutil.copyfile(ROOT/'Tools/run_original_checks.py',tools/'run_original_checks.py')
    support=tools/'CheckSupport';support.mkdir()
    shutil.copyfile(ROOT/'Tools/CheckSupport/sitecustomize.py',support/'sitecustomize.py')
    manifest=tools/'OriginalTutorialChecks/tutorial-assets.json';manifest.parent.mkdir()
    evidence=root/'분석/Saver_Late/waiting_tutorial_resources.json';evidence.parent.mkdir(parents=True)
    lf=b'{"control":1}\n';crlf=lf.replace(b'\n',b'\r\n')
    manifest.write_text(json.dumps({'sources':{'분석/Saver_Late/waiting_tutorial_resources.json':hashlib.sha256(lf).hexdigest()}}),encoding='utf-8')
    probe=support/'check_probe.py';probe.write_text("assert True\n",encoding='utf-8');probe_bytes=probe.read_bytes()
    cmd=[sys.executable,str(tools/'run_original_checks.py'),'--unity-data',unity,'--work-dir',str(root/'work with spaces'),'--normalize-evidence-eol','--check','Tools/CheckSupport/check_probe.py']
    evidence.write_bytes(crlf)
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode==0);check(evidence.read_bytes()==lf);check(probe.read_bytes()==probe_bytes)
    summary=json.loads((root/'work with spaces/run-results.json').read_text())
    check(summary['normalizations']==['분석/Saver_Late/waiting_tutorial_resources.json'])
    bad=b'{"control":2}\r\n';evidence.write_bytes(bad)
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode!=0 and 'differs beyond CRLF' in r.stderr);check(evidence.read_bytes()==bad)
    evidence.write_bytes(lf);probe.write_text("assert False, 'intentional validation failure'\n",encoding='utf-8');original=probe.read_bytes()
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode!=0);check(probe.read_bytes()==original)
    summary=json.loads((root/'work with spaces/run-results.json').read_text())
    check(summary['results'][0]['status']=='failed' and summary['results'][0]['exitCode']!=0)
    check('intentional validation failure' in Path(summary['results'][0]['log']).read_text())
    spec=importlib.util.spec_from_file_location('runner_controls',tools/'run_original_checks.py');runner=importlib.util.module_from_spec(spec);spec.loader.exec_module(runner)
    for source,expected in [
        ("data=(OUT/'growth-check.json').read_text()",False),
        ("(OUT/'growth-check.json').write_text(data)",True),
        ("path=OUT/'growth-check.json'\npath.write_bytes(data)",True),
        ("path=OUT/'growth-check.json'\nwith path.open('r') as f: data=f.read()",False),
        ("path=OUT/'growth-check.json'\nwith path.open(mode='w') as f: f.write(data)",True),
        ("data=(OUT/'growth-check.json').read_text()\n(OUT/'other.json').write_text(data)",False),
        ("data=json.loads((OUT/'growth-check.json').read_text())\npath=OUT/data['output']\npath.write_text(data)",False),
        ("path=Path('growth-check.json').resolve()\npath.write_text(data)",True),
        ("Path(__file__).with_name('growth-check.json').write_text(data)",True),
        ("path=(OUT/'growth-check.json').read_text()\npath.write_text(data)",False),
        ("# 'growth-check.json'\n(OUT/'other.json').write_text(data)",False)]:
        candidate=support/'check_producer.py';candidate.write_text(source,encoding='utf-8');check(runner.writes_report(candidate,'growth-check.json')==expected)
    check(runner.writes_report(ROOT/'Tools/OriginalWorldChecks/check_growth.py','growth-check.json'))
    check(not runner.writes_report(ROOT/'Tools/OriginalWorldChecks/check_training.py','growth-check.json'))
    check(runner.writes_report(ROOT/'Tools/OriginalWorldChecks/check_nullable_contracts.py','nullable-contract-check.json'))
    check(not runner.writes_report(ROOT/'Tools/OriginalWorldChecks/check_nullable_serialization_analyzer.py','nullable-contract-check.json'))
    world=tools/'OriginalWorldChecks';world.mkdir()
    (root/'fixture.txt').write_text('fixture',encoding='utf-8')
    (world/'growth-check.json').write_text(json.dumps({'sources':{'fixture.txt':'stale'}}),encoding='utf-8')
    (world/'runtime-resource-bindings-check.json').write_text(json.dumps({'inventorySha256':'fixture'}),encoding='utf-8')
    (world/'check_runtime_resource_bindings.py').write_text("def inventory(): return None,None,'fixture'\n",encoding='utf-8')
    (world/'check_growth.py').write_text("from pathlib import Path\nimport json\nOUT=Path(__file__).parent\n(OUT/'growth-check.json').write_text(json.dumps({'sources':{}}))\n",encoding='utf-8')
    (world/'check_training.py').write_text("from pathlib import Path\nOUT=Path(__file__).parent\n(OUT/'growth-check.json').read_text()\nassert False,'read-only consumer was selected'\n",encoding='utf-8')
    (world/'check_client_windows_integration.py').write_text("names=['growth-check.json']\n",encoding='utf-8')
    refresh=[sys.executable,str(tools/'run_original_checks.py'),'--unity-data',unity,'--work-dir',str(root/'refresh with spaces'),'--refresh-integration']
    r=subprocess.run(refresh,capture_output=True,text=True);check(r.returncode==0)
    summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
    check([row['check']for row in summary['results']]==['Tools/OriginalWorldChecks/check_growth.py','Tools/OriginalWorldChecks/check_client_windows_integration.py'])
    (world/'growth-check.json').write_text(json.dumps({'sources':{'fixture.txt':'stale'}}),encoding='utf-8')
    (world/'check_duplicate.py').write_bytes((world/'check_growth.py').read_bytes())
    r=subprocess.run(refresh,capture_output=True,text=True);check(r.returncode!=0 and 'Cannot identify unique producer' in r.stderr)
    check(json.loads((world/'growth-check.json').read_text())['sources']=={'fixture.txt':'stale'})
print(json.dumps(dict(checks=checks,unityPlay=False,scope='Isolated runner controls; original account/source data not used.')))
