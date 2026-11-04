"""Isolated runner controls: LF repair is bounded and assertions stay failures."""
import hashlib
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
print(json.dumps(dict(checks=checks,unityPlay=False,scope='Isolated runner controls; original account/source data not used.')))
