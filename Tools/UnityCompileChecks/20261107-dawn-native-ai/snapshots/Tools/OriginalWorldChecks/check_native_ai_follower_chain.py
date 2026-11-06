"""0C/FIFO/Update -> full follower second-tick movement/attack/rest wire."""
import csv,hashlib,json,subprocess
from pathlib import Path
from native_ai_follower_chain import NativeAiFollowerQueue
from native_ai_frame_chain import NativeAiQueueGame
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-ai-follower-chain')
def main():
    WORK.mkdir(exist_ok=True);table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';raw=next(csv.DictReader(table.open(encoding='utf-8-sig')))
    row={k:int(raw[c])for k,c in [('id','번호'),('mp','MP'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
    specs=[('move',1,{},[1,1]),('attack',1,{'attackRange':2},[1,1]),('rest',1,{'hp':20},[1,1]),
      ('hp0',1,{'hp':0},[1,1]),('moving',1,{'nativeState':1},[1,1]),('ap-short',1,{'ap':19},[1,1]),
      ('inactive',1,{'nativeActive':False},[1,1]),('disabled',1,{'nativeEnabled':False},[1,1]),
      ('leader-inactive',1,{},[1,1]),('enemy',401,{},[1,1]),('modal-wait',1,{},[1,2,1]),
      ('duplicate-no-requeue',1,{},[2,0]),('ring-wrap',1,{},[1,1]),('no-response',1,{},[1,1,1,1])]
    cases=[];projections=[]
    for name,member,changes,steps in specs:
        native=NativeAiFollowerQueue(row,head=99 if name=='ring-wrap'else 0);game=NativeAiQueueGame(native)
        actor=next(u for u in native.case['units']if u['id']==member);base=native.RUNTIME+member*0x2e0
        fields={'hp':0x20,'ap':0x60,'nativeState':0x100,'attackRange':0x30,'nativeActive':0,'nativeEnabled':4}
        for key,value in changes.items():native.write(base+fields[key],int(value));actor[key]=value
        if name=='leader-inactive':native.write(native.RUNTIME,0)
        def snapshot():return {u['id']:bytes(native.uc.mem_read(native.RUNTIME+u['id']*0x2e0,0x2e0))for u in native.case['units']}
        before=snapshot();frames=[]
        for i,requests in enumerate(steps):
            modal=name=='modal-wait'and i==1
            for _ in range(0 if modal else requests):game.receive(member)
            gate=game.frame(1,2 if modal else 1,1,1,1)
            frames.append(dict(modal=modal,receiptCount=0 if modal else requests,gate=gate,native=native.snapshot()))
        mutations={}
        for unit,old in before.items():
            new=snapshot()[unit];diff=[i for i,(a,b)in enumerate(zip(old,new))if a!=b]
            if diff:mutations[str(unit)]=diff
            assert not diff or unit in [member,member//10*10]and all(i in range(0x60,0x64)or i in range(0x100,0x10c)or i in range(0x11c,0x120)or i in range(0x1fc,0x200)or i in range(0x298,0x29c)for i in diff),(name,unit,diff)
            assert new[0x20:0x30]==old[0x20:0x30]and new[0x70:0x78]==old[0x70:0x78],(name,'No server vitals/positions inferred')
        kinds=[x['kind']for x in native.input]
        if name in ['move','leader-inactive','ring-wrap','no-response','modal-wait']:assert kinds and kinds[0]=='move',(name,kinds)
        if name in ['rest','hp0']:assert kinds==['rest'],(name,kinds)
        if name in ['attack','enemy']:assert kinds==['attack'],(name,kinds)
        if name in ['ap-short','inactive','disabled','duplicate-no-requeue']:assert not kinds,(name,kinds)
        assert len(native.sent)==len(native.input),(name,native.sent,native.input)
        cases.append(dict(name=name,member=member,changes=changes,frames=frames,mutations=mutations))
        projections.append(dict(inputs=list(native.input),packets=list(native.sent)));native.close()
    inp=WORK/'cases.json';output=WORK/'managed.json';inp.write_text(json.dumps(projections,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'AiFollowerChain.dll';harness=OUT/'NativeAiFollowerPacketCorpus.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(output)])]:
        result=subprocess.run(command,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(result.stdout+result.stderr);assert result.returncode==0,(result.stdout+result.stderr)[-2500:]
    actual=json.loads(output.read_text());assert actual==[p['packets']for p in projections]
    from check_native_battle_responses import contract_sources
    sources=contract_sources(args,__file__)
    for path in [table,harness,*[OUT/name for name in ['native_ai_follower_chain.py','native_ai_frame_chain.py','native_ai_frame_emulator.py','native_commander_action_corpus.py','native_battle_options_emulator.py','native_ai_emulator.py','check_native_magic_target_catalog.py','check_native_magic_packets.py']]]:sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    counts={kind:sum(i['kind']==kind for p in projections for i in p['inputs'])for kind in ['move','attack','rest']}
    for path in (ROOT/'분석/Ghidra/Saver_Late/AiFollowerFrames').iterdir():
        if path.is_file():sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    report=dict(nativeChains=len(cases),producedPackets=sum(len(p['packets'])for p in projections),requestKinds=counts,mismatches=0,unityPlay=False,runtime0CConnected=False,sources=sources,cases=cases,projections=projections,logs=str(WORK),
      scope='Actual GameCtl EB10/0C/Update1 and original UserUnit activation/FIFO/role/follower041F0/movement/target/path/builders across multiple receipt/frame steps. Real24-byte move, attack and rest requests compare against production codecs. HP0, moving, AP-short, inactive/disabled, inactive leader, enemy401, modal wait, duplicate empty-queue, wrap and no server response controlled. Only actor/leader control flags, AP, MoveCharPath scratch11C, hint and clock may change; HP/MP and tile positions remain unchanged. Caller-supplied metadata101/map/teams/rand/allocator/free/presentation queue/script return/send remain boundaries. No runtime0C, server approval, one Windows process, original FPS or Unity rendered Play claim.')
    (OUT/'native-ai-follower-chain-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases','projections']},ensure_ascii=False))
if __name__=='__main__':main()
