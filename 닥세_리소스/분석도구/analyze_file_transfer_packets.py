"""Saver pre-dispatch file transfer with modeled Win32 file I/O, no disk writes/network."""
import json,struct,subprocess,hashlib
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EAX
from analyze_packet_requests import Harness
from analyze_local_packet_helpers import putstr
from analyze_special_skills import ROOT,OUT,write,PE

def setup(data=b''):
 h=Harness('Saver.exe');root=0x30008000;obj=0x30006000;state={'pos':0,'written':bytearray()}
 h.put(0x4533e0,root);h.put(obj+8,putstr(h,'data.bin'));h.put(obj+0x14,len(data),0,7,0)
 def seek():
  sp=h.u.reg_read(UC_X86_REG_ESP);assert h.get(sp+16)==0;state['pos']=h.get(sp+8);h.ret(state['pos'],clean=16)
 def read():
  sp=h.u.reg_read(UC_X86_REG_ESP);out,n,count=h.get(sp+8),h.get(sp+12),h.get(sp+16);b=data[state['pos']:state['pos']+n];h.u.mem_write(out,b or b'\0');h.put(count,len(b));state['pos']+=len(b);h.ret(1,clean=20)
 def fwrite():
  sp=h.u.reg_read(UC_X86_REG_ESP);p,n,count=h.get(sp+8),h.get(sp+12),h.get(sp+16);b=bytes(h.u.mem_read(p,n));end=state['pos']+n
  if len(state['written'])<end:state['written'].extend(bytes(end-len(state['written'])))
  state['written'][state['pos']:end]=b;state['pos']=end;h.put(count,n);h.ret(1,clean=20)
 h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:lambda:h.send(True)}
 for j,(iat,fn) in enumerate([(0x446188,seek),(0x446180,read),(0x446184,fwrite),(0x446178,lambda:h.ret(1,clean=4)),(0x446640,lambda:h.ret(1,clean=16))]):
  addr=0x300f0000+16*j;h.put(iat,addr);h.stubs[addr]=fn
 return h,obj,state

def main():
 rows=[];spec=[]
 for length in [0,1,511,512,513,1024]:
  data=bytes(i%256 for i in range(length));h,obj,state=setup(data);h.run(0x401c60,(),obj);n=(length+511)//512
  for _ in range(1,n):h.run(0x401d10,(),obj)
  if n:h.run(0x401d10,(),obj)
  expected=[b'\x64'+struct.pack('<II',n,length)+b'\x08data.bin']
  for i in range(n):
   chunk=data[i*512:(i+1)*512];expected.append(b'\x65'+struct.pack('<III',i,i*512,len(chunk))+chunk)
  expected.append(b'\x66\x08data.bin');assert h.sent==expected
  rows.append(dict(case='send_file_'+str(length),packets=len(expected),result='metadata,512-byte chunks,offsets and completion matched'))
 for received_index,expected_index in [(0,0),(1,0),(0,1)]:
  h,obj,state=setup();h.put(obj+0x18,expected_index);payload=b'\x65'+struct.pack('<III',received_index,20,3)+b'abc';h.u.mem_write(0x30001000,payload);h.run(0x401730,(0x30001000,len(payload)),obj)
  if received_index==expected_index:
   assert state['written'][20:23]==b'abc' and h.sent==[b'\x67'+struct.pack('<I',received_index)] and h.get(obj+0x18)==expected_index+1
  else:assert state['written']==b'' and h.sent==[b'\x68'+struct.pack('<I',expected_index)]
  assert h.u.reg_read(UC_X86_REG_EAX)==1
  rows.append(dict(case=f'receive_chunk_{received_index}_expected_{expected_index}',packets=1,result='write+ACK67 or no write+retry68 matched'))
 for op,index,expectdata in [(0x67,0,True),(0x67,1,False),(0x68,1,True),(0x68,2,False)]:
  h,obj,state=setup(bytes(1024));h.put(obj+0x18,1);payload=bytes([op])+struct.pack('<I',index);h.u.mem_write(0x30001000,payload);h.run(0x4018b0,(0x30001000,len(payload)),obj)
  assert h.sent[0][0]==(0x65 if expectdata else 0x69)
  if expectdata:assert struct.unpack_from('<I',h.sent[0],1)[0]==1
  rows.append(dict(case=f'upload_control_{op:x}_{index}',packets=len(h.sent),result='next/retry data or cancel69 matched'))
 # Single dword helpers carry raw32-bit acknowledgement/retry values.
 for entry,op in [(0x4013b0,0x67),(0x401400,0x68)]:
  for value in [0,7,0xffffffff]:
   h,obj,state=setup();h.run(entry,(value,));assert h.sent==[bytes([op])+struct.pack('<I',value)]
   rows.append(dict(case=f'control_send_{op:x}_{value}',packets=1,result='exact u32 control frame payload'))
 pe=PE(ROOT/'Saver.exe')
 for mode,lo,hi,table,default in [('receive',0x61,0x69,0x40183c,0x401836),('upload_control',0x63,0x69,0x4019cc,0x4019c4)]:
  for op in range(lo,hi+1):
   case=pe.u32(table+4*(op-lo))
   if case!=default:spec.append(dict(mode=mode,opcode=hex(op),case_va=hex(case),scope='active file transfer pre-dispatch only'))
 write('패킷_파일전송_분기.csv',spec);write('패킷_파일전송_원본명령대조.csv',rows)
 fields=[dict(opcode=op,body=fmt,payload_length=n,scope='file transfer context; not GameCtl5C subtype') for op,fmt,n in [('64','u32 blockCount,u32 totalBytes,u8 L,filename','10+L'),('65','u32 index,u32 byteOffset,u32 bytes,data','13+N'),('66','u8 L,filename','2+L'),('67','u32 acknowledgedIndex','5'),('68','u32 requestedIndex','5'),('69','u8 L,filename','2+L')]]
 write('패킷_파일전송_명세.csv',fields)
 asm=[];evidence=[]
 for a,b in [(0x4013b0,0x40144e),(0x4016a0,0x4019e9),(0x401c60,0x401f2d),(0x43ba1e,0x43ba55)]:
  p=ROOT/'Saver.exe';evidence.append(dict(file=p.name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_파일전송_근거.csv',evidence);(OUT/'패킷_파일전송_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,scope='original file transfer control/serializer with in-memory Win32 I/O; no actual filesystem writes or original server',blocks=512)
 (OUT/'패킷_파일전송검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
