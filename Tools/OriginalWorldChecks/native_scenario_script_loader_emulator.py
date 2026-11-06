"""Actual GameCtl66 assignment and FAB0 script loading with bounded MFC/archive/file/profile hooks."""
from native_event_loader_emulator import NativeEventLoader
from native_ai_emulator import UC_X86_REG_ECX
class NativeScenarioScriptLoader(NativeEventLoader):
 def raw_string(self,obj):
  p=self.read(obj);count=self.read(p-8);assert count<65536;return bytes(self.uc.mem_read(p,count))
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if at==0x1000f7b0:self.events.append(['first-clear-gate']);self.ret(0)
  elif name=='#859':
   this=uc.reg_read(UC_X86_REG_ECX);v=self.args(1)[0]&255;self.write(this,self.string(bytes([v])));self.ret(this,4)
  elif at==0x10014864:
   out,left,right=self.args(3);self.write(out,self.string(self.raw_string(left)+self.raw_string(right)));self.ret(out,12)
  elif name=='brief-concat':
   out,left,right=self.args(3);self.write(out,self.string(self.raw_string(left)+self.text(right)));self.ret(out,12)
  elif at==0x1000bdf0:self.events.append(['intro-skip']);self.ret()
  elif name in ['_stricmp','stricmp']:
   a,b=self.args(2);x=self.text(a).lower();y=self.text(b).lower();self.ret((x>y)-(x<y))
  elif name and 'userclearstatusall@@'in name:self.events.append(['clear-units']);self.ret()
  else:super().hook(uc,at,size,data)
 def script(self,source,key='01',ext='dee',suffix=b'',tutorial=False,archive=True,opened=True,extracted=True):
  self.setup(['END']);self.lines=source.splitlines();self.row=0;self.paths=[];self.opened=opened;self.extracted=extracted
  self.write(0x1001a8b0,0x0d060000 if archive else 0);self.write(0x1001a8b4,self.string(b'root\\'));self.write(0x1006c0e8,self.string(key.encode()));self.write(0x1006bd0c,self.string(suffix));self.write(0x1006c118,int(tutorial))
  extension=self.string(ext.encode());self.uc.reg_write(UC_X86_REG_ECX,self.SCRIPT);self.run(0x1000fab0,[extension]);return dict(paths=self.paths,commands=[self.text(self.read(self.SCRIPT+0x50+i*4)).decode('cp949')for i in range(1,min(self.row,999)+1)],mode=self.read(self.SCRIPT+4),active=self.read(self.SCRIPT+12),outro=self.read(self.SCRIPT+8),events=list(self.events))

 def suffix(self,value):
  self.setup(['END']);self.write(0x1006bd0c,self.string(b'old'));self.uc.mem_write(0x0d040000,bytes([0x5c,0x66,value]));self.run(0x1000eb10,[0x0d040000]);return self.raw_string(0x1006bd0c)
