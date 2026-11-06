"""Actual0C/FIFO/Update1 chain with original user-unit AI and wire builders."""
import csv,hashlib,json,subprocess
from pathlib import Path
from native_ai_frame_chain import NativeAiQueueUser,NativeAiQueueGame
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-ai-frame-chain')
def main():
    WORK.mkdir(exist_ok=True);table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';raw=next(csv.DictReader(table.open(encoding='utf-8-sig')))
    row={k:int(raw[c])for k,c in [('id','번호'),('mp','MP'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
    assert row['id']==101
    specs=[('commander',[400],0,[(1,1,1),(1,1,1)],1),('duplicate',[400,400],0,[(1,1,1),(1,1,1)],1),
      ('follower-first',[1,400],0,[(1,1,1),(1,1,1)],1),('commander-first',[400,1],0,[(1,1,1),(1,1,1)],1),
      ('modal-ready',[400],0,[(1,2,1),(1,1,1)],1),('modal-loading',[400],0,[(0,2,1),(1,1,1)],1),
      ('phase0',[400],0,[(1,1,0),(1,1,1)],1),('own-commander',[0],0,[(1,1,1)],0),('excluded800',[800],0,[(1,1,1)],0),
      ('missing-occupant',[400],0,[(1,1,1)],0),('inactive',[400],0,[(1,1,1)],0),('ring-wrap',[400],99,[(1,1,1)],1)]
    cases=[];projections=[]
    for name,ids,head,phases,count in specs:
        native=NativeAiQueueUser(row,'inactive'if name=='inactive'else'ready',head);game=NativeAiQueueGame(native)
        def vitals():return {u['id']:bytes(native.uc.mem_read(native.RUNTIME+u['id']*0x2e0+0x20,16))for u in native.case['units']}
        before_vitals=vitals()
        if name=='missing-occupant':
            u=next(u for u in native.case['units']if u['id']==400);native.write(0x1003d5a0+(u['x']*200+u['y'])*4,-1)
        for unit in ids:game.receive(unit)
        assert game.imports==[call for unit in ids for call in [['setmonsterai',unit],['AIStart',unit]]],(name,game.imports)
        queued=[]if name in ['own-commander','excluded800','missing-occupant']else list(dict.fromkeys(ids))
        state=native.snapshot();assert(state['head'],state['tail'])==(head,(head+len(queued))%100),(name,state)
        frames=[];consumed=[];cursor=head
        for ready,mode,phase in phases:
            gate=game.frame(ready,mode,1,phase,1)
            runs=phase==1 and mode!=2
            if runs and queued:consumed.append(queued.pop(0));cursor=(cursor+1)%100
            state=native.snapshot();assert state['head']==cursor and state['tail']==(head+len(set(ids))*(name not in ['own-commander','excluded800','missing-occupant']))%100,(name,state)
            frames.append(dict(input=[ready,mode,phase],gate=gate,native=state))
        assert len(native.sent)==count and not queued,(name,native.sent,queued)
        assert vitals()==before_vitals,'AI activation/processing changed HP/MP instead of client control: '+name
        if 1 in ids:assert native.read(native.RUNTIME+0x2e0+0x100)==48,(name,'full AP follower prepares30 and emits no packet on first consumption')
        cases.append(dict(name=name,receivedUnits=ids,initialHead=head,consumed=consumed,frames=frames,imports=game.imports))
        projections.append(dict(metadata=row,inputs=list(native.input),packets=list(native.sent)));native.close()
    input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(projections,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'AiFrameChain.dll';harness=OUT/'NativeCommanderPacketCorpus.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(input),str(output)])]:
        run=subprocess.run(command,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(run.stdout+run.stderr);assert run.returncode==0,(run.stdout+run.stderr)[-2500:]
    actual=json.loads(output.read_text());assert actual==[c['packets']for c in projections]
    from check_native_battle_responses import contract_sources
    sources=contract_sources(args,__file__)
    for path in [table,*[OUT/name for name in ['native_ai_frame_chain.py','native_ai_frame_emulator.py','native_commander_action_corpus.py','native_battle_options_emulator.py','native_ai_emulator.py','check_native_magic_target_catalog.py','check_native_magic_packets.py']]]:sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    report=dict(nativeChains=len(cases),producedPackets=sum(len(c['packets'])for c in projections),mismatches=0,unityPlay=False,runtime0CConnected=False,sourceGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',sourceUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources=sources,cases=cases,logs=str(WORK),
      scope='Actual GameCtl EB10/0C/Update1 imports scalar calls into a persistent original UserUnit instance. Real setmonsterai/AIStart/100-slot FIFO/role/action/target/CheckMagicTarget/wire builders execute. Dedup, commander/follower FIFO, modal gates, phase0, wrap, excluded0/800, missing native map occupancy and inactive actor checked; follower first tick only prepares30. HP/MP bytes unchanged, emitted packet inputs match current production codecs. Metadata101/map/team/rand/allocator/free/script-return/send boundaries supplied. No one-process relocation, original heap/Windows timing, follower movement second tick, all metadata in this chain, production0C scheduling or server rules/Unity Play claim.')
    (OUT/'native-ai-frame-chain-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
