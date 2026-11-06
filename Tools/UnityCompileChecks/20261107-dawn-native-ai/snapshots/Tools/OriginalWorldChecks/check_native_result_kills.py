"""Actual GameCtl31 decode/result kill-list draw versus production pure model."""
import hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
from native_result_kills_emulator import NativeResultKills
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-result-kills-checks')
def vectors():
    cases=[]
    for count in [0,1,2,3,4,10,11,12,13,21,22,23,100,1000]:
        for large in [False,True]:
            jobs=[100+i%3 for i in range(count)];pictures={100:700 if large else 100,101:701 if large else 101,102:102}
            cases.append(dict(name=f'{count}-{large}',jobs=jobs,pictures=pictures,steps=12*((count+10)//11)+20,deliveries=[]))
    for before,at,after in [(0,3,1),(1,1,2),(12,7,3),(23,17,12),(23,17,0),(1,9,0),(12,8,23),(100,95,1)]:
        cases.append(dict(name=f'replace-{before}-{at}-{after}',jobs=[100]*before,pictures={100:700},steps=at+100,
                          deliveries=[dict(step=at,jobs=[101]*after)]))
    # Decode-only signed boundaries: never invoke ability/graphics lookup with
    # negative or unknown table keys from these deliberately invalid game records.
    for jobs in [[-32768],[-1],[0,32767],[-32768,-1,0,32767]*250]:
        cases.append(dict(name='signed-decode-'+str(len(jobs)),jobs=jobs,pictures={},steps=0,deliveries=[]))
    return cases
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'NativeResultKillsDifferential.dll'
    if not(old/'checks.rsp').is_file():
        r=subprocess.run([sys.executable,str(OUT/'check_gateways.py')],cwd=ROOT,text=True,capture_output=True)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeResultKillsDifferential.cs'))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n"'+str(source)+'"\n'
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(input),str(output)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[];frames=icons=texts=canaries=0
    for index,(c,current)in enumerate(zip(cases,managed)):
        native=NativeResultKills({int(k):v for k,v in c['pictures'].items()});native.open()
        for address,value in [(0x1006bcf0,0x76543210),(0x1006bd10,0x12345678)]:native.write(address,value)
        result_table=bytes(native.uc.mem_read(0x1006a740,336))
        decoded=native.receive_jobs(c['jobs']);trace=[]
        for address,value in [(0x1006bcf0,0x76543210),(0x1006bd10,0x12345678)]:assert native.read(address)==value;canaries+=1
        assert bytes(native.uc.mem_read(0x1006a740,336))==result_table;canaries+=1
        for step in range(c['steps']):
            for delivery in c['deliveries']:
                if delivery['step']==step:native.receive_jobs(delivery['jobs'])
            frame=native.frame();trace.append(frame);frames+=1;icons+=len(frame['icons']);texts+=len(frame['texts'])
        if decoded!=current['jobs']or not current['roundtrip']:mismatches.append(dict(index=index,decoder=True))
        for step,(a,b)in enumerate(zip(trace,current['frames'])):
            if a!=b:mismatches.append(dict(index=index,name=c['name'],step=step,native=a,managed=b));break
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,ensure_ascii=False,indent=2))
    confirms=0;confirm_traces=[]
    for tutorial in [0,1]:
        for visible in [0,1]:
            for outcome in [1,2,3,4]:
                for control in [0,1]:
                    reference=None
                    for count in [0,1,11]:
                        native=NativeResultKills();native.open();trace=native.confirm(outcome,count,tutorial,visible,control);confirms+=1
                        enabled=not tutorial and visible and control==0
                        assert bool(trace['actions']or trace['callbacks'])==enabled,(tutorial,visible,outcome,control,count,trace)
                        if reference is None:reference=trace
                        else:assert trace==reference,'kill animation count must not gate source confirm'
                        if enabled and outcome==1:assert len(trace['callbacks'])==10 and trace['actions'][0][0]=='script-boundary'
                        if enabled and outcome!=1:assert [a[2]for a in trace['actions']if a[0]=='post']==[0x590,0x4cd]
                        confirm_traces.append(dict(tutorial=tutorial,visible=visible,outcome=outcome,control=control,count=count,**trace))
    (WORK/'confirm-traces.json').write_text(json.dumps(confirm_traces,ensure_ascii=False,indent=2))
    paths=[Path(__file__),OUT/'native_result_kills_emulator.py',OUT/'native_result_emulator.py',OUT/'native_battle_status_emulator.py',OUT/'native_battle_briefing_emulator.py',
        OUT/'native_battle_options_emulator.py',OUT/'native_ai_emulator.py',OUT/'NativeResultKillsDifferential.cs',ROOT/'Assets/_Original/Runtime/Campaign/CampaignResult.cs']
    result=dict(cases=len(cases),nativeDrawCalls=frames,nativeIconComparisons=icons,nativeTextComparisons=texts,nativeCanaryComparisons=canaries,nativeConfirmCases=confirms,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,
        originalGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},
        scope='Actual5C31 dispatcher/DEC0 signed job-list receiver and10007350 kill-list drawing;0..1000 records,11-slot pages, source body/fx rectangles, big picture group7, labels and replacement without resetting source counters. Scene8 confirm-control0 branches vary tutorial/visibility/outcome/remaining count; follow-up script/cleanup/PostMessage are captured host boundaries. Graphic/CString/ability lookup uses explicit fixtures. No death inference, XP/rewards, original wall-clock cadence, pixels, Windows process or Unity Play.',logs=str(WORK))
    (OUT/'native-result-kills-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False))
    if mismatches:print(json.dumps(mismatches[0],ensure_ascii=False)[:2200]);raise SystemExit(1)
if __name__=='__main__':main()
