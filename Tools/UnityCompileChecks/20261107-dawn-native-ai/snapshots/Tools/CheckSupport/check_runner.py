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
    assembly=root/'분석/Ghidra/Saver_Late/ClientWindows/native-boundaries.asm.txt';assembly.parent.mkdir(parents=True)
    assembly_lf=b'; native control\nret\n';assembly.write_bytes(assembly_lf)
    assembly_manifest=assembly.parent/'evidence.json'
    assembly_manifest.write_text(json.dumps({'assembly':{'asset':assembly.relative_to(root).as_posix(),'sha256':hashlib.sha256(assembly_lf).hexdigest()}}),encoding='utf-8')
    probe=support/'check_probe.py';probe.write_text("assert True\n",encoding='utf-8');probe_bytes=probe.read_bytes()
    cmd=[sys.executable,str(tools/'run_original_checks.py'),'--unity-data',unity,'--work-dir',str(root/'work with spaces'),'--normalize-evidence-eol','--check','Tools/CheckSupport/check_probe.py']
    evidence.write_bytes(crlf)
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode==0);check(evidence.read_bytes()==lf);check(probe.read_bytes()==probe_bytes)
    summary=json.loads((root/'work with spaces/run-results.json').read_text())
    check(summary['normalizations']==['분석/Saver_Late/waiting_tutorial_resources.json'])
    assembly.write_bytes(assembly_lf.replace(b'\n',b'\r\n'));evidence.write_bytes(crlf)
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode==0);check(assembly.read_bytes()==assembly_lf);check(evidence.read_bytes()==lf)
    summary=json.loads((root/'work with spaces/run-results.json').read_text())
    check(summary['normalizations']==['분석/Saver_Late/waiting_tutorial_resources.json',assembly.relative_to(root).as_posix()])
    assembly_bad=assembly_lf.replace(b'ret',b'int3').replace(b'\n',b'\r\n')
    assembly.write_bytes(assembly_bad);evidence.write_bytes(crlf)
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode!=0 and 'differs beyond CRLF' in r.stderr)
    check(assembly.read_bytes()==assembly_bad);check(evidence.read_bytes()==crlf)
    assembly.write_bytes(assembly_lf)
    bad=b'{"control":2}\r\n';evidence.write_bytes(bad)
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode!=0 and 'differs beyond CRLF' in r.stderr);check(evidence.read_bytes()==bad)
    evidence.write_bytes(lf);probe.write_text("assert False, 'intentional validation failure'\n",encoding='utf-8');original=probe.read_bytes()
    r=subprocess.run(cmd,capture_output=True,text=True)
    check(r.returncode!=0);check(probe.read_bytes()==original)
    summary=json.loads((root/'work with spaces/run-results.json').read_text())
    check(summary['results'][0]['status']=='failed' and summary['results'][0]['exitCode']!=0)
    check('intentional validation failure' in Path(summary['results'][0]['log']).read_text())
    # A stale timestamp/size-valid source pyc must not substitute old test code.
    import py_compile
    cached=support/'cached_fixture.py';cached.write_text('VALUE=1\n',encoding='utf-8');stamp=cached.stat().st_mtime_ns
    py_compile.compile(str(cached),doraise=True)
    cached.write_text('VALUE=2\n',encoding='utf-8');os.utime(cached,ns=(stamp,stamp))
    probe.write_text("import cached_fixture\nassert cached_fixture.VALUE==2,'stale Python bytecode was loaded'\n",encoding='utf-8')
    r=subprocess.run(cmd,capture_output=True,text=True);check(r.returncode==0)
    check(cached.read_text(encoding='utf-8')=='VALUE=2\n')
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
    entry=tools/'OriginalEntryChecks/check_entry.py';entry.parent.mkdir()
    entry_source="from pathlib import Path\nimport hashlib,json\np=Path('/tmp/ds-original-entry-checks');p.mkdir(exist_ok=True)\n(p/'contracts.rsp').write_text('fixture')\n(p/'EntryChecks.runtimeconfig.json').write_text('{}')\nr=Path(__file__).resolve().parents[2]\nfiles=[p for folder in ['Runtime','Editor']for p in (r/'Assets/_Original'/folder).rglob('*.cs')]\nPath(__file__).with_name('entry-check.json').write_text(json.dumps({'codeSha256':{p.relative_to(r).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in files}}))\n"
    entry.write_text(entry_source,encoding='utf-8')
    (world/'growth-check.json').write_text(json.dumps({'sources':{'fixture.txt':'stale'}}),encoding='utf-8')
    r=subprocess.run(refresh,capture_output=True,text=True);check(r.returncode==0)
    summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
    check([row['check']for row in summary['results']]==['Tools/OriginalEntryChecks/check_entry.py','Tools/OriginalWorldChecks/check_growth.py','Tools/OriginalWorldChecks/check_client_windows_integration.py'])
    entry.write_text("assert False,'bootstrap should be skipped for existing templates'\n",encoding='utf-8')
    r=subprocess.run(refresh,capture_output=True,text=True);check(r.returncode==0)
    summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
    check([row['check']for row in summary['results']]==['Tools/OriginalWorldChecks/check_client_windows_integration.py'])
    # Existing templates must not mask changed, added or removed C# inputs.
    check(runner.api_sources_current(root))
    code=root/'Assets/_Original/Runtime/Fixture.cs';code.parent.mkdir(parents=True)
    code.write_text('first source',encoding='utf-8');check(not runner.api_sources_current(root))
    entry.write_text(entry_source,encoding='utf-8')
    probe.write_text("from pathlib import Path\nimport json,hashlib\nr=Path(__file__).resolve().parents[2]\nd=json.loads((r/'Tools/OriginalEntryChecks/entry-check.json').read_text())\nassert all(hashlib.sha256((r/p).read_bytes()).hexdigest()==h for p,h in d['codeSha256'].items())\n",encoding='utf-8')
    for changed in ['first source','changed source',None]:
        if changed is None:code.unlink()
        else:code.write_text(changed,encoding='utf-8')
        r=subprocess.run(refresh+['--check','Tools/CheckSupport/check_probe.py'],capture_output=True,text=True);check(r.returncode==0)
        summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
        check([row['check']for row in summary['results']][:2]==['Tools/OriginalEntryChecks/check_entry.py','Tools/CheckSupport/check_probe.py'])
        check(runner.api_sources_current(root))
    code.write_text('compile must fail',encoding='utf-8')
    stale=(entry.parent/'entry-check.json').read_bytes()
    entry.write_text("assert False,'preflight compilation failure'\n",encoding='utf-8')
    r=subprocess.run(refresh+['--check','Tools/CheckSupport/check_probe.py'],capture_output=True,text=True);check(r.returncode!=0)
    summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
    check(len(summary['results'])==1 and summary['results'][0]['check']=='Tools/OriginalEntryChecks/check_entry.py')
    check((entry.parent/'entry-check.json').read_bytes()==stale)
    code.unlink()
    (root/'refresh with spaces/ds-original-entry-checks/EntryChecks.runtimeconfig.json').unlink()
    r=subprocess.run(refresh,capture_output=True,text=True);check(r.returncode!=0)
    summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
    check(len(summary['results'])==1 and summary['results'][0]['check']=='Tools/OriginalEntryChecks/check_entry.py'and summary['results'][0]['status']=='failed')
    entry.unlink()
    (world/'growth-check.json').write_text(json.dumps({'sources':{'fixture.txt':'stale'}}),encoding='utf-8')
    (world/'check_duplicate.py').write_bytes((world/'check_growth.py').read_bytes())
    r=subprocess.run(refresh,capture_output=True,text=True);check(r.returncode!=0 and 'Cannot identify unique producer' in r.stderr)
    check(json.loads((world/'growth-check.json').read_text())['sources']=={'fixture.txt':'stale'})
    summary=json.loads((root/'refresh with spaces/run-results.json').read_text())
    check(summary['status']=='failed'and not summary['completed'])
    check(summary['results']==[] and summary['failure']['type']=='RuntimeError')
    check('unique producer'in summary['failure']['message'])
