"""Actual464150 bar field selection and462740 magic description; GDI/MFC boundaries."""
import struct
from native_shop_details_emulator import ShopDetailsNative
from native_class_adjustments_emulator import ClassAdjustmentsNative
from native_ai_emulator import UC_X86_REG_EAX,UC_X86_REG_ECX
class ClassMagicNative(ShopDetailsNative):
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if name=='#2614':self.write(uc.reg_read(UC_X86_REG_ECX),self.empty);self.ret()
  elif name and 'GetMagicTbl2@@'in name:self.ret(self.magic)
  else:super().hook(uc,at,size,data)
 def description(self,value):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.magic=0 if value is None else 0x0d060000
  if value is not None:
   self.uc.mem_write(self.magic,bytes(512));self.write(self.magic,value['id']);self.uc.mem_write(self.magic+4,value['name'].encode('cp949')+b'\0')
   for offset,key in [(0x108,'distance'),(0x10c,'range'),(0x110,'mp'),(0x118,'team')]:self.write(self.magic+offset,value[key])
  self.run(0x462740,[value['id']if value else 101],self.window);return [self.text(self.read(self.window+offset)).decode('cp949')for offset in [0x130,0x134,0x138]]
class ClassBarsNative(ClassAdjustmentsNative):
 def hook(self,uc,at,size,data):
  if self.hooks.get(at)=='MulDiv':
   signed=lambda n:n if n<2**31 else n-2**32;a,b,c=map(signed,self.args(3));product=a*b
   if c==0:value=-1
   else:
    value=(abs(product)+abs(c)//2)//abs(c);value=-value if (product<0)!=(c<0)else value
    if value<-(2**31)or value>=2**31:value=-1
   self.ret(value&0xffffffff,12)
  else:super().hook(uc,at,size,data)
 def metric(self,stat,baseline,before,after,limit,width=176):
  for i in range(6):
   self.short(self.baseline+0x18+i*6,baseline)
   for j,value in enumerate([before,after,limit]):self.short(self.current+0x18+i*6+j*2,value)
   self.uc.mem_write(self.window+0x3dc+i*16,struct.pack('<4i',285,226+i*22,285+width,236+i*22))
  out=0x0d009000;self.run(0x464150,[stat,out,out+4,out+8,out+12],self.window);signed=lambda n:n if n<2**31 else n-2**32
  return dict(filled=signed(self.uc.reg_read(UC_X86_REG_EAX)),baseline=signed(self.read(out+12)),before=signed(self.read(out)),after=signed(self.read(out+4)),limit=signed(self.read(out+8)))
from native_mercenary_window_emulator import MercenaryNative
class ClassHeaderNative(MercenaryNative):
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if name and 'BltFast@CDDSurface@@'in name:self.ret(1,20 if 'PAVCRect' in name else 16)
  elif name and 'GetCharSurface@@'in name:self.ret(0)
  else:super().hook(uc,at,size,data)
 def header(self,level,xp,x=0,y=0):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.draws=[];self.ability=0x0d050000;self.uc.mem_write(self.ability,bytes(512));self.write(self.ability+4,100);self.uc.mem_write(self.ability+160,'파이터'.encode('cp949')+b'\0')
  self.uc.mem_write(self.records,bytes(732));self.uc.mem_write(self.records+10,struct.pack('<h',100));self.uc.mem_write(self.records+14,struct.pack('<h',level));self.write(self.records+16,xp);self.uc.mem_write(self.records+0xbb,'지휘관'.encode('cp949')+b'\0')
  self.write(self.window+0x13c,self.records);self.write(self.window+0xac,0x0d056000);self.write(0x0d056000,0);self.write(self.window+0x40,x);self.write(self.window+0x44,y)
  for offset,rect in [(0x1b4,[20,47,84,111]),(0x1c4,[127,79,191,143])]:self.uc.mem_write(self.window+offset,struct.pack('<4i',*rect))
  for offset,value in [(0x1d4,50),(0x1d8,128),(0x1dc,50),(0x1e0,145),(0x1e4,50),(0x1e8,162),(0x1ec,50),(0x1f0,179)]:self.write(self.window+offset,value)
  self.run(0x462c50,[0x0d080000],self.window);return list(self.draws)
from native_shop_selection_emulator import ShopSelectionNative
class ClassSelectionNative(ShopSelectionNative):
 def hook(self,uc,at,size,data):
  if self.hooks.get(at)=='FillRect':
   hdc,p,brush=self.args(3);self.fills.append(dict(rect=list(struct.unpack('<4i',uc.mem_read(p,16))),brush=brush));self.ret(1,12)
  else:super().hook(uc,at,size,data)
 def selection(self,list_kind,index,slot,x=0,y=0):
  self.rectangles=[];self.fills=[];self.stock=[];self.pens=[];self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(self.window+0xb8,index);self.write(self.window+0xbc,list_kind);self.write(self.window+0xf4,slot);self.write(self.window+0x154,0x12345678)
  for i in range(18):left=208+i%8*33;top=47+i//8*33;self.uc.mem_write(self.window+0x1f4+i*16,struct.pack('<4i',left,top,left+32,top+32))
  for i in range(8):left=208+i*33;top=121;self.uc.mem_write(self.window+0x2f4+i*16,struct.pack('<4i',left,top,left+32,top+32))
  for at,(left,top)in zip([0x38c,0x3ac,0x3bc],[(32,228),(167,228),(100,296)]):self.uc.mem_write(self.window+at,struct.pack('<4i',left,top,left+32,top+32))
  self.run(0x463ce0,[0x0d080000],self.window);return dict(rectangles=list(self.rectangles),fills=list(self.fills),stock=list(self.stock))
class ClassIconNative(MercenaryNative):
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if name and 'GetIconSurface@@'in name:self.ret(0x0d080000 if self.surface else 0)
  elif name and 'GetMagicTbl2@@'in name:self.ret(self.magic if self.args(1)[0]==self.magic_id else 0)
  elif name=='SetRect':
   p,l,t,r,b=self.args(5);uc.mem_write(p,struct.pack('<4I',l,t,r,b));self.ret(1,20)
  elif name and 'BltFast@CDDSurface@@'in name:
   x,y,source,p,flags=self.args(5);self.blits.append(dict(x=x,y=y,rect=list(struct.unpack('<4i',uc.mem_read(p,16))),flags=flags));self.ret(1,20)
  else:super().hook(uc,at,size,data)
 def icons(self,item,list_kind,index=0,drag=False,surface=True,matching=True,x=0,y=0):
  self.blits=[];self.surface=surface;self.magic=0x0d050000;self.magic_id=item['id'];self.uc.mem_write(self.magic,bytes(320));self.write(self.magic,item['id']if matching else item['id']+1);self.write(self.magic+0x138,item['iconX']);self.write(self.magic+0x13c,item['iconY'])
  self.uc.mem_write(self.records,bytes(732));self.write(self.window+0x13c,self.records);self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(self.window+0xc4,int(drag));self.write(self.window+0xc8,int(drag and list_kind==0));self.write(self.window+0xd8,int(drag and list_kind==1));self.write(self.window+(0xcc if list_kind==0 else 0xdc),index)
  self.uc.mem_write(self.records+(0x44 if list_kind==0 else 0x6e),struct.pack('<h',index+1));self.uc.mem_write(self.records+(0x46 if list_kind==0 else 0x70)+index*2,struct.pack('<h',item['id']))
  for i in range(16 if list_kind==0 else 8):
   left=208+i%8*33;top=(47 if list_kind==0 else 121)+i//8*33;self.uc.mem_write(self.window+(0x1f4 if list_kind==0 else 0x2f4)+i*16,struct.pack('<4i',left,top,left+32,top+32))
  self.run(0x4635c0 if list_kind==0 else 0x463720,[0x0d080000],self.window);return list(self.blits)
class ClassGhostNative(ClassIconNative):
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if name and 'BltFast@CDDSurface@@'in name and 'PAVCRect'not in name:
   x,y,source,flags=self.args(4);self.denied.append(dict(x=x,y=y,flags=flags));self.ret(1,16)
  else:super().hook(uc,at,size,data)
 def ghost(self,list_kind,opposite,x,y,origin=(0,0)):
  item=dict(id=101,iconX=0,iconY=32);ox,oy=origin;self.icons(item,list_kind,0,True,x=ox,y=oy);self.blits=[];self.denied=[]
  self.uc.mem_write(self.records+(0x6e if list_kind==0 else 0x44),struct.pack('<h',opposite));self.write(self.window+0x10,x);self.write(self.window+0x14,y);self.write(self.window+0xc0,0x0d099000)
  self.write(self.window+(0xd0 if list_kind==0 else 0xe0),224);self.write(self.window+(0xd4 if list_kind==0 else 0xe4),63 if list_kind==0 else 137)
  for k in [0,1]:
   for i in range(16 if k==0 else 8):
    l=208+i%8*33;t=(47 if k==0 else 121)+i//8*33;self.uc.mem_write(self.window+(0x1f4 if k==0 else 0x2f4)+i*16,struct.pack('<4i',l,t,l+32,t+32))
  self.run(0x464380,[0x0d080000],self.window);return dict(icon=list(self.blits),denied=list(self.denied))
