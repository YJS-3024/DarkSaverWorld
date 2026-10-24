"""Original x86 receive parser with synthetic rings; original sender frames, no socket."""
import json,struct,random
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_EIP,UC_X86_REG_EAX
from analyze_packet_requests import Harness
from analyze_network_packets import encode_frame
from analyze_special_skills import OUT,write
class Receiver:
 def __init__(self,start=0):
  self.h=h=Harness('Saver.exe');self.capacity=16384
  h.put(0x30000044,0x30001000);h.put(0x30000048,0x30002000)
  h.put(0x30001000,0,self.capacity,0x30004000,start,start);h.put(0x30002000,0,self.capacity,0x30008000,0,0)
  # malloc temporary copied input; free is no-op, same scratch reused after return.
  h.stubs={0x440728:lambda:h.ret(0x30020000),0x44072e:lambda:h.ret()}
 def feed(self,data):
  h=self.h;read=h.get(0x3000100c);w=h.get(0x30001010);assert len(data)<self.capacity-(w-read)%self.capacity
  for b in data:h.u.mem_write(0x30004000+w,bytes([b]));w=(w+1)%self.capacity
  h.put(0x30001010,w);out=[]
  for _ in range(32):
   h.put(0x20010000,0x300ff000);h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.reg_write(UC_X86_REG_ECX,0x30000000)
   h.u.emu_start(0x439a60,0x300ff000,count=1000000);assert h.u.reg_read(UC_X86_REG_EIP)==0x300ff000
   flag=h.u.reg_read(UC_X86_REG_EAX)
   if not flag:break
   assert flag==1
   a=h.get(0x3000200c);b=h.get(0x30002010);out.append(bytes(h.u.mem_read(0x30008000+a,(b-a)%self.capacity)))
  else:raise AssertionError('no progress')
  return out

def main():
 rng=random.Random(20261023);rows=[]
 def check(label,payloads,wire,chunks,start=0):
  r=Receiver(start);out=[];offset=0
  for n in chunks:out+=r.feed(wire[offset:offset+n]);offset+=n
  if offset<len(wire):out+=r.feed(wire[offset:])
  assert out==payloads,(label,[p.hex() for p in out])
  rows.append(dict(case=label,wire_bytes=len(wire),payloads=len(payloads),raw_ring_start=start,match=True))
 samples=[bytes([i]) for i in range(256)]+[b'',bytes(range(256)),b'G'*2048,b'\x09'*2048,b'\x55\xaa\xaa\x55\x01']
 for i,p in enumerate(samples):
  wire=encode_frame(p);check('roundtrip_'+str(i),[p],wire,[len(wire)])
 for p in [b'abc',b'G\x09G',bytes(range(256)),b'G'*71]:
  wire=encode_frame(p)
  for cut in range(1,len(wire)):check(f'split_{len(p)}_{cut}',[p],wire,[cut])
 wire=b''.join(encode_frame(p) for p in [b'one',b'G\x09',b'three']);check('coalesced',[b'one',b'G\x09',b'three'],wire,[len(wire)])
 check('bytewise',[b'one',b'G\x09',b'three'],wire,[1]*len(wire))
 check('ring_wrap',[bytes(range(256))],encode_frame(bytes(range(256))),[20],16380)
 # Corruption recovery is observed separately and not required to equal reference decoder.
 observations=[]
 for label,bad in [('noise',b'noise'),('bad_checksum',bytes.fromhex('aa550103006162630055aa'))]:
  r=Receiver();out=r.feed(bad+encode_frame(b'good'));observations.append(dict(case=label,decoded=[p.hex() for p in out],after_empty_poll=[p.hex() for p in r.feed(b'')],after_new_frame=[p.hex() for p in r.feed(encode_frame(b'next'))],scope='observed original parser behavior'))
 write('패킷_원본수신_대조.csv',rows)
 result=dict(cases=len(rows),mismatches=0,return_semantics='EAX1=frame success,0=no complete frame; decoded length is output ring count',observations=observations,scope='original receive instructions and synthetic ring/malloc model; fixed capacity16384; original socket and ring-growth path not executed')
 (OUT/'패킷_원본수신검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
