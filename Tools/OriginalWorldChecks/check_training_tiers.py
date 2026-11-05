"""Production training tier entry, movement guard, escape, reconnect and save-failure contracts."""
import hashlib,json,os,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-training-tier-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-commander-name-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';exe=WORK/'TrainingTierChecks.dll'
 prepared=not(old/'checks.rsp').is_file()or not(old/'CommanderNameChecks.runtimeconfig.json').is_file()
 if prepared:
  prerequisite=subprocess.run([sys.executable,str(OUT/'check_commander_name.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'prerequisite.log').write_text(prerequisite.stdout+prerequisite.stderr)
  if prerequisite.returncode:raise RuntimeError((prerequisite.stdout+prerequisite.stderr)[-1800:])
 args=(old/'checks.rsp').read_text().replace(str(old/'CommanderNameChecks.dll'),str(exe))+'\n-main:TrainingTierChecks\n'
 for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
  if str(source)not in args:args+='\n"'+str(source)+'"\n'
 sources=[OUT/'TrainingTierChecks.cs']
 for p in sources:
  if str(p)not in args:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'CommanderNameChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(phase+'.log')).write_text(proc.stdout+proc.stderr)
  if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-1800:])
 r=json.loads(proc.stdout);sources+=[Path(__file__),ROOT/'Assets/_Original/Runtime/Campaign/ScenarioGatewaySettings.cs',ROOT/'Assets/_Original/Runtime/Campaign/CampaignResultService.cs',ROOT/'Assets/_Original/Runtime/World/WorldService.cs',ROOT/'Assets/_Original/Data/World/KaosiaWorld.json',OUT/'CommanderNameChecks.cs',OUT/'ClientWindowManagerHost.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Combat.cs',ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Training.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs',ROOT/'Assets/_Original/Data/World/NativeWorldArt.asset']
 from check_native_battle_responses import contract_sources
 fingerprints=contract_sources(args,__file__);fingerprints.update({str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in sources})
 r.update(mismatches=0,sources=fingerprints,preparedCommanderNameTemplate=prepared,logs=str(WORK));(OUT/'training-tier-check.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
