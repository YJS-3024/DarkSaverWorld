"""Execute original warehouse97/98/99/9A; isolate allocation, item table and final I/O."""
import struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EIP
from unicorn.x86_const import UC_X86_REG_ESI
class WarehouseNative(ComposerNative):
 def __init__(self):
  super().__init__();self.heap=0x0d060000;self.info=0x0d005800;self.known=set();self.closed=[];self.messages=[]
  self.hooks.update({0x47958c:'warehouse-new',0x479580:'warehouse-delete',0x4758e0:'warehouse-close',0x4795a4:'warehouse-message-string',0x476870:'warehouse-message',0x4795ce:'warehouse-format',0x43ddf0:'warehouse-text',0x43e0c0:'warehouse-text'})
  self.write(self.window,0x486430);self.write(self.window+0xb0,self.info);self.write(self.window+0xa0,self.manager);self.write(self.window+0xbc,0);self.write(self.window+0x378,0)
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='warehouse-new':
   size=self.args(1)[0];assert size<=131072;p=self.heap;self.heap+=max(16,(size+15)&~15);assert self.heap<0x0d0a0000;self.ret(p)
  elif name=='warehouse-delete':self.ret()
  elif name=='?GetItemTable@@YAPBUitemrecord@@H@Z':
   item=self.args(1)[0];p=0x0d055000;self.write(p,item);self.ret(p if item in self.known else 0)
  elif name=='send':length,p=self.args(2);assert length in [3,7];self.sent.append(bytes(uc.mem_read(p,length)).hex());self.ret(0,8)
  elif name=='warehouse-close':self.closed.append(self.args(1)[0]);self.ret(1,4)
  elif name=='warehouse-message-string':self.write(uc.reg_read(UC_X86_REG_ECX),self.string(self.text(self.args(1)[0])));self.ret(0,4)
  elif name=='warehouse-message':self.messages.append(self.text(self.read(self.args(2)[0])).decode('cp949'));self.ret(0,8)
  elif name=='warehouse-format':
   out,fmt,value=self.args(3);pattern=self.text(fmt).decode('cp949');assert pattern in ['%lu','보관료 : %lu'];self.write(out,self.string(pattern.replace('%lu',str(value)).encode('cp949')));self.ret()
  elif name=='warehouse-text':
   args=self.args(36);self.draws.append(dict(x=args[0],y=args[1],text=self.text(args[2]).decode('cp949'),color=args[3],alignment=args[-1]));self.ret(0,144)
  else:super().hook(uc,a,size,data)
 def query(self,key):
  self.sent=[];self.run(0x471640,[key],self.window);return self.sent
 def contents(self,rows,fee,known):
  self.heap=0x0d060000;self.known=set(known);payload=struct.pack('<h',len(rows))+b''.join(struct.pack('<hH',item,token)for item,token in rows)+struct.pack('<I',fee)
  self.uc.mem_write(0x0d030000,payload);self.write(self.window+0x36c,0);self.run(0x4713e0,[0x0d030000],self.window)
  signed=lambda value:value if value<2**31 else value-2**32
  result=[dict(item=signed(self.read(self.read(self.window+0xbc)+i*4)),token=self.read(self.read(self.window+0x378)+i*4)&65535)for i in range(self.read(self.window+0xb8))]
  return dict(rows=result,fee=self.read(self.window+0x370),loaded=self.read(self.window+0x36c),packet=(b'\x98'+payload).hex())
 def move(self,deposit,key,item,token,count=1):
  self.heap=0x0d060000;self.sent=[];self.messages=[];self.write(self.window+0x374,key);self.write(self.info+0x40,count)
  self.write(self.window+0x360,0);self.write(self.window+0x368,0)
  self.write(self.info+0x44,0x0d040000);self.write(self.info+0x48,0x0d041000);self.write(0x0d040000,item);self.write(0x0d041000,token)
  self.write(self.window+0xbc,0x0d042000);self.write(self.window+0x378,0x0d043000);self.write(0x0d042000,item);self.write(0x0d043000,token)
  self.run(0x4712e0 if deposit else 0x471180,[],self.window);return dict(packets=self.sent,messages=self.messages)
 def close(self):
  self.closed=[];self.sent=[];self.run(0x471670,[],self.window);return dict(closed=self.closed,packets=self.sent)
 def money_label(self,fee,value,x,y,depth=16):
  # Bounded renderer blocks; surface description/lock and other window layers are
  # explicit preconditions, not a claim of a complete DirectDraw frame.
  self.draws=[];self.write(self.window+0x40,x);self.write(self.window+0x44,y);self.write(self.window+0x370,value);self.write(self.info+0x4c,value);self.write(self.window+0xac,0x0d056000);self.write(0x0d056000,0 if depth==16 else 1)
  self.uc.reg_write(UC_X86_REG_ESI,self.window);self.uc.reg_write(UC_X86_REG_ESP,0x0f080000);start,end=(0x470e1a,0x470ec5)if fee else(0x44a7da,0x44a87e)
  self.uc.emu_start(start,end,timeout=1000000,count=20000);assert self.uc.reg_read(UC_X86_REG_EIP)==end and len(self.draws)==1;return self.draws[0]
