"""Original reset/update/draw trace versus production presentation-reset helper."""
import hashlib,json,subprocess
from pathlib import Path
import check_magic_projectiles as magic
from native_unit_reset_emulator import NativeUnitReset
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-unit-reset-checks')
def vectors():
    cases=[]
    for operation in ['hp','mp','miss','attack','death','hp-death']:
        for actor in [0,1,400]:
            for enabled in [False,True]:
                for before in [0,7,28,52]:
                    for job in ([100,700]if 'death'in operation else[100]):
                        units=[dict(id=i,life=1,job=100,x=5+n,y=5,hp=100,maxHp=200,mp=45,maxMp=65,ap=20,maxAp=20,direction=1,nativeState=0,nativeActive=True,nativeEnabled=True)for n,i in enumerate([0,1,400])]
                        unit=next(u for u in units if u['id']==actor);unit.update(nativeEnabled=enabled,job=job)
                        source=actor if actor!=400 else 0;target=400
                        cases.append(dict(name=f'{operation}-{actor}-{enabled}-{before}-{job}',operation=operation,unit=actor,source=source,target=target,value=17 if operation in ('hp','hp-death')else 3,newHp=17,newJob=job,beforeTicks=before,ticks=52,width=12,height=12,units=units))
    for actor in [0,1,400]:
        for before in [0,7]:
            for direction in range(4):
                basis=next(c for c in cases if c['operation']=='attack'and c['unit']==actor and c['units'][0]['nativeEnabled']and c['beforeTicks']==before and next(u for u in c['units']if u['id']==actor)['nativeEnabled'])
                cases.append(dict(basis,name=f'attack-facing-{actor}-{before}-{direction}',directionBeforeReset=direction))
    return cases
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();hint_cases=0
    for actor in [0,1,400]:
        for enabled in [False,True]:
            c=next(c for c in cases if c['operation']=='hp'and c['unit']==actor and c['beforeTicks']==0 and next(u for u in c['units']if u['id']==actor)['nativeEnabled']==enabled)
            native=NativeUnitReset(c)
            try:
                base=native.prepare();native.write(base+0x1fc,99);native.reset();assert native.read(base+0x1fc)==0;hint_cases+=1
            finally:native.close()
    input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeUnitResetDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeUnitResetDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[];samples=0
    for c,current in zip(cases,managed):
        native=NativeUnitReset(c).trace();samples+=len(native)
        if native!=current:
            first=next(i for i,(a,b)in enumerate(zip(native,current))if a!=b);mismatches.append(dict(name=c['name'],step=first,native=native[first],managed=current[first]))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    paths=[Path(__file__),OUT/'native_unit_reset_emulator.py',OUT/'NativeUnitResetDifferential.cs',OUT/'native_feedback_renderer.py',OUT/'check_native_feedback.py',OUT/'native_ai_emulator.py',ROOT/'분석/InstallerVersions/installer_inventory.json',*[ROOT/'Assets/_Original/Runtime/Combat'/name for name in ['NativeUnitSnapshotService.cs','TrainingCombat.cs','BattleFeedback.cs','BattleFeedbackFrames.cs','BattleLifecycle.cs']]]
    result=dict(cases=len(cases),updateSamples=samples,aiHintNativeResetCases=hint_cases,aiHintManagedResetCases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},scope='Actual late UserUnit feedback/death setters, resetcharacter prefix throughB43F, MagicFlag0 and10010210/FCD0 update/draw after already-decoded explicit unit fields. Production reset helper plus production counter/frame models. CString/graphics/audio/path allocation are isolated host boundaries; no bitmap load/SetAIArea/moving-reset continuation, original clock speed, socket/Windows process or Unity Play.',logs=str(WORK))
    (OUT/'native-unit-reset-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[:3]));raise SystemExit(1)
if __name__=='__main__':main()
