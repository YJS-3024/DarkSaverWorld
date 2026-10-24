"""Original x86 data-copy/dispatch probes. Synthetic units, no OS or game execution."""
import struct,json,hashlib,subprocess,csv
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ESI,UC_X86_REG_EDI,UC_X86_REG_EBP,UC_X86_REG_EDX,UC_X86_REG_EIP
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write

def main():
 checks=[];relations=[]
 # resetcharacter scalar and list-copy kernel, bypassing allocation and rendering.
 for n1,n2 in [(0,0),(1,3),(7,8),(8,1),(8,8)]:
  h=Harness('userunit.dll');base=0x30040000;idx=2;unit=base+idx*736;src=0x30001000
  words=[10000+i for i in range(44)];words[25:33]=[20+i for i in range(n1)]+[0xffffffff]*(8-n1);words[33:41]=[40+i for i in range(n2)]+[0xffffffff]*(8-n2)
  h.put(src,*words);h.put(0x1010a3a0,base);h.put(0x20010028,0)
  h.stubs={0x100117d4:lambda:h.ret(clean=4)}
  u=h.u;u.reg_write(UC_X86_REG_ESP,0x20010000);u.reg_write(UC_X86_REG_ESI,idx*736);u.reg_write(UC_X86_REG_EDI,src);u.reg_write(UC_X86_REG_EBP,idx);u.reg_write(UC_X86_REG_EDX,base)
  u.emu_start(0x1000b194,0x1000b38e,count=10000);assert u.reg_read(UC_X86_REG_EIP)==0x1000b38e
  assert h.get(unit+0xb8)==n1 and h.get(unit+0xdc)==n2
  for j in range(n1):assert h.get(unit+0xbc+j*4)==20+j
  for j in range(n2):assert h.get(unit+0xe0+j*4)==40+j
  for source,target,label in [(0x20,0x20,'HP display/current field'),(0x28,0x28,'MP display/current field'),(0x48,0x70,'tile x'),(0x4c,0x74,'tile y'),(0xac,0x30,'additional scalar; name unresolved')]:
   assert h.get(unit+target)==words[source//4]
   if (n1,n2)==(0,0):relations.append(dict(unitinfo_offset=hex(source),runtime_offset=hex(target),meaning=label,evidence='original resetcharacter copy kernel; HP/MP updater and path coordinate consumers'))
  checks.append(dict(case=f'unit_lists_{n1}_{n2}',result='scalar mapping, sentinel counts and list values matched'))
 # Additional scalar wire offset now included by 0xB0 capture in world tool.
 for n in [0,1,3]:
  h=Harness('GameCtl.dll');records=[]
  for k in range(n):records.append(struct.pack('<14HI',*[100+k*20+i for i in range(14)],100000+k))
  body=struct.pack('<HHHIH',1,2,3,123456,n)+b''.join(records);h.u.mem_write(0x30001000,b'\x5c\x54'+body)
  h.run(0x1000ea20,(0x30001000,));assert h.get(0x1006a890)==1
  assert bytes(h.u.mem_read(0x1006a740,6))==struct.pack('<3H',1,2,3)
  assert h.get(0x1006a748)==123456 and bytes(h.u.mem_read(0x1006a74c,2))==struct.pack('<H',n)
  for k,r in enumerate(records):assert bytes(h.u.mem_read(0x1006a750+32*k,32))==r
  checks.append(dict(case=f'dispatch_5c54_{n}',result=f'14+32N-byte payload; phase fields and record stride matched'))
 for value in [0,1,2,255]:
  h=Harness('GameCtl.dll');h.u.mem_write(0x30001000,bytes([0x5c,0x5c,value]));h.run(0x1000ea20,(0x30001000,));assert h.get(0x1006c084)==value
  checks.append(dict(case=f'dispatch_5c5c_{value}',result='byte at payload offset2 stored in scenario control field'))
 # Client map initialization suffix sends 5A even when earlier load callback failed.
 for loadflag in [0,1]:
  h=Harness('GameCtl.dll');h.put(0x1001c46c,loadflag);h.stubs={0x10003d00:lambda:h.ret(),0x10013c20:lambda:h.ret(),0x1000e1e0:h.send}
  for j,iat in enumerate([0x1001628c,0x10016290,0x100162b8,0x10016294]):
   dest=0x300f0000+j*16;h.put(iat,dest);h.stubs[dest]=lambda:h.ret()
  h.put(0x20010000,0x300ff000);h.u.reg_write(UC_X86_REG_ESP,0x20010000)
  # Actual suffix expects saved ebx/esi/edi at its end; stop after the send call.
  h.u.emu_start(0x100042ba,0x100042ff,count=10000);assert h.sent==[b'\x5a']
  checks.append(dict(case=f'map_init_suffix_loadflag_{loadflag}',result='5A emitted by common suffix; load flag alone is not success proof'))
 for value in [0,1,2,255]:
  h=Harness('GameCtl.dll');h.put(0x1006a708,0x30004000);h.u.mem_write(0x30001000,bytes([0x5c,0x61,value]));calls=[]
  def finish():calls.append('finish');h.ret()
  h.stubs={0x1000cfe0:finish,0x100029e0:lambda:h.ret()}
  for j,iat in enumerate([0x100161d8,0x10016294]):
   dest=0x300f0000+j*16;h.put(iat,dest);h.stubs[dest]=lambda:h.ret(clean=16 if h.u.reg_read(UC_X86_REG_EIP)==0x300f0000 else 0)
  h.run(0x1000ea20,(0x30001000,));assert calls==(['finish'] if value==1 else [])
  checks.append(dict(case=f'dispatch_5c61_{value}',result='phase0 fixture; completion handler invoked only for value1'))
 for scenarioflag in [0,1]:
  h=Harness('GameCtl.dll');h.put(0x1006c118,scenarioflag);h.put(0x1001a8ec,0x400);h.stubs={0x1000e1e0:h.send}
  for j,iat in enumerate([0x100162f0,0x100161d0,0x10016294]):
   dest=0x300f0000+j*16;h.put(iat,dest);h.stubs[dest]=lambda:h.ret(clean=16 if h.u.reg_read(UC_X86_REG_EIP)==0x300f0010 else 0)
  h.run(0x1000cfe0);assert h.sent==([b'\x5b'] if scenarioflag==0 else [])
  assert h.get(0x1001a8ec)==(0 if scenarioflag==0 else 0x400)
  checks.append(dict(case=f'completion_scenarioflag_{scenarioflag}',result='phase0 fixture; non-scenario sends5B and resets input state; scenario branch does not'))
 h=Harness('GameCtl.dll');calls=[]
 def initvalue():calls.append('InitValue');h.ret()
 h.put(0x100161f0,0x300f0000);h.stubs={0x300f0000:initvalue};h.u.mem_write(0x30001000,b'\x5a');h.run(0x1000ea20,(0x30001000,));assert calls==['InitValue']
 checks.append(dict(case='dispatch_5a',result='server5A forwards to userunit InitValue'))
 write('패킷_상태필드_소비경로.csv',relations);write('패킷_초기화_원본명령대조.csv',checks)
 mapping=list(csv.DictReader((OUT/'패킷_캐릭터_unitinfo_대응.csv').open(encoding='utf-8-sig')))
 semantic=[]
 for r in mapping:
  for rel in relations:
   if r['unitinfo_offset']==rel['unitinfo_offset']:semantic.append(dict(subtype=r['subtype'],payload_offset=r['payload_offset'],field=r['wire_field'],meaning=rel['meaning'],unitinfo_offset=rel['unitinfo_offset'],runtime_offset=rel['runtime_offset']))
 write('패킷_캐릭터_의미확인.csv',semantic)
 ranges=[('userunit.dll',0x1000b194,0x1000b38e),('userunit.dll',0x1000cd80,0x1000cdb7),('userunit.dll',0x1000ce60,0x1000ce98),('GameCtl.dll',0x1000d1a0,0x1000d317),('GameCtl.dll',0x1000dec0,0x1000dece),('GameCtl.dll',0x100042b4,0x10004305),('GameCtl.dll',0x1000cfe0,0x1000d0c1)]
 evidence=[];asm=[]
 for name,a,b in ranges:
  p=ROOT/name;evidence.append(dict(file=name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_초기화_근거.csv',evidence);(OUT/'패킷_초기화_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(checks),mismatches=0,scope='isolated original copy/dispatch/suffix with external effects stubbed; five list combinations; no live session or full ordering proof')
 (OUT/'패킷_초기화검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
