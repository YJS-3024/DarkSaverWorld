"""Actual GameCtl status numeric/bar dispatch; supplied unit records and graphics boundaries.

The real CString formatting operands, selection branches, signed/unsigned native
32-bit arithmetic and font glyph loop run. No pixels or final combat-stat formula.
"""
import re,struct
from native_battle_briefing_emulator import BattleBriefingNative
from native_ai_emulator import UC_X86_REG_ECX
class BattleStatusNative(BattleBriefingNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x1001478c:'status-format',0x100053f0:'status-digits'});self.record=0x0d050000
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if at==0x0d000020:self.ret() # supplied software-text callback
  elif at==0x10005300:
   surface,x,y,source,l,t,r,b,flip=self.args(9);self.bodies.append([x,y,l,t,r,b]);self.ret()
  elif name and 'GetUnitInfoP@@'in name:self.ret(self.record if self.args(1)[0]==0 else 0x0d060000)
  elif name and ('getsnum@@'in name or'GetCharSurface@@'in name):self.ret(0)
  elif name=='SetRect':
   p,x,y,r,b=self.args(5);uc.mem_write(p,struct.pack('<4I',x,y,r,b));self.ret(1,20)
  elif name and 'BltFast@CDDSurface@@'in name:
   x,y,source,p,flags=self.args(5);self.blits.append([x,y,list(struct.unpack('<4i',uc.mem_read(p,16)))]);self.ret(1,20)
  elif name=='status-format':
   args=self.args(8);out,fmt=args[:2];pattern=self.text(fmt).decode('cp949');i=2
   def substitute(match):
    nonlocal i
    v=args[i];i+=1
    if match[0]=='%s':return self.text(v).decode('cp949')
    if match[0]=='%d'and v>=2**31:v-=2**32
    return str(v)
   value=re.sub(r'%lu|%s|%d',substitute,pattern);self.formats.append([pattern,value]);self.write(out,self.string(value.encode('cp949')));self.ret()
  elif name=='status-digits':
   x,y,p,color=self.args(4);self.digits.append([x,y,self.text(p).decode('cp949'),color]);self.ret()
  else:super().hook(uc,at,size,data)
 def draw(self,scene,hp,max_hp,mp,max_mp,ap,max_ap,xp,next_xp,phase=0,conditions=0):
  self.strings=0x0d080000;self.empty=self.string(b'');self.digits=[];self.blits=[];self.formats=[];self.bodies=[]
  self.uc.mem_write(self.record,bytes(512));self.uc.mem_write(0x0d060000,bytes(512))
  for at,value in [(0,1),(4,1),(8,xp),(12,next_xp),(16,0),(20,3),(32,hp),(36,max_hp),(40,mp),(44,max_mp),(52,60),(56,20),(92,max_ap),(96,ap),(100,0),(104,100),(0x104,conditions),(0x114,self.string(b'Commander')),(0x118,self.string(b'Fighter'))]:self.write(self.record+at,value)
  self.write(0x1001a380,0);self.write(0x1006c118,0);self.write(0x1001c450,0x0d000020 if scene==24 else 0);self.write(0x1006a738,0)
  for p in [0x1001c4e8,0x1001c4ec,0x1001c4f0]:self.write(p,phase)
  self.run({1:0x100054c0,9:0x10006830,24:0x100063e0}[scene],[0,0]);return dict(digits=self.digits,blits=self.blits,bodies=self.bodies)
 def glyphs(self,text,color=2):
  self.strings=0x0d080000;self.blits=[];self.hooks.pop(0x100053f0,None)
  p=self.string(text.encode('cp949'));self.run(0x100053f0,[13,17,p,color]);self.hooks[0x100053f0]='status-digits';return list(self.blits)
