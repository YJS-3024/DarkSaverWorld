"""Original Saver input-pump held-left cadence; window/input/draw are boundaries."""
import hashlib,struct
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX,UC_X86_REG_ECX
class NativePointerRepeat:
    def __init__(self,mode=1):
        raw=open('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe','rb').read();self.sha=hashlib.sha256(raw).hexdigest()
        assert self.sha=='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
        pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x400000,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x400000,pe.get_memory_mapped_image())
        for at in [0x0c000000,0x0d000000,0x0f000000]:self.uc.mem_map(at,0x100000)
        self.stop=0x0d000000;self.obj=0x0c000000;self.input=0x0c001000;self.scene=0x0c002000;self.hooks={};self.events=[];self.clock=0
        for i,imp in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
            target=0x0d001000+i*16;self.write(imp.address,target);self.hooks[target]=imp.name.decode()if imp.name else '#'+str(imp.ordinal)
        self.write(self.obj+0x128,mode);self.write(self.obj+0xe0,1);self.write(self.obj+0xe8,self.input);self.write(self.obj+0xec,self.input)
        self.write(self.obj+0xf0,self.scene);self.write(self.obj+0x148,self.scene);self.write(self.scene,0x0c003000);self.write(self.scene+0xbc,1)
        self.write(0x0c003018,0x0d000020);self.write(self.input+4,1);self.write(self.input+0x14,1)
        self.uc.hook_add(UC_HOOK_CODE,self.hook)
    def mode(self,value):self.write(self.obj+0x128,value)
    def write(self,p,v):self.uc.mem_write(p,struct.pack('<I',v&0xffffffff))
    def read(self,p):return struct.unpack('<I',self.uc.mem_read(p,4))[0]
    def ret(self,value=0,cleanup=0):
        sp=self.uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);self.uc.reg_write(UC_X86_REG_EAX,value&0xffffffff);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EIP,pc)
    def hook(self,uc,at,size,data):
        if at==self.stop:uc.emu_stop();return
        name=self.hooks.get(at)
        if name=='clock':self.ret(self.clock)
        elif name and 'MouseInputCheck' in name:self.ret()
        elif at==0x004127e0:self.events.append(self.clock);self.ret(0,4)
        elif at==0x0d000020:self.ret(1,4)
        elif name:raise RuntimeError('Unimplemented native input boundary '+name+' '+hex(at))
    def trace(self,start,times):
        self.write(self.input+0x2c,start);self.write(self.input+0x30,0);self.events=[];rows=[]
        for now in times:
            before=len(self.events);self.clock=now
            sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<II',self.stop,0));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.reg_write(UC_X86_REG_ECX,self.obj)
            self.uc.emu_start(0x4119b0,self.stop+1,timeout=2000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop
            last=self.read(self.input+0x2c);rows.append(dict(repeated=len(self.events)!=before,last=last if last<2**31 else last-2**32,counter=self.read(self.input+0x30)))
        return rows
