"""Exact DManage Flip/ErrorHandle control flow with COM/window boundaries.
No DirectDraw driver, display timing, Unity render or measured FPS is exercised.
"""
import hashlib,json,struct
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class NativeFlip:
    STOP=0x0d000000;OBJECT=0x0c001000;SURFACE=0x0c002000;VTABLE=0x0c003000;WINDOW=0x0c004000
    def __init__(self):
        path=ROOT.parent/'.tools/ds-ghidra/installed/saver/DManage.dll';raw=path.read_bytes();self.sha=hashlib.sha256(raw).hexdigest();assert self.sha=='08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3'
        self.pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x10000000,(self.pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x10000000,self.pe.get_memory_mapped_image())
        for p in [0x0c000000,0x0d000000,0x0f000000]:self.uc.mem_map(p,0x100000)
        self.write(self.SURFACE,self.VTABLE);self.write(self.WINDOW,0x0c005000);self.write(0x0c00507c,0x0d000130);self.write(self.WINDOW+0x20,0x12345678);self.events=[];self.imports={}
        for i,imp in enumerate(i for d in self.pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
            target=0x0d001000+i*16;self.write(imp.address,target);self.imports[target]=imp.name.decode()if imp.name else'#'+str(imp.ordinal)
        for offset,address in [(0x2c,0x0d000100),(0x60,0x0d000110),(0x6c,0x0d000120)]:self.write(self.VTABLE+offset,address)
        self.uc.hook_add(UC_HOOK_CODE,self.hook)
    def write(self,p,v):self.uc.mem_write(p,struct.pack('<I',v&0xffffffff))
    def read(self,p):return struct.unpack('<I',self.uc.mem_read(p,4))[0]
    def args(self,count):return list(struct.unpack('<'+'I'*count,self.uc.mem_read(self.uc.reg_read(UC_X86_REG_ESP)+4,count*4)))
    def ret(self,value=0,cleanup=0):
        sp=self.uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);self.uc.reg_write(UC_X86_REG_EAX,value&0xffffffff);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EIP,pc)
    def hook(self,uc,address,size,data):
        if address==self.STOP:uc.emu_stop();return
        if address in [0x0d000100,0x0d000110,0x0d000120]:
            name,count,value={0x0d000100:('Flip',3,self.flip_result),0x0d000110:('IsLost',1,self.lost_result),0x0d000120:('Restore',1,self.restore_result)}[address]
            self.events.append([name,self.args(count)]);self.ret(value,count*4);return
        if address==0x0d000130:self.events.append(['window-vcall7c-boundary']);self.ret(self.WINDOW);return
        if address==0x1000c1e0:self.events.append(['main-window-boundary']);self.ret(self.WINDOW);return
        name=self.imports.get(address)
        if name=='PostMessageA':self.events.append([name,self.args(4)]);self.ret(1,16)
        elif name:raise RuntimeError('Unexpected renderer host boundary '+name+' '+hex(address))
    def invoke(self,address,value):
        sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<II',self.STOP,value&0xffffffff));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.reg_write(UC_X86_REG_ECX,self.OBJECT)
        self.uc.emu_start(address,self.STOP+1,timeout=2000000,count=10000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP;return self.uc.reg_read(UC_X86_REG_EAX)
    def flip(self,flip_result,lost_result,restore_result,flags=0,present=True):
        self.flip_result=flip_result;self.lost_result=lost_result;self.restore_result=restore_result;self.events=[];self.write(self.OBJECT+0x84,self.SURFACE if present else 0);self.write(self.OBJECT+4,0xabcdef01)
        value=self.invoke(0x10006380,flags);return dict(success=value,lastError=self.read(self.OBJECT+4),calls=list(self.events))
    def error(self,value):
        self.events=[];result=self.invoke(0x10005000,value);return dict(result=result,calls=list(self.events))
def main():
    n=NativeFlip();rows=[]
    for flip in [0,0x8876021c,0x887601c2,0x80004005]:
        for lost in [0,0x887601c2]:
            for restored in [0,0x80004005]:
                for flags in [0,1]:
                    r=n.flip(flip,lost,restored,flags);expected=restored if lost==0x887601c2 else flip
                    assert r['lastError']==expected and r['success']==int(expected==0)
                    assert r['calls']==[['Flip',[n.SURFACE,0,flags]],['IsLost',[n.SURFACE]]]+([['Restore',[n.SURFACE]]]if lost==0x887601c2 else[])
                    rows.append(dict(flip=flip,lost=lost,restored=restored,flags=flags,native=r))
    absent=n.flip(0x80004005,0,0,present=False);assert absent==dict(success=1,lastError=0xabcdef01,calls=[])
    errors=[0,1,0xffffffff,0x80004005,*range(0x88760000,0x88760400)];posts=[]
    for value in errors:
        r=n.error(value);assert r['result']==0,(hex(value),r)
        if r['calls']:assert r['calls']==[['main-window-boundary'],['window-vcall7c-boundary'],['PostMessageA',[0x12345678,0x402,0,0]]];posts.append(value)
    paths=[Path(__file__),OUT/'native_ai_emulator.py'];result=dict(flipCases=len(rows)+1,errorCases=len(errors),recoveryPostCodes=posts,mismatches=0,unityPlay=False,originalDManageSha256=n.sha,
        scope='Exact DManage10006380 COM Flip/IsLost/Restore branching, HRESULT overwrite/BOOL result and10005000 error switch. COM and MFC main-window/PostMessage are supplied boundaries; no real surfaces, driver/vblank, OS recovery, measured FPS or Unity timing equivalence.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},flips=rows,missingSurface=absent)
    (OUT/'native-flip-policy-check.json').write_bytes((json.dumps(result,ensure_ascii=False,indent=2)+'\n').encode());print(json.dumps({k:v for k,v in result.items()if k not in ['sources','flips']},ensure_ascii=False))
if __name__=='__main__':main()
