"""Exercise production drag cancellation without invoking Unity or real saves."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-drag-lifecycle-checks')
def main():
 WORK.mkdir(exist_ok=True);sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';exe=WORK/'DragLifecycleChecks.dll';sources=[OUT/'DragLifecycleChecks.cs',ROOT/'Assets/_Original/Runtime/Tutorial/NativeTutorialDrag.cs',ROOT/'Assets/_Original/Runtime/UI/NativeClassMagicPointer.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowClickInput.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowPlacement.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowDrag.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowDragInput.cs'];refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))];args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe),*refs,*map(str,sources)];rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args));exe.with_suffix('.runtimeconfig.json').write_bytes(Path('/tmp/ds-original-entry-checks/EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
  result=subprocess.run(command,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(result.stdout+result.stderr)
  if result.returncode:raise RuntimeError((result.stdout+result.stderr)[-1600:])
 report=json.loads(result.stdout);report.update(logs=str(WORK),sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources+[Path(__file__)]});(OUT/'drag-lifecycle-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
