"""31 ingress and result persistence/callback contracts on actual managed services."""
import hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
from check_native_battle_responses import contract_sources
from check_picture_initialization import block
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-result-kill-contracts')
def main():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'NativeResultKillChecks.dll'
    if not(old/'checks.rsp').is_file():
        r=subprocess.run([sys.executable,str(OUT/'check_gateways.py')],cwd=ROOT,text=True,capture_output=True)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'ResultChecks.cs'))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n"'+str(source)+'"\n'
    for name in ['NativeResultChecks.cs','NativeResultKillChecks.cs','NativeResultManagerHost.cs']:args+='\n"'+str(OUT.resolve()/name)+'"\n'
    for name in ['LocalServerManager.Training.cs','LocalServerManager.NativeBattlePackets.cs','LocalServerManager.NativeBattleResponses.cs','LocalServerManager.BattleItems.cs']:args+='\n"'+str(ROOT/'Assets/_Original/Runtime/Entry'/name)+'"\n'
    world=ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs';campaign=ROOT/'Assets/_Original/Runtime/Campaign/LocalServerManager.Campaign.cs';host=WORK/'ActualWorldRequests.cs'
    host.write_text('using System;using DarkSaver.Original.World;using DarkSaver.Original.Campaign;namespace DarkSaver.Original.Entry {public sealed partial class LocalServerManager {'
        +block(world.read_text(),'private WorldReply WorldRequest(')+block(world.read_text(),'private bool IsCurrentWorldReply(')
        +block(campaign.read_text(),'public WorldReply Request_ConfirmScenarioResult(')+block(campaign.read_text(),'private void AdvanceScenarioWorld(')
        +'public void TestAdvanceWorld(double ms)=>AdvanceScenarioWorld(ms);}}')
    args+='\n"'+str(host)+'"\n-main:NativeResultKillEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2600:])
    result=json.loads(r.stdout);api=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    for path,digest in api['codeSha256'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    sources=contract_sources(args,__file__);sources[campaign.relative_to(ROOT).as_posix()]=hashlib.sha256(campaign.read_bytes()).hexdigest()
    result.update(sources=sources,currentApiCodeMatched=True,**{key:api[key]for key in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},logs=str(WORK))
    (OUT/'native-result-kill-contract-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
