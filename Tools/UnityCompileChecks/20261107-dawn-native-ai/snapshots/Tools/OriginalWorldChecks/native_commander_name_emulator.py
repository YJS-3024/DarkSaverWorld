"""Original4587C0 serializer and458750 Enter/Escape dispatch; MFC/Win32 isolated."""
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX
class CommanderNameNative(ComposerNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x47958c:'allocate-name',0x479a42:'name-edit-text',0x424b60:'hide-name-edit'});self.hidden=0;self.focused=[]
 def hook(self,uc,address,size,data):
  name=self.hooks.get(address);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='allocate-name':assert self.args(1)[0]==200;self.ret(0x0d050000)
  elif name=='name-edit-text':self.write(self.args(1)[0],self.string(self.editBytes));self.ret(0,4)
  elif name=='hide-name-edit':self.hidden+=1;self.ret()
  elif name=='SetFocus':self.focused.append(self.args(1)[0]);self.ret(0,4)
  else:super().hook(uc,address,size,data)
 def submit(self,record,name,key=13):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.sent=[];self.hidden=0;self.focused=[];self.editBytes=name.encode('cp949');assert len(self.editBytes)<=196,'Stay within original200-byte allocation for native execution'
  self.write(0x497b84,self.window);self.write(self.window+0xcc,0x0d009400);self.write(self.window+0xc8,0x12345);self.write(self.window+0xb4,self.records);self.write(self.records+4,record)
  self.run(0x458750,[key]);return dict(packets=self.sent,hidden=self.hidden,focus=self.focused)