# Real subprocess controls for dependency order, mapped host reuse and refusal.
with tempfile.TemporaryDirectory(prefix='ds-host-preflight-control-')as temporary:
    root=Path(temporary);tools=root/'Tools';tools.mkdir();support=tools/'CheckSupport';support.mkdir();world=tools/'OriginalWorldChecks';world.mkdir()
    shutil.copyfile(ROOT/'Tools/run_original_checks.py',tools/'run_original_checks.py');shutil.copyfile(ROOT/'Tools/CheckSupport/sitecustomize.py',support/'sitecustomize.py')
    spec=importlib.util.spec_from_file_location('host_runner_controls',tools/'run_original_checks.py');host_runner=importlib.util.module_from_spec(spec);spec.loader.exec_module(host_runner)
    marker='/tmp/'+'ds-npc-gateway-checks';progress_marker='/tmp/'+'ds-scenario-progress-checks';campaign_marker='/tmp/'+'ds-original-campaign-checks'
    def producer(marker,names):
        return 'from pathlib import Path\np=Path('+repr(marker)+');p.mkdir(parents=True,exist_ok=True)\n'+''.join('(p/'+repr(name)+').write_text('+repr('actual isolated producer')+')\n'for name in names)
    gateway=world/'check_gateways.py';gateway.write_text(producer(marker,['checks.rsp','GatewayChecks.runtimeconfig.json']))
    consumer=world/'check_consumer.py';consumer.write_text('from pathlib import Path\np=Path('+repr(marker)+')\nassert (p/"checks.rsp").read_text()=="actual isolated producer"\n')
    def invoke(work,*extra,script='Tools/OriginalWorldChecks/check_consumer.py'):
        return subprocess.run([sys.executable,str(tools/'run_original_checks.py'),'--unity-data',unity,'--work-dir',str(work),'--check',script,*extra],capture_output=True,text=True)
    work=root/'first host with spaces';r=invoke(work);check(r.returncode==0)
    summary=json.loads((work/'run-results.json').read_text());check([x['check']for x in summary['results']]==['Tools/OriginalWorldChecks/check_gateways.py','Tools/OriginalWorldChecks/check_consumer.py'])
    check(summary['results'][0]['preparedFor']=='Tools/OriginalWorldChecks/check_consumer.py')
    stamp=work/'ds-npc-gateway-checks/compiler-host-preparation.json'
    check(json.loads(stamp.read_text())['status']=='passed')
    # A prior failed/interrupted producer is not trusted just because its files exist.
    for state in ['failed','running']:
        stamp.write_text(json.dumps({'status':state}))
        r=invoke(work);check(r.returncode==0)
        check(len(json.loads((work/'run-results.json').read_text())['results'])==2)
        check(json.loads(stamp.read_text())['status']=='passed')
    for malformed_state in [[],None,3]:
        stamp.write_text(json.dumps(malformed_state))
        r=invoke(work);check(r.returncode==0)
        check(len(json.loads((work/'run-results.json').read_text())['results'])==2)
        check(json.loads(stamp.read_text())['status']=='passed')
    (work/'ds-npc-gateway-checks/checks.rsp').write_text('changed compiler input')
    r=invoke(work);check(r.returncode==0)
    check((work/'ds-npc-gateway-checks/checks.rsp').read_text()=='actual isolated producer')
    gateway.write_text("assert False,'existing mapped template must be reused'\n")
    r=invoke(work);check(r.returncode==0);check(len(json.loads((work/'run-results.json').read_text())['results'])==1)
    shared=root/'explicit shared host with spaces';shared.mkdir()
    for name in ['checks.rsp','GatewayChecks.runtimeconfig.json']:(shared/name).write_text('actual isolated producer')
    mapped=root/'mapped work';r=invoke(mapped,'--path-map',marker+'='+str(shared));check(r.returncode==0)
    summary=json.loads((mapped/'run-results.json').read_text());check(len(summary['results'])==1)
    check(host_runner.mapped_template_path(marker,[('/tmp',str(mapped)),(marker,str(shared))])==shared)
    check(host_runner.mapped_template_path(str(shared),[('/tmp',str(mapped)),(marker,str(shared))])==shared)
    # One missing required config must rerun the producer, not trust rsp alone.
    (work/'ds-npc-gateway-checks/GatewayChecks.runtimeconfig.json').unlink();report=world/'unchanged-report.json';report.write_text('original report bytes')
    r=invoke(work);check(r.returncode!=0)
    summary=json.loads((work/'run-results.json').read_text());check(len(summary['results'])==1 and summary['results'][0]['status']=='failed')
    check(report.read_text()=='original report bytes')
    gateway.write_text('pass\n');missing=root/'missing outputs';r=invoke(missing);check(r.returncode!=0 and 'did not create required files'in r.stderr)
    summary=json.loads((missing/'run-results.json').read_text());check(summary['status']=='failed'and not summary['completed'])
    # A source mentioning a path in documentation is not a Path consumer.
    consumer.write_text('"documentation '+marker+'"\nassert True\n');r=invoke(root/'documentation only');check(r.returncode==0)
    check(len(json.loads((root/'documentation only/run-results.json').read_text())['results'])==1)
    consumer.write_text('def invalid syntax\n');malformed=root/'syntax failure';r=invoke(malformed);check(r.returncode!=0)
    summary=json.loads((malformed/'run-results.json').read_text());check(summary['completed']and summary['status']=='failed'and len(summary['results'])==1)
    # Progress consumes a real campaign host; dependencies are logged first.
    campaign=world/'check_campaign.py';campaign.write_text(producer(campaign_marker,['checks.rsp','CampaignChecks.runtimeconfig.json']))
    progress=world/'check_scenario_progress.py';progress.write_text('from pathlib import Path\nassert (Path('+repr(campaign_marker)+')/"checks.rsp").is_file()\n'+producer(progress_marker,['checks.rsp','ScenarioProgressChecks.runtimeconfig.json','ActualWorldCallbacks.cs']))
    consumer.write_text('from pathlib import Path\nassert (Path('+repr(progress_marker)+')/"ActualWorldCallbacks.cs").is_file()\n');nested=root/'nested progress';r=invoke(nested);check(r.returncode==0)
    summary=json.loads((nested/'run-results.json').read_text());check([Path(x['check']).stem for x in summary['results']]==['check_campaign','check_scenario_progress','check_consumer'])
    # A prerequisite cycle is an error before any consumer or producer executes.
    consumer.write_text('from pathlib import Path\nassert (Path('+repr(marker)+')/"checks.rsp").is_file()\n')
    gateway.write_text('from pathlib import Path\np=Path('+repr(campaign_marker)+')\n')
    campaign.write_text('from pathlib import Path\np=Path('+repr(marker)+')\n');cycle=root/'cycle';r=invoke(cycle);check(r.returncode!=0 and 'prerequisite cycle'in r.stderr)
    summary=json.loads((cycle/'run-results.json').read_text());check(summary['status']=='failed'and summary['results']==[])
sources=[Path(__file__),ROOT/'Tools/run_original_checks.py',ROOT/'Tools/CheckSupport/sitecustomize.py']
report=dict(checks=checks,mismatches=0,unityPlay=False,
    scope='Isolated runner controls: declared-SHA two-target EOL repair, semantic-change refusal, failure propagation, unique AST producer selection and fresh-work-dir compiler bootstrap and stale API source coverage refresh before explicit consumers, isolated Python cache prefix rejects stale source pyc; real missing compiler-host producers run before consumers, explicit mapped hosts are reused, missing output/failure/cycles stop dependent execution and recorded failed/interrupted/changed hosts are rebuilt. Original accounts/source data not used; no Windows process or Unity execution.',
    sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources})
Path(__file__).with_name('runner-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
