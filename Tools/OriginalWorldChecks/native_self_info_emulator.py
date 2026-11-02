"""Execute the16 numeric DrawText calls of original575F0 (457F0E..458302).

This is an explicitly scoped basic block with initialized native register/stack state,
not a reconstructed whole-window/GDI execution. MFC formatting and Win32/GDI drawing
are captured boundaries; raw record field loads, coordinates and call order execute.
"""
import struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_ECX
from unicorn.x86_const import UC_X86_REG_EBP,UC_X86_REG_ESI,UC_X86_REG_EDI,UC_X86_REG_EBX,UC_X86_REG_EIP
class SelfInfoNative(ComposerNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x4795ce:'format-int',0x0e008000:'draw-numeric'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='format-int':
   out,fmt,value=self.args(3);assert self.text(fmt)==b'%d';value=value if value<2147483648 else value-4294967296;self.write(out,self.string(str(value).encode()));self.ret()
  elif name=='draw-numeric':
   p,count,rect,flags=self.args(4);assert flags==0x25;self.draws.append(dict(text=self.text(p).decode(),rect=list(struct.unpack('<4i',uc.mem_read(rect,16)))));self.ret(count,16)
  elif name=='SetRect':
   p,l,t,r,b=self.args(5);uc.mem_write(p,struct.pack('<4I',l,t,r,b));self.ret(1,20)
  elif name=='OffsetRect':
   p,x,y=self.args(3);x=x if x<2147483648 else x-4294967296;y=y if y<2147483648 else y-4294967296;l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));uc.mem_write(p,struct.pack('<4i',l+x,t+y,r+x,b+y));self.ret(1,12)
  else:super().hook(uc,a,size,data)
 def values(self,record,x=0,y=0):
  assert len(record)==732;self.draws=[];self.strings=0x0d0a0000;self.uc.mem_write(self.records,record);self.write(self.window+0x40,x);self.write(self.window+0x44,y)
  hdc=0x0d006000;vtable=0x0d006800;self.write(hdc,vtable);self.write(vtable+0x70,0x0e008000);self.uc.reg_write(UC_X86_REG_ESP,0x0f080000)
  self.uc.reg_write(UC_X86_REG_EBP,self.records+4);self.uc.reg_write(UC_X86_REG_ESI,hdc);self.uc.reg_write(UC_X86_REG_EDI,self.window);self.uc.reg_write(UC_X86_REG_EBX,next(a for a,name in self.hooks.items()if name=='OffsetRect'))
  self.uc.emu_start(0x457f0e,0x458302,timeout=1000000,count=20000);assert self.uc.reg_read(UC_X86_REG_EIP)==0x458302;assert len(self.draws)==16;return self.draws
