"""Actual native DCL explode and its actual input/output/CRC callbacks; no network."""
import struct,sys,zlib
from pathlib import Path
from check_native_commanders import Native
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX,UC_X86_REG_ECX
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'OriginalClientChecks'));from native_pe import PE
SHA='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
def tables():
 pe=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');assert pe.sha256==SHA
 return dict(literal=list(pe.read(0x491cb8,256)),length=list(pe.read(0x491c98,16)),distance=list(pe.read(0x491be8,64)),extra=list(pe.read(0x491c68,16)),base=list(struct.unpack('<16H',pe.read(0x491c78,32))))
def codes(lengths):
 count=[0]*17
 for n in lengths:count[n]+=1
 nxt=[0]*17;code=0
 for n in range(1,17):code=(code+count[n-1])*2;nxt[n]=code
 result=[]
 for n in lengths:
  c=nxt[n];nxt[n]+=1;wire=0
  for bit in range(n):wire|=((~c>>(n-1-bit))&1)<<bit
  result.append((wire,n))
 return result
class DclWriter:
 def __init__(self,coded=0,dictionary=4):
  self.bits=[];self.coded=coded;self.dictionary=dictionary;self.t=tables();self.lit=codes(self.t['literal']);self.length=codes(self.t['length']);self.dist=codes(self.t['distance']);self.put(coded,8);self.put(dictionary,8);self.output=bytearray()
 def put(self,value,n):self.bits.extend((value>>i)&1 for i in range(n))
 def literal(self,value):
  for b in value:
   self.put(0,1)
   if self.coded:self.put(*self.lit[b])
   else:self.put(b,8)
   self.output.append(b)
 def match(self,count,distance):
  index=next(i for i,(b,e)in enumerate(zip(self.t['base'],self.t['extra']))if b+2<=count<=b+(1<<e)+1);self.put(1,1);self.put(*self.length[index]);self.put(count-2-self.t['base'][index],self.t['extra'][index]);low=2 if count==2 else self.dictionary;self.put(*self.dist[(distance-1)>>low]);self.put((distance-1)&((1<<low)-1),low)
  for _ in range(count):self.output.append(self.output[-distance])
 def finish(self):
  self.put(1,1);self.put(*self.length[15]);self.put(255,8);out=bytearray((len(self.bits)+7)//8)
  for i,b in enumerate(self.bits):out[i//8]|=b<<(i%8)
  return bytes(out),bytes(self.output)
def envelope(compressed,plain):return struct.pack('<hhI',len(compressed),len(plain),(~zlib.crc32(plain))&0xffffffff)+compressed
class TransferNative(Native):
 def run(self,address,args,ecx=0):
  sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.stop,*args));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.reg_write(UC_X86_REG_ECX,ecx);self.uc.emu_start(address,self.stop+1,timeout=3000000,count=30000000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop
 def decode(self,compressed,maximum):
  context=0x0d005000;work=0x0d020000;source=0x0d040000;target=0x0d050000
  self.uc.mem_write(context,bytes(128));self.uc.mem_write(work,bytes(0x8dd8));self.uc.mem_write(source,compressed+b'\0');self.uc.mem_write(target,bytes(maximum+1))
  for offset,value in [(0x1c,source),(0x24,len(compressed)+1),(0x20,target),(0x28,maximum+1),(0x34,2),(0x14,0xffffffff)]:self.write(context+offset,value)
  self.run(0x478930,[0x401226,0x401717,work,context]);count=self.read(context+0x30)
  return dict(result=self.uc.reg_read(UC_X86_REG_EAX)&255,count=count,crc=self.read(context+0x14),overflow=self.read(context+0x38),payload=bytes(self.uc.mem_read(target,min(count,maximum+1))).hex())
class EnvelopeNative(TransferNative):
 def __init__(self):
  super().__init__();self.hooks.update({0x47958c:'outer-allocate',0x479580:'outer-free',0x43c690:'outer-record',0x4797c6:'outer-append',0x43c420:'outer-count'})
 def hook(self,uc,at,size,data):
  name=self.hooks.get(at)
  if name=='outer-allocate':
   n=self.args(1)[0];assert 0<n<65536;p=self.heap;self.heap=(p+n+15)&~15;assert self.heap<0x0d0f8000;uc.mem_write(p,bytes(n));self.ret(p)
  elif name=='outer-free':self.ret()
  elif name=='outer-record':self.records+=1;self.ret(0x0d009000)
  elif name=='outer-append':self.ret(0,8)
  elif name=='outer-count':self.accepted=True # Execute the real BYTE reader after observing entry.
  else:super().hook(uc,at,size,data)
 def body(self,body):
  self.heap=0x0d060000;self.records=0;self.accepted=False;self.uc.mem_write(0x4950f8,bytes(64));self.write(0x495108,0x0d020000);self.uc.mem_write(0x0d040000,bytes(body));self.uc.mem_write(0x0d005000,bytes(64))
  self.run(0x427d30,[0x0d040000],0x0d005000);return dict(accepted=self.accepted,records=self.records)
