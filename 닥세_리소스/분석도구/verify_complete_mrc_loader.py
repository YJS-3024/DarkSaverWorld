"""Complete original BB_Map::LoadMap body, including local animation row initialization."""
import hashlib,json,struct
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_ECX,UC_X86_REG_EAX,UC_X86_REG_EIP
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write
from decode_visuals import read_mrc

def load(data):
 h=Harness('DManage.dll');o=0x30000000;pos=0;reads=[]
 def read():
  nonlocal pos
  sp=h.u.reg_read(UC_X86_REG_ESP);dest,n=h.get(sp+4),h.get(sp+8);part=data[pos:pos+n];pos+=len(part)
  if part:h.u.mem_write(dest,part)
  reads.append((n,len(part)));h.ret(len(part),8)
 def seek():
  nonlocal pos
  sp=h.u.reg_read(UC_X86_REG_ESP);offset,origin=h.get(sp+4),h.get(sp+8);pos=offset if origin==0 else pos+offset if origin==1 else len(data)+offset;h.ret(pos,8)
 def resize():
  sp=h.u.reg_read(UC_X86_REG_ESP);obj=h.u.reg_read(UC_X86_REG_ECX);n=h.get(sp+4);ptr=h.heap;h.heap+=n*4+32
  h.put(obj+4,ptr,n,n);h.ret(clean=8)
 h.put(0x1000d1b4,0x300f0000);h.put(0x1000d1a8,0x300f0010);h.put(0x1000d1ac,0x300f0020);h.put(0x1000d1b8,0x300f0030)
 h.stubs={0x1000beb8:lambda:h.ret(),0x1000beb2:lambda:h.ret(1,12),0x1000beac:lambda:h.ret(),0x1000be94:lambda:h.ret(),0x1000be9a:read,0x1000bfb4:seek,0x1000bea0:h.alloc,0x300f0000:h.alloc,0x1000be8e:lambda:h.ret(),0x1000bf60:resize,0x300f0010:lambda:h.ret(1000),0x300f0020:lambda:h.ret(),0x300f0030:lambda:h.ret(7)}
 h.put(0x20010000,0x300ff000,0x30001000,0);h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.reg_write(UC_X86_REG_ECX,o)
 h.u.emu_start(0x1000a580,0x300ff000,count=10000000);assert h.u.reg_read(UC_X86_REG_EIP)==0x300ff000
 return h,h.u.reg_read(UC_X86_REG_EAX),pos,reads

def main():
 rows=[];seen=set()
 for p in sorted((ROOT/'MAP').iterdir()):
  if p.suffix.lower() not in ('.mrc','.mrtc'):continue
  data=p.read_bytes();digest=hashlib.sha256(data).hexdigest()
  if digest in seen:continue
  seen.add(digest);d=read_mrc(p);h,ok,pos,reads=load(data);w,ht=d['width'],d['height'];o=0x30000000
  assert ok==1 and pos==len(data)
  assert bytes(h.u.mem_read(h.get(o+0x10),w*ht))==d['a']
  assert bytes(h.u.mem_read(h.get(o+0xc),w*ht))==d['b']
  assert bytes(h.u.mem_read(h.get(o+0x14),w*ht*5))==b''.join(d['tiles'])
  assert h.get(o+0x24)==ht
  for y in range(ht):assert bytes(h.u.mem_read(h.get(h.get(o+0x20)+4*y),w))==bytes([7])*w
  rows.append(dict(map=p.name,sha256=digest,bytes_consumed=pos,width=w,height=ht,sprites=len(d['sprites']),result='full native LoadMap returns1; both background arrays/tile records/random animation rows match',match=True))
 for n in (0,4,12,20):
  h,ok,pos,reads=load((ROOT/'MAP/01.mrc').read_bytes()[:n]);assert ok==0
  rows.append(dict(map='truncated_'+str(n),sha256='',bytes_consumed=pos,width='',height='',sprites='',result='complete native loader returns0',match=True))
 write('맵_전체로더_원본명령대조.csv',rows)
 result=dict(cases=len(rows),distinct_maps=len(seen),mismatches=0,scope='complete native LoadMap through return; CFile/allocator/CPtrArray resize/time/srand/rand modeled; rand supplies7; raw maps unmodified; no Windows or graphics',binary_sha256=hashlib.sha256((ROOT/'DManage.dll').read_bytes()).hexdigest())
 (OUT/'맵_전체로더검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
