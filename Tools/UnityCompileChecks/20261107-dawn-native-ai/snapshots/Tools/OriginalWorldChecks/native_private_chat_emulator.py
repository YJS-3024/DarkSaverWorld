"""Read-only476230 private routing and4537A0 reply packet/order.

MFC value operations, decoded edit lookup, existing-log append/new-window creation,
Win32 edit clear and printf are explicit isolated boundaries. Actual native routing,
12-byte split, exact matching, packet header/strlen and7D helper execute.
"""
import struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX
class PrivateChatNative(ComposerNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x477520:'create-private',0x453760:'append-private',0x476840:'restore-private',0x425680:'read-private-edit',0x479a78:'clear-edit',0x479b7a:'trim-right-chars',0x47958c:'private-allocate',0x452d60:'construct-private',0x434ea0:'private-initialized',0x4797c6:'array-grow'})
 def hook(self,uc,address,size,data):
  name=self.hooks.get(address);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='create-private':
   sender,body=self.args(2);self.events.append(dict(op='create',sender=self.text(sender).decode('cp949'),body=self.text(body).decode('cp949')));self.ret(0,8)
  elif name=='append-private':self.events.append(dict(op='append',sender=self.names[this],body=self.text(self.read(self.args(1)[0])).decode('cp949')));self.ret(0,4)
  elif name=='restore-private':assert self.args(1)[0]==80;self.ret(0,4)
  elif name=='private-allocate':
   size=self.args(1)[0];assert 0<size<=1024;p=self.allocate;self.allocate+=1024;uc.mem_write(p,bytes(1024));self.ret(p)
  elif name=='construct-private':
   sender=self.args(6)[5];self.names[this]=self.text(sender).decode('cp949');self.created_window=this;self.ret(this,24)
  elif name=='private-initialized':self.ret(1)
  elif name=='array-grow':
   index,value=self.args(2);assert this in [self.manager+0x14,self.manager+0x28]and index<100;self.write(self.read(this+4)+index*4,value);self.write(this+8,max(index+1,self.read(this+8)));self.ret(0,8)
  elif name=='read-private-edit':assert self.args(1)[0]==2;self.ret(self.string(self.processed.encode('cp949')),4)
  elif name=='clear-edit':assert self.text(self.args(1)[0])==b'';self.cleared=True;self.ret(1,4)
  elif name=='trim-right-chars':self.write(this,self.string(self.text(self.read(this)).rstrip(self.text(self.args(1)[0]))));self.ret(this,4)
  elif name=='sprintf':
   out,fmt,opcode,kind,count,sender,body=self.args(7);assert self.text(fmt)==b'%c%c%c%s%s';packet=bytes([opcode&255,kind&255,count&255])+self.text(sender)+self.text(body);assert len(packet)<100;uc.mem_write(out,packet+b'\0');self.ret(len(packet))
  elif name=='send':
   length,pointer=self.args(2);assert 2<=length<100;self.sent.append(bytes(uc.mem_read(pointer,length)).hex());self.ret(0,8)
  else:super().hook(uc,address,size,data)
 def receive(self,text,senders,suppression):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.events=[];self.names={};self.write(self.manager+0x144,suppression);self.write(self.manager+0x2c,0x0d009000);self.write(self.manager+0x30,len(senders))
  for index,name in enumerate(senders):
   entry=0x0d009100+index*16;window=0x0d020000+index*1024;self.names[window]=name;self.write(0x0d009000+index*4,entry);self.write(entry+4,self.string(name.encode('cp949')));self.write(entry+8,window)
  self.write(0x0d008100,self.string(text.encode('cp949')));self.run(0x476230,[9,0x0d008100],self.manager);return self.events
 def reply(self,sender,text,whisper):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.processed=text;self.cleared=False;self.sent=[];self.write(self.window+0xb8,self.string(sender.encode('cp949')));self.write(self.window+0xd0,0x0d008000);self.write(0x490ae4,int(whisper));self.run(0x4537a0,[],self.window)
  return dict(packets=self.sent,cleared=self.cleared,whisper=bool(self.read(0x490ae4)))
 def private_filter(self,text,terms):
  hook=self.hooks.pop(0x425680)
  try:return self.filter(text,terms,2)
  finally:self.hooks[0x425680]=hook
 def create(self,sender,body,room,count):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.names={};self.events=[];self.allocate=0x0d050000;self.created_window=0
  self.write(0x0d005004,0x0d005800);self.uc.mem_write(0x0d0058d2,struct.pack('<H',room&65535));self.write(self.manager+0x18,0x0d034000);self.write(self.manager+0x1c,0);self.write(self.manager+0x2c,0x0d035000);self.write(self.manager+0x30,count);self.write(self.manager+0x40,0xffffffff)
  hook=self.hooks.pop(0x477520)
  try:self.run(0x477520,[self.string(sender.encode('cp949')),self.string(body.encode('cp949'))],self.manager)
  finally:self.hooks[0x477520]=hook
  result=dict(count=self.read(self.manager+0x30),events=self.events)
  if result['count']>count:result.update(x=self.read(self.created_window+0x40),y=self.read(self.created_window+0x44))
  return result
