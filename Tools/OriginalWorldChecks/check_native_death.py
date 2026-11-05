"""Run actual death/remove setters and gated death update with captured native sound calls."""
import hashlib,json,subprocess
from pathlib import Path
from check_native_feedback import NativeFeedback
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-death-checks')
class NativeDeath(NativeFeedback):
    def __init__(self,case):
        super().__init__(case);self.sounds=[]
        for u in case['units']:self.write(0x100e42a0+4*(u['y']+u['x']*200),u['id'])
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='set-occupant':
            x,y,unit=self.args(3);self.write(0x100e42a0+4*(y+x*200),unit)
        if name and ('S_PlaySound' in name or 'StopSound' in name):
            assert self.args(1)[0]==1;self.sounds.append('stop'if 'StopSound'in name else'play');self.ret();return
        super()._dispatch(uc,address,size,data)
    def run_death(self):
        c=self.case;unit=c['unit'];base=self.RUNTIME+unit*0x2e0;self.write(base+0x68,c['job'])
        if c['numeric']:self.invoke(self.exports['SetHpDisplay'],[unit|((c['units'][0]['hp']&65535)<<16)])
        self.invoke(self.exports['dmKillChar2'if c['operation']=='death'else'dmKillChar'],[unit]);trace=[]
        for step in range(c['ticks']+1):
            trace.append(dict(active=self.read(base)!=0,enabled=self.read(base+4)!=0,dying=(self.read(base+0x100)&128)!=0,held=(self.read(base+0x100)&64)!=0,counter=self.read(base+0x164),occupied=self.read(0x100e42a0+4*(c['units'][0]['y']+c['units'][0]['x']*200))==unit,sounds=self.sounds));self.sounds=[]
            if step<c['ticks']and self.read(base)!=0 and self.read(base+4)!=0:self.invoke(0x10010210,[unit])
        self.close();return trace

def vectors():
    cases=[]
    for job in [100,301,302,401,700,799]:
        for numeric in [False,True]:
            for op,state in [('death',0),('death',16),('death',48),('remove',0),('remove',16),('remove',48),('remove',128)]:
                for hp in [0,100]:
                    units=[dict(id=0,x=5,y=5,life=1,hp=hp,maxHp=200,mp=45,maxMp=65,direction=1,nativeState=state,nativeActive=True,nativeEnabled=True)]
                    cases.append(dict(name=f'{op}-{job}-{numeric}-{state}-hp{hp}',operation=op,unit=0,job=job,numeric=numeric,ticks=48,width=12,height=12,units=units))
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'DeathDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'DeathDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());mismatches=[];samples=0
    for case,current in zip(cases,managed):
        native=NativeDeath(case).run_death();samples+=len(native)
        if native!=current['trace']:mismatches.append(dict(name=case['name'],native=native,managed=current['trace']))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/CommandRange.cs','Assets/_Original/Runtime/Combat/BattleLifecycle.cs','Assets/_Original/Runtime/Combat/DeathSoundPlayback.cs','Tools/OriginalWorldChecks/DeathDifferential.cs','Tools/OriginalWorldChecks/check_native_death.py','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/check_native_feedback.py','Assets/_Original/Runtime/Combat/BattleFeedback.cs']
    result=dict(cases=len(cases),updateSamples=samples,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual dmKillChar/dmKillChar2 and10010210/10010120 death/update code, normal/7xx jobs, numeric gate and captured sound1 calls. DirectDraw, original real-time speed, ghost/HP/XP/respawn policy are not inferred.',logs=str(WORK))
    (OUT/'native-death-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:
        m=mismatches[0];i=next(i for i,(a,b)in enumerate(zip(m['native'],m['managed']))if a!=b);print(json.dumps(dict(name=m['name'],step=i,native=m['native'][i],managed=m['managed'][i])));raise SystemExit(1)
if __name__=='__main__':main()
