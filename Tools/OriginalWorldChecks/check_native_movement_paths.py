"""Run actual D2E0 backtracker and D740 compression with isolated MFC node storage."""
import hashlib,json,struct,subprocess
from pathlib import Path
from check_native_move_hints import vectors,NativeMove
from native_ai_emulator import UC_X86_REG_ECX,UC_X86_REG_EAX
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-movement-path-checks')
class NativePath(NativeMove):
    def __init__(self,case):
        super().__init__(case);self.next_node=self.DATA+0x20000
        for address,name in [(0x1000f7a0,'list-new'),(0x1000f510,'list-tail'),(0x1000f590,'list-clear'),(0x1000f5c0,'list-destruct')]:
            self.hooks[address]=name;self._hook_add(4,self._dispatch,begin=address,end=address)
    def node(self,prev,next,value=0):
        ptr=self.next_node;self.next_node+=12;self.uc.mem_write(ptr,struct.pack('<III',next,prev,value&0xffffffff));return ptr
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='append-direction':
            target,value=self.args(2);head=self.read(target+4);ptr=self.node(0,head,value)
            if head:self.write(head+4,ptr)
            else:self.write(target+8,ptr)
            self.write(target+4,ptr);self.write(target+12,self.read(target+12)+1);self.ret();return
        if name=='list-new':
            target=uc.reg_read(UC_X86_REG_ECX);prev,next=self.args(2);ptr=self.node(prev,next);self.write(target+12,self.read(target+12)+1);self.ret(ptr,8);return
        if name=='list-tail':
            target=uc.reg_read(UC_X86_REG_ECX);value=self.args(1)[0];tail=self.read(target+8);ptr=self.node(tail,0,value)
            if tail:self.write(tail,ptr)
            else:self.write(target+4,ptr)
            self.write(target+8,ptr);self.write(target+12,self.read(target+12)+1);self.ret(ptr,4);return
        if name in ['list-clear','list-destruct']:
            target=uc.reg_read(UC_X86_REG_ECX)
            for offset in [4,8,12,16,20]:self.write(target+offset,0)
            self.ret();return
        if name=='free-null-path-chain':self.ret();return
        super()._dispatch(uc,address,size,data)
    def run_path(self):
        c=self.case;actor=next(u for u in c['units']if u['id']==c['source']);scores=0x101324ec;base=self.RUNTIME+c['source']*0x2e0;count=self.DATA+0x6000
        self.write(0x1001616c,c['axis']);self.write(base+0x144,10);self.write(count,0)
        if not actor.get('nativeActive',True):self.close();return dict(success=False)
        self.invoke(self.exports['ClearTa'],[scores]);self.invoke(self.exports['SetArea2'],[actor['x'],actor['y'],actor['movementBudget']+7,c['source'],actor['movementType'],scores])
        self.invoke(0x1000d2e0,[c['x'],c['y'],c['source'],actor['x'],actor['y'],scores,count])
        if not self.uc.reg_read(UC_X86_REG_EAX):self.close();return dict(success=False)
        phase=self.read(0x1001616c);steps=self.read(count);self.invoke(0x1000d740,[base+0x12c]);directions=[];ptr=self.read(base+0x130)
        while ptr:
            assert len(directions)<40001;directions.append(self.read(ptr+8));ptr=self.read(ptr)
        self.close();return dict(success=True,axis=phase,steps=steps,directions=directions)
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeMovementPathDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeMovementPathDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[]
    for case,current in zip(cases,managed):
        native=NativePath(case).run_path()
        if current!=native:mismatches.append(dict(input=case,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeMovement.cs','Assets/_Original/Runtime/Combat/RegroupMovement.cs','Tools/OriginalWorldChecks/NativeMovementPathDifferential.cs','Tools/OriginalWorldChecks/check_native_movement_paths.py','Tools/OriginalWorldChecks/check_native_move_hints.py','Tools/OriginalWorldChecks/check_native_outbound_packets.py','Tools/OriginalWorldChecks/native_ai_emulator.py']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),scope='Actual original SetArea2/D2E0 full backtracking and D740 compression. Compare complete direction sequence, sentinel9, cardinal step count and independent1001616C axis. Only MFC node storage/free is isolated, not selection/compression. Not rendered Windows game.',logs=str(WORK))
    (OUT/'native-movement-path-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
