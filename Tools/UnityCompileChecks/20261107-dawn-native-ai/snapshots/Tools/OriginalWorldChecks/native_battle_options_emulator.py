"""Actual late GameCtl UI option dispatch and position gateway; no host network/UI."""
import hashlib,struct
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX,UC_X86_REG_ECX
class BattleOptionsNative:
 def __init__(self):
  raw=Path('/workspace/.tools/ds-ghidra/installed/saver/GameCtl.dll').read_bytes();self.sha256=hashlib.sha256(raw).hexdigest();assert self.sha256=='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
  pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x10000000,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x10000000,pe.get_memory_mapped_image())
  for at in [0,0x0d000000,0x0f000000]:self.uc.mem_map(at,0x100000 if at else 0x1000)
  self.stop=0x0d000000;self.callback=0x0d000010;self.hooks={}
  for i,imp in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
   target=0x0d001000+i*16;self.write(imp.address,target);self.hooks[target]=imp.name.decode()if imp.name else '#'+str(imp.ordinal)
  self.hooks.update({0x10014792:"empty-ctor",0x1001476e:"string-dtor"});self.write(0x1001c448,self.callback);self.uc.hook_add(UC_HOOK_CODE,self.hook)
 def write(self,p,v):self.uc.mem_write(p,struct.pack('<I',v&0xffffffff))
 def read(self,p):return struct.unpack('<I',self.uc.mem_read(p,4))[0]
 def args(self,n):return list(struct.unpack('<'+'I'*n,self.uc.mem_read(self.uc.reg_read(UC_X86_REG_ESP)+4,n*4)))
 def ret(self,value=0,cleanup=0):
  sp=self.uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);self.uc.reg_write(UC_X86_REG_EAX,value&0xffffffff);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EIP,pc)
 def hook(self,uc,at,size,data):
  if at==self.stop:uc.emu_stop();return
  if at==self.callback:
   values=self.args(4);self.trace.append(values);self.ret(int(values[1]in self.existing)if values[0]==0x4d1 else 0);return
  name=self.hooks.get(at)
  if name=='empty-ctor':self.write(uc.reg_read(UC_X86_REG_ECX),0x0d00f000);self.ret(uc.reg_read(UC_X86_REG_ECX))
  elif name=='string-dtor':self.ret()
  elif name=='clock':self.ret(self.clock)
  elif name and 'mainmapgetchar@@'in name:self.ret(self.found)
  elif name and 'GetUnitInfoP@@'in name:
   id=self.args(1)[0];assert id in [0,10,20,30];p=0x0d010000+id*0x100;self.write(p,int(self.active[id//10]));self.write(p+4,int(self.enabled[id//10]));self.ret(p)
  elif name:raise RuntimeError('Unimplemented native boundary '+name+' '+hex(at))
 def run(self,address,args):
  sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.stop,*args));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.emu_start(address,self.stop+1,timeout=1000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop
 def option(self,control,active,enabled,existing=(),tutorial=False):
  self.trace=[];self.existing=existing;self.active=active;self.enabled=enabled;self.write(0x1006c118,int(tutorial));self.write(0x1006a738,0);self.run(0x10004a80,[4,control]);return self.trace
 def key(self,key,active,enabled,existing=(),team=0):
  self.trace=[];self.existing=existing;self.active=active;self.enabled=enabled;self.write(0x1006c118,0);self.write(0x1006a738,team);self.run(0x10002fb0,[key]);return self.trace
 def help_scroll(self,offset,control,count,held=False):
  self.write(0x1001c4b4,offset);self.write(0x1001c4d0,count);self.write(0x1006c118,0);self.run(0x10005200 if held else 0x10004a80,[7,control]);v=self.read(0x1001c4b4);return v if v<2**31 else v-2**32
 def compact(self,clocks):
  self.trace=[];self.existing=[];self.write(0x1006c118,0);self.write(0x1001a0ec,0);results=[]
  for clock in clocks:
   self.clock=clock;self.trace=[];self.run(0x10004830,[9,0,0]);results.append(dict(clock=clock,trace=list(self.trace),last=self.read(0x1001a0ec)))
  return results
 def minimap(self,x,y,width,height,left=0,top=0):
  self.trace=[];self.write(0x1006c118,0)
  for p,v in [(0x1001c45c,width),(0x1001c458,height),(0x1001c460,left),(0x1001c464,top)]:self.write(p,v)
  self.run(0x10004830,[0,x&0xffffffff,y&0xffffffff]);signed=lambda v:v if v<2**31 else v-2**32
  return [signed(self.read(p))for p in [0x1001c460,0x1001c464]]

 def hover(self,selected,found):
  self.found=found;self.write(0x1006c118,0);self.write(0x1006a70c,0);self.write(0x1001a380,selected)
  self.run(0x10002d50,[320,240]);v=self.read(0x1001a380);return v if v<2**31 else v-2**32
