"""Compile production drag ownership guards and exercise real context-change cases."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-item-drag-context')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ItemDragContextChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 source=ROOT/'Assets/_Original/Runtime/UI/NativeItemDragContext.cs';args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'ItemDragContextChecks.cs'))+'\n"'+str(source)+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1900:])
 r=json.loads(p.stdout);r.update(mismatches=0,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [Path(__file__),OUT/'ItemDragContextChecks.cs',source,*[ROOT/'Assets/_Original/Runtime/World'/name for name in ['WorldSceneRoot.Personal.cs','WorldSceneRoot.Facilities.cs','WorldSceneRoot.Commanders.cs']]]});(OUT/'item-drag-context-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
