"""Production commander roster/profile contracts; no Unity Play or original server grants."""
import json,os,subprocess,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-commander-checks');SDK=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');exe=WORK/'CommanderChecks.dll'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'CommanderChecks.cs'))
 rsp=WORK/'checks.rsp';rsp.write_text(args);env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(SDK/'dotnet'),str(next(SDK.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(SDK/'dotnet'),str(exe),str(ROOT)])]:
  if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
  r=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
  if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2200:])
 result=json.loads(r.stdout);result['logs']=str(WORK);result['sources']={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [ROOT/'Assets/_Original/Runtime/Growth/CommanderRoster.cs',ROOT/'Assets/_Original/Runtime/Growth/CommanderRosterService.cs',ROOT/'Assets/_Original/Runtime/World/WorldModel.cs',ROOT/'Assets/_Original/Runtime/World/WorldService.cs']}
 (OUT/'commander-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
