"""Actual1000A800 menu atlas phase; supplied unit/menu/clock and graphics boundaries."""
import sys,struct
from native_battle_status_emulator import BattleStatusNative
from native_ai_emulator import UC_X86_REG_ECX
class BattleMenuNative(BattleStatusNative):
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if at==0x10008d40:self.ret()
  elif name and ('_drawactivecharacter@@'in name):self.ret()
  elif name and 'GetItemSurface@@'in name:self.ret(0x98765432)
  elif name=='OffsetRect':
   p,x,y=self.args(3);s=lambda v:v if v<2**31 else v-2**32;l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));uc.mem_write(p,struct.pack('<4i',l+s(x),t+s(y),r+s(x),b+s(y)));self.ret(1,12)
  else:super().hook(uc,at,size,data)
 def icon(self,clock,base_y,disabled=False):
  self.clock=clock;self.blits=[];self.bodies=[];obj=0x0d070000;self.uc.mem_write(obj,bytes(1024));self.uc.mem_write(self.record,bytes(512))
  for p,v in [(self.record,1),(self.record+4,1),(self.record+0x70,10),(self.record+0x74,10),(self.record+0x10c,0 if disabled else 0xffff),(0x1001a0b8,20),(0x1001a0bc,15),(0x1001c460,0),(0x1001c464,0),(0x1001c47c,0x12345678),(obj+0x10,1),(obj+0x3c,0x12345678),(obj+0x1f8,0x5f4 if disabled else 1515),(obj+0x30,-9999),(obj+0x34,-9999)]:self.write(p,v)
  self.uc.mem_write(obj+0x40,struct.pack('<4i',-32,-32,0,0));self.uc.mem_write(obj+0xe0,struct.pack('<4i',0,base_y,32,base_y+32));self.uc.reg_write(UC_X86_REG_ECX,obj);self.run(0x1000a800,[]);return self.blits
