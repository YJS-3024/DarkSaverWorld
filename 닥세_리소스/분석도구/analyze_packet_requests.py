"""Isolated x86 serializers with explicit allocation, table and send stubs; no OS/socket."""
import struct,json,hashlib,subprocess
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX,UC_X86_REG_ECX
from analyze_special_skills import PE,ROOT,OUT,write
class Harness:
 def __init__(self,name):
  self.p=PE(ROOT/name);self.u=u=Uc(UC_ARCH_X86,UC_MODE_32);self.sent=[];self.transitions=[];self.stubs={};self.heap=0x30010000
  u.mem_map(0,4096);u.mem_map(self.p.base,(max(rva+max(vs,rs) for vs,rva,rs,off in self.p.sections)+4095)&~4095)
  for vs,rva,rs,off in self.p.sections:u.mem_write(self.p.base+rva,self.p.b[off:off+rs])
  u.mem_map(0x20000000,0x20000);u.mem_map(0x30000000,0x100000)
  u.hook_add(UC_HOOK_CODE,self.hook)
 def put(self,a,*values):self.u.mem_write(a,struct.pack('<'+'I'*len(values),*values))
 def get(self,a):return struct.unpack('<I',self.u.mem_read(a,4))[0]
 def ret(self,value=0,clean=0):
  sp=self.u.reg_read(UC_X86_REG_ESP);dest=self.get(sp);self.u.reg_write(UC_X86_REG_EAX,value);self.u.reg_write(UC_X86_REG_ESP,sp+4+clean);self.u.reg_write(UC_X86_REG_EIP,dest)
 def hook(self,u,a,size,data):
  if a in self.stubs:self.stubs[a]()
 def alloc(self):
  sp=self.u.reg_read(UC_X86_REG_ESP);n=self.get(sp+4);a=self.heap;self.heap+=(n+31)&~15;self.u.mem_write(a,b'\xcd'*n);self.ret(a)
 def send(self,app=False):
  sp=self.u.reg_read(UC_X86_REG_ESP);a,b=self.get(sp+4),self.get(sp+8);n,p=(a,b) if app else (b,a)
  self.sent.append(bytes(self.u.mem_read(p,n)));self.ret(clean=8 if app else 0)
 def run(self,entry,args=(),this=0):
  self.put(0x20010000,0x300ff000,*args);self.u.reg_write(UC_X86_REG_ESP,0x20010000);self.u.reg_write(UC_X86_REG_ECX,this)
  self.u.emu_start(entry,0x300ff000,count=100000)
  assert self.u.reg_read(UC_X86_REG_EIP)==0x300ff000,'instruction limit'

def game_case(entry,mode,argument,flag=0,targets=()):
 h=Harness('GameCtl.dll');h.put(0x1001a8ec,mode);h.u.mem_write(0x30001000,argument)
 if targets:h.u.mem_write(0x30002000,struct.pack('<'+'h'*len(targets),*targets))
 h.stubs={0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x1000e1e0:h.send}
 # GetMagicTbl controlled fixture: id=3, components 2+4+6+8+10 -> derived value90.
 h.put(0x30003000,3,0,2,4,6,8);h.put(0x30003170,10)
 h.stubs[0x300f0000]=lambda:h.ret(0x30003000);h.put(0x100162d4,0x300f0000)
 h.run(entry,(0x30001000,flag));return h

