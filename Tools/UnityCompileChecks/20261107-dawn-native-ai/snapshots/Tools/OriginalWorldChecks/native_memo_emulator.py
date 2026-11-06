"""Read-only original memo record constructor/delete code; MFC object storage isolated."""
import struct
from check_native_commanders import Native
from native_ai_emulator import UC_X86_REG_ECX,UC_X86_REG_EAX
class MemoNative(Native):
 def __init__(self):
  super().__init__();self.strings={};self.removed=[];self.hooks.update({0x47958c:'new-memo',0x479598:'string-empty',0x4795b6:'string-assign',0x479aba:'remove-at'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='new-memo':assert self.args(1)[0]==20;self.uc.mem_write(0x0d080000,b'\0'*20);self.ret(0x0d080000)
  elif name=='string-empty':self.strings[uc.reg_read(UC_X86_REG_ECX)]=b'';self.ret(uc.reg_read(UC_X86_REG_ECX))
  elif name=='string-assign':
   p=self.args(1)[0];value=b''
   for i in range(512):
    c=bytes(uc.mem_read(p+i,1))
    if c==b'\0':break
    value+=c
   else:raise AssertionError('unterminated native memo string')
   self.strings[uc.reg_read(UC_X86_REG_ECX)]=value;self.ret(uc.reg_read(UC_X86_REG_ECX),4)
  elif name=='send':n,p=self.args(2);assert n==2;self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret(0,8)
  elif name=='remove-at':index,count=self.args(2);assert count==1;self.removed.append(index);self.ret(0,8)
  else:super().hook(uc,a,size,data)
 def record(self,payload):
  self.uc.mem_write(0x0f0fe000,b'\xcc'*0x1000);self.uc.mem_write(0x0d040000,payload);self.write(0x0d006000,1);self.run(0x43c690,[0x0d040000,0x0d006000]);record=self.uc.reg_read(UC_X86_REG_EAX)
  return dict(id=bytes(self.uc.mem_read(record+4,1))[0],sender=self.strings[record+8].decode('cp949'),date=self.strings[record+12].decode('cp949'),body=self.strings[record+16].decode('cp949'),cursor=self.read(0x0d006000))
 def delete(self,count,page,row):
  self.sent=[];self.removed=[];info=0x0d006000;array=0x0d007000;self.write(self.manager+0xdc,info);self.write(info+8,array);self.write(info+12,count)
  for i in range(count):self.write(array+i*4,0x0d009000+i*32);self.uc.mem_write(0x0d009004+i*32,bytes([(i*7)%256]))
  self.uc.mem_write(self.window+0xb5,bytes([page]));self.run(0x4673a0,[row],self.window)
  return dict(packet=self.sent[0]if self.sent else None,removed=self.removed)
