"""Native ordinary-shop drag requests; house-purchase D8 dialog is a separate path."""
from native_warehouse_emulator import WarehouseNative
from native_ai_emulator import UC_X86_REG_EAX
class ShopNative(WarehouseNative):
 def __init__(self):
  super().__init__();self.write(self.window,0x484e10)
 def hook(self,uc,a,size,data):
  if self.hooks.get(a)=='send':
   length,p=self.args(2);assert length in [3,5];self.sent.append(bytes(uc.mem_read(p,length)).hex());self.ret(0,8)
  else:super().hook(uc,a,size,data)
 def drop(self,buy,item,token=0,base=0,count=1):
  self.heap=0x0d060000;self.sent=[];self.messages=[];self.closed=[];self.write(self.window+0xb4,base);self.write(self.info+0x40,count)
  self.write(self.window+0x360,0);self.write(self.window+0x368,0);self.write(self.window+0xbc,0x0d042000);self.write(0x0d042000,item)
  self.write(self.info+0x44,0x0d040000);self.write(self.info+0x48,0x0d041000);self.write(0x0d040000,item);self.write(0x0d041000,token)
  self.run(0x44af40 if buy else 0x44b060,[],self.window);return dict(packets=self.sent,messages=self.messages,closed=self.closed)
 def allows(self,item,base):
  self.write(self.window+0xb4,base);self.run(0x44b8d0,[item&0xffffffff],self.window);return bool(self.uc.reg_read(UC_X86_REG_EAX))
