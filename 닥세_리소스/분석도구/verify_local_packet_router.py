"""Original internal simulation routing; exhaust one-byte command selectors."""
import struct,json
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 rows=[]
 for sub in range(256):
  h=Harness('Saver.exe');buf=0x30001000;obj=0x30000000;h.u.mem_write(buf,b'\x2c'+bytes([sub])+b'\0'*18);seen=[]
  def option(label):
   def run():
    sp=h.u.reg_read(UC_X86_REG_ESP);seen.append((label,h.get(sp+4)));h.ret(clean=4)
   return run
  def inject():
   sp=h.u.reg_read(UC_X86_REG_ESP);n,p=h.get(sp+4),h.get(sp+8);seen.append(('inject',bytes(h.u.mem_read(p,n)).hex()));h.ret(clean=8)
  h.stubs={0x402a60:option('unit_state'),0x402ce0:option('synthetic_91'),0x402a40:inject,0x440728:h.alloc,0x44072e:lambda:h.ret()}
  h.run(0x402900,(20,buf),obj)
  expected=[('unit_state',1),('unit_state',0)] if sub==0x20 else [('inject',(b'\x2c\x41'+struct.pack('<hhhh',1,1,19,7)).hex())] if sub==0x40 else [('inject',(b'\x2c'+bytes([sub])+b'\0'*18).hex())] if sub in [0x22,0x25] else []
  assert seen==expected,(sub,seen,expected)
  rows.append(dict(case='2C_'+hex(sub),events=str(seen),match=True))
 for opcode in [0,0x19,0x52,0x54,0x90,0xff]:
  h=Harness('Saver.exe');h.u.mem_write(0x30001000,bytes([opcode])+b'\0'*19);seen=[]
  def synth():
   sp=h.u.reg_read(UC_X86_REG_ESP);seen.append(h.get(sp+4));h.ret(clean=4)
  h.stubs[0x402ce0]=synth;h.run(0x402900,(20,0x30001000),0x30000000)
  assert seen==([1,0] if opcode==0x90 else [])
  rows.append(dict(case='single_'+hex(opcode),events=str(seen),match=True))
 for mode in [0,1]:
  for n in [0,1]:
   h=Harness('Saver.exe');h.put(0x30000104,mode);seen=[]
   def target(label):
    def run():seen.append(label);h.ret(clean=8)
    return run
   h.stubs={0x439320:target('frame'),0x402900:target('local')};h.run(0x43b840,(n,0x30001000),0x30000000)
   assert seen==(['local'] if mode else ['frame'] if n else [])
   rows.append(dict(case=f'route_{mode}_{n}',events=str(seen),match=True))
 for entry,op in [(0x43b820,0x30),(0x43b880,0)]:
  h=Harness('Saver.exe');packets=[]
  def frame():
   sp=h.u.reg_read(UC_X86_REG_ESP);n,p=h.get(sp+4),h.get(sp+8);packets.append(bytes(h.u.mem_read(p,n)));h.ret(clean=8)
  h.stubs[0x439320]=frame;h.run(entry,(),0x30000000);assert packets==[bytes([op])]
  rows.append(dict(case=hex(entry),events=str([p.hex() for p in packets]),match=True))
 write('패킷_내부라우터_원본명령대조.csv',rows)
 (OUT/'패킷_내부라우터검증.json').write_text(json.dumps(dict(cases=len(rows),mismatches=0,scope='Original routing/serialization with synthetic receive and socket boundaries modeled; not a general local server'),indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),mismatches=0)))
if __name__=='__main__':main()
