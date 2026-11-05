"""Execute actual GameCtl dispatch/readers to the UserUnit import boundary; compare codecs."""
import hashlib,json,struct,subprocess
from pathlib import Path
from native_ai_emulator import NativeAi,pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-battle-decoder-checks')
class NativeDecoder:
    BASE=0x10000000;STACK=0x0f000000;STOP=0x0e000000;STUB=0x0d000000;DATA=0x0c000000
    def __init__(self):
        inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());digest='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0';row=next(f for p in inventory['payloads']for f in p['files']if f.get('sha256')==digest);raw=Path(row['extractedFile']).read_bytes();assert hashlib.sha256(raw).hexdigest()==digest;pe=pefile.PE(data=raw)
        self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(self.BASE,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(self.BASE,pe.get_memory_mapped_image())
        for address,size in [(self.STACK,0x100000),(self.STOP,0x1000),(self.STUB,0x100000),(self.DATA,0x10000)]:self.uc.mem_map(address,size)
        self.uc.mem_write(self.STOP,b'\xf4');self.hooks={};self.handles=[];self.calls=[];self.reads=0
        for n,imp in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
            address=self.STUB+n*16;self.uc.mem_write(imp.address,struct.pack('<I',address));self.hooks[address]=imp.name.decode()if imp.name else'#'+str(imp.ordinal);self.uc.mem_write(address,b'\xc3')
        for begin,end in [(self.STOP,self.STOP),(self.STUB,self.STUB+0xfffff),(0x1000c540,0x1000c540)]:self.handles.append(self.uc.hook_add(UC_HOOK_CODE,self.dispatch,begin=begin,end=end))
    def read(self,address):return struct.unpack('<I',self.uc.mem_read(address,4))[0]
    def args(self,n):return[self.read(self.uc.reg_read(UC_X86_REG_ESP)+4+i*4)for i in range(n)]
    def signed(self,v):return v if v<32768 else v-65536
    def dispatch(self,uc,address,size,data):
        if address==self.STOP:uc.emu_stop();return
        if address==0x1000c540:self.reads+=1;return # Run actual cursor/read function.
        name=self.hooks.get(address)
        if name and 'IncTick@@'in name:call=dict(kind='pulse',values=[])
        elif name and 'SetExp@@'in name:
            packed,value=self.args(2);call=dict(kind='experience',values=[self.signed(packed&65535),value])
        elif name and ('SetHpDisplay@@'in name or 'SetMpDisplay@@'in name):
            v=self.args(1)[0];call=dict(kind='hp'if'SetHp'in name else'mp',values=[self.signed(v&65535),self.signed(v>>16)])
        elif name and 'SetFail@@'in name:call=dict(kind='miss',values=[self.args(1)[0]])
        elif name and ('MoveChar@@'in name or 'MoveFail@@'in name):call=dict(kind='move-fail'if'MoveFail'in name else'move',values=[v if v<0x80000000 else v-0x100000000 for v in self.args(3)])
        elif name and 'AttackChar@@'in name:
            target,source,flag=struct.unpack('<hhh',self.uc.mem_read(self.args(1)[0],6));call=dict(kind='attack',values=[source,target,flag])
        elif name and 'SetTickMenu@@'in name:
            packed,mask=self.args(2);call=dict(kind='menu',values=[self.signed(packed&65535),self.signed(packed>>16),mask&65535])
        elif name and ('dmKillChar@@'in name or'dmKillChar2@@'in name):call=dict(kind='death'if'dmKillChar2'in name else'remove',values=[self.args(1)[0]])
        else:raise RuntimeError('Unexpected native decoder host boundary '+str(name)+' at '+hex(address))
        self.calls.append(call);sp=uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);uc.reg_write(UC_X86_REG_ESP,sp+4);uc.reg_write(UC_X86_REG_EAX,0);uc.reg_write(UC_X86_REG_EIP,pc)
    def run(self,packet):
        self.calls=[];self.reads=0;self.uc.mem_write(self.DATA,bytes(packet));sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<II',self.STOP,self.DATA));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(0x1000eb10,self.STOP+1,timeout=5000000,count=100000)
        if self.uc.reg_read(UC_X86_REG_EIP)!=self.STOP or len(self.calls)!=1:raise RuntimeError('Native decoder bound/call count')
        return dict(**self.calls[0],wordReads=self.reads)
    def close(self):
        for handle in self.handles:self.uc.hook_del(handle)
        self.handles.clear()

