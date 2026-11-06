"""Actual433BE0 title press and433EC0 motion, no button dispatch or drawing."""
import struct
from check_native_commanders import Native
class WindowDragNative(Native):
 def sequence(self,x,y,region,down,steps,enabled=True):
  self.uc.mem_write(self.window,bytes(256));self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(self.window+0x78,int(enabled));self.uc.mem_write(self.window+0x30,struct.pack('<4i',*region));point=0x0d009000;self.uc.mem_write(point,struct.pack('<2i',*down));self.run(0x433be0,[0,point],self.window);active=bool(self.read(self.window+0x24));positions=[]
  signed=lambda value:value if value<2**31 else value-2**32
  for px,py,enable,lock in steps:
   self.write(self.window+0x78,int(enable));self.write(self.window+0x7c,int(lock));self.uc.mem_write(point,struct.pack('<2i',px,py));self.run(0x433ec0,[0,point],self.window);positions.append([signed(self.read(self.window+0x40)),signed(self.read(self.window+0x44))])
  return dict(active=active,positions=positions)
