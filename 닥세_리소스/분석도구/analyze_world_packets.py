"""Offline character-record, movement and map initialization evidence and x86 probes."""
import struct,json,hashlib,subprocess
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_ESI
from analyze_packet_requests import Harness,game_case
from analyze_special_skills import ROOT,OUT,write,PE

def decode_record(sub,body):
 h=Harness('GameCtl.dll');trace=[];record=[];strings=[]
 h.u.mem_write(0x30001000,body)
 def reader(u,a,n,d):
  kinds={0x1000c470:'uint16_le',0x1000c490:'uint8',0x1000c4f0:'uint32_le',0x1000c4b0:'bytes'}
  if a in kinds:
   sp=u.reg_read(UC_X86_REG_ESP);pos=h.get(h.get(sp+8));trace.append((pos,kinds[a]))
 def ctor():h.put(h.u.reg_read(UC_X86_REG_ECX),0);h.ret()
 def assign():
  sp=h.u.reg_read(UC_X86_REG_ESP);p=h.get(sp+4);raw=bytes(h.u.mem_read(p,256)).split(b'\0')[0];strings.append(raw.hex());h.put(h.u.reg_read(UC_X86_REG_ECX),p);h.ret(clean=4)
 def capture():
  sp=h.u.reg_read(UC_X86_REG_ESP);record.append(bytes(h.u.mem_read(h.get(sp+4),0xb0)));h.ret()
 h.stubs={0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x10014652:ctor,0x100146b2:lambda:h.ret(),0x100146a0:assign,0x1001462e:lambda:h.ret(),0x1000c5b0:lambda:h.ret(),0x300f0000:capture}
 h.put(0x1001621c,0x300f0000);h.u.hook_add(UC_HOOK_CODE,reader)
 h.run(0x1000c700 if sub==0x50 else 0x1000cac0,(0x30001000,))
 assert len(record)==1
 return trace,record[0],strings

