"""Execute original bank93/94/95/96 and main/amount-window callbacks.
MFC edit/CString, representable atoi and final transport/window operations isolated.
"""
import re,struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX
class BankNative(ComposerNative):
 def __init__(self):
  super().__init__();self.child=0x0d004800;self.info=0x0d005800;self.hooks.update({0x47958c:'allocate-bank',0x479a42:'bank-edit',0x475f50:'bank-find',0x4758e0:'bank-close',0x476870:'bank-message',0x0e008700:'bank-open',0x4795a4:'bank-message-string'})
  self.write(self.window,0x484a90);self.write(self.child,0x4862ec);self.write(self.manager,0x0d004000);self.write(0x0d004004,0x0e008700);self.write(self.window+0xa0,self.manager);self.write(self.child+0xa0,self.manager);self.write(self.window+0xb0,self.info);self.messages=[];self.opened=[];self.closed=[]
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='allocate-bank':assert self.args(1)[0]in [10,20];self.ret(0x0d050000)
  elif name=='bank-edit':self.write(self.args(1)[0],self.string(self.edit.encode('cp949')));self.ret(0,4)
  elif name=='atoi':
   text=self.text(self.args(1)[0]).decode('ascii');match=re.match(r'^[ \t\r\n\v\f]*([+-]?[0-9]+)',text);value=int(match[1])if match else 0;assert -2147483648<=value<=2147483647;self.ret(value)
  elif name=='send':length,p=self.args(2);assert length in [1,5];self.sent.append(bytes(uc.mem_read(p,length)).hex());self.ret(0,8)
  elif name=='bank-open':self.opened.append(self.args(2));self.ret(1,8)
  elif name=='bank-find':assert self.args(1)[0]==35;self.ret(self.child,4)
  elif name=='bank-close':self.closed.append(self.args(1)[0]);self.ret(1,4)
  elif name=='bank-message-string':self.write(uc.reg_read(UC_X86_REG_ECX),self.string(self.text(self.args(1)[0])));self.ret(0,4)
  elif name=='bank-message':self.messages.append(self.text(self.read(self.args(2)[0])).decode('cp949'));self.ret(0,8)
  else:super().hook(uc,a,size,data)
 def query(self):
  self.sent=[];self.write(self.window+0xc0,1);self.run(0x445690,[],self.window);return dict(packets=self.sent,loaded=self.read(self.window+0xc0))
 def balance(self,value):
  self.uc.mem_write(0x0d009000,b'\x94'+value.to_bytes(8,'little'));self.write(self.window+0xc0,0);self.run(0x445620,[0x0d009000],self.window);return dict(value=self.read(self.window+0xb8)|(self.read(self.window+0xbc)<<32),loaded=self.read(self.window+0xc0))
 def amount(self,deposit,text,key=13):
  self.edit=text;self.sent=[];self.closed=[];self.write(self.child+0xb8,0x0d009800);self.write(self.child+0xcc,int(deposit));self.write(0x497c40,self.child);self.run(0x46ef00,[key]);return dict(packets=self.sent,closed=self.closed)
 def open_amount(self,control,wallet,savings,loaded):
  self.messages=[];self.opened=[];self.closed=[];self.write(self.window+0xc0,int(loaded));self.write(self.info+0x4c,wallet);self.write(self.window+0xb8,savings&0xffffffff);self.write(self.window+0xbc,savings>>32);self.run(0x4451e0,[control],self.window);return dict(messages=self.messages,opened=self.opened,closed=self.closed,deposit=self.read(self.child+0xcc)if self.opened else None)
