"""Original local map helpers with modeled CFile/CString/archive boundaries."""
import struct,json,re,hashlib,subprocess
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_EAX
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write

def cstr(h,p):return bytes(h.u.mem_read(p,1024)).split(b'\0')[0].decode('ascii')
def putstr(h,text):
 b=text.encode('ascii');p=h.heap+8;h.heap+=(len(b)+32+15)&~15;h.put(p-8,len(b));h.u.mem_write(p,b+b'\0');return p

def strings(h):
 def ctor():h.put(h.u.reg_read(UC_X86_REG_ECX),putstr(h,''));h.ret()
 def fmt():
  sp=h.u.reg_read(UC_X86_REG_ESP);out,formatp,arg=h.get(sp+4),h.get(sp+8),h.get(sp+12)
  h.put(out,putstr(h,cstr(h,formatp)%cstr(h,arg)));h.ret()
 def assign():
  sp=h.u.reg_read(UC_X86_REG_ESP);h.put(h.u.reg_read(UC_X86_REG_ECX),h.get(h.get(sp+4)));h.ret(clean=4)
 h.stubs.update({0x44085a:ctor,0x440890:fmt,0x440848:lambda:h.ret(),0x440884:assign})

def muldiv(a,b,c):
 def signed(n):return n if n<0x80000000 else n-0x100000000
 a,b,c=map(signed,(a,b,c));prod=a*b
 if not c:return 0xffffffff
 q=(abs(prod)+abs(c)//2)//abs(c);q=-q if (prod<0)!=(c<0) else q
 return q&0xffffffff if -2147483648<=q<=2147483647 else 0xffffffff

def reference(b):
 n=struct.unpack_from('<I',b)[0];values=struct.unpack_from('<'+'I'*n,b,4);x,y,k=struct.unpack_from('<3I',b,4+4*n)
 acc=(n*len(b)+sum(values))&0xffffffff;acc=muldiv(acc,x,y)
 if k:acc=acc*k&0xffffffff
 for _ in range(10):
  pos=acc%(len(b)-5);acc=(acc+struct.unpack_from('<I',b,pos)[0])&0xffffffff
 return acc>>1

def native_map(b,opened=True):
 h=Harness('Saver.exe');strings(h);state={'pos':0};paths=[]
 def fopen():
  sp=h.u.reg_read(UC_X86_REG_ESP);paths.append(cstr(h,h.get(sp+4)));h.ret(int(opened),clean=12)
 def read():
  sp=h.u.reg_read(UC_X86_REG_ESP);out,n=h.get(sp+4),h.get(sp+8);chunk=b[state['pos']:state['pos']+n];assert len(chunk)==n
  h.u.mem_write(out,chunk);state['pos']+=n;h.ret(n,clean=8)
 def seek():
  sp=h.u.reg_read(UC_X86_REG_ESP);assert h.get(sp+8)==0;state['pos']=h.get(sp+4);h.ret(state['pos'],clean=8)
 def md():
  sp=h.u.reg_read(UC_X86_REG_ESP);h.ret(muldiv(*[h.get(sp+4+4*i) for i in range(3)]),clean=12)
 h.stubs.update({0x440914:lambda:h.ret(),0x44090e:fopen,0x440bb4:lambda:h.ret(len(b)),0x440908:read,0x4409ce:seek,0x4408f6:lambda:h.ret(),0x4408f0:lambda:h.ret(),0x440728:h.alloc,0x44072e:lambda:h.ret(),0x300f0000:md})
 h.put(0x4461a0,0x300f0000);h.run(0x43d500,(putstr(h,'M001'),));assert paths==['Map\\M001.mrc']
 return h.u.reg_read(UC_X86_REG_EAX)

def native_assets(scene,initialize,ai,evt):
 h=Harness('Saver.exe');strings(h);calls=[];deletes=[];h.put(0x30005004,0x30006000);h.u.mem_write(0x300060ce,struct.pack('<h',99))
 def atoi():
  sp=h.u.reg_read(UC_X86_REG_ESP);m=re.match(r'\s*([+-]?\d+)',cstr(h,h.get(sp+4)));h.ret((int(m[1]) if m else 0)&0xffffffff)
 def extract():
  sp=h.u.reg_read(UC_X86_REG_ESP);dest,archive,entry=[h.get(sp+4+4*i) for i in range(3)];a,e=cstr(h,h.get(archive)),cstr(h,h.get(entry));calls.append((a,e))
  ok=ai if e.endswith('.ai') else evt;h.put(dest,putstr(h,'tmp_'+e if ok else ''));h.ret(dest,clean=12)
 def delete():
  sp=h.u.reg_read(UC_X86_REG_ESP);deletes.append(cstr(h,h.get(h.get(sp+4))));h.ret(1,clean=4)
 h.stubs[0x4408a2]=lambda:h.ret(0x30005000)
 for j,(iat,fn) in enumerate([(0x44658c,atoi),(0x446010,lambda:h.ret()),(0x44600c,lambda:h.ret(int(initialize))),(0x446004,lambda:h.ret()),(0x446008,extract),(0x446068,delete)]):
  addr=0x300f0000+j*16;h.put(iat,addr);h.stubs[addr]=fn
 h.run(0x43d320,(putstr(h,scene),));return h.u.reg_read(UC_X86_REG_EAX),calls,deletes

def main():
 maps=[];checks=[]
 files=sorted((ROOT/'MAP').glob('*.mrc'));assert files
 for p in files:
  b=p.read_bytes();expected=reference(b);actual=native_map(b);assert actual==expected,p.name
  maps.append(dict(file=p.name,bytes=len(b),sha256=hashlib.sha256(b).hexdigest(),value=actual,match=True))
 assert native_map(b'',opened=False)==0;checks.append(dict(case='map_open_failure',result='returns0'))
 for scene,init,ai,evt,expected,ncalls,ndel in [('99',False,False,False,1,0,0),('99abc',False,False,False,1,0,0),('M001',False,True,True,0,0,0),('M001',True,False,True,0,1,0),('M001',True,True,False,0,2,1),('M001',True,True,True,1,2,2)]:
  value,calls,deletes=native_assets(scene,init,ai,evt);assert (value,len(calls),len(deletes))==(expected,ncalls,ndel)
  if calls:assert calls[0]==('Map\\M001set.arc','M001.ai')
  if len(calls)>1:assert calls[1]==('Map\\M001set.arc','M001.evt')
  checks.append(dict(case=f'assets_{scene}_{init}_{ai}_{evt}',result=f'return{value},extract{len(calls)},delete{len(deletes)}'))
 # UI ordinal selection versus raw slot lookup.
 for ordinal,slot in [(0,0),(1,4),(2,19),(3,None)]:
  h=Harness('Saver.exe');h.put(0x30008028,0x30010000)
  for i in [0,4,19]:h.put(0x30010000+732*i+728,1)
  h.run(0x42c740,(ordinal,),0x30008000);assert h.u.reg_read(UC_X86_REG_EAX)==(0 if slot is None else 0x30010000+732*slot)
  checks.append(dict(case='ordinal_'+str(ordinal),result='slot'+str(slot)))
 for start,slot in [(0,0),(1,4),(5,19),(20,None)]:
  h=Harness('Saver.exe');h.put(0x30008028,0x30010000)
  for i in [0,4,19]:h.put(0x30010000+732*i+728,1)
  h.run(0x42c6f0,(start,),0x30008000);assert h.u.reg_read(UC_X86_REG_EAX)==(0 if slot is None else 0x30010000+732*slot)
  checks.append(dict(case='raw_search_'+str(start),result='slot'+str(slot)))
 # Additional app52 generator. MRC helper value supplied from verified real01.mrc.
 checksum=reference((ROOT/'MAP'/'01.mrc').read_bytes())
 for mode in [1,2]:
  for optional in ['', 'Bob']:
   h=Harness('Saver.exe');root=0x30008000;h.put(0x4533e0,root);h.put(0x30005004,0x30006000);h.put(0x30006020,root);h.u.mem_write(0x300060ce,struct.pack('<h',1))
   scene=putstr(h,'01');other=putstr(h,'Hero');opt=putstr(h,optional) if optional else 0
   h.stubs={0x4408a2:lambda:h.ret(0x30005000),0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43d500:lambda:h.ret(checksum),0x43b840:lambda:h.send(True)}
   h.put(0x44658c,0x300f0000);h.stubs[0x300f0000]=lambda:h.ret(1)
   h.run(0x43d680,(mode,opt,scene,other))
   expected=b'\x52'+struct.pack('<HH',0x32,mode)+(bytes([len(optional)])+optional.encode() if mode==2 else b'')+b'\x0201'+struct.pack('<I',checksum)+b'\x04Hero'
   assert h.sent==[expected] and h.get(root+0x15c)==1
   checks.append(dict(case=f'app52_mode_{mode}_optional_{len(optional)}',result='exact bytes; original asset-check same-team shortcut; MRC value from verified01.mrc'))
 # Coordinate request2A clips negative signed16 coordinates to0.
 for x,y in [(-1,-1),(20,30),(32768,65535)]:
  h=Harness('Saver.exe');h.put(0x30005004,0x30006000);h.u.mem_write(0x300060ce,struct.pack('<H',7));h.stubs={0x4408a2:lambda:h.ret(0x30005000),0x43b840:lambda:h.send(True)}
  h.run(0x43ac20,(x&0xffffffff,y&0xffffffff),0x30008000)
  def clip(v):return 0 if v&0x8000 else v&65535
  assert h.sent==[b'\x2a'+struct.pack('<HHH',7,clip(x),clip(y))]
  checks.append(dict(case=f'app2a_{x}_{y}',result='session/team word plus signed16 nonnegative coordinate clipping'))
 write('패킷_MRC_계산대조.csv',maps);write('패킷_로컬도우미_원본명령대조.csv',checks)
 evidence=[];asm=[]
 for a,b in [(0x43d320,0x43d674),(0x42c6f0,0x42c785),(0x402a7f,0x402b1e),(0x402d00,0x402d28),(0x43d680,0x43d804),(0x43ac20,0x43acb4)]:
  p=ROOT/'Saver.exe';evidence.append(dict(file=p.name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_로컬도우미_근거.csv',evidence);(OUT/'패킷_로컬도우미_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(mrc_files=len(maps),additional_cases=len(checks),mismatches=0,scope='full original helper control/arithmetic with modeled CString,CFile,MulDiv,archive boundaries; installed MRC bytes; archive extraction success/failure fixtures; no OS or original session')
 (OUT/'패킷_로컬도우미검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
