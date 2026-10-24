"""Original x86 inventory, equipment, shop and NPC dialogue paths; OS/UI effects intercepted."""
import json,struct,subprocess,hashlib
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX
from analyze_packet_requests import Harness
from analyze_saver_messages import decode
from analyze_special_skills import ROOT,OUT,write

def fixture():
 h=Harness('Saver.exe')
 h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:lambda:h.send(True),0x4408d2:lambda:h.ret(clean=4),0x427600:lambda:h.ret(clean=8),0x440848:lambda:h.ret()}
 return h

def main():
 rows=[]
 def ok(case,detail):rows.append(dict(case=case,result=detail,match=True))
 for pairs in [[],[(12,3)],[(12,3),(12,-1),(-1,32767)],[(i,100+i) for i in range(28)]]:
  body=struct.pack('<h',len(pairs))+b''.join(struct.pack('<hh',*p) for p in pairs)
  h,_,decoded=decode(0x36,0x43e9c0,body)
  h.stubs[0x440728]=h.alloc;freed=[]
  def free():freed.append(h.get(h.u.reg_read(UC_X86_REG_ESP)+4));h.ret()
  h.stubs[0x44072e]=free;manager=0x30007000
  h.put(manager+0x30,0x30008000,0x30009000)
  h.run(0x42c530,(0x30003000,),manager)
  assert h.get(manager+0x2c)==len(pairs) and freed==[0x30008000,0x30009000]
  for i,p in enumerate(pairs):
   assert tuple(struct.unpack('<i',h.u.mem_read(h.get(manager+offset)+4*i,4))[0] for offset in (0x30,0x34))==p
  ok('inventory_'+str(len(pairs)),'36 decoder -> full replacement; signed pairs and prior-array release')
 for base in (100,500):
  h=fixture();h.put(0x300000b4,base);valid={base,base+23,base+99};lookups=[]
  def table():
   ident=h.get(h.u.reg_read(UC_X86_REG_ESP)+4);lookups.append(ident);h.put(0x30007000,ident);h.ret(0x30007000 if ident in valid else 0)
  h.put(0x4466b4,0x300f0000);h.stubs[0x300f0000]=table;h.run(0x4099e0,this=0x30000000)
  assert h.get(0x300000b8)==3 and [h.get(h.get(0x300000bc)+i*4) for i in range(3)]==sorted(valid)
  assert lookups==list(range(base,base+100))*2
  ok('catalog_'+str(base),'local 100-ID range enumerated twice; only matching existing item table rows retained')
 body=struct.pack('<6H2I22H',*range(6),10,20,*range(22))+bytes(6)+struct.pack('<16H',*range(600,616))+bytes(1)+bytes(20)
 h,_,record=decode(0x34,0x43e5b0,body,shared=True)
 assert struct.unpack_from('<7H',record,0x9a)==tuple(range(602,616,2))
 assert struct.unpack_from('<7H',record,0xa8)==tuple(range(603,616,2))
 ok('equipment_snapshot34','seven wire pairs deinterleaved into record9A item IDs and A8 auxiliary shorts')
 for value in (0,1,999999,0xffffffff):
  h=fixture();h.put(0x30001000,value);h.put(0x30000038,777);h.run(0x42c5c0,(0x30001000,),0x30000000)
  assert h.get(0x30000038)==value;ok('money_'+str(value),'absolute dword assignment, no addition')
 for count in (0,27,28,29):
  h=fixture();o=0x30000000;m=0x30004000
  h.put(o+0xb0,m);h.put(m+0x2c,count);h.put(o+0xbc,0x30005000);h.put(o+0x360,2);h.put(0x30005000,10,20,321)
  before=bytes(h.u.mem_read(m,64));h.run(0x40a150,this=o)
  assert h.sent==([b'\x4b'+struct.pack('<h',321)] if count<28 else []) and bytes(h.u.mem_read(m,64))==before
  ok('buy_'+str(count),'4B selected catalog item; capacity gate; no local inventory/money change')
 for allowed in (0,1):
  h=fixture();o=0x30000000;m=0x30004000
  h.put(o,0x30006000);h.put(0x3000605c,0x300f0000);h.stubs[0x300f0000]=lambda:h.ret(allowed,clean=4)
  h.put(o+0xb0,m);h.put(m+0x2c,1,0x30005000,0x30005100);h.put(0x30005000,321);h.put(0x30005100,65535)
  before=bytes(h.u.mem_read(m,64));h.run(0x40a230,this=o)
  assert h.sent==([b'\x4c'+struct.pack('<HH',321,65535)] if allowed else []) and bytes(h.u.mem_read(m,64))==before
  ok('sell_'+str(allowed),'4C item ID plus auxiliary low16; virtual acceptance gate; no local mutation')
 for slot in range(7):
  for item in (-1,321):
   h=fixture();h.put(0x3000031c,0x30004000);h.u.mem_write(0x30004004,struct.pack('<h',19))
   h.run(0x420800,(slot,item&0xffffffff,23,47),0x30000000)
   assert h.sent==[b'\x40'+struct.pack('<hhhhh',19,slot,item,23,47)]
   assert struct.unpack('<h',h.u.mem_read(0x30004004+0x9a+slot*2,2))[0]==item
   ok(f'equipment_{slot}_{item}','40 unit/slot/item/aux/aux; optimistic equipment ID write')
 for state in (0,1,2):
  h=fixture();h.put(0x300000b4,101,state)
  h.run(0x4071f0,this=0x30000000)
  assert h.sent==[b'\x72'+struct.pack('<hh',101,state)] and h.get(0x300000b8)==2
  assert h.get(0x300000d4)==(1 if state==2 else 0)
  ok('dialogue_request_'+str(state),'72 dialogue ID/action; next action reset to2; repeated action2 sets D4')
 for kind in (1,2,3):
  for flags in ((0,1),(2,2),(-1,3)):
   h=fixture();o=0x30000000;ui=0x30006000;opened=[];texts=[]
   h.put(o+0xd8,0x30004000);h.put(0x30004000,0x30005000);h.put(0x30005004,0x300f0000)
   def show():
    sp=h.u.reg_read(UC_X86_REG_ESP);opened.append(h.get(sp+4));h.ret(clean=8)
   def assign():
    p=h.get(h.u.reg_read(UC_X86_REG_ESP)+4);texts.append(bytes(h.u.mem_read(p,20)).split(b'\0')[0]);h.ret(clean=4)
   h.stubs[0x300f0000]=show;h.stubs[0x427270]=lambda:h.ret(ui,clean=4);h.stubs[0x440872]=assign
   h.u.mem_write(0x30001000,struct.pack('<hhB',101,kind,3)+b'NPC'+struct.pack('<hh',*flags))
   h.run(0x430500,(0x30001000,),o)
   assert opened==[{1:0x24,2:0x34,3:0x35}[kind]]
   if kind in (1,2):
    assert h.sent==[b'\x72'+struct.pack('<hh',101,0)] and texts==[b'NPC']
    assert [h.get(ui+x) for x in (0xd8,0xdc)]==[(0 if f==2 else f)&0xffffffff for f in flags]
   else:assert not h.sent
   ok(f'dialogue_open_{kind}_{flags}','dialogue UI branch, normalized flags, automatic72 request for kinds1/2')
 for strings in ((b'',b'',b''),(b'NPC',b'yes',b'no')):
  h=fixture();captured=[]
  def display():
   sp=h.u.reg_read(UC_X86_REG_ESP);a=[h.get(sp+4+i*4) for i in range(6)]
   captured.append((a[:3],[bytes(h.u.mem_read(p,20)).split(b'\0')[0] for p in a[3:]]));h.ret(clean=24)
  h.stubs[0x4073c0]=display
  payload=b'\x73'+struct.pack('<Ihh',123456,11,22)+b''.join(bytes([len(s)])+s for s in strings)
  h.u.mem_write(0x30001000,payload);h.run(0x407280,(0x30001000,len(payload)),0x30000000)
  assert captured==[([123456,11,22],list(strings))]
  ok('dialogue_response_'+str(tuple(map(len,strings))),'73 dword, two shorts, three u8-length strings -> UI display helper')
 write('싱글플레이_소지품_대화_원본명령대조.csv',rows)
 ranges=[(0x42c530,0x42c5cc),(0x40a150,0x40a335),(0x420800,0x4208ab),(0x4071d0,0x407540),(0x430500,0x4306b6),(0x41fff5,0x420030)]
 asm=[subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'Saver.exe')],text=True) for a,b in ranges]
 (OUT/'싱글플레이_소지품_대화_근거.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,binary_sha256=hashlib.sha256((ROOT/'Saver.exe').read_bytes()).hexdigest(),scope='original x86 consumers/serializers; allocation/free, UI, CString and virtual sell predicate modeled; no complete Windows session',opaque_fields='inventory auxiliary and equipment auxiliary fields preserved, not guessed',quest_boundary='dialogue transport and presentation confirmed; historical quest eligibility/reward decisions belong to excluded server logic')
 (OUT/'싱글플레이_소지품_대화검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
