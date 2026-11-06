"""Bounded cross-instance0C -> native activation/FIFO -> Update1 -> real AI/wire.
Both DLLs retain preferred bases in separate Unicorn instances; only import calls
cross this bridge. It is not one original Windows process or new runtime0C.
"""
import struct
from native_commander_action_corpus import NativeCommanderActionCorpus
from native_ai_frame_emulator import NativeAiFrame
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_EIP
class NativeAiQueueUser(NativeCommanderActionCorpus):
 def __init__(self,row,variant='ready',head=0):
  super().__init__(row,variant);record=self.DATA+0x90000;self.write(0x1010b3e4,record);self.write(record+0x88,24);self.write(record+0x8c,24)
  self.uc.mem_write(0x1003d5a0,b'\xff'*160000)
  for u in self.case['units']:self.write(0x1003d5a0+(u['x']*200+u['y'])*4,u['id'])
  for i in range(100):self.write(0x1003d3b0+i*4,-1)
  self.write(0x100e03e4,head);self.write(0x100e03e8,head);self.write(0x10016020,-1)
 def _dispatch(self,uc,address,size,data):
  if hasattr(self,'exports')and address==self.exports.get('AIStart'):return
  return super()._dispatch(uc,address,size,data)
 def call(self,address,args=()):
  sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*[v&0xffffffff for v in args]));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.emu_start(address,self.STOP+1,timeout=5000000,count=5000000)
  assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and self.uc.reg_read(UC_X86_REG_ESP)==sp+4
 def snapshot(self):return dict(head=self.read(0x100e03e4),tail=self.read(0x100e03e8),states={str(u['id']):self.read(self.RUNTIME+u['id']*0x2e0+0x100)for u in self.case['units']},packets=list(self.sent),inputs=list(self.input))
class NativeAiQueueGame(NativeAiFrame):
 def __init__(self,user):self.user=user;self.imports=[];super().__init__()
 def hook(self,uc,address,size,data):
  name=self.hooks.get(address)
  if name and('setmonsterai@@'in name or'AIStart@@'in name):
   unit=self.args(1)[0];entry='setmonsterai'if'setmonsterai@@'in name else'AIStart';self.imports.append([entry,unit]);self.user.call(self.user.exports[entry],[unit]);self.ret();return
  if name and'aiexcute@@'in name:
   self.trace.append('ai');self.imports.append(['aiexcute']);self.user.call(self.user.exports['aiexcute']);self.ret();return
  super().hook(uc,address,size,data)
 def receive(self,unit):
  self.trace=[];self.existing=[];self.uc.mem_write(0x0d040000,b'\x5c\x0c'+struct.pack('<h',unit));self.run(0x1000eb10,[0x0d040000])
