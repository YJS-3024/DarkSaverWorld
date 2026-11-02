"""Actual native MoveCharPath/DD30 five reverse points and543 byte builder, including tie axis."""
import hashlib,json,random,struct,subprocess
from pathlib import Path
from check_native_outbound_packets import NativeOutgoing
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-move-hint-checks')
class NativeMove(NativeOutgoing):
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='bounded-new':
            count=self.args(1)[0];assert count in [4,10,24];self.ret(self.DATA+0x70000);return
        if name=='send-move':return
        if name=='send-bytes':
            ptr,count=self.args(2);assert count in [4,10,24];self.sent.append(bytes(self.uc.mem_read(ptr,count)).hex());self.ret();return
        super()._dispatch(uc,address,size,data)
    def invoke(self,address,args):
        sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*[v&0xffffffff for v in args]));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(address,self.STOP+1,timeout=5000000,count=5000000)
        if self.uc.reg_read(UC_X86_REG_EIP)!=self.STOP:raise RuntimeError('Native movement bound')
    def run_hint(self):
        c=self.case;self.write(0x10016170,c['axis']);ptr=self.DATA+0x6000;self.uc.mem_write(ptr,b'\xa5'*32)
        self.invoke(self.exports['MoveCharPath'],[ptr,c['source'],c['x'],c['y']]);success=self.uc.reg_read(UC_X86_REG_EAX)!=0
        if not success:self.close();return dict(success=False)
        values=struct.unpack('<11h',self.uc.mem_read(ptr,22));axis=self.read(0x10016170);self.invoke(0x10004ff0,[ptr]);assert len(self.sent)==1
        result=dict(success=True,axis=axis,points=[list(values[i:i+2])for i in range(1,11,2)],packet=self.sent[0]);self.close();return result

def vectors():
    costs=[0]*810
    for i in range(2,810,3):costs[i]=1
    rng=random.Random(202611025);cases=[]
    for axis in [0,1]:
        for dx,dy in [(0,0),(1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,1),(3,2),(-3,-2),(5,0),(0,5)]:
            cases.append(dict(width=12,height=12,source=1,x=5+dx,y=5+dy,axis=axis,attributes=[0]*144,terrain=costs,units=[dict(id=1,job=100,life=1,x=5,y=5,hp=100,maxHp=100,movementBudget=5,movementType=0)]))
    for index in range(180):
        attrs=[rng.randrange(3)if rng.random()<.3 else 0 for _ in range(144)];terrain=costs.copy()
        for attr in [1,2]:
            for kind in range(10):terrain[attr*30+kind*3+2]=rng.choice([0,1,2,3,99])
        units=[dict(id=1,job=100,life=1,x=5,y=5,hp=100,maxHp=100,movementBudget=rng.randrange(9),movementType=rng.randrange(10))]
        for id in [2,400]:
            x,y=rng.randrange(1,11),rng.randrange(1,11)
            if (x,y)!=(5,5):units.append(dict(id=id,job=100,life=1,x=x,y=y,hp=100,maxHp=100))
        cases.append(dict(width=12,height=12,source=1,x=rng.randrange(1,11),y=rng.randrange(1,11),axis=rng.randrange(2),attributes=attrs,terrain=terrain,units=units))
    inactive=json.loads(json.dumps(cases[1]));inactive['units'][0]['nativeActive']=False;cases.append(inactive)
    for source in [0,9,49,50,999,2999]:
        c=json.loads(json.dumps(cases[7]));c['source']=source;c['units'][0]['id']=source;cases.append(c)
    for origin,target,axis in [((1,1),(1,4),0),((1,1),(4,1),1),((198,198),(199,199),0),((198,198),(198,199),1)]:
        c=json.loads(json.dumps(cases[1]));c.update(width=200,height=200,attributes=[0]*40000,x=target[0],y=target[1],axis=axis)
        c['units'][0].update(x=origin[0],y=origin[1]);cases.append(c)
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeMoveHintDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeMoveHintDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases),'Every input must have one managed result';mismatches=[]
    for case,current in zip(cases,managed):
        native=NativeMove(case).run_hint()
        if current!=native:mismatches.append(dict(input=case,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeMoveHints.cs','Assets/_Original/Runtime/Combat/RegroupMovement.cs','Assets/_Original/Runtime/Combat/CommandRange.cs','Tools/OriginalWorldChecks/NativeMoveHintDifferential.cs','Tools/OriginalWorldChecks/check_native_move_hints.py','Tools/OriginalWorldChecks/check_native_outbound_packets.py','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/check_native_feedback.py']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual native MoveCharPath1000E310, SetArea2, DD30 reverse-neighbour selection and10004FF0 builder execute. Compare five signed coordinate pairs, axis phase and all24 application bytes; presentation queue/empty MFC/allocator/send boundaries isolated. Not complete client movement replay or socket framing.',logs=str(WORK))
    (OUT/'native-move-hint-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
