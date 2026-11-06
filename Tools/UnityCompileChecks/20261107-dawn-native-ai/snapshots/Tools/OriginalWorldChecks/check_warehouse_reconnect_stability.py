"""Repeat the existing warehouse harness across fresh processes/temp roots.
This investigates a retained Windows first-run failure; Linux passes do not close it.
"""
import hashlib,json,os,platform,subprocess,sys,tempfile
from pathlib import Path
from check_native_battle_responses import contract_sources
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-warehouse-reconnect-stability')
def main():
 WORK.mkdir(exist_ok=True)
 folder=Path('/tmp/ds-facility-contract-regressions');exe=folder/'WarehouseChecks.dll';rsp=folder/'WarehouseChecks.rsp'
 # Recompile the actual current harness and its production dependencies first.
 p=subprocess.run([sys.executable,str(OUT/'check_facility_contract_regressions.py')],cwd=ROOT,capture_output=True,text=True)
 (WORK/'prerequisite.log').write_text(p.stdout+p.stderr)
 if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 dotnet=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk/dotnet';rows=[]
 with tempfile.TemporaryDirectory(prefix='ds-warehouse-repeat-')as temp:
  for group in ['plain','공백 경로','long-'+('x'*100)]:
   target=Path(temp)/group;target.mkdir()
   for repetition in range(4):
    env=dict(os.environ,TMPDIR=str(target),TMP=str(target),TEMP=str(target))
    p=subprocess.run([str(dotnet),str(exe),str(ROOT)],cwd=ROOT,env=env,capture_output=True,text=True)
    name=f'{len(rows):02d}';(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
    row=dict(group=group if group!='plain'and not group.startswith('long-')else('long'if group.startswith('long-')else group),repetition=repetition,exitCode=p.returncode)
    if p.returncode:
     rows.append(row);(WORK/'failed-attempts.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2));raise RuntimeError('Warehouse reconnect stability failure; see '+str(WORK/(name+'.log')))
    row['assertions']=json.loads(p.stdout)['assertions'];rows.append(row)
 sources=contract_sources(rsp.read_text(),__file__);producer=OUT/'check_facility_contract_regressions.py';sources[producer.relative_to(ROOT).as_posix()]=hashlib.sha256(producer.read_bytes()).hexdigest()
 host=platform.system()
 result=dict(processes=len(rows),mismatches=0,unityPlay=False,host=host,windowsProcessExecuted=host=='Windows',windowsFirstFailureResolved=False,assertionsPerProcess=sorted(set(r['assertions']for r in rows)),attempts=rows,
  scope='Existing warehouse ownership/staging/confirmation/file reconnect assertions in new processes with isolated ordinary/Unicode-space/long temp roots. Host and Windows-process execution are explicit. Counts are not summed; repeated success alone does not identify a filesystem fault or close the preserved Windows first failure.',
  sources=sources,logs=str(WORK))
 (OUT/'warehouse-reconnect-stability-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','attempts']},ensure_ascii=False))
if __name__=='__main__':main()
