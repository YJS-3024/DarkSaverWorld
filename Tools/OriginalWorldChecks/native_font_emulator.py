"""Read-only late Saver software-font execution. Windows rectangle predicates isolated."""
import sys,struct
from pathlib import Path
from check_native_commanders import Native
from native_ai_emulator import UC_X86_REG_EAX
class Font(Native):
 def __init__(self,raw,depth=8):
  super().__init__();self.obj=0x0d000500;self.glyph=0x0d030000;self.canvas=0x0d080000;self.text=0x0d060000;self.width=100;self.height=40;self.depth=depth
  self.uc.mem_write(self.glyph,raw)
  for i,v in enumerate([int(depth==8),14,14,32,self.glyph,self.glyph,self.glyph+3072,self.glyph+6144]):self.write(self.obj+i*4,v)
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='IntersectRect':
   dst,p,q=self.args(3);p=struct.unpack('<4i',uc.mem_read(p,16));q=struct.unpack('<4i',uc.mem_read(q,16));r=(max(p[0],q[0]),max(p[1],q[1]),min(p[2],q[2]),min(p[3],q[3]));valid=r[0]<r[2]and r[1]<r[3];uc.mem_write(dst,struct.pack('<4i',*(r if valid else (0,0,0,0))));self.ret(int(valid),12)
  elif name=='EqualRect':p,q=self.args(2);self.ret(int(uc.mem_read(p,16)==uc.mem_read(q,16)),8)
  else:super().hook(uc,a,size,data)
 def draw(self,value,x=2,y=2,alignment=0):
  data=value.encode('cp949');self.uc.mem_write(self.text,data+b'\0');self.uc.mem_write(self.canvas,bytes(self.width*self.height*(self.depth//8)));d=[0]*31;d[0]=124;d[2]=self.height;d[3]=self.width;d[4]=self.width*(self.depth//8);d[9]=self.canvas
  self.run(0x43ddf0 if self.depth==8 else 0x43e0c0,[x&0xffffffff,y&0xffffffff,self.text,255 if self.depth==8 else 65535,*d,alignment],self.obj)
  return bytes(self.uc.mem_read(self.canvas,self.width*self.height*(self.depth//8)))
 def paragraph(self,value,width,height):
  self.width=640;self.height=80;self.uc.mem_write(self.text,value.encode('cp949')+b'\0');self.uc.mem_write(self.canvas,bytes(self.width*self.height));d=[0]*31;d[0]=124;d[2]=self.height;d[3]=self.width;d[4]=self.width;d[9]=self.canvas
  self.run(0x43e390,[2,2,2+width,2+height,self.text,255,*d,0,0,0xffffffff,0,0],self.obj)
  return bytes(self.uc.mem_read(self.canvas,self.width*self.height))
 def measure(self,value):
  self.uc.mem_write(self.text,value.encode('cp949')+b'\0');self.run(0x43fcc0,[self.text],self.obj);return self.uc.reg_read(UC_X86_REG_EAX)
