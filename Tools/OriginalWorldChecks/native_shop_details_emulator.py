"""Execute44B1C0 item description; isolate table/MFC CString/list allocation only.

The original level/class predicate, field selection, format operands and call
order execute as x86. Wrapping and final DirectDraw/GDI pixels are separate.
"""
import re,struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX

class ShopDetailsNative(ComposerNative):
 def __init__(self):
  super().__init__();self.parts=[];self.formats=[]
  self.hooks.update({0x47958c:'details-new',0x479580:'details-delete',0x4795a4:'details-string',0x4795ce:'details-format',0x4797f0:'details-append',0x479a06:'details-clear',0x436b00:'details-line'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='details-new':assert self.args(1)[0]==255;self.ret(0x0d080000)
  elif name=='details-delete':self.ret()
  elif name=='details-string':self.write(this,self.string(self.text(self.args(1)[0])));self.ret(this,4)
  elif name=='details-append':self.write(this,self.string(self.text(self.read(this))+self.text(self.read(self.args(1)[0]))));self.ret(this,4)
  elif name=='details-clear':self.parts=[];self.ret()
  elif name=='details-line':self.parts.append(self.text(self.read(self.args(1)[0])).decode('cp949'));self.ret(0,4)
  elif name=='?GetItemTable@@YAPBUitemrecord@@H@Z':self.ret(self.item)
  elif name=='?GetCharAbility@@YAPBUabilityrecord@@H@Z':self.ret(self.ability)
  elif name=='details-format':
   args=self.args(8);out,fmt=args[:2];pattern=self.text(fmt).decode('cp949');at=2
   def replace(match):
    nonlocal at
    kind=match[0];value=args[at];at+=1
    if kind=='%s':return self.text(value).decode('cp949')
    if kind=='%I64u':value|=args[at]<<32;at+=1
    elif kind=='%d'and value>=2**31:value-=2**32
    return str(value)
   value=re.sub(r'%I64u|%lu|%s|%d',replace,pattern);self.formats.append(pattern);self.write(out,self.string(value.encode('cp949')));self.ret()
  else:super().hook(uc,a,size,data)
 def describe(self,record,level,kind,available=True,owned=False):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.parts=[];self.formats=[];self.item=0x0d060000;self.ability=0x0d050000 if available else 0
  self.uc.mem_write(self.item,bytes(record));self.write(self.window+0x34c,0);self.write(self.window+0x350,int(owned));self.write(self.window+0xbc,0x0d041000);self.write(0x0d041000,0)
  info=0x0d005800;self.write(self.window+0xb0,info);self.write(info+0x3c,0x0d048000);self.write(info+0x40,1);self.write(info+0x44,0x0d041000);self.write(info+0x48,0x0d042000)
  self.uc.mem_write(0x0d04800a,struct.pack('<hh',100,level));self.write(0x0d050058,kind);self.write(self.window+0x354,0x0d058000)
  self.run(0x44b1c0,[],self.window);return list(self.parts)

def item_record(row):
 from build_native_shop_details import integer
 b=bytearray(660)
 for at,key in enumerate(['번호','그림1','그림2','장착위','장착렙','내구']):struct.pack_into('<i',b,at*4,integer(row[key]))
 for at,key in enumerate(['보','비','기','수','창','궁','승','신','마','사']):b[24+at]=integer(row[key])&255
 struct.pack_into('<Q',b,40,integer(row['가격'])&((1<<64)-1));struct.pack_into('<i',b,48,integer(row['마법번']))
 fields=['이동','체력','공격','방어','공범위','마법력','마법공','마법방','마법범위','지휘범위','명중률','회피율','크컬율','마명률','마회율','마렙치','이틱감','공틱감','휴틱감','마틱감']
 for at,key in enumerate(fields):struct.pack_into('<i',b,52+at*4,integer(row[key]))
 for at,key in [(148,'이름'),(404,'설명')]:
  value=row[key].replace('_',' ').encode('cp949');assert len(value)<256;b[at:at+len(value)]=value
 return bytes(b)
