"""Production raw0D FIFO, incoming20 shared notices, grants and real file save."""
import hashlib,json,subprocess
from pathlib import Path
from check_native_battle_responses import contract_sources
from check_picture_initialization import block
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-directed-events')
def main():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-scenario-progress-checks');exe=WORK/'DirectedEvents.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'ScenarioProgressChecks.dll'),str(exe))
    args+='\n'+str(OUT.resolve()/'NativeDirectedEventChecks.cs')+'\n/main:DirectedEventEntry\n'
    sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';rsp=WORK/'checks.rsp';rsp.write_text(args)
    baseline=contract_sources(args,__file__)
    for name in ['Growth/CharacterGrowth.json','Combat/OriginalCombat.json','Combat/MagicMenu.json','Combat/MagicProjectiles.json','Campaign/CampaignCatalog.json','World/KaosiaWorld.json']:
        path=ROOT/'Assets/_Original/Data'/name;baseline[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    exe.with_suffix('.runtimeconfig.json').write_bytes((old/'ScenarioProgressChecks.runtimeconfig.json').read_bytes())
    for phase,cmd in [('compile',[str(dotnet),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        assert r.returncode==0,(r.stdout+r.stderr)[-3500:]
    service_result=json.loads(r.stdout)
    # Reuse the same scenario fixtures but compile the actual manager packet
    # partial. Only its existing minimal host transport is supplied.
    host_source=OUT/'ScenarioManagerHost.cs';host_text=host_source.read_text()
    event='        public event Action<byte[]> Notify_BattlePacketRequested;'
    assert event in host_text
    host_text=host_text.replace(event,'')
    marker='        public void TestAdvance('
    assert marker in host_text
    at=host_text.index(marker)
    host_text=host_text[:at]+'        private WorldReply BattleResult(Func<EntryAccountService,WorldReply> apply)=>WorldRequest(apply);\n        private WorldReply TrainingResult(Func<EntryAccountService,WorldReply> apply)=>WorldRequest(apply);\n'+host_text[at:]
    manager_host=WORK/'ManagerHost.cs';manager_host.write_text(host_text)
    manager_source=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.NativeBattlePackets.cs'
    manager_args=args.replace(str(host_source.resolve()),str(manager_host)).replace('/main:DirectedEventEntry','/main:VictoryManagerEntry')+'\n'+str(manager_source)+'\n/define:VICTORY_MANAGER\n'
    callbacks=old/'ActualWorldCallbacks.cs';callback_text=callbacks.read_text();copied=block(callback_text,'private void PublishNativeScenarioReturn(')
    revised=WORK/'ManagerWorldCallbacks.cs';revised.write_text(callback_text.replace(copied,''));manager_args=manager_args.replace(str(callbacks),str(revised))
    manager_rsp=WORK/'manager.rsp';manager_rsp.write_text(manager_args)
    baseline.update(contract_sources(manager_args,__file__))
    for phase,cmd in [('manager-compile',[str(dotnet),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(manager_rsp)]),('manager-run',[str(dotnet),str(exe),str(ROOT)])]:
        m=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(m.stdout+m.stderr);assert m.returncode==0,(m.stdout+m.stderr)[-3500:]
    manager_result=json.loads(m.stdout)
    for path,digest in baseline.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('source-changed-during-check',path)
    result=service_result;result.update(logs=str(WORK),sources=baseline,manager=manager_result)
    (OUT/'native-directed-event-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
