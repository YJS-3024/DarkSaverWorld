"""Complete GameCtl dispatch wire inventory; new isolated reader/serializer checks."""
import csv,json,struct,subprocess,hashlib
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_EIP
from analyze_packet_requests import Harness,game_case
from analyze_special_skills import ROOT,OUT,write,PE

S={
'5a':('','1','InitValue'), '5b':('u8 length; bytes[length]','2+L','string read; S_PlaySound(1)'),
'1':('i16 unit,x,y','8','MoveChar'),'3':('i16 unit','4','dmKillChar'),'4':('','2','IncTick'),'5':('i16 unit,hp','6','SetHpDisplay'),'6':('i16 unit,mp','6','SetMpDisplay'),'7':('i16 unit; i16 ignored','6','dmKillChar2(first short only)'),
'c':('i16 unit','4','setmonsterai + AIStart'),'d':('i16 event_id,actor_id','6','event enqueue via ef80'),'e':('i16 unit,x,y','8','MoveFail'),'f':('i16 unit','4','SetFail'),'13':('i16 unit; u32 flags','8','MagicFlag'),
'15':('i16 count; i16 event_id[count]','4+2N','event activation f620; sequential scene stepping'),'20':('u8 ignored; u8 length; bytes[length]','4+L','name comparison/message'),
'30':('i16 count; {i16 slot,i16 value,u8 flag}[count]','4+5N','slot grid update'),'31':('i16 count; i16[count]','4+2N','displayed ability list'),'40':('i16 value','4','global1001a4b8'),
'50':('i16[16];u32[2];u8 L;bytes[L]','43+L','resetcharacter short form'),'51':('i16[45];u32[4];u8 L1;bytes[L1];u8 L2;bytes[L2]','110+L1+L2','resetcharacter long form'),
'52':('i16[3]','8','SetTickMenu'),'53':('i16 unit;u32 exp','8','SetExp'),'54':('i16[3];u32 value;i16 count;{i16[14],u32}[count]','14+32N','battle results'),
'5c':('u8 value','3','scenario control byte'),'60':('i16 count;{i16 id,u8 L,bytes[L]}[count]','4+sum(3+L)','name list; own team name CString'),
'61':('u8 value','3','completion/UI branch'),'64':('i16 unit','4','camera focus and magic4001 request'),'65':('i16 unit','4','same handler as64'),
'66':('u8 value','3','CString clear or append(value+0x60 byte)'),'67':('i16[4];u8 count;i16[count]','11+2N','Magic; no targets -> no playback'),
'68':('i16[4]','10','CheckMagicTarget then optional request/playback'),'82':('i16[3]','8','AttackChar'),'89':('i16[4];u8 count;i16[count]','11+2N','Magic'),
'b2':('i16 count;{i16,i16}[count]','4+4N','SetAllyInfo'),'7a':('i16 ignored;u8 type;u8 length;bytes[length]','5+L','chat/internal eda0; type modulo9'),'7b':('u8 value','2','value6 UI key sequence')}

