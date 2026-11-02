"""Read-only scene1 mouse/toggle callbacks and430FA0 post-send command handling.

Standard MFC CString value operations are isolated; source branch logic, native packet
builders, mutual exclusion and430FA0/431650 call order execute unmodified x86.
"""
import struct
from check_native_commanders import Native
from native_ai_emulator import UC_X86_REG_ECX
class ComposerNative(Native):
 def __init__(self):
  super().__init__();self.world=0x0d004800;self.strings=0x0d0a0000;self.empty=self.string(b'');self.hooks.update({0x479598:'string-empty',0x4795e6:'string-copy',0x4795da:'string-copy',0x479586:'string-destroy',0x479aa8:'string-lower',0x479a4e:'find-string',0x479ac6:'string-mid',0x479ac0:'trim-left',0x479a54:'find-char',0x479a18:'string-left',0x475f50:'find-input',0x479592:'module',0x479a1e:'repeat-char',0x479850:'string-mid-count',0x479856:'find-char-from',0x479a84:'string-right',0x4795b6:'assign-chars',0x4797ea:'append-chars',0x479aa2:'replace-chars',0x4799c4:'concat-strings'})
  self.write(self.world+0x150,0x0d005000);self.write(self.window+0x54,0x0d006800)
  self.write(self.window,0x0d005800);self.write(0x0d00580c,0x470790)
  for i in range(2):self.write(0x0d006800+i*4,0x0d007000+i*64)
  self.write(self.window+0xa8,self.empty)
 def string(self,b):
  p=self.strings+12;self.uc.mem_write(self.strings,struct.pack('<iii',1,len(b),len(b))+b+b'\0');self.strings=(p+len(b)+16)&~15;assert self.strings<0x0d0fe000;return p
 def text(self,p):
  out=bytearray()
  for i in range(65536):
   b=bytes(self.uc.mem_read(p+i,1))
   if b==b'\0':return bytes(out)
   out+=b
  raise AssertionError('unterminated CString')
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='string-empty':self.write(this,self.empty);self.ret(this)
  elif name=='module':self.ret(0x0d005000)
  elif name=='string-copy':self.write(this,self.read(self.args(1)[0]));self.ret(this,4)
  elif name=='string-destroy':self.ret()
  elif name=='string-lower':self.write(this,self.string(self.text(self.read(this)).lower()));self.ret()
  elif name=='find-string':self.ret(self.text(self.read(this)).find(self.text(self.args(1)[0])),4)
  elif name=='find-char':self.ret(self.text(self.read(this)).find(bytes([self.args(1)[0]&255])),4)
  elif name in ['string-mid','string-left']:
   out,count=self.args(2);b=self.text(self.read(this));self.write(out,self.string(b[count:]if name=='string-mid'else b[:count]));self.ret(out,8)
  elif name=='trim-left':self.write(this,self.string(self.text(self.read(this)).lstrip(b' \t\r\n\v\f')));self.ret()
  elif name=='repeat-char':
   value,count=self.args(2);self.write(this,self.string(bytes([value&255])*count));self.ret(this,8)
  elif name=='string-mid-count':
   out,start,count=self.args(3);self.write(out,self.string(self.text(self.read(this))[start:start+count]));self.ret(out,12)
  elif name=='find-char-from':
   value,start=self.args(2);self.ret(self.text(self.read(this)).find(bytes([value&255]),start),8)
  elif name=='string-right':
   out,count=self.args(2);b=self.text(self.read(this));self.write(out,self.string(b[-count:]if count else b''));self.ret(out,8)
  elif name in ['assign-chars','append-chars']:
   value=self.text(self.args(1)[0]);self.write(this,self.string((self.text(self.read(this))if name=='append-chars'else b'')+value));self.ret(this,4)
  elif name=='replace-chars':
   old,new=self.args(2);old,new=self.text(old),self.text(new);value=self.text(self.read(this));count=value.count(old)if old else 0
   if old:self.write(this,self.string(value.replace(old,new)))
   self.ret(count,8)
  elif name=='concat-strings':
   out,a,b=self.args(3);self.write(out,self.string(self.text(self.read(a))+self.text(self.read(b))));self.ret(out,12)
  elif name=='find-input':assert self.args(1)[0]==17;self.ret(self.window,4)
  elif name=='send':
   n,p=self.args(2);assert 3<=n<=258;self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret(0,8)
  else:super().hook(uc,a,size,data)
 def mode(self,channel):
  for i in range(2):self.write(0x0d007000+i*64+12,2 if channel==i+2 else 1)
  self.run(0x470b00,[],self.window)
 def click(self,control):
  self.write(self.window+0x78,1);self.write(self.window+0x1c,0);self.write(self.window+0x58,2);self.write(self.window+0x40,33);self.write(self.window+0x44,435)
  for i in range(2):
   self.uc.mem_write(0x0d007000+i*64+32,struct.pack('<4i',498+i*31,6,528+i*31,21));self.write(0x0d007000+i*64+60,1)
  self.uc.mem_write(0x0d006000,struct.pack('<2i',33+503+control*31,446));self.run(0x470800,[0,0x0d006000],self.window)
  return dict(channel=self.read(self.window+0xb0),checked=[self.read(0x0d007000+i*64+12)==2 for i in range(2)])
 def submit(self,text,channel,whisper):
  self.strings=0x0d0a0000;self.empty=self.string(b'');self.write(self.window+0xa8,self.empty);self.sent=[];self.mode(channel);self.write(0x490ae4,int(whisper));self.write(0x0d006000,self.string(text.encode('cp949')))
  self.run(0x430fa0,[0x0d006000,channel],self.world)
  recipient=self.text(self.read(self.window+0xa8)).decode('cp949');return dict(packets=self.sent,recipient=recipient or None,whisper=bool(self.read(0x490ae4)),channel=self.read(self.window+0xb0))
 def filter(self,text,terms,mode=1):
  self.strings=0x0d0a0000;self.empty=self.string(b'');frame=0x0d050000;self.write(0x0d005004,0x0d005800);self.write(0x0d005820,frame);self.write(frame+0x178,0x0d009000);self.write(frame+0x17c,len(terms))
  for i,term in enumerate(terms):self.write(0x0d009000+i*4,self.string(term.encode('cp949')))
  self.write(0x0d008050,self.string(text.encode('cp949')));self.run(0x425680,[mode],0x0d008000)
  return self.text(self.read(0x0d008050)).decode('cp949')
