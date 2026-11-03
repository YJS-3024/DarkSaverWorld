"""Original scene28 drawing and first-left-press dismissal; surfaces/fonts/transport isolated."""
import struct
from native_warehouse_emulator import WarehouseNative
from native_ai_emulator import UC_X86_REG_EAX
class MessageNative(WarehouseNative):
 def __init__(self):
  super().__init__();self.write(self.window,0x485eec);self.write(self.window+0xac,0x0d056000);self.hooks.update({0x43e390:'message-paragraph',0x43ea00:'message-paragraph'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name and name.startswith('?GetDescription@CDDSurface@@'):
   p=self.args(1)[0];uc.mem_write(p,bytes(124));self.ret(0,4)
  elif name and name.startswith('?Lock@CDDSurface@@'):self.ret(0x0d080000)
  elif name and name.startswith('?Unlock@CDDSurface@@'):self.ret(0)
  elif name=='OffsetRect':
   p,x,y=self.args(3);x=x if x<2**31 else x-2**32;y=y if y<2**31 else y-2**32;l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));uc.mem_write(p,struct.pack('<4i',l+x,t+y,r+x,b+y));self.ret(1,12)
  elif name=='message-paragraph':
   args=self.args(42);self.draws.append(dict(rect=list(args[:4]),text=self.text(args[4]).decode('cp949'),color=args[5],flags=list(args[-5:])));self.ret(0,168)
  else:super().hook(uc,a,size,data)
 def draw(self,text,x=0,y=0,paragraph=False,depth=16):
  self.draws=[];self.write(self.window+0xb4,self.string(text.encode('cp949')));self.write(self.window+0xb8,int(paragraph));self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(0x0d056000,0 if depth==16 else 1)
  self.run(0x467d10,[0x0d080000],self.window);assert len(self.draws)==1;return self.draws[0]
 def clear(self,present=True,through_mouse=False):
  self.closed=[];self.write(0x497c04,self.window if present else 0);self.write(self.manager+0x58,0)
  self.run(0x475c50 if through_mouse else 0x467f30,[0,0]if through_mouse else[],self.manager)
  return dict(result=self.uc.reg_read(UC_X86_REG_EAX),closed=self.closed,pointer=self.read(0x497c04))
