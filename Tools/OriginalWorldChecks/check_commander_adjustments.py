"""Owned adjustment contracts and focused existing primary regressions, without repeating the full check chain."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-commander-adjustment-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet');results={}
 for name in ['CommanderAdjustmentChecks','ClassChangeChecks','StatAdjustmentChecks','MagicLoadoutChecks','CommanderChecks']:
  exe=WORK/(name+'.dll');args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/(name+'.cs')));rsp=WORK/(name+'.rsp');rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
  for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
   p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'-'+phase+'.log')).write_text(p.stdout+p.stderr)
   if p.returncode:raise RuntimeError(name+' '+(p.stdout+p.stderr)[-2000:])
  results[name]=json.loads(p.stdout)
 r=dict(results=results,unityPlay=False,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_commander_adjustments.py',OUT/'CommanderAdjustmentChecks.cs',*sorted((ROOT/'Assets/_Original/Runtime/Growth').glob('*.cs')),ROOT/'Assets/_Original/Runtime/World/WorldModel.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.ClassChange.cs']});(OUT/'commander-adjustments-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
