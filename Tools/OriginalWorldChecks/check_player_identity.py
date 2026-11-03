"""Shared F3 metadata receiver/persistence; no inferred native server header wire format."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-player-identity-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');exe=WORK/'PlayerIdentityChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'PlayerIdentityChecks.cs'))
 for p in [ROOT/'Assets/_Original/Runtime/UI/NativeCodePage949.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoInbox.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoTransfer.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Memos.cs',ROOT/'Assets/_Original/Runtime/World/LocalServerManager.PlayerIdentity.cs',OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 r=json.loads(p.stdout);r.update(logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_player_identity.py',OUT/'PlayerIdentityChecks.cs',OUT/'IdentityManagerHost.cs',ROOT/'Assets/_Original/Runtime/World/PlayerIdentity.cs',ROOT/'Assets/_Original/Runtime/World/PlayerIdentityService.cs',ROOT/'Assets/_Original/Runtime/World/LocalServerManager.PlayerIdentity.cs',ROOT/'Assets/_Original/Runtime/World/WorldModel.cs',ROOT/'Assets/_Original/Runtime/World/WorldService.cs']});(OUT/'player-identity-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
