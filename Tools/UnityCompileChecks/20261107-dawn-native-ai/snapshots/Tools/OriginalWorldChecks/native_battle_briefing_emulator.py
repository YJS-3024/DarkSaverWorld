"""Actual10003DA0/1000F200 SRF comparisons; archive/file/MFC allocation are boundaries."""
import re,struct
from native_battle_options_emulator import BattleOptionsNative
from native_ai_emulator import UC_X86_REG_ECX
class BattleBriefingNative(BattleOptionsNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x10014792:'brief-empty',0x10014762:'brief-copy',0x10014768:'brief-copy',0x1001476e:'brief-destroy',0x100147fe:'brief-empty',0x100147e0:'brief-assign',0x100147f2:'brief-append-copy',0x100147f8:'brief-append',0x100147da:'brief-concat',0x1001482e:'brief-find',0x10014828:'brief-left',0x1001484c:'brief-right',0x10014834:'brief-trim-left',0x10014852:'brief-trim-right',0x100146d2:'brief-extract',0x100146cc:'brief-delete',0x100147b6:'brief-file-ctor',0x100147aa:'brief-file-dtor',0x100147b0:'brief-file-open',0x1001479e:'brief-file-close',0x100147a4:'brief-file-read'})
 def string(self,value):
  p=self.strings+12;self.uc.mem_write(self.strings,struct.pack('<iii',1,len(value),len(value))+value+b'\0');self.strings=(p+len(value)+16)&~15;assert self.strings<0x0d0fe000;return p
 def text(self,p):
  out=bytearray()
  for i in range(65536):
   b=bytes(self.uc.mem_read(p+i,1))
   if b==b'\0':return bytes(out)
   out+=b
  raise AssertionError('unterminated native CString')
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='brief-empty':self.write(this,self.string(b''));self.ret(this)
  elif name=='brief-copy':self.write(this,self.read(self.args(1)[0]));self.ret(this,4)
  elif name=='brief-assign':self.write(this,self.string(self.text(self.args(1)[0])));self.ret(this,4)
  elif name in ['brief-append-copy','brief-append']:
   p=self.args(1)[0];self.write(this,self.string(self.text(self.read(this))+self.text(self.read(p)if name=='brief-append-copy'else p)));self.ret(this,4)
  elif name in ['brief-destroy','brief-file-ctor','brief-file-dtor','brief-file-close']:self.ret()
  elif name=='brief-find':self.ret(self.text(self.read(this)).find(bytes([self.args(1)[0]&255])),4)
  elif name in ['brief-left','brief-right']:
   out,count=self.args(2);value=self.text(self.read(this));self.write(out,self.string(value[:count]if name=='brief-left'else value[-count:]if count else b''));self.ret(out,8)
  elif name in ['brief-trim-left','brief-trim-right']:
   value=self.text(self.read(this));self.write(this,self.string(value.lstrip(b' \t\r\n\v\f')if name=='brief-trim-left'else value.rstrip(b' \t\r\n\v\f')));self.ret()
  elif name=='brief-concat':
   out,left,right=self.args(3);self.write(out,self.string(self.text(self.read(left))+self.text(right)));self.ret(out,12)
  elif name=='brief-extract':
   out,archive,file=self.args(3);self.paths=[self.text(self.read(archive)).decode('cp949'),self.text(self.read(file)).decode('cp949')];self.write(out,self.string(b'tmp.srf'if self.extracted else b''));self.ret(out,12)
  elif name=='brief-delete':self.ret(0,4)
  elif name=='brief-file-open':self.ret(int(self.opened),12)
  elif name=='brief-file-read':
   out=self.args(1)[0]
   if self.row>=len(self.lines):self.ret(0,4)
   else:self.write(out,self.string(self.lines[self.row].encode('cp949')));self.row+=1;self.ret(1,4)
  else:super().hook(uc,at,size,data)
 def briefing(self,source,team=0,opened=True,extracted=True,archive=True):
  self.strings=0x0d080000;self.lines=(source or'').splitlines();self.row=0;self.paths=[];self.extracted=extracted;self.opened=opened
  self.write(0x1006a738,team);self.write(0x1001a8b0,0x0d060000 if archive else 0);self.write(0x1001a8b4,self.string(b'root\\'));self.write(0x1006c0e8,self.string(b'01'))
  self.run(0x10003da0,[]);return [self.text(self.read(p)).decode('cp949')for p in [0x1001bdd8,0x1001bddc]]
