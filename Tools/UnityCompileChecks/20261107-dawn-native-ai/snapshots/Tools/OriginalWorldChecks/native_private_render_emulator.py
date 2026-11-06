"""Actual422500 private log marker lookback, colors, row clipping and shadow calls.

MFC CString/List and Win32 surface/rect APIs plus final software-font drawing are
isolated boundaries. No framebuffer, GPU or actual Windows device claim.
"""
import sys,struct
from native_private_chat_emulator import PrivateChatNative
from native_ai_emulator import UC_X86_REG_ECX
class PrivateRenderNative(PrivateChatNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x479a24:'find-row',0x479a2a:'normalize',0x43ea00:'draw-row',0x43e390:'draw-row'})
  for a,name in [(0x498f38,'surface-desc'),(0x498f04,'surface-lock'),(0x498f00,'surface-unlock')]:self.hooks[self.read(a)]=name
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='find-row':
   index=self.args(1)[0];self.ret(self.nodes[index]if index<len(self.nodes)else 0,4)
  elif name=='normalize':
   l,t,r,b=struct.unpack('<4i',uc.mem_read(this,16));uc.mem_write(this,struct.pack('<4i',min(l,r),min(t,b),max(l,r),max(t,b)));self.ret()
  elif name=='surface-desc':
   out=self.args(1)[0];d=[0]*31;d[0]=124;d[2]=480;d[3]=640;d[4]=1280;uc.mem_write(out,struct.pack('<31I',*d));self.ret(0,4)
  elif name=='surface-lock':self.ret(0x0d070000)
  elif name=='surface-unlock':self.ret()
  elif name=='draw-row':
   x,y,r,b,text,color=self.args(6);self.draws.append(dict(text=self.text(text).decode('cp949'),color=color&65535,rect=[x,y,r,b]));self.ret(0,168)
  elif name=='SetRect':p,l,t,r,b=self.args(5);uc.mem_write(p,struct.pack('<4I',l,t,r,b));self.ret(1,20)
  elif name=='OffsetRect':
   p,x,y=self.args(3);x=x if x<2147483648 else x-4294967296;y=y if y<2147483648 else y-4294967296;l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));uc.mem_write(p,struct.pack('<4i',l+x,t+y,r+x,b+y));self.ret(1,12)
  elif name=='IntersectRect':
   dst,p,q=self.args(3);p=struct.unpack('<4i',uc.mem_read(p,16));q=struct.unpack('<4i',uc.mem_read(q,16));r=(max(p[0],q[0]),max(p[1],q[1]),min(p[2],q[2]),min(p[3],q[3]));ok=r[0]<r[2]and r[1]<r[3];uc.mem_write(dst,struct.pack('<4i',*(r if ok else [0,0,0,0])));self.ret(int(ok),12)
  elif name=='EqualRect':p,q=self.args(2);self.ret(int(uc.mem_read(p,16)==uc.mem_read(q,16)),8)
  else:super().hook(uc,a,size,data)
 def render(self,rows,first,depth=16,residue=0):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.nodes=[0x0d030000+i*32 for i in range(len(rows))];self.draws=[];obj=0x0d00b000;self.uc.mem_write(obj,bytes(256));font=0x0d00c000;self.write(font,int(depth==8));self.write(obj+4,font);self.write(obj+0x10,14);self.write(obj+0x28,len(rows));self.write(obj+0x4c,first);self.write(obj+0x64,len(rows));self.write(obj+0x5c,0x0d040000);self.uc.mem_write(obj+0x38,struct.pack('<4i',4,27,181,115))
  for i,row in enumerate(rows):
   self.write(self.nodes[i],self.nodes[i+1]if i+1<len(rows)else 0);self.write(self.nodes[i]+8,self.string(row.encode('cp949')));c=0x0d040000+i*16;self.write(c,c+16 if i+1<len(rows)else 0);self.write(c+8,9)
  for i in range(5):self.uc.mem_write(obj+0x76+i*2,struct.pack('<H',i+1))
  # Source422500 never initializes its marker byte unless '>'/'<' is found.
  # Supply this stack input explicitly; do not mistake it for native defaults.
  self.uc.mem_write(0x0f0ff000-0x2b5,bytes([residue]));self.run(0x422500,[0x0d00d000,0,0],obj);return self.draws
