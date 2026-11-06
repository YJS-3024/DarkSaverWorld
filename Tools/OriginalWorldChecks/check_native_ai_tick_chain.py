"""Actual52 command-menu enqueue versus04 AP pulse through the AI frame chain."""
import csv,hashlib,itertools,json,struct,subprocess
from pathlib import Path
from native_ai_follower_chain import NativeAiFollowerQueue
from native_ai_tick_chain import NativeAiTickGame
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-ai-tick-chain')
def main():
    WORK.mkdir(exist_ok=True);table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';raw=next(csv.DictReader(table.open(encoding='utf-8-sig')))
    row={k:int(raw[c])for k,c in [('id','번호'),('mp','MP'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
    specs=[dict(name=f'order{o}-mask{m}-ap{a}-local{local}-team{team}',order=o,mask=m,ap=a,local=local,team=team,active=True,enabled=True)for o,m,a,local,same in itertools.product([0,1,2,4,8],[0,1,8,16,17,255],[0,20],[0,7],[False,True])for team in [local if same else 1]]
    for active,enabled,mask,ap in itertools.product([False,True],[False,True],[1,16],[0,20]):
        specs.append(dict(name=f'lifecycle-{active}-{enabled}-{mask}-{ap}',order=2,mask=mask,ap=ap,local=0,team=0,active=active,enabled=enabled))
    cases=[];projections=[]
    for spec in specs:
        native=NativeAiFollowerQueue(row);game=NativeAiTickGame(native);actor=native.RUNTIME+0x2e0
        native.write(0x1003d540,spec['local']);native.write(native.RUNTIME+0x108,spec['order'])
        native.write(actor+0x64,spec['team']);native.units[1]['team']=spec['team'];native.write(actor+0x30,2)
        native.write(actor,int(spec['active']));native.write(actor+4,int(spec['enabled']));native.write(actor+0x60,19)
        def vitals():return {u['id']:bytes(native.uc.mem_read(native.RUNTIME+u['id']*0x2e0+0x20,16))for u in native.case['units']}
        before_vitals=vitals();initial=native.snapshot()
        game.packet(b'\x5c\x04');pulse=native.snapshot()
        assert pulse['head']==pulse['tail']==0 and not native.input,spec
        assert native.read(actor+0x60)==(20 if spec['active']and spec['enabled']else 19),spec
        # Readiness/activeAI control is delivered separately. AP reaching max
        # from04 alone did not enqueue and no elapsed-time policy was supplied.
        game.packet(b'\x5c\x52'+struct.pack('<hhH',1,spec['ap'],spec['mask']));menu=native.snapshot()
        queued=spec['active']and spec['team']==spec['local']and spec['order']not in [0,1]and(spec['mask']&17)!=0
        assert(menu['head'],menu['tail'])==(0,int(queued)),(spec,menu)
        menuAp=native.read(actor+0x60);assert menuAp==(spec['ap']if spec['active']else 19),spec
        assert native.read(actor+0x10c)==(sum(v for b,v in enumerate([2,16,8,32,1,4,64,128])if spec['mask']&(1<<b))if spec['active']else 0),spec
        gate=game.frame(1,1,1,1,1);after=native.snapshot()
        assert after['head']==after['tail']==int(queued),spec
        assert vitals()==before_vitals,(spec,'No HP/MP authority from readiness control')
        if not queued or not spec['enabled']:assert not native.input,(spec,native.input)
        elif spec['mask']&1:assert len(native.input)==1 and native.input[0]['kind']=='attack',(spec,native.input)
        else:assert len(native.input)<=1 and all(i['kind']=='move'for i in native.input),(spec,native.input)
        assert len(native.sent)==len(native.input)
        cases.append(dict(**spec,initial=initial,pulse=pulse,menu=menu,menuAp=menuAp,queued=bool(queued),gate=gate,after=after,imports=game.imports))
        projections.append(dict(inputs=list(native.input),packets=list(native.sent)));native.close()
    inp=WORK/'cases.json';output=WORK/'managed.json';inp.write_text(json.dumps(projections,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'AiTickChain.dll';harness=OUT/'NativeAiFollowerPacketCorpus.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(output)])]:
        result=subprocess.run(command,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(result.stdout+result.stderr);assert result.returncode==0,(result.stdout+result.stderr)[-2500:]
    actual=json.loads(output.read_text());assert actual==[p['packets']for p in projections]
    from check_native_battle_responses import contract_sources
    sources=contract_sources(args,__file__)
    for path in [table,harness,*[OUT/name for name in ['native_ai_tick_chain.py','native_ai_follower_chain.py','native_ai_frame_chain.py','native_ai_frame_emulator.py','native_commander_action_corpus.py','native_battle_options_emulator.py','native_ai_emulator.py','check_native_magic_target_catalog.py','check_native_magic_packets.py']]]:sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    evidence=ROOT/'분석/Ghidra/Saver_Late/AiTickDispatch'
    for path in evidence.iterdir():
        if path.is_file():sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    report=dict(nativeChains=len(cases),queuedMenuCases=sum(c['queued']for c in cases),producedPackets=sum(len(p['packets'])for p in projections),apZeroAttackRequests=sum(c['ap']==0 and any(i['kind']=='attack'for i in p['inputs'])for c,p in zip(cases,projections)),pulseOnlyEnqueues=0,mismatches=0,unityPlay=False,runtime0CConnected=False,sources=sources,cases=cases,projections=projections,logs=str(WORK),
      scope='Actual GameCtl04->IncTick and52->by-value SetTickMenu->AIStart->FIFO->Update1->full follower action/path/wire. Current team/actor team/order/readiness masks/AP0or20/active/enabled matrix.04 increments AP but never queues;52 queues only active matching local team, noncommander, leader order not0/1, mapped move/attack bits. Disabled+active actor can queue but execution exits. AP0+ready attack can emit a request; no server approval/cost claim. Metadata101/grid/received-team values/rand/allocator/free/presentation/script/send boundaries supplied. No automatic server tick period, actual B2 history, runtime0C scheduling, original whole process or Unity Play.')
    (OUT/'native-ai-tick-chain-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases','projections']},ensure_ascii=False))
if __name__=='__main__':main()