def main():
 rows=[]
 def check(label,h,expected,entry):
  assert h.sent==expected,(label,[x.hex() for x in h.sent],[x.hex() for x in expected])
  rows.append(dict(case=label,entry_va=hex(entry),sent_payloads=';'.join(x.hex() for x in h.sent),match=True))
 for mode in [0,0x200,0x400]:
  for skill in [9999,10000]:
   for flag in [0,1]:
    for targets in [(),(7,-1,300)]:
     data=struct.pack('<hhhhIB3xI',skill,11,-2,33,0,len(targets),0x30002000)
     h=game_case(0x1000e3e0,mode,data,flag,targets)
     expected=b'\x5c'+bytes([0x89 if skill<10000 else 0x92,1 if flag==0 else 2])+struct.pack('<hhhhIB',skill,11,-2,33,90,len(targets))+struct.pack('<'+'h'*len(targets),*targets)
     check(f'magic_{mode:x}_{skill}_{flag}_{len(targets)}',h,[expected] if mode in [0x200,0x400] else [],0x1000e3e0)
 for mode in [0,0x200,0x400]:
  data=struct.pack('<hhhh',1,-2,300,400)
  h=game_case(0x1000e2a0,mode,data);check(f'attack_{mode:x}',h,[b'\x5c\x82'+data] if mode==0x400 else [],0x1000e2a0)
 for entry,payload in [(0x1000e670,b'\x5b'),(0x1000e6a0,b'\x5c\x61')]:
  h=game_case(entry,0,b'\0');check('short_'+hex(entry),h,[payload],entry)
 # App callbacks, stage absent fixture. All external side effects intercepted.
 for entry,op in [(0x43c360,0x7c),(0x43cf30,0x2d)]:
  h=Harness('Saver.exe');body=struct.pack('<HHH',9,0x4709,65535);h.u.mem_write(0x30001000,bytes([op])+body)
  h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:lambda:h.send(True)}
  h.put(0x300000c0,0x30004000)
  h.run(entry,(0x30001001 if op==0x7c else 0x30001000,),0x30000000)
  check('app_'+hex(op),h,[bytes([0x2a if op==0x7c else 0x2d])+body],entry)
  if op==0x2d:assert h.get(0x30004018)==1
 for status in [0,1,2,255]:
  h=Harness('Saver.exe');h.u.mem_write(0x30001000,b'\x2b'+bytes([status])+struct.pack('<HHHHB',101,202,303,404,2))
  h.put(0x30005004,0x30006000);h.u.mem_write(0x300060ce,struct.pack('<H',777))
  def transition():
   h.transitions.append(h.get(h.u.reg_read(UC_X86_REG_ESP)+4));h.ret(clean=4)
  h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:lambda:h.send(True),0x4408a2:lambda:h.ret(0x30005000),0x43afc0:transition}
  h.run(0x43c170,(0x30001000,),0x30000000)
  check('app_2b_status_'+str(status),h,[] if status==1 else [b'\x2a'+struct.pack('<HHH',777,20,20)],0x43c170)
  if status==1:
   assert h.transitions==[0,0xffffffff] and bytes(h.u.mem_read(0x300060ce,2))==struct.pack('<H',101)
   stored=[struct.unpack('<H',h.u.mem_read(a,2))[0] for a in [0x4532e0,0x4532de,0x4532dc]]
   assert stored==[404,202,303]
 schemas=[]
 def fields(direction,op,entry,items):
  for offset,kind,name in items:schemas.append(dict(direction=direction,opcode=op,handler_va=hex(entry),payload_offset=offset,type=kind,field=name))
 fields('C2S','5C 82',0x1000e2a0,[(2+2*i,'uint16_le','input_short'+str(i)) for i in range(4)])
 for op in ['5C 89','5C 92']:
  fields('C2S',op,0x1000e3e0,[(2,'uint8','flag_zero_1_else_2')]+[(3+2*i,'uint16_le','input_short'+str(i)) for i in range(4)]+[(11,'uint32_le','table_derived_value'),(15,'uint8','target_count'),(16,'uint16_le[]','target_indices')])
 fields('S2C','2B',0x43c170,[(1,'uint8','status'),(2,'uint16_le','map_number'),(4,'uint16_le','world_x'),(6,'uint16_le','world_y'),(8,'uint16_le','local_unit_id'),(10,'uint8','mode')])
 fields('S2C','7C',0x43c360,[(1+2*i,'uint16_le','echo_word'+str(i)) for i in range(3)])
 fields('C2S','2A',0x43c360,[(1+2*i,'uint16_le','echo_word'+str(i)) for i in range(3)])
 write('패킷_추가교환_필드.csv',schemas)
 write('패킷_요청_원본명령대조.csv',rows)
 ranges=[('Saver.exe',0x43c170,0x43c3f3),('Saver.exe',0x43afc0,0x43b04a),('Saver.exe',0x43cd10,0x43cd27),('GameCtl.dll',0x1000e1e0,0x1000e66f)]
 evidence=[];asm=[]
 for name,a,b in ranges:
  p=ROOT/name;evidence.append(dict(file=name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_추가교환_근거.csv',evidence);(OUT/'패킷_추가교환_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,scope='original serializer and app handler instructions with allocation/send/GetMagicTbl/stage-switch stubs; no live session; table-derived90 is synthetic fixture',external_effects='captured before OS messaging/socket send; stage transition mocked')
 (OUT/'패킷_요청검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
