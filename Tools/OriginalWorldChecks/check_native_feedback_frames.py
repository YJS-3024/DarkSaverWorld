"""Compare actual native numeric/attack DirectDraw blits to production Unity frame descriptions."""
import hashlib,json,struct,subprocess
from pathlib import Path
from native_feedback_renderer import Renderer
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-feedback-frame-checks')
def vectors():
    units=[dict(id=0,x=5,y=5,life=1,hp=100,maxHp=200,mp=45,maxMp=65,direction=1,nativeState=0),dict(id=400,x=6,y=5,life=1,hp=50,maxHp=100,mp=7,maxMp=10,direction=0,nativeState=0)]
    cases=[]
    for op in ['hp','mp','miss']:
        for value in [0,29,30,70,71,100,175]if op=='hp'else[0,44,45,65]if op=='mp'else[0]:
            for counter in [0,1,5,10,11,13,14,19,20,25,26,27]:cases.append(dict(name=f'{op}-{value}-{counter}',operation=op,unit=0,value=value,counter=counter,width=12,height=12,units=units))
    for dx,dy in [(1,0),(0,-1),(1,1),(2,0),(3,0),(-2,1)]:
        for flag in [0,1,-1]:
            for counter in [0,1,2,7,8,15,16,47,48,49]:
                copy=json.loads(json.dumps(units));copy[1].update(x=5+dx,y=5+dy)
                cases.append(dict(name=f'attack-{dx}-{dy}-{flag}-{counter}',operation='attack',unit=0,target=400,value=flag,targetFlag=7,counter=counter,width=12,height=12,units=copy))
    for unit in [0,700,701]:
        for job in [100,302,700]:
            for counter in [0,1,7,8,18,19,20]:
                copy=json.loads(json.dumps(units[:1]));copy[0].update(id=unit,job=job)
                cases.append(dict(name=f'death-{unit}-{job}-{counter}',operation='death',unit=unit,job=job,value=0,counter=counter,width=12,height=12,units=copy))
    return cases

def native_frames(case):
    r=Renderer(case);unit=case['unit'];target=case.get('target',400);value=case['value'];counter=case['counter'];base=r.RUNTIME+unit*0x2e0
    for u in case['units']:
        b=r.RUNTIME+u['id']*0x2e0;r.write(b+0x28,u['mp']);r.write(b+0x2c,u['maxMp']);r.write(b+0x294,case.get('targetFlag',0)if u['id']==target else 0)
    if case['operation']=='death':
        r.write(base+0x18,case['job']);r.write(base+0x68,case['job']);r.write(base+0x164,counter);r.write(base+0x100,128);r.write(0x100e040c+case['job']*16,5)
        if counter>=(1 if case['job']//100==7 else 19):r.write(base+0x100,0)
        r.invoke(0x1000fcd0,[unit])
    elif case['operation']=='attack':
        r.uc.mem_write(r.DATA+0x6000,struct.pack('<hhh',target,unit,value));r.invoke(r.exports['AttackChar'],[r.DATA+0x6000])
        for actor in [unit,target]:
            b=r.RUNTIME+actor*0x2e0;r.write(b+0x158,counter)
            if counter>=49:r.write(b+0x100,0)
            r.invoke(0x1000fcd0,[actor])
    else:
        if case['operation']=='miss':r.invoke(r.exports['SetFail'],[unit])
        else:r.invoke(r.exports['SetHpDisplay'if case['operation']=='hp'else'SetMpDisplay'],[(unit&65535)|((value&65535)<<16)])
        r.write(base+0x15c,counter)
        if counter>=27:r.write(base+0x100,0)
        r.invoke(0x1000f970,[unit])
    result=r.frames;r.close();return result

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'FeedbackFrameDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'FeedbackFrameDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());mismatches=[];draws=0
    for case,current in zip(cases,managed):
        native=native_frames(case);draws+=len(native)
        if native!=current:mismatches.append(dict(name=case['name'],native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/BattleFeedback.cs','Assets/_Original/Runtime/Combat/BattleFeedbackFrames.cs','Assets/_Original/Runtime/Combat/BattleLifecycle.cs','Tools/OriginalWorldChecks/FeedbackFrameDifferential.cs','Tools/OriginalWorldChecks/check_native_feedback_frames.py','Tools/OriginalWorldChecks/native_feedback_renderer.py','Tools/OriginalWorldChecks/check_native_feedback.py','Tools/OriginalWorldChecks/native_ai_emulator.py']
    result=dict(cases=len(cases),nativeBlits=draws,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual native1000F970/1000FCD0 and number glyph1000B4E0 execute; capture source sheet/rectangle, destination position and blit order. Empty/format/copy CString and DirectDraw are bounded test boundaries; living body renderer skipped; corpse source rectangles execute with palette index0 and a provided body surface. Not actual Unity rendering, host Windows or frame-rate measurement.',logs=str(WORK))
    (OUT/'native-feedback-frame-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
