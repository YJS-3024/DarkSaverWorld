"""Actual late46F7F0 labels and470070/4701F0 requests, MFC/surface/I/O isolated."""
import struct
from native_shop_details_emulator import ShopDetailsNative
from native_ai_emulator import UC_X86_REG_ECX
class MercenaryNative(ShopDetailsNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x43e390:'mercenary-paragraph',0x43ea00:'mercenary-paragraph',0x43ddf0:'mercenary-line',0x43e0c0:'mercenary-line'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name and name.startswith('?GetDescription@CDDSurface@@'):uc.mem_write(self.args(1)[0],bytes(124));self.ret(0,4)
  elif name and name.startswith('?Lock@CDDSurface@@'):self.ret(0x0d080000)
  elif name and name.startswith('?Unlock@CDDSurface@@'):self.ret()
  elif name=='OffsetRect':
   p,x,y=self.args(3);signed=lambda v:v if v<2**31 else v-2**32;l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));uc.mem_write(p,struct.pack('<4i',l+signed(x),t+signed(y),r+signed(x),b+signed(y)));self.ret(1,12)
  elif name=='mercenary-paragraph':
   args=self.args(42);signed=lambda v:v if v<2**31 else v-2**32;self.draws.append(dict(rect=list(map(signed,args[:4])),text=self.text(args[4]).decode('cp949'),color=args[5],flags=list(args[-5:])));self.ret(0,168)
  elif name=='mercenary-line':
   args=self.args(36);signed=lambda v:v if v<2**31 else v-2**32;self.draws.append(dict(x=signed(args[0]),y=signed(args[1]),text=self.text(args[2]).decode('cp949'),color=args[3],alignment=args[-1]));self.ret(0,144)
  elif name=='details-new':self.ret(0x0d080000)
  elif name=='send':length,p=self.args(2);assert length in [5,7];self.sent.append(bytes(uc.mem_read(p,length)).hex());self.ret(0,8)
  else:super().hook(uc,a,size,data)
 def setup(self,commander,hired,count,offers,selected):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.sent=[];self.write(self.window+0x184,self.records);self.uc.mem_write(self.records+4,struct.pack('<H',commander));self.uc.mem_write(self.records+0x9a,struct.pack('<hh',hired,count))
  self.write(self.window+0x174,len(offers));self.write(self.window+0x178,0x0d041000);self.write(self.window+0x17c,0x0d042000);self.write(self.window+0x188,selected)
  for i,(job,maximum)in enumerate(offers):self.write(0x0d041000+i*4,job);self.write(0x0d042000+i*4,maximum)
 def request(self,hire,commander,hired,count,offers,selected=-1):
  self.setup(commander,hired,count,offers,selected);self.run(0x470070 if hire else 0x4701f0,[],self.window)
  job,number=struct.unpack('<hh',self.uc.mem_read(self.records+0x9a,4));return dict(packets=list(self.sent),previewJob=job,previewCount=number)
 def labels(self,unit,hired,count,offers,selected,gold,x=0,y=0,depth=16):
  self.setup(0,hired,count,offers,selected);self.draws=[];self.parts=[];self.formats=[];self.ability=0x0d050000;self.uc.mem_write(self.ability,bytes(512));self.write(self.window+0xac,0x0d056000);self.write(0x0d056000,int(depth==8));self.write(self.window+0x40,x);self.write(self.window+0x44,y)
  self.write(self.window+0xb0,0x0d005800);self.uc.mem_write(0x0d00584c,struct.pack('<Q',gold));self.uc.mem_write(self.window+0xc4,struct.pack('<4i',189,48,211,64));self.uc.mem_write(self.window+0xd4,struct.pack('<4i',19,148,148,234))
  if unit is not None:
   for at,key in [(8,'hp'),(24,'attack'),(32,'defense'),(128,'price')]:self.write(self.ability+at,unit[key])
   self.uc.mem_write(self.ability+160,unit['name'].encode('cp949')+b'\0')
  self.run(0x46f7f0,[0x0d080000],self.window);return list(self.draws)
 def bank_labels(self,wallet,savings,loaded,x=0,y=0,depth=16):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.draws=[];self.formats=[];self.write(self.window+0xac,0x0d056000);self.write(0x0d056000,int(depth==8));self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(self.window+0xb0,0x0d005800)
  self.uc.mem_write(0x0d00584c,struct.pack('<Q',wallet));self.uc.mem_write(self.window+0xb8,struct.pack('<Q',savings));self.write(self.window+0xc0,int(loaded));self.uc.mem_write(self.window+0xc4,struct.pack('<4i',115,47,311,19));self.run(0x4453b0,[0x0d080000],self.window);return list(self.draws)
