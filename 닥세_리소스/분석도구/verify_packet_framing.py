"""Compare extracted x86 sender instructions with reference codec, without OS/network."""
import struct,json,hashlib,random
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_EAX
from analyze_special_skills import PE,ROOT,OUT,write
from analyze_network_packets import encode_frame,StreamDecoder

def native_encode(payload):
 p=PE(ROOT/'Saver.exe');u=Uc(UC_ARCH_X86,UC_MODE_32)
 u.mem_map(0,4096);u.mem_map(p.base,0x100000)
 for vs,rva,rs,off in p.sections:u.mem_write(p.base+rva,p.b[off:off+rs])
 u.mem_map(0x20000000,0x20000);u.mem_map(0x30000000,0x20000)
 u.mem_write(0x3000004c,struct.pack('<I',0x30001000))
 u.mem_write(0x30004000,payload or b'\0')
 u.mem_write(0x20010000,struct.pack('<III',0,len(payload),0x30004000))
 u.reg_write(UC_X86_REG_ESP,0x20010000);u.reg_write(UC_X86_REG_ECX,0x30000000)
 u.emu_start(0x439320,0x439418,count=300000)
 return bytes(u.mem_read(0x30001000,u.reg_read(UC_X86_REG_EAX)+1))

def main():
 rng=random.Random(20261023)
 samples=[bytes([i]) for i in range(256)]
 samples += [b'',bytes(range(256)),bytes(range(256))*8,b'\x09'*2048,b'G'*2048]
 samples += [bytes(rng.randrange(256) for _ in range(n)) for n in [9,71,255,256,511,1024,2047,2048]]
 rows=[]
 for i,payload in enumerate(samples):
  wire=native_encode(payload);assert wire==encode_frame(payload),(i,wire.hex())
  d=StreamDecoder();decoded=[]
  for byte in wire:decoded.extend(d.feed(bytes([byte])))
  assert decoded==[payload],i
  rows.append(dict(case=i,payload_length=len(payload),wire_length=len(wire),wire_sha256=hashlib.sha256(wire).hexdigest(),native_match=True,reference_bytewise_roundtrip=True))
 payloads=[b'\x2d'+struct.pack('<hhh',1,2,3),b'\x5c\x05'+struct.pack('<hh',7,100),bytes(range(256))]
 wire=b'noise'+b''.join(encode_frame(p) for p in payloads)
 assert StreamDecoder().feed(wire)==payloads
 bad=bytearray(encode_frame(b'abc'));bad[5]^=1
 assert StreamDecoder().feed(bad+encode_frame(b'good'))==[b'good']
 try:encode_frame(b'x'*2049)
 except ValueError:pass
 else:raise AssertionError('limit')
 write('패킷_원본송신_대조.csv',rows)
 result=dict(native_sender_cases=len(rows),native_mismatches=0,reference_bytewise_roundtrips=len(rows),reference_extra_checks=['noise+three coalesced frames','bad checksum followed by valid frame','2049-byte rejection'],original_sha256=hashlib.sha256((ROOT/'Saver.exe').read_bytes()).hexdigest(),emulation_range=['0x439320','0x439418 exclusive'],scope='isolated original x86 encoding only; reference decoder recovery is not original receiver verification; no socket/session/Windows execution')
 (OUT/'패킷_프레임검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