def main():
 fields=[];mapping=[];checks=[]
 for sub,nshort,nint in [(0x50,16,2),(0x51,45,4)]:
  for lengths in [(0,0),(3,5),(255,255)]:
   s1=b'A'*lengths[0];s2=b'B'*lengths[1];words=list(range(1000,1000+nshort));ints=list(range(100000,100000+nint))
   body=struct.pack('<'+'H'*nshort,*words)+struct.pack('<'+'I'*nint,*ints)+bytes([len(s1)])+s1
   if sub==0x51:body+=bytes([len(s2)])+s2
   trace,record,strings=decode_record(sub,body)
   expected=[s1.hex()] if sub==0x50 else [s2.hex(),s1.hex()]
   assert strings==expected,(hex(sub),strings)
   checks.append(dict(case=f'char_{sub:x}_{lengths[0]}_{lengths[1]}',payload_length=len(body)+2,read_operations=len(trace),result='original reader executed; string assignments captured'))
   if lengths==(3,5):
    for pos,kind in trace:fields.append(dict(direction='S2C',opcode='0x5c',subtype=hex(sub),payload_offset=pos+2,type=kind,scope='sample string lengths3/5; offsets after variable fields depend on lengths'))
    vals=struct.unpack('<44I',record)
    for i,v in enumerate(vals):
     if v in words:source='short'+str(words.index(v));offset=2+2*words.index(v)
     elif v in ints:source='dword'+str(ints.index(v));offset=2+2*nshort+4*ints.index(v)
     else:continue
     mapping.append(dict(subtype=hex(sub),payload_offset=offset,wire_field=source,unitinfo_offset=hex(i*4),sample_value=v,scope='original handler mapping; names not inferred'))
 # Signed expansion probe for the 16-bit field copied to unitinfo offset0.
 for sub,nshort,nint in [(0x50,16,2),(0x51,45,4)]:
  body=struct.pack('<H',65535)+bytes((nshort-1)*2+nint*4)+b'\0'+(b'\0' if sub==0x51 else b'')
  _,record,_=decode_record(sub,body);assert struct.unpack_from('<I',record)[0]==0xffffffff
  checks.append(dict(case=f'char_{sub:x}_signed',payload_length=len(body)+2,read_operations=0,result='short0 sign extended to -1'))
 # Path sender: one unit short and five pairs of x/y shorts.
 for mode in [0,0x200,0x400]:
  for values in [list(range(11)),[65535,32768,32767,0,9,71,255,256,1000,2000,3000]]:
   data=struct.pack('<11H',*values);h=game_case(0x1000e200,mode,data)
   assert h.sent==([b'\x5c\x43'+data] if mode==0x400 else [])
   checks.append(dict(case=f'move_{mode:x}_{values[0]}',payload_length=24 if h.sent else 0,read_operations=0,result='exact original serializer bytes and state gate matched'))
 for i,name in enumerate(['unit_index']+[f'path_{i//2}_{"x" if i%2==0 else "y"}' for i in range(10)]):
  fields.append(dict(direction='C2S',opcode='0x5c',subtype='0x43',payload_offset=2+2*i,type='uint16_le',scope=name+'; source is MoveCharPath output'))
 # Isolated path suffix pads unfilled coordinate pairs with -1/-1.
 for used in range(1,5):
  h=Harness('userunit.dll');h.put(0x20010044,0x30001000);h.u.mem_write(0x30001000,b'\x34\x12'*11)
  h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.reg_write(UC_X86_REG_ESI,used)
  h.u.emu_start(0x1000e23b,0x1000e29a,count=1000)
  assert bytes(h.u.mem_read(0x30001002+used*4,(5-used)*4))==b'\xff'*((5-used)*4)
  checks.append(dict(case='path_padding_'+str(used),payload_length=0,read_operations=0,result='original suffix writes -1/-1 in unused coordinate slots'))
 # Original map response decoder: success body carries six shorts, name, bounds, mode.
 for name in [b'M001',b'X'*255]:
  for bounds in [(1,2,100,200),(65535,2,100,200)]:
   for flag in [2,3,65533]:
    h=Harness('Saver.exe');body=b'\x01'+struct.pack('<6H',11,22,33,44,55,66)+bytes([len(name)])+name+struct.pack('<5H',*bounds,flag)
    h.u.mem_write(0x30001000,body);h.run(0x43ec40,(0x30003000,0x30001000))
    out=bytes(h.u.mem_read(0x30003000,544))
    assert out[0]==1 and out[15:15+len(name)]==name and out[15+len(name)]==0
    assert struct.unpack_from('<6H',out,2)==(11,22,33,44,55,66)
    assert struct.unpack_from('<I',out,528)[0]==int(65535 not in bounds)
    assert struct.unpack_from('<4H',out,532)==bounds
    signedflag=flag if flag<32768 else flag-65536
    expected=signedflag%2 if signedflag>=0 else -((-signedflag)%2)
    assert struct.unpack_from('<h',out,540)[0]==expected
    checks.append(dict(case=f'map55_{len(name)}_{bounds[0]}_{flag}',payload_length=1+len(body),read_operations=0,result='original decoder name/bounds/flag matched'))
 for status in [0,2,255]:
  h=Harness('Saver.exe');h.u.mem_write(0x30001000,bytes([status]));h.run(0x43ec40,(0x30003000,0x30001000))
  assert bytes(h.u.mem_read(0x30003000,1))==bytes([status])
  checks.append(dict(case=f'map55_status_{status}',payload_length=2,read_operations=0,result='non-success decoder branches before success fields'))
 # Scenario kickoff: external scenario call stubbed, two-byte request captured.
 h=Harness('Saver.exe');h.put(0x4533e0,0x30005000);h.put(0x30005160,0x30006000);h.u.mem_write(0x30006000,b'M001\0');seen=[]
 def scenario():
  sp=h.u.reg_read(UC_X86_REG_ESP);seen.append(h.get(sp+4));h.ret()
 h.put(0x446118,0x300f0000);h.stubs={0x300f0000:scenario,0x43b840:lambda:h.send(True)}
 h.run(0x42a020);assert seen==[0x30006000] and h.sent==[b'\x5c\x5c']
 checks.append(dict(case='scenario_kickoff',payload_length=2,read_operations=0,result='stored name forwarded to scenario setter then 5C5C request captured'))
 for offset,kind,label in [(1,'uint8','status'),*[(2+2*i,'uint16_le','initial_short'+str(i)) for i in range(6)],(14,'uint8','name_byte_length'),(15,'bytes','scenario_name; later offsets depend on length')]:
  fields.append(dict(direction='S2C',opcode='0x55',subtype='',payload_offset=offset,type=kind,scope=label+'; success status1 only after status'))
 write('패킷_월드_필드.csv',fields);write('패킷_캐릭터_unitinfo_대응.csv',mapping);write('패킷_월드_원본명령대조.csv',checks)
 ranges=[('userunit.dll',0x1000e2f0,0x1000e3ca),('userunit.dll',0x1000e23b,0x1000e29a),('GameCtl.dll',0x1000c700,0x1000ca60),('GameCtl.dll',0x1000cac0,0x1000cfe0),('GameCtl.dll',0x10009e67,0x10009eda),('GameCtl.dll',0x1000e200,0x1000e29b),('GameCtl.dll',0x100040f5,0x10004134),('GameCtl.dll',0x10004710,0x1000471a),('GameCtl.dll',0x10008040,0x10008063),('Saver.exe',0x4297e0,0x42992c),('Saver.exe',0x429bc5,0x429bd0),('Saver.exe',0x42a020,0x42a078),('Saver.exe',0x43ec40,0x43ede7),('Saver.exe',0x43be45,0x43bf13)]
 evidence=[];asm=[]
 for name,a,b in ranges:
  p=ROOT/name;evidence.append(dict(file=name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_월드_근거.csv',evidence);(OUT/'패킷_월드_코드.asm.txt').write_text('\n'.join(asm))
 summary=dict(cases=len(checks),mismatches=0,character50='16 shorts + 2 dwords + byte length + string; payload43+L',character51='45 shorts + 4 dwords + two byte-length strings; payload110+L1+L2',movement='C2S 5C43 + unit short + five coordinate short pairs =24 bytes',scope='isolated original handlers; resetcharacter, CString side effects and sender external effects stubbed; map response decoder and scenario kickoff emulated with external calls stubbed; file/graphics loading static only; not full session')
 (OUT/'패킷_월드검증.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