def run_experience_set(unit,value):
    native=NativeAi(dict(width=12,height=12,units=[dict(id=unit,x=5,y=5),dict(id=2 if unit!=2 else 3,x=6,y=5)]))
    for id in native.units:native.write(native.RUNTIME+id*0x2e0+8,0x12345678)
    before={id:bytes(native.uc.mem_read(native.RUNTIME+id*0x2e0,0x2e0))for id in native.units}
    sp=native.STACK+0xff000;native.uc.mem_write(sp,struct.pack('<III',native.STOP,unit,value));native.uc.reg_write(UC_X86_REG_ESP,sp)
    native.uc.emu_start(native.exports['SetExp'],native.STOP+1,timeout=5000000,count=100000)
    assert native.uc.reg_read(UC_X86_REG_EIP)==native.STOP
    for id,record in before.items():
        expected=bytearray(record)
        if id==unit:struct.pack_into('<I',expected,8,value)
        assert bytes(native.uc.mem_read(native.RUNTIME+id*0x2e0,0x2e0))==bytes(expected),'SetExp changed a field outside the target XP DWORD'
    result=native.read(native.RUNTIME+unit*0x2e0+8);native.close();return result

def vectors():
    cases=[dict(packet=[0x5c,4])]
    for unit in [0,1,999,2999]:
        for value in [0,1,0x7fffffff,0x80000000,0xffffffff]:cases.append(dict(packet=[0x5c,0x53,*struct.pack('<hI',unit,value)]))
        for x,y in [(-32768,32767),(-1,-1),(0,0),(1,1),(199,198)]:
            for opcode in [1,14]:cases.append(dict(packet=[0x5c,opcode,*struct.pack('<hhh',unit,x,y)]))
        for opcode in [3,0x0f]:cases.append(dict(packet=[0x5c,opcode,*struct.pack('<h',unit)]))
        for value in [-32768,-1,0,1,32767]:
            for opcode in [5,6,7]:cases.append(dict(packet=[0x5c,opcode,*struct.pack('<hh',unit,value)]))
            cases.append(dict(packet=[0x5c,0x82,*struct.pack('<hhh',400,unit,value)]))
            for mask in [0,17,32768,65535]:cases.append(dict(packet=[0x5c,0x52,*struct.pack('<hhH',unit,value,mask)]))
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'BattleDecoderDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'BattleDecoderDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[];native=NativeDecoder();experience_sets=0
    for case,current in zip(cases,managed):
        result=native.run(case['packet'])
        if result['kind']=='experience':
            unit,value=result['values'];assert run_experience_set(unit,value)==current['values'][1];experience_sets+=1
        if result!=current:mismatches.append(dict(input=case,native=result,managed=current))
    native.close();(WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeMovement.cs','Assets/_Original/Runtime/Combat/NativeBattlePackets.cs','Assets/_Original/Runtime/Combat/BattleFeedback.cs','Assets/_Original/Runtime/Combat/BattleLifecycle.cs','Assets/_Original/Runtime/Combat/BattleTicks.cs','Tools/OriginalWorldChecks/BattleDecoderDifferential.cs','Tools/OriginalWorldChecks/check_native_battle_decoder.py','Tools/OriginalWorldChecks/native_ai_emulator.py','분석/InstallerVersions/installer_inventory.json']
    result=dict(cases=len(cases),nativeExperienceSetCases=experience_sets,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual late GameCtl1000EB10 dispatch and WORD/DWORD cursor execute; capture UserUnit import arguments. Experience5C53 also executes original UserUnit.SetExp1000CE10 and compares the full736-byte records: only the target+8 DWORD changes, including uint32 high-bit/max. Other setters/updates have separate feedback/death/tick checks. Strict malformed-length/domain checks are adapter protections, not native buffer bounds. No socket/Windows UI or inferred level calculation.',logs=str(WORK))
    (OUT/'native-battle-decoder-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
