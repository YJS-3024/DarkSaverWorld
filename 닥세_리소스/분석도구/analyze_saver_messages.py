"""Saver variable record decoders and storage/loop probes, no Windows/session execution."""
import struct,json,hashlib,subprocess
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EBX,UC_X86_REG_EBP,UC_X86_REG_EIP
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write

def decode(op,entry,body,shared=False,valueonly=False):
 h=Harness('Saver.exe');h.u.mem_write(0x30001000,body or b'\0');trace=[]
 def hook(u,a,n,d):
  widths={0x43e2f0:2,0x43e310:1,0x43e380:4,0x43e330:0}
  if a in widths:
   sp=u.reg_read(UC_X86_REG_ESP);trace.append(dict(opcode=hex(op),cursor=h.get(h.get(sp+8)),width=widths[a],string_length=h.get(sp+16) if a==0x43e330 else ''))
 h.u.hook_add(UC_HOOK_CODE,hook)
 h.run(entry,(0x30001000,) if valueonly else (0x30003000,0x30001000,0x30002000) if shared else (0x30003000,0x30001000))
 return h,trace,bytes(h.u.mem_read(0x30003000,1026))

def main():
 checks=[];reads=[]
 def checked(name):checks.append(dict(case=name,result='matched original decoder/store/loop instructions'))
 for length in [0,3,255]:
  for flag in [0,1,2,255]:
   text=b'A'*length;body=struct.pack('<6H',1,2,3,4,5,6)+bytes([length])+text+bytes([flag]);h,t,out=decode(0x32,0x43e4b0,body)
   assert struct.unpack_from('<6H',out)==(1,2,3,4,5,6) and out[13:13+length]==text and (length==255 or out[13+length]==0) and out[268]==flag%2
   checked(f'32_{length}_{flag}')
   if length==3 and flag==1:reads+=t
 for length in [0,3,255]:
  for counts in [(0,0,0),(1,2,3),(20,10,10)]:
   body=struct.pack('<6H2I22H',*range(1,7),100001,100002,*range(20,42))
   for count in counts:body+=struct.pack('<h',count)+struct.pack('<'+'H'*count,*range(100,100+count))
   body+=struct.pack('<16H',*range(60,76))+bytes([length])+b'B'*length+struct.pack('<10H',*range(70,80))
   h,t,out=decode(0x34,0x43e5b0,body,shared=True);assert h.get(0x30002000)==123+length+2*sum(counts)
   assert len(t)==61+sum(counts) and t[-1]['cursor']==121+length+2*sum(counts)
   checked('34_record_'+str(length)+'_'+str(counts))
   if length==3 and counts==(1,2,3):reads+=t
 for count in [0,1,3]:
  vals=list(range(1,2*count+1));body=struct.pack('<h',count)+struct.pack('<'+'h'*len(vals),*vals)
  h,t,out=decode(0x36,0x43e9c0,body);assert out[:len(body)]==body;checked('36_'+str(count))
  if count==3:reads+=t
 for texts in [[],[b'a',b'bc'],[b'C'*255]]:
  body=struct.pack('<h',len(texts))+b''.join(bytes([len(x)])+x for x in texts);h,t,out=decode(0x38,0x43ea60,body)
  assert out[:2]==body[:2];pos=2
  for x in texts:assert out[pos]==len(x) and out[pos+1:pos+1+len(x)]==x;pos+=1+len(x)
  checked('38_'+str(len(texts))+'_'+str(sum(map(len,texts))))
  if len(texts)==2:reads+=t
 for value in [0,9,0x474709,0xffffffff]:
  h,t,out=decode(0x3a,0x43ea40,struct.pack('<I',value),valueonly=True)
  from unicorn.x86_const import UC_X86_REG_EAX
  assert h.u.reg_read(UC_X86_REG_EAX)==value;checked('3a_'+str(value))
 for status in [0,2,255]:
  h,t,out=decode(0x53,0x43eaf0,bytes([status]));assert out[0]==status and len(t)==1;checked('53_status_'+str(status))
 for sentinel in [False,True]:
  rect=(65535 if sentinel else 11,22,33,44);body=b'\x01'+struct.pack('<11H',1,2,3,4,5,6,7,*rect);h,t,out=decode(0x53,0x43eaf0,body)
  assert struct.unpack_from('<I',out,16)[0]==int(not sentinel) and struct.unpack_from('<4H',out,20)==rect
  checked('53_rect_'+str(sentinel));reads+=t
 # Manager consumer uses first short as slot and copies724 bytes into 732-byte records.
 for slot in [0,1,19]:
  h=Harness('Saver.exe');source=bytearray((i%251 for i in range(724)));struct.pack_into('<h',source,0,slot);h.u.mem_write(0x30003000,bytes(source));h.put(0x30008028,0x30010000)
  h.run(0x42c4f0,(0x30003000,),0x30008000);dest=0x30010000+slot*732
  assert bytes(h.u.mem_read(dest+4,724))==source and h.get(dest+728)==1;checked('manager_slot_'+str(slot))
 # Full original byte-count loop plus original record decoder; consumer side effects stubbed.
 for count in [0,1,2]:
  h=Harness('Saver.exe');expected=256 if count==0 else count;h.u.mem_write(0x30010000,b'\x34'+bytes([count])+bytes(123*expected));calls=[]
  def consume():calls.append(1);h.ret(clean=4)
  h.stubs={0x42c4f0:consume};h.put(0x300080d4,0x30009000);h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.reg_write(UC_X86_REG_EBX,0x30010000);h.u.reg_write(UC_X86_REG_EBP,0x30008000)
  h.u.emu_start(0x43bac9,0x43bb49,count=1000000);assert h.u.reg_read(UC_X86_REG_EIP)==0x43bb49 and len(calls)==expected
  checked('34_count_'+str(count)+'_iterations_'+str(expected))
 # Original window-message bridge403 -> transition -> scenario kickoff.
 for team,aux in [(7,True),(65535,True),(7,False)]:
  h=Harness('Saver.exe');root=0x30008000;h.put(0x4533e0,root);h.put(root+0x160,0x30007000);h.u.mem_write(0x30007000,b'M001\0');events=[]
  if aux:h.put(root+0x13c,0x30009000);h.put(0x30009000,0x3000a000);h.put(0x3000a00c,0x300f0000)
  h.stubs={0x300f0000:lambda:h.ret(),0x43b840:lambda:h.send(True)}
  for j,(iat,label) in enumerate([(0x446140,'tutorial'),(0x446118,'scenario'),(0x446144,'team')],1):
   dest=0x300f0000+j*16;h.put(iat,dest)
   def capture(label=label):
    sp=h.u.reg_read(UC_X86_REG_ESP);events.append((label,h.get(sp+4)));h.ret()
   h.stubs[dest]=capture
  h.run(0x43c5d0,(team,0),root)
  assert h.sent==([b'\x5c\x5c'] if aux else [])
  if aux:assert events==[('tutorial',0),('scenario',0x30007000),('team',0xffffffff if team==65535 else team)] and h.get(root+0x138)==1
  else:assert events==[] and h.get(root+0x138)==0
  checked(f'window403_team_{team}_aux_{aux}')
 # App54 sender with opaque helpers stubbed; exact optional string and flag layout.
 for mode in [1,2]:
  for flag in [0,1]:
   h=Harness('Saver.exe');root=0x30008000;h.put(0x4533e0,root);h.put(root+0x160,0x30007000);h.u.mem_write(0x30007000,b'M001\0');h.put(0x30006000,3);h.u.mem_write(0x30006008,b'Bob\0')
   h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x440848:lambda:h.ret(),0x43d500:lambda:h.ret(0x12345678),0x43d320:lambda:h.ret(1),0x43b840:lambda:h.send(True)}
   h.run(0x43d810,(7,mode,flag,0x30006008))
   expected=b'\x54'+struct.pack('<HH',7,mode)+(b'\x03Bob' if mode==2 else b'')+struct.pack('<HI',2 if flag==0 else 1,0x12345678)
   assert h.sent==[expected];checked(f'app54_mode_{mode}_flag_{flag}')
 write('패킷_Saver_가변읽기.csv',reads);write('패킷_Saver_가변_원본명령대조.csv',checks)
 specs=[dict(opcode=op,body=fmt,payload_length=length,status='original decoder verified with synthetic valid inputs; fields neutral unless consumer verified') for op,fmt,length in [('32','u16[6];u8 L;bytes[L];u8 flag(bit0)','15+L'),('34','u8 N; {u16[6],u32[2],u16[22],three counted short lists(max20/10/10),u16[16],u8 L,bytes[L],u16[10]} repeated','2+sum(123+2*(A+B+C)+L); normal N>=1; zero count original loops256'),('36','i16 N; i16[2*N]','3+4N'),('38','i16 N; {u8 L,bytes[L]} repeated','3+sum(1+L)'),('3A','u32 value','5'),('53','u8 status; if1:u16[11]','2 if status!=1 else24')]]
 write('패킷_Saver_가변명세.csv',specs)
 evidence=[];asm=[]
 for a,b in [(0x43e4b0,0x43e9c0),(0x43e9c0,0x43eaf0),(0x43eaf0,0x43ec40),(0x42c3b0,0x42c48f),(0x42c4f0,0x42c524),(0x43bac9,0x43bb49),(0x43c400,0x43c498),(0x43c5d0,0x43c5e7),(0x43bdab,0x43bf13),(0x43d810,0x43d916)]:
  p=ROOT/'Saver.exe';evidence.append(dict(file=p.name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_Saver_가변_근거.csv',evidence);(OUT/'패킷_Saver_가변_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(checks),mismatches=0,messages=['32','34','36','38','3A','53'],additional_sender='54',transition='WM403 bridge to scenario kickoff verified with external effects stubbed',record34='123+2*(A+B+C)+string bytes; stored724 in732-stride manager',zero34='original byte decrement loop executes256 records; isolated zero-filled fixture only',scope='isolated decoders, manager copy, transition bridge and54 sender; CString/external effects/hash and validation helpers stubbed where needed; no Windows/session execution')
 (OUT/'패킷_Saver_가변검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