def main():
 dispatch=list(csv.DictReader((OUT/'패킷_GameCtl_처리함수.csv').open(encoding='utf-8-sig')));catalog=[]
 for r in dispatch:
  k=format(int(r['subtype'] or r['opcode'],16),'x');fmt,length,consumer=S[k]
  catalog.append(dict(opcode=r['opcode'],subtype=r['subtype'],handler_va=r['handler_va'],body_format=fmt,payload_consumed_length=length,consumer=consumer,status='wire reads statically traced; semantic completeness varies',constraint='N nonnegative supported count; length denotes consumed layout, not original strict minimum validation'))
 assert len(catalog)==36
 write('패킷_GameCtl_수신명세.csv',catalog)
 checks=[];traces=[]
 # Fixed handlers: real reader and dispatch execution, imported effects replaced.
 cases=[(3,0x1000d720,1,[0x10016204]),(7,0x1000d750,2,[0x100162c8]),(12,0x1000d790,1,[0x100161fc,0x10016200]),(15,0x1000e1b0,1,[0x100161ec]),(0x13,0x1000dbd0,1,[0x100161f8]),(0x52,0x1000d320,3,[0x10016218]),(0x53,0x1000d390,1,[0x100162c4])]
 for sub,entry,n,imports in cases:
  for negative in [False,True]:
   h=Harness('GameCtl.dll');body=struct.pack('<'+'H'*n,*([65535 if negative else 7]*n))+(struct.pack('<I',0x12345678) if sub in [0x13,0x53] else b'')
   h.u.mem_write(0x30001000,b'\x5c'+bytes([sub])+body);seen=[];reads=[]
   for j,iat in enumerate(imports):
    dest=0x300f0000+j*16;h.put(iat,dest)
    def capture(iat=iat):
     sp=h.u.reg_read(UC_X86_REG_ESP);seen.append((iat,h.get(sp+4)));h.ret()
    h.stubs[dest]=capture
   def trace(u,a,n,d):
    if a in [0x1000c470,0x1000c4f0]:
     sp=u.reg_read(UC_X86_REG_ESP);reads.append((h.get(h.get(sp+8)),2 if a==0x1000c470 else 4))
   h.u.hook_add(UC_HOOK_CODE,trace);h.run(0x1000ea20,(0x30001000,))
   assert reads==[(i*2,2) for i in range(n)]+([(2*n,4)] if sub in [0x13,0x53] else [])
   assert [x[0] for x in seen]==imports
   if sub in [3,7,12,15,0x13]:assert seen[0][1]==(0xffffffff if negative else 7)
   checks.append(dict(case=f'fixed_{sub:x}_{negative}',result='real dispatcher/read order/import forwarding matched'))
 # Variable list handlers with no external side effects.
 for sub,entry in [(0x30,0x1000dce0),(0x31,0x1000dde0)]:
  for count in [0,1,3]:
   h=Harness('GameCtl.dll')
   body=struct.pack('<h',count)+(b''.join(struct.pack('<hhB',i,100+i,1 if i%2==0 else 0) for i in range(count)) if sub==0x30 else struct.pack('<'+'h'*count,*range(100,100+count)))
   h.u.mem_write(0x30001000,b'\x5c'+bytes([sub])+body);h.run(0x1000ea20,(0x30001000,))
   if sub==0x31:
    assert h.get(0x1006ad4c)==count
    for i in range(count):assert h.get(0x1006ad50+4*i)==100+i
   else:
    for i in range(count):assert [h.get(0x1006a89c+12*i+j*4) for j in range(3)]==[100+i,1,i%2]
   checks.append(dict(case=f'list_{sub:x}_{count}',result='count-dependent payload and stored values matched'))
 # Previously untested short senders: value passed by value, not pointer.
 senders=[(0x1000e340,b'\x3a',4,False),(0x1000e390,b'\x49',2,False),(0x1000e820,b'\x5c\x0b',2,True),(0x1000e880,b'\x5c\xb0',2,True),(0x1000e8e0,b'\x5c\xb1',2,True),(0x1000e940,b'\x3c',2,False),(0x1000e9c0,b'\x5c\x20',1,False)]
 sends=[]
 for entry,prefix,size,gated in senders:
  sends.append(dict(entry_va=hex(entry),prefix=prefix.hex(),body='u'+str(size*8)+'_le',payload_length=len(prefix)+size,state_gate='0x400' if gated else 'none'))
  for mode in [0,0x400]:
   h=Harness('GameCtl.dll');h.put(0x1001a8ec,mode);h.stubs={0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x1000e1e0:h.send};h.run(entry,(0x12344709,))
   expected=prefix+(0x12344709&((1<<(size*8))-1)).to_bytes(size,'little')
   assert h.sent==([] if gated and mode!=0x400 else [expected])
   checks.append(dict(case=f'send_{entry:x}_{mode:x}',result='exact serialized bytes and gate matched'))
 # Additional multi-target request5C91, controlled magic-table fixture.
 for mode in [0,0x200,0x400]:
  for targets in [(),(7,-1,300)]:
   body=struct.pack('<6hIB3xI',1,2,3,4,5,6,0,len(targets),0x30002000)
   h=game_case(0x1000e530,mode,body,targets=targets)
   expected=b'\x5c\x91'+struct.pack('<6hIB',1,2,3,4,5,6,90,len(targets))+struct.pack('<'+'h'*len(targets),*targets)
   assert h.sent==([expected] if mode in [0x200,0x400] else [])
   checks.append(dict(case=f'send_5c91_{mode:x}_{len(targets)}',result='original multi-target serializer and state gate matched'))
 sends.append(dict(entry_va='0x1000e530',prefix='5c91',body='i16[6],u32 derived,u8 N,i16[N]',payload_length='19+2N',state_gate='0x200 or0x400'))
 # CString chat serializer with controlled strings; no real user account data.
 pe=PE(ROOT/'GameCtl.dll');modebytes={0x1000e744:2,0x1000e74b:3,0x1000e752:4,0x1000e759:1}
 for mode in range(1,9):
  h=Harness('GameCtl.dll');h.put(0x1001a8f4,mode);h.put(0x1001a8f0,0x30006008)
  for addr,data in [(0x30005008,b'hello'),(0x30006008,b'Bob')]:h.put(addr-8,len(data));h.u.mem_write(addr,data+b'\0')
  h.stubs={0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x1001462e:lambda:h.ret(),0x1000e1e0:h.send}
  h.run(0x1000e6e0,(0x30005008,))
  kind=modebytes[pe.u32(0x1000e7fc+(mode-1)*4)]
  expected=b'\x7a'+bytes([kind])+(b'\x03Bob' if mode==8 else b'')+b'\x05hello'
  assert h.sent==[expected],(mode,h.sent,expected)
  checks.append(dict(case='send_chat_mode_'+str(mode),result='mode conversion, optional recipient and byte-length text matched'))
 sends.append(dict(entry_va='0x1000e6e0',prefix='7a',body='u8 converted_mode; optional(u8 recipientLen,recipient);u8 textLen,text',payload_length='3+L or4+R+L',state_gate='mode8 includes recipient'))
 write('패킷_추가송신_명세.csv',sends);write('패킷_분기확장_원본명령대조.csv',checks)
 # Evidence includes every wire handler; adjacent wrappers omitted where already captured.
 evidence=[];asm=[]
 for a,b in [(0x1000d720,0x1000d930),(0x1000dbd0,0x1000e120),(0x1000d320,0x1000d530),(0x1000e130,0x1000e1e0),(0x1000e340,0x1000e3e0),(0x1000e820,0x1000ea20)]:
  path=ROOT/'GameCtl.dll';evidence.append(dict(file=path.name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(path.read_bytes()).hexdigest()));asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(path)],text=True))
 write('패킷_분기확장_근거.csv',evidence);(OUT/'패킷_분기확장_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(receive_branches=36,wire_layouts_cataloged=36,new_cases=len(checks),mismatches=0,scope='all GameCtl branch read layouts cataloged statically; new isolated tests cover seven fixed receivers,two list receivers,nine sender functions; not all application layers or handler side effects verified',excluded=['server internal decisions','historical full send order'])
 (OUT/'패킷_분기확장검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
