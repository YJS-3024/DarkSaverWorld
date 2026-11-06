"""Run actual UserUnit HP/MP/MISS/attack setters and update counters against production models."""
import hashlib,json,struct,subprocess
from pathlib import Path
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EIP
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-feedback-checks')
class NativeFeedback(NativeAi):
    def invoke(self,address,args):
        sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*[v&0xffffffff for v in args]));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(address,self.STOP+1,timeout=5000000,count=100000)
        if self.uc.reg_read(UC_X86_REG_EIP)!=self.STOP:raise RuntimeError('Feedback native instruction bound')
    def run_feedback(self):
        c=self.case;unit=c['unit'];target=c.get('target',400)
        for u in c['units']:
            b=self.RUNTIME+u['id']*0x2e0;self.write(b+0x28,u['mp']);self.write(b+0x2c,u['maxMp']);self.write(b+0x148,{0:3,1:4,2:2,3:1}[u['direction']]);self.write(b+0x294,c.get('targetFlag',0)if u['id']==target else 0)
        operation=c['operation'];value=c['value'];base=self.RUNTIME+unit*0x2e0
        if operation=='attack':
            self.uc.mem_write(self.DATA+0x6000,struct.pack('<hhh',target,unit,value));self.invoke(self.exports['AttackChar'],[self.DATA+0x6000])
        elif operation=='miss':self.invoke(self.exports['SetFail'],[unit])
        else:self.invoke(self.exports['SetHpDisplay'if operation=='hp'else'SetMpDisplay'],[(unit&65535)|((value&65535)<<16)])
        trace=[]
        for step in range(c['ticks']+1):
            if operation=='attack':
                row=[]
                for actor in [unit,target]:
                    b=self.RUNTIME+actor*0x2e0
                    row.append(dict(unit=actor,counter=self.read(b+0x158),expired=(self.read(b+0x100)&4)==0,offsetX=self.signed(self.read(b+0x284)),offsetY=self.signed(self.read(b+0x288)),nativeDirection=self.read(b+0x148),flag=self.signed(self.read(b+0x294)),partner=self.signed(self.read(b+0x280))))
                trace.append(row)
            else:trace.append(dict(hp=self.signed(self.read(base+0x20)),mp=self.signed(self.read(base+0x28)),delta=self.signed(self.read(base+0x160)),counter=self.read(base+0x15c),kind=self.signed(self.read(base+0x2ac)),expired=(self.read(base+0x100)&64)==0))
            if step<c['ticks']:
                self.invoke(0x10010210,[unit])
                if operation=='attack':self.invoke(0x10010210,[target])
        self.close();return trace

def vectors():
    units=[dict(id=0,x=5,y=5,life=1,hp=100,maxHp=200,mp=45,maxMp=65,direction=1,nativeState=16),dict(id=400,x=6,y=5,life=1,hp=50,maxHp=100,mp=7,maxMp=10,direction=0,nativeState=16)]
    cases=[]
    for op in ['hp','mp','miss']:
        for value in [-32768,-1,0,1,44,45,100,175,32767]if op!='miss'else[0]:
            for unit in [0,400]:cases.append(dict(name=f'{op}-{unit}-{value}',operation=op,unit=unit,value=value,ticks=29,width=12,height=12,units=units))
    for dx in range(-3,4):
        for dy in range(-3,4):
            if dx==dy==0:continue
            for flag in [-1,0,1]:
                copy=json.loads(json.dumps(units));copy[1].update(x=5+dx,y=5+dy)
                cases.append(dict(name=f'attack-{dx}-{dy}-{flag}',operation='attack',unit=0,target=400,value=flag,targetFlag=7,ticks=51,width=12,height=12,units=copy))
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'FeedbackDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'FeedbackDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());mismatches=[];samples=0
    for case,current in zip(cases,managed):
        native=NativeFeedback(case).run_feedback();samples+=len(native)
        if current['trace']!=native:mismatches.append(dict(name=case['name'],native=native,managed=current['trace']))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/BattleFeedback.cs','Tools/OriginalWorldChecks/FeedbackDifferential.cs','Tools/OriginalWorldChecks/check_native_feedback.py','Tools/OriginalWorldChecks/native_ai_emulator.py']
    result=dict(cases=len(cases),updateSamples=samples,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual late UserUnit SetHpDisplay/SetMpDisplay/SetFail/AttackChar and10010210 update instructions; signed fields, shared slot, opposed offsets/facing, target flag retention and full expiry traces. No body rendering, host Windows, network, original frame-rate measurement or invented negative-HP policy.',logs=str(WORK))
    (OUT/'native-feedback-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:
        first=mismatches[0];index=next(i for i,(a,b)in enumerate(zip(first['native'],first['managed']))if a!=b);print(json.dumps(dict(name=first['name'],step=index,native=first['native'][index],managed=first['managed'][index])));raise SystemExit(1)
if __name__=='__main__':main()
