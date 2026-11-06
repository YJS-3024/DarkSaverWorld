"""Exact supplied MFC42/CRT ANSI Format, C-locale image state.

CString allocation/release, string-length, locks, heap and TLS are host
boundaries. No DLL entrypoint, OS locale initialization or real allocator runs.
The actual MFC format parser and actual CRT vsprintf execute unmodified.
"""
import hashlib,json,struct
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP,UC_X86_REG_ECX
ROOT=Path(__file__).resolve().parents[2]
class MfcFormat:
    def __init__(self):
        inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
        hashes={'msvcrt.dll':'d7cea69a98579d928e534070f5293e80ed7df38baf611b20717ef55aa1344a18','mfc42.dll':'2598103e2ce4a5f30772b6a8a5fab26baab098c1fe94b744cf6f5dfb3dea1d37'}
        self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.hooks={};self.sources={};self.heap=0x0c010000
        self.uc.mem_map(0,4096);self.uc.mem_map(0x0c000000,0x100000);self.uc.mem_map(0x0f000000,0x100000);self.stop=0x0c0ff000
        for index,name in enumerate(['msvcrt.dll','mfc42.dll']):
            row=next(f for p in inventory['payloads']if p.get('payloadId','').startswith('a6f518676a553098')for f in p['files']if f.get('sha256')==hashes[name])
            raw=Path(row['extractedFile']).read_bytes();assert hashlib.sha256(raw).hexdigest()==hashes[name];self.sources[name]=hashes[name]
            pe=pefile.PE(data=raw);base=pe.OPTIONAL_HEADER.ImageBase;self.uc.mem_map(base,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(base,pe.get_memory_mapped_image())
            if index==0:self.exports={e.name.decode():base+e.address for e in pe.DIRECTORY_ENTRY_EXPORT.symbols if e.name}
            else:self.target=base+next(e.address for e in pe.DIRECTORY_ENTRY_EXPORT.symbols if e.ordinal==2818)
            for i,(dll,imp)in enumerate((d.dll.decode().lower(),i)for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
                symbol=imp.name.decode()if imp.name else'#'+str(imp.ordinal)
                if index==1 and dll=='msvcrt.dll'and symbol in self.exports:address=self.exports[symbol]
                else:address=0x0c0d0000+index*0x10000+i*16;self.hooks[address]=symbol
                self.uc.mem_write(imp.address,struct.pack('<I',address))
        self.uc.hook_add(UC_HOOK_CODE,self.hook)
    def ret(self,value=0,cleanup=0):
        sp=self.uc.reg_read(UC_X86_REG_ESP);pc=struct.unpack('<I',self.uc.mem_read(sp,4))[0]
        self.uc.reg_write(UC_X86_REG_EAX,value&0xffffffff);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EIP,pc)
    def args(self,count):return list(struct.unpack('<'+'I'*count,self.uc.mem_read(self.uc.reg_read(UC_X86_REG_ESP)+4,count*4)))
    def hook(self,uc,address,size,data):
        if address==self.stop:uc.emu_stop();return
        name=self.hooks.get(address)
        if address==0x6c0f2400:
            self.uc.mem_write(uc.reg_read(UC_X86_REG_ECX),struct.pack('<I',0x0c004000));self.ret(0x0c004000,4)
        elif address==0x6c0f2476:self.ret(0,4)
        elif name in ['lstrlenA','lstrlenW']:
            pointer=self.args(1)[0];step=2 if name.endswith('W')else 1;length=0
            while bytes(uc.mem_read(pointer+length*step,step))!=bytes(step):length+=1;assert length<2048
            self.ret(length,4)
        elif name in ['GetLastError','SetLastError','TlsGetValue','EnterCriticalSection','LeaveCriticalSection','InitializeCriticalSection']:
            self.ret(0x0c003000 if name=='TlsGetValue'else 0,0 if name=='GetLastError'else 4)
        elif name=='HeapAlloc':
            pointer=self.heap;self.heap+=0x2000;assert self.heap<0x0c0c0000;self.ret(pointer,12)
        elif name in ['GetCurrentThreadId','TlsSetValue']:self.ret(123 if name=='GetCurrentThreadId'else 1,0 if name=='GetCurrentThreadId'else 8)
        elif name:raise RuntimeError(('Unimplemented MFC/CRT boundary',name,hex(address)))
    def format(self,pattern,values):
        self.uc.mem_write(0x0c000000,struct.pack('<I',0x0c004000));self.uc.mem_write(0x0c004000,bytes(2048));self.uc.mem_write(0x0c001000,pattern+b'\0')
        args=[]
        for i,value in enumerate(values):
            if isinstance(value,bytes):pointer=0x0c006000+i*1024;assert len(value)<900;self.uc.mem_write(pointer,value+bytes(16));args.append(pointer)
            else:args.append(value&0xffffffff)
        sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(4+len(args)),self.stop,0x0c000000,0x0c001000,*args,0));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(self.target,self.stop+1,timeout=1000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop
        return bytes(self.uc.mem_read(0x0c004000,2048)).split(b'\0')[0]
