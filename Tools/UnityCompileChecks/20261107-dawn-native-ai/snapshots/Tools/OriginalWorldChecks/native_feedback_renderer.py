"""Original feedback render code; isolated CString/DirectDraw calls, captured native blits."""
import sys,struct
sys.path.insert(0,str(__import__('pathlib').Path(__file__).resolve().parent))
from check_native_feedback import NativeFeedback
from native_ai_emulator import UC_HOOK_CODE,UC_X86_REG_ECX
class Renderer(NativeFeedback):
 def __init__(self,c):
  super().__init__(c);self.frames=[];self.nextstring=self.DATA+0x30000
  for ptr,val in [(0x1010b3ac,1),(0x1010b3b8,2),(0x1010b3b4,3),(0x1010b3c0,4)]:self.write(ptr,val)
  for addr,name in [(self.exports['getsnum'],'palette-zero'),(0x1000b5d0,'skip-body'),(0x1000f900,'visible'),(0x100118de,'format'),(0x1001192c,'plus'),(0x10011884,'assign'),(0x1001187e,'copy'),(0x100118a2,'destroy')]:
   known=addr in self.hooks;self.hooks[addr]=name
   if not known:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=addr,end=addr)
 def cstring(self,ptr):
  data=[]
  while True:
   b=bytes(self.uc.mem_read(ptr+len(data),1))
   if b==b'\0':return bytes(data).decode()
   data.append(b[0]);assert len(data)<100
 def put(self,obj,text):
  raw=text.encode()+b'\0';p=self.nextstring;self.nextstring+=(len(raw)+31)&~15
  self.write(p,len(raw)-1);self.write(p+4,len(raw));self.uc.mem_write(p+8,raw);self.write(obj,p+8)
 def _dispatch(self,uc,address,size,data):
  name=self.hooks.get(address)
  if name in ['skip-body','palette-zero']:self.ret();return
  if name=='visible':self.ret(1);return
  if name=='format':
   obj,fmt,value=self.args(3);self.put(obj,self.cstring(fmt)%value);self.ret();return
  if name=='plus':
   obj,prefix,other=self.args(3);self.put(obj,self.cstring(prefix)+self.cstring(self.read(other)));self.ret(obj,12);return
  if name in ['assign','copy']:
   obj=uc.reg_read(UC_X86_REG_ECX);other=self.args(1)[0];self.write(obj,self.read(other));self.ret(obj,4);return
  if name=='destroy':self.ret();return
  if name=='SetRect':
   ptr,*v=self.args(5);self.uc.mem_write(ptr,struct.pack('<iiii',*v));self.ret(1,20);return
  if name and 'BltFast' in name:
   x,y,surface,rect,flags=self.args(5);a,b,c,d=struct.unpack('<iiii',self.uc.mem_read(rect,16));self.frames.append(dict(surface=surface-1,x=x,y=y,atlasX=a,atlasY=b,width=c-a,height=d-b));self.ret(0,20);return
  super()._dispatch(uc,address,size,data)
