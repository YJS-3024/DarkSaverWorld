"""Actual44AA70 outline and actual MFC CDC stock-object slot; GDI raster isolated."""
import struct,hashlib
from pathlib import Path
from native_mercenary_window_emulator import MercenaryNative
from native_ai_emulator import pefile
class ShopSelectionNative(MercenaryNative):
 def __init__(self):
  super().__init__();raw=Path('/workspace/.tools/ds-ghidra/installed/saver/MFC42D.DLL').read_bytes();self.mfcSha256=hashlib.sha256(raw).hexdigest();assert self.mfcSha256=='3a3fa253ce80cf55027abc7918f8984057a94d1832302fb9893402aba9d35dff';pe=pefile.PE(data=raw);self.uc.mem_map(pe.OPTIONAL_HEADER.ImageBase,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(pe.OPTIONAL_HEADER.ImageBase,pe.get_memory_mapped_image())
  self.write(0x5f4ab2ec,0x0e009900);self.write(0x5f4ab210,0x0e009910);self.hooks.update({0x0e009900:'selection-stock',0x0e009910:'selection-gdi-object',0x5f4408d1:'selection-from-handle',0x479b74:'selection-pen'})
  self.context=0x0d072000;self.write(self.context,0x5f4b3b48);self.write(self.context+4,1);self.write(self.context+8,1)
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name and name.startswith('?GetDC@CDDSurface@@'):self.ret(self.context)
  elif name and name.startswith('?ReleaseDC@CDDSurface@@'):self.ret(0,4)
  elif name=='selection-pen':self.pens.append(self.args(1)[0]);self.ret(0x0d078000,4)
  elif name=='selection-stock':self.stock.append(self.args(1)[0]);self.ret(0x1234,4)
  elif name=='selection-gdi-object':self.ret(0x5678,8)
  elif name=='selection-from-handle':self.ret(0x0d077000,4)
  elif name=='Rectangle':
   args=self.args(5);signed=lambda v:v if v<2**31 else v-2**32;self.rectangles.append(list(map(signed,args[1:])));self.ret(1,20)
  else:super().hook(uc,a,size,data)
 def outline(self,right,index,offset=0,x=0,y=0,empty=False):
  self.rectangles=[];self.stock=[];self.pens=[];self.parts=['old'];self.write(self.window+0x34c,index);self.write(self.window+0x350,int(not right));self.write(self.window+0xc0,offset);self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(self.window+0xb0,0x0d005800);self.write(self.window+0x354,0x0d058000)
  self.write(0x0d005840,28);self.write(0x0d005844,0x0d041000);self.write(0x0d005848,0x0d042000)
  for i in range(28):
   self.write(0x0d041000+i*4,-1 if empty else 1);left=20+i%7*35;top=40+i//7*35;self.uc.mem_write(self.window+0xc4+i*16,struct.pack('<4i',left,top,left+32,top+32))
  for i in range(12):
   left=278+i%3*35;top=40+i//3*35;self.uc.mem_write(self.window+0x284+i*16,struct.pack('<4i',left,top,left+32,top+32))
  self.run(0x44aa70,[0x0d080000],self.window);return dict(rectangles=list(self.rectangles),stock=list(self.stock),cleared=self.read(self.window+0x34c)==0xffffffff and not self.parts)
