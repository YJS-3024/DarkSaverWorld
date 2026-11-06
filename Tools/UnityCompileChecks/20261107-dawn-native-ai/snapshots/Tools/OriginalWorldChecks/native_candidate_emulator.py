"""Actual4161E0 candidate parser with MFC strings/array append and CRT scanf isolated."""
import re,struct
from native_chat_composer_emulator import ComposerNative
from native_ai_emulator import UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EIP
class CandidateNative(ComposerNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x4795a4:'construct-chars',0x416ab0:'candidate-append'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='construct-chars':self.write(this,self.string(self.text(self.args(1)[0])));self.ret(this,4)
  elif name=='candidate-append':
   assert this in self.rows,hex(this);self.rows[this].append(self.text(self.args(1)[0]));self.ret(len(self.rows[this])-1,4)
  elif name=='sscanf':
   pointer,format_,output=self.args(3);text=self.text(pointer);format_=self.text(format_)
   if format_ in [b'%s',b'%s\n']:
    found=re.match(rb'\s*(\S+)',text)
    if found:self.uc.mem_write(output,found[1]+b'\0')
   elif format_==b'%d':
    found=re.match(rb'\s*([+-]?\d+)',text)
    if found:self.write(output,int(found[1]))
   else:raise AssertionError(format_)
   self.ret(int(found is not None))
  else:super().hook(uc,a,size,data)
 def parse(self,payload):
  frame=0x0d050000;self.rows={frame+0x174:[],frame+0x188:[]};self.strings=0x0d0a0000;self.empty=self.string(b'');self.uc.mem_write(frame,bytes(0x200));self.uc.mem_write(0x0d040000,payload+b'\0');self.run(0x4161e0,[0x0d040000],frame)
  return dict(terms=[v.decode('cp949')for v in self.rows[frame+0x174]],windowClasses=[v.decode('cp949')for v in self.rows[frame+0x188]])

from native_memo_transfer_emulator import EnvelopeNative
class CandidateEnvelopeNative(CandidateNative,EnvelopeNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x47984a:'candidate-file-ctor',0x479844:'candidate-exception-ctor',0x47983e:'candidate-file-open',0x479838:'candidate-file-write',0x479832:'candidate-file-close',0x47982c:'candidate-file-read',0x479826:'candidate-file-dtor',0x4161e0:'candidate-parse-entry'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='candidate-parse-entry':self.parsed+=1 # Observe entry, execute the actual parser.
  elif name in ['candidate-file-ctor','candidate-file-dtor']:
   if name=='candidate-file-ctor':self.uc.mem_write(this,bytes(16))
   self.ret(this)
  elif name=='candidate-exception-ctor':self.ret(this)
  elif name=='candidate-file-open':
   path,mode,error=self.args(3);assert self.text(path)==b'Candid.Dat';self.file_opens.append(mode);self.offset=0;self.ret(1,12)
  elif name=='candidate-file-write':
   pointer,count=self.args(2);assert 0<=count<=8192;self.writes.append(bytes(uc.mem_read(pointer,count)));self.ret(0,8)
  elif name=='candidate-file-read':
   pointer,count=self.args(2);assert 0<=count<=8192;chunk=self.cache[self.offset:self.offset+count];uc.mem_write(pointer,chunk);self.offset+=len(chunk);self.ret(len(chunk),8)
  elif name=='candidate-file-close':self.ret()
  elif name=='outer-allocate':
   count=self.args(1)[0];assert 0<count<=65536;pointer=self.heap;self.heap=(pointer+count+15)&~15;assert self.heap<0x0d0a0000;uc.mem_write(pointer,bytes(count));self.ret(pointer)
  else:super().hook(uc,a,size,data)
 def run(self,address,args,ecx=0):
  sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.stop,*args));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.reg_write(UC_X86_REG_ECX,ecx);self.uc.emu_start(address,self.stop+1,timeout=10000000,count=30000000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop,'candidate native bound at '+hex(self.uc.reg_read(UC_X86_REG_EIP))
 def receive(self,packet,cache=b''):
  frame=0x0d050000;self.rows={frame+0x174:[],frame+0x188:[]};self.strings=0x0d0a0000;self.empty=self.string(b'');self.heap=0x0d060000;self.cache=cache;self.writes=[];self.file_opens=[];self.offset=0;self.parsed=0
  self.uc.mem_write(frame,bytes(0x200));self.uc.mem_write(0x4950f8,bytes(64));self.write(0x495108,0x0d020000);self.uc.mem_write(0x0d040000,packet+b'\0');self.run(0x415ba0,[0x0d040000],frame)
  return dict(accepted=self.parsed==1,terms=[v.decode('cp949')for v in self.rows[frame+0x174]],windowClasses=[v.decode('cp949')for v in self.rows[frame+0x188]],written=b''.join(self.writes).hex(),fileModes=self.file_opens)

class CandidateWindowNative(CandidateNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x416a70:'candidate-array-at'})
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a);this=uc.reg_read(UC_X86_REG_ECX)
  if name=='GetClassNameA':
   handle,output,limit=self.args(3);value=self.window_class[:limit-1];uc.mem_write(output,value+b'\0');self.ret(len(value),12)
  elif name=='candidate-array-at':
   output,index=self.args(2);self.write(output,self.string(self.window_classes[index]));self.ret(output,8)
  elif name=='PostMessageA':self.posts.append(list(self.args(4)));self.ret(1,16)
  else:super().hook(uc,a,size,data)
 def inspect(self,classes,name):
  self.window_classes=[v.encode('cp949')for v in classes];self.window_class=name.encode('cp949');self.posts=[];self.strings=0x0d0a0000;self.empty=self.string(b'');self.write(0x4952a0,0x0d050000);self.write(0x0d050190,len(classes));self.write(0x0d050020,0x123456);self.run(0x416430,[0x654321]);return dict(posts=self.posts)
