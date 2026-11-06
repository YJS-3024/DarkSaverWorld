"""Original GameCtl Update entry; AI/interpreter and unused CString are boundaries."""
from native_battle_options_emulator import BattleOptionsNative
from native_ai_emulator import UC_HOOK_CODE,UC_X86_REG_EAX,UC_X86_REG_ECX
class NativeAiFrame(BattleOptionsNative):
 def __init__(self):
  super().__init__()
 def hook(self,uc,at,size,data):
  if at==0x10010f20:
   assert uc.reg_read(UC_X86_REG_ECX)==0x0d020000;self.trace.append('script');self.script+=1;self.ret(0 if self.script<self.yields else 1);return
  name=self.hooks.get(at)
  if name and'aiexcute@@'in name:self.trace.append('ai');self.ret();return
  super().hook(uc,at,size,data)
 def frame(self,ready,mode,active,phase,yields):
  self.script=0;self.yields=yields;self.trace=[];self.existing=[];self.clock=1000
  self.write(0x1001c46c,ready);self.write(0x10093864,0x0d020000);self.write(0x0d020000,active);self.write(0x0d020004,mode)
  before=self.read(0x1001c4e4);self.run(0x10003840,[phase]);return dict(calls=list(self.trace),returned=self.uc.reg_read(UC_X86_REG_EAX),counterDelta=(self.read(0x1001c4e4)-before)&0xffffffff)
