"""Execute actual IncTick/SetTickMenu machine code; compare counter, state and menu bits."""
import hashlib,json,subprocess
from pathlib import Path
from native_ai_emulator import NativeAi
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-battle-tick-checks')
def vectors():
    units=[dict(id=0,x=5,y=5,job=100,life=1,hp=100,maxHp=100,ap=20,maxAp=20,nativeActive=True,nativeEnabled=True,nativeState=0,troopOrder=2),dict(id=1,x=4,y=5,job=100,life=1,hp=100,maxHp=100,ap=7,maxAp=20,nativeActive=True,nativeEnabled=True,nativeState=64)]
    cases=[]
    for mask in list(range(256))+[256,32768,65535]:
        for ap in [-32768,-1,0,7,32767]:cases.append(dict(name=f'menu-{mask}-{ap}',operation='menu',width=12,height=12,member=1,apInput=ap,menuInput=mask,units=units))
    for active,enabled in [(False,False),(False,True),(True,False)]:
        copy=json.loads(json.dumps(units));copy[1].update(nativeActive=active,nativeEnabled=enabled)
        for operation in ['pulse','menu']:cases.append(dict(name=f'flags-{active}-{enabled}-{operation}',operation=operation,width=12,height=12,member=1,apInput=15,menuInput=255,units=copy))
    for ap in [0,1,19,20]:
        copy=json.loads(json.dumps(units));copy[1]['ap']=ap;cases.append(dict(name=f'pulse-{ap}',operation='pulse',width=12,height=12,member=1,units=copy))
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'BattleTickDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'BattleTickDifferential.cs'));rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    managed=json.loads(output.read_text());mismatches=[];starts=0
    for case,current in zip(cases,managed):
        native=NativeAi(case).run_tick(None if case['operation']=='pulse'else(case['apInput'],case['menuInput']));starts+=len(native['packets'])
        if current['units']!=native['units']:mismatches.append(dict(name=case['name'],native=native['units'],managed=current['units']))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/BattleTicks.cs','Tools/OriginalWorldChecks/BattleTickDifferential.cs','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/check_native_battle_ticks.py']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),
        sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},observedNativeAiStartRequests=starts,scope='Actual x86 IncTick/SetTickMenu counter, native action state and command-mask permutation. Native AIStart attempts are captured for evidence, not claimed to reproduce the entire native enqueue cadence.',logs=str(WORK))
    (OUT/'native-battle-tick-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[:3]));raise SystemExit(1)
if __name__=='__main__':main()
