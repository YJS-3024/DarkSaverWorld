"""Actual UserUnit AI loader/helpers linked to relocated original setunit string helpers.
CString memory, archive/file/format and allocation remain bounded host services.
"""
import sys,struct,re,hashlib
from pathlib import Path
from native_ai_emulator import pefile,NativeAi,UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
from native_battle_briefing_emulator import BattleBriefingNative
ALIASES={'#537':'ai-char-ctor','#533':'brief-file-ctor','#798':'brief-file-dtor','#5194':'brief-file-open','#1997':'brief-file-close','#5465':'brief-file-read','?WriteTmpFile@BB_CompressDllMng@@QAE?AVCString@@AAV2@0@Z':'brief-extract','?DeleteTmpFile@BB_CompressDllMng@@QAEHAAVCString@@@Z':'brief-delete','#540':'brief-empty','#800':'brief-destroy','#535':'brief-copy','#858':'brief-copy','#2614':'brief-empty','#860':'brief-assign','#939':'brief-append-copy','#941':'brief-append','#924':'brief-concat','#2763':'brief-find','#5572':'ai-release','#2915':'ai-buffer','#4278':'ai-mid','#4129':'brief-left','#5710':'brief-right','#6282':'brief-trim-left','#6283':'brief-trim-right'}
class AiParserNative(NativeAi,BattleBriefingNative):
 def __init__(self):
  NativeAi.__init__(self,dict(width=1,height=1,units=[]));self.strings=self.DATA+0x300000
  raw=Path('/workspace/.tools/ds-ghidra/installed/saver/setunit.dll').read_bytes();self.setunit_sha=hashlib.sha256(raw).hexdigest();assert self.setunit_sha=="71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca";pe=pefile.PE(data=raw);base=0x30000000;pe.relocate_image(base);self.uc.mem_map(base,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(base,pe.get_memory_mapped_image());self.str_exports={e.name.decode():base+e.address for e in pe.DIRECTORY_ENTRY_EXPORT.symbols if e.name and b'GetStr'in e.name}
  for j,i in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
   stub=self.STUB+0x80000+j*16;self.write(i.address,stub);self.hooks[stub]=i.name.decode()if i.name else '#'+str(i.ordinal)
  for a,k in list(self.hooks.items()):
   if k in ALIASES:self.hooks[a]=ALIASES[k]
   elif k=='empty-string-create':self.hooks[a]='brief-empty'
   elif k=='empty-string-destroy':self.hooks[a]='brief-destroy'
 def _dispatch(self,uc,address,size,data):
  k=self.hooks.get(address)
  if k in self.str_exports:self.uc.reg_write(UC_X86_REG_EIP,self.str_exports[k])
  elif k=='#2818':
   args=self.args(6);pattern=self.text(args[1]);values=iter(args[2:]);value=re.sub(rb'%s',lambda _:self.text(next(values)),pattern);self.write(args[0],self.string(value));self.ret()
  elif k=='ai-char-ctor':
   this=uc.reg_read(UC_X86_REG_ECX);self.write(this,self.string(self.text(self.args(1)[0])));self.ret(this,4)
  elif k=='ai-release':
   this=uc.reg_read(UC_X86_REG_ECX);p=self.read(this);count=self.signed(self.args(1)[0]);count=len(self.text(p))if count<0 else count;self.uc.mem_write(p+count,b'\0');self.write(p-8,count);self.ret(0,4)
  elif k=='ai-buffer':
   this=uc.reg_read(UC_X86_REG_ECX);minimum=self.args(1)[0];raw=self.text(self.read(this));p=self.string(raw);self.uc.mem_write(p+len(raw),bytes(max(1,minimum-len(raw)+1)));self.write(this,p);self.ret(p,4)
  elif k=='ai-mid':
   this=uc.reg_read(UC_X86_REG_ECX);out,start,count=self.args(3);raw=self.text(self.read(this));start=max(0,self.signed(start));count=max(0,self.signed(count));self.write(out,self.string(raw[start:start+count]));self.ret(out,12)
  elif k and k.startswith('brief-'):BattleBriefingNative.hook(self,uc,address,size,data)
  elif k=='atoi':
   raw=self.text(self.args(1)[0]);m=re.match(rb'\s*([+-]?\d+)',raw);self.ret(int(m[1])if m else 0)
  else:
   try:NativeAi._dispatch(self,uc,address,size,data)
   except Exception as e:raise RuntimeError((k,hex(address),hex(self.read(uc.reg_read(UC_X86_REG_ESP))),str(e)))from e
 def call(self,entry,args):
  sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*args));self.uc.reg_write(UC_X86_REG_ESP,sp)
  self.uc.emu_start(entry,self.STOP+1,timeout=1000000,count=1000000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
  assert self.uc.reg_read(UC_X86_REG_ESP)==sp+4
 def load(self,text,key='04',seed=None,opened=True,extracted=True):
  self.strings=self.DATA+0x300000;self.lines=text.split('\n');self.lines=self.lines[:-1]if text.endswith('\n')else self.lines;self.lines=[]if text==''else self.lines;self.row=0;self.paths=[];self.extracted=extracted;self.opened=opened;self.write(0x100e0408,self.string(key.encode('ascii')));self.write(0x1010b3e0,self.DATA+0x10000);self.uc.mem_write(0x100d99a0,struct.pack('<6800i',*(seed or [0]*6800)));self.call(0x10002b40,[]);return list(struct.unpack('<6800i',self.uc.mem_read(0x100d99a0,27200)))
 def move(self,text):
  self.strings=self.DATA+0x300000;obj=self.DATA+0x20000;row=obj+0x100;self.uc.mem_write(row,bytes([0x5a])*272);self.write(obj,self.string(text.encode()))
  self.call(0x10002670,[obj,row]);return list(struct.unpack('<8i',self.uc.mem_read(row,32)))
 def actions(self,text):
  self.strings=self.DATA+0x300000;obj=self.DATA+0x20000;row=obj+0x100;self.uc.mem_write(row,bytes([0x5a])*272);self.write(obj,self.string(text.encode()))
  self.call(0x100027d0,[obj,row]);return list(struct.unpack('<68i',self.uc.mem_read(row,272)))
