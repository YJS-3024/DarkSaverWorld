"""Late source class/stat/magic packets; allocator, transport and UI refresh boundaries."""
import struct
from native_ai_emulator import UC_X86_REG_EAX
from check_native_commanders import Native
class ClassAdjustmentsNative(Native):
 def __init__(self):
  super().__init__();self.current=0x0d040000;self.baseline=0x0d041000;self.write(self.window+0x13c,self.current);self.write(self.window+0x140,self.baseline);self.hooks[0x462630]="class-description"
 def hook(self,uc,at,size,data):
  if self.hooks.get(at)=='class-description':self.ret()
  elif self.hooks.get(at)=='send':
   n,p=self.args(2);assert n in [3,5,7];self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret(0,8)
  else:super().hook(uc,at,size,data)
 def short(self,p,v):self.uc.mem_write(p,struct.pack('<H',v&65535))
 def signed_short(self,p):return struct.unpack('<h',self.uc.mem_read(p,2))[0]
 def simple(self,kind,record,points=0):
  self.sent=[];self.short(self.current+4,record);self.short(self.current+0x3e,points)
  self.run({'costs':0x462b10,'options':0x4623a0,'confirm':0x462900,'cancel':0x462980}[kind],[],self.window);return list(self.sent)
 def stat(self,record,stat,direction,current,baseline,points,cost):
  self.sent=[];self.short(self.current+4,record);self.short(self.current+0x3c,points)
  for i in range(6):self.short(self.current+0x1a+i*6,current);self.short(self.baseline+0x1a+i*6,baseline);self.write(self.window+0x19c+i*4,cost)
  self.run(0x461dd0,[stat,direction],self.window);return dict(packets=list(self.sent),points=self.signed_short(self.current+0x3c),value=self.signed_short(self.current+0x1a+stat*6))
 def magic(self,record,magic,mode,list_kind,index=0,ready=1):
  self.sent=[];self.short(self.current+4,record);self.short(self.current+0x42,ready);self.write(self.window+0xb8,index);self.write(self.window+0xbc,list_kind)
  if index>=0:self.short(self.current+(0x46 if list_kind==0 else 0x70)+index*2,magic)
  self.write(self.window+0x198,99) # Exclude the no-selection message-box branch, not a wire branch.
  self.run(0x462000,[mode],self.window);return dict(packets=list(self.sent),ready=self.signed_short(self.current+0x42))
 def change(self,record,job,slot,points,loaded=True):
  self.sent=[];self.short(self.current+4,record);self.short(self.current+0x3e,points);self.write(self.window+0xf8,int(loaded));self.write(self.window+0xf4,slot);self.write(self.window+0xfc,2)
  self.write(self.window+0xec,job);self.write(self.window+0xe8,job);self.write(self.window+0x108,job);self.run(0x462a00,[],self.window)
  return dict(packets=list(self.sent),points=self.signed_short(self.current+0x3e),loaded=self.read(self.window+0xf8),slot=self.read(self.window+0xf4))
 def receive(self,packet):
  self.uc.mem_write(0x0d030000,packet);self.write(self.window+0xf4,-1);self.write(self.window+0xf8,0)
  self.run(0x462440,[0x0d030000,len(packet)],self.window)
  signed=lambda v:v if v<2**31 else v-2**32
  return dict(handled=self.uc.reg_read(UC_X86_REG_EAX),costs=[signed(self.read(self.window+0x19c+i*4))for i in range(6)],right=signed(self.read(self.window+0xe8)),left=signed(self.read(self.window+0xec)),count=signed(self.read(self.window+0xf0)),choices=[signed(self.read(self.window+0x100+i*4))for i in range(10)],slot=signed(self.read(self.window+0xf4)),loaded=self.read(self.window+0xf8))

 def drag(self,record,list_kind,index,count):
  self.sent=[];self.short(self.current+4,record);self.write(self.window+0xb8,index)
  offset=0x46 if list_kind==0 else 0x70;counter=0x44 if list_kind==0 else 0x6e
  self.short(self.current+counter,count)
  for i in range(16):self.short(self.current+offset+i*2,101+i)
  self.run(0x462190 if list_kind==0 else 0x462250,[],self.window)
  n=self.signed_short(self.current+counter)
  return dict(packets=list(self.sent),count=n,source=[self.signed_short(self.current+offset+i*2)for i in range(n)])
