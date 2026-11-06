"""Actual GameCtl54 decoder and result drawing versus production result projections."""
import hashlib,json,struct,subprocess
from pathlib import Path
import check_magic_projectiles as magic
from native_result_emulator import NativeResult
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-result-checks')
def vectors():
    cases=[]
    for count in [0,1,3,5,10]:
        for outcome in [0,1,2,3,4]:
            for variant in [0,1,2]:
                words=[]
                for i in range(count):
                    row=[-123+i,100+i%2,2 if (i+variant)%2 else 0,(i+variant)%3,200 if i%2 else 0,3,1,-1,101,-1,102,-1,-1,103]
                    value=[0,0x80000000,0xffffffff][(variant+i)%3];words.append(struct.pack('<14hI',*row,value))
                packet=b'\x5c\x54'+struct.pack('<hhhIh',outcome,4,2,[0,0x80000000,0xffffffff][variant],count)+b''.join(words)
                cases.append(dict(packet=list(packet)))
    # Vary the two independent repeat counts, including their display-adapter
    # boundary. These are decoder/draw fixtures, not inferred party limits.
    for count in [1,3,5,10]:
        for companions in [0,1,2,4,15,16,50]:
            rows=[]
            for i in range(count):
                row=[-123+i,100,2,i%3,200,companions,companions//2,-1,101,-1,102,-1,-1,103]
                rows.append(struct.pack('<14hI',*row,0x80000000))
            cases.append(dict(packet=list(b'\x5c\x54'+struct.pack('<hhhIh',3,4,2,0xffffffff,count)+b''.join(rows))))
    return cases
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeResultDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeResultDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        run=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(run.stdout+run.stderr)
        if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[];icons=texts=0
    for index,(case,current)in enumerate(zip(cases,managed)):
        native=NativeResult();report=native.receive(case['packet']);draw=native.draw();icons+=len(draw['icons']);texts+=len(draw['texts'])
        if report!=current['report']or draw['icons']!=current['icons']or draw['texts']!=current['texts']or not current['roundtrip']:
            mismatches.append(dict(index=index,native=dict(report=report,**draw),managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,ensure_ascii=False,indent=2))
    sources=[Path(__file__),OUT/'native_result_emulator.py',OUT/'NativeResultDifferential.cs',OUT/'native_battle_status_emulator.py',OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_options_emulator.py',ROOT/'Assets/_Original/Runtime/Campaign/CampaignResult.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Result.cs']
    result=dict(cases=len(cases),nativeIconComparisons=icons,nativeTextComparisons=texts,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,originalGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},scope='Actual GameCtl54 dispatcher/D280 destination records and10007350 result draw versus production decoder/projection helpers used by uGUI. Four outcomes plus unknown0,0/1/3/5/10 rows, signed row-XP text versus unsigned spoils, fixed positions/spacing/dead markers/item overlays. Ability/item lookup returns fixture picture keys; CString/graphics are host boundaries. Enemy-list animation/count is separate from54 and excluded. No server rewards, account growth, pixels, Windows process or Unity Play.',logs=str(WORK))
    (OUT/'native-result-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0],ensure_ascii=False)[:1800]);raise SystemExit(1)
if __name__=='__main__':main()
