"""Execute late Saver chat routing, splitting, color initialization and scroll kernels read-only."""
import struct
from native_font_emulator import Font
from native_ai_emulator import UC_X86_REG_ECX
class ChatNative(Font):
 def __init__(self,raw):
  super().__init__(raw);self.hooks.update({0x475f50:'find-chat',0x453ee0:'append-chat',0x437980:'chat-base',0x4795e6:'copy-cstring',0x479586:'destroy-cstring'})
  self.write(self.manager+0x10,0x0d001000);self.write(0x0d001084,0x0d001100);self.next_color=1
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='find-chat':assert self.args(1)[0]==19;self.ret(self.window if self.exists else 0,4)
  elif name=='append-chat':self.received.append(self.args(2)[0]);self.ret(0,8)
  elif name=='chat-base':self.ret(uc.reg_read(UC_X86_REG_ECX),36)
  elif name=='copy-cstring':self.write(uc.reg_read(UC_X86_REG_ECX),0x0d004000);self.ret(uc.reg_read(UC_X86_REG_ECX),4)
  elif name=='destroy-cstring':self.ret()
  elif a==self.color_target:
   surface,color=self.args(2);assert surface==0x0d001100;self.colors.append(color);self.ret(self.next_color);self.next_color+=1
  else:super().hook(uc,a,size,data)
 def colors_init(self):
  self.color_target=self.read(0x498efc);self.colors=[];self.run(0x421cd0,[self.manager,0,14,14,6,52,551,124,300,0],self.window)
  assert [struct.unpack('<H',self.uc.mem_read(self.window+0x76+2*i,2))[0]for i in range(5)]==[1,2,3,4,5]
  return self.colors
 def route(self,kind,global_on,exists):
  self.received=[];self.exists=exists;self.write(0x490ae0,int(global_on));self.write(self.manager+0x144,1)
  self.run(0x476230,[kind,0x0d004000],self.manager);return self.received
 def split(self,value,width):
  b=value.encode('cp949');self.uc.mem_write(self.text,b+b'\0');left=0x0d064000;right=0x0d066000;self.uc.mem_write(left,bytes(2048));self.uc.mem_write(right,bytes(2048));self.run(0x43fdf0,[self.text,left,right,width],self.obj)
  read=lambda p:bytes(self.uc.mem_read(p,2048)).split(b'\0')[0].decode('cp949')
  return read(left),read(right)
 def lines(self,value,width):
  rows=[];steps=0
  while self.measure(value)>width:
   left,value=self.split(value,width);rows.append(left);steps+=1;assert steps<2000
  rows.append(value);return rows
 def scroll(self,count,visible,first,direction):
  self.write(self.window+0x28,count);self.write(self.window+0x50,visible);self.write(self.window+0x4c,first);self.run(0x437b80 if direction<0 else 0x437ba0,[],self.window);return self.read(self.window+0x4c)
 def resize(self,count,expanded):
  self.write(self.window+0x28,count);self.write(self.window+0x10,14);self.write(self.window+0x48,0);self.write(self.window+0x4c,123)
  self.run(0x423060,[10 if expanded else 6,52,555 if expanded else 551,381 if expanded else 124],self.window)
  return dict(visible=self.read(self.window+0x50),first=self.read(self.window+0x4c))
