"""Production temple alignment manager contracts; paired with original native request evidence."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-temple-alignment-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-commander-name-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';exe=WORK/'TempleAlignmentChecks.dll'
 args=(old/'checks.rsp').read_text().replace(str(old/'CommanderNameChecks.dll'),str(exe))+'\n-main:TempleAlignmentChecks\n'
 sources=[OUT/'TempleAlignmentChecks.cs',ROOT/'Assets/_Original/Runtime/World/TempleRevivalAlignment.cs',ROOT/'Assets/_Original/Runtime/World/LocalServerManager.TempleAlignment.cs']
 for p in sources:
  if str(p)not in args:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'CommanderNameChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(phase+'.log')).write_text(proc.stdout+proc.stderr)
  if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-1800:])
 r=json.loads(proc.stdout);sources+=[Path(__file__),OUT/'CommanderNameChecks.cs',OUT/'ClientWindowManagerHost.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.TempleAlignment.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs']
 r.update(mismatches=0,sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},logs=str(WORK));(OUT/'temple-alignment-check.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
