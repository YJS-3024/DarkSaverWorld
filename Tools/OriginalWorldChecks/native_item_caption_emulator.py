"""Original46BF20 caption formatting and F4's inherited double-click dispatch.

MFC CString/format/allocator and clock are isolated. F4+28 executes its actual RET8;
minimized restore is a UI-construction boundary. No inventory-drop handler claim.
"""
import struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX
class ItemCaptionNative(ComposerNative):
 def __init__(self):
  super().__init__();self.hooks.pop(0x433d00);self.hooks.update({0x479a72:'empty-caption',0x47958c:'allocate-caption',0x4795ce:'format-caption',0x0e008400:'caption-mode',0x4344b0:'restore-caption-window'});self.noops=0
 def hook(self,uc,address,size,data):
  name=self.hooks.get(address);this=uc.reg_read(UC_X86_REG_ECX)
  if address==0x433e20:self.noops+=1 # Observe only: execute the actual inherited RET8.
  if name=='empty-caption':self.write(this,self.empty);self.ret()
  elif name=='allocate-caption':assert self.args(1)[0]==255;self.ret(0x0d050000)
  elif name=='format-caption':
   out,fmt,name,uses=self.args(4);assert self.text(fmt).decode('cp949')=='%s, 사용회수: %d';uses=uses if uses<2147483648 else uses-4294967296;value=self.text(name).decode('cp949')+', 사용회수: '+str(uses);self.write(out,self.string(value.encode('cp949')));self.ret()
  elif name=='caption-mode':self.ret(self.mode)
  elif name=='clock':self.ret(self.now)
  elif name=='restore-caption-window':self.restores+=1;self.write(this+0x80,0);self.ret(1)
  else:super().hook(uc,address,size,data)
 def caption(self,name,uses,mode):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.mode=mode;self.write(self.window+0x324,self.empty);self.write(self.window+0xa0,self.manager);self.write(self.manager+0x48,0x0d004400);self.write(0x0d004400,0x0d004800);self.write(0x0d004804,0x0e008400);self.write(self.window+0x2f8,uses)
  item=0 if name is None else 0x0d060000
  if item:self.uc.mem_write(item+0x94,name.encode('cp949')+b'\0')
  self.run(0x46bf20,[item],self.window);return self.text(self.read(self.window+0x324)).decode('cp949')
 def double_click(self,delta,minimized):
  self.uc.mem_write(self.window,bytes(1024));self.write(self.window,0x486108);self.write(self.window+0x78,1);self.write(self.window+0x84,1000);self.write(self.window+0x80,int(minimized));self.write(self.window+0x40,10);self.write(self.window+0x44,20);self.uc.mem_write(0x0d009000,struct.pack('<2i',123,45));self.now=1000+delta;self.noops=0;self.restores=0;self.sent=[]
  self.run(0x433d00,[0,0x0d009000],self.window);return dict(noop=self.noops,restore=self.restores,packets=self.sent,localX=self.read(self.window+0x10),localY=self.read(self.window+0x14))
