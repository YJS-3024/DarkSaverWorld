"""Run the actual local training test authority against an isolated real account service.
Requires check_gateways.py response-file prerequisites (portable runner supported).
"""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).parent
WORK=Path('/tmp/ds-training-test-authority')
def main():
 WORK.mkdir(exist_ok=True)
 old=Path('/tmp/ds-npc-gateway-checks')
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 runtime=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.TrainingTest.cs'
 harness=OUT/'TrainingTestAuthorityChecks.cs'
 policy=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Training.cs'
 constant=next(line.strip()for line in policy.read_text().splitlines()if line.strip().startswith('public const double TrainingMonsterRespawnDelaySeconds'))
 host=WORK/'Policy.cs'
 host.write_text('namespace DarkSaver.Original.Entry {public sealed partial class LocalServerManager {'+constant+'}}')
 exe=WORK/'checks.dll'
 args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe))
 args+='\n-main:TrainingTestAuthorityChecks\n"'+str(harness)+'"\n"'+str(runtime)+'"\n'
 args+='\n\"'+str(host)+'\"\n'
 (WORK/'checks.rsp').write_text(args)
 exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(WORK/'checks.rsp')]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),text=True,capture_output=True)
  (WORK/(name+'.log')).write_text(proc.stdout+proc.stderr)
  if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-2400:])
 result=json.loads(proc.stdout)
 sources=[runtime,policy,harness,Path(__file__)]
 for n in (101,102):
  source=ROOT/f'Assets/_Original/Editor/ScenarioLayouts/{n}.json'
  exported=ROOT/f'Assets/_Original/Resources/TrainingTestLayouts/{n}.json'
  assert source.read_bytes()==exported.read_bytes(),f'{n}: authored placement changed during export'
  sources.extend([source,exported])
 result.update(sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},scope='Production local training test adapter, real isolated service, Unity/resource API stubs; no Unity Play.')
 (OUT/'training-test-authority-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(result))
if __name__=='__main__':main()
