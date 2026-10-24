"""Compare original x86 pixel-loop instructions with the reconstructed blend formula.
No surface lock/Windows code executes; only explicitly bounded pixel-loop ranges.
"""
import struct,json,hashlib
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32
from unicorn.x86_const import *
from analyze_special_skills import PE,ROOT,OUT,write

def blend(d,s,a,masks):return sum((((d&m)+(((s&m)-(d&m))*a>>8))&m) for m in masks)
def main():
 pe=PE(ROOT/'DManage.dll');u=Uc(UC_ARCH_X86,UC_MODE_32)
 for a,n in [(0x10000000,0x10000),(0x20000000,0x10000),(0x30000000,0x10000)]:u.mem_map(a,n)
 for start,end in [(0x10001dd3,0x10001e87),(0x10002673,0x100026f9)]:u.mem_write(start,pe.data(start,end-start))
 def w(a,v):u.mem_write(a,struct.pack('<I',v&0xffffffff))
 samples=[0,1,31,32,0x03e0,0x07e0,0x1234,0x7c00,0x7fff,0xf800,0xfffe,0xffff];rows=[];total=0
 for fmt,masks in [('RGB565',(0xf800,0x07e0,0x001f)),('RGB555',(0x7c00,0x03e0,0x001f))]:
  for d in samples:
   for s in samples:
    for a in [0,1,2,127,128,254,255]:
     for mode in ['image','color']:
      if mode=='color' and s==0:continue # Full color function skips converted source 0 before this kernel.
      sp=0x20001000;u.mem_write(sp,b'\0'*0x100);u.mem_write(0x30000000,struct.pack('<H',s));u.mem_write(0x30001000,struct.pack('<H',d));u.mem_write(0x30002000,struct.pack('<H',d));u.mem_write(0x30003000,bytes([a]));u.reg_write(UC_X86_REG_ESP,sp)
      if mode=='image':
       for off,v in [(0x14,0x30000000),(0x28,masks[0]),(0x10,masks[1]),(0x2c,masks[2]),(0x78,0x30002000),(0x30,1)]:w(sp+off,v)
       u.reg_write(UC_X86_REG_ESI,0x30001000);u.reg_write(UC_X86_REG_EDX,0x30002000);u.reg_write(UC_X86_REG_EBP,0x30003000);start,end=0x10001dd3,0x10001e87;expected=d if s==0 else blend(d,s,a,masks)
      else:
       for off,v in [(0x28,masks[0]),(0x6c,masks[1]),(0x10,masks[2]),(0x14,1),(0x74,0x30002000)]:w(sp+off,v)
       u.reg_write(UC_X86_REG_ECX,0x30001000);u.reg_write(UC_X86_REG_EDX,0x30003000);u.reg_write(UC_X86_REG_EBP,s);start,end=0x10002673,0x100026f9;expected=blend(d,s,a,masks)
      u.emu_start(start,end,count=1000);actual=struct.unpack('<H',u.mem_read(0x30002000,2))[0];assert actual==expected,(fmt,mode,hex(d),hex(s),a,hex(actual),hex(expected));total+=1
      if a in [0,128,255] and d in [0,0xffff] and s in [0,0xffff]:rows.append(dict(format=fmt,mode=mode,dst=hex(d),src=hex(s),alpha=a,original_instruction_result=hex(actual),formula_result=hex(expected),matches=True))
 write('RBM_원본명령_픽셀대조.csv',rows)
 summary=dict(cases=total,mismatches=0,formats=['RGB565','RGB555'],denominator=256,image_zero_source_skipped=True,color_zero_source='full function early return before pixel kernel',scope='original bounded pixel instructions in Unicorn; no Windows surface lock, clipping, driver, or native screen validation',sha256=hashlib.sha256((ROOT/'DManage.dll').read_bytes()).hexdigest())
 (OUT/'RBM_픽셀커널_검증요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
