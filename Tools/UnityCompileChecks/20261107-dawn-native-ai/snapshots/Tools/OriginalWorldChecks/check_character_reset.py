"""Run real editor-only character reset helper against isolated disk stores."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-character-reset-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');exe=WORK/'CharacterResetChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 sources=[OUT/'CharacterResetChecks.cs',ROOT/'Assets/_Original/Editor/LocalCharacterReset.cs']
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(sources[0]))+'\n"'+str(sources[1])+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
  proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(phase+'.log')).write_text(proc.stdout+proc.stderr)
  if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-1800:])
 r=json.loads(proc.stdout);sources+=[Path(__file__),ROOT/'Assets/_Original/Runtime/Entry/LocalAccountFileStore.cs',ROOT/'Assets/_Original/Runtime/Entry/EntryAccountService.cs',ROOT/'Assets/_Original/Editor/LocalEntryAccountWindow.cs']
 r.update(mismatches=0,sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},logs=str(WORK));(OUT/'character-reset-check.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
