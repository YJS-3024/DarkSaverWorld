"""Follow map positions and DManage Load branches with modeled external effects."""
import json,struct
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 rows=[]
 def result(label,detail):rows.append(dict(case=label,detail=str(detail),match=True))
 # Full native Load wrapper. Children which allocate graphics/parse files are modeled.
 for cached,same,opens,parses in [(False,False,False,False),(False,False,True,False),(False,False,True,True),(True,True,False,False),(True,False,False,True)]:
  h=Harness('DManage.dll');obj=0x30000000;name=0x30004000;old=0x30005000;calls=[]
  h.u.mem_write(name,b'Map\\01.mrc\0');h.u.mem_write(old,b'Map\\'+(b'01' if same else b'02')+b'.mrc\0')
  h.put(obj+0x30,old,int(cached))
  def stub(label,value=0,clean=0):
   def run():calls.append(label);h.ret(value,clean)
   return run
  h.stubs={0x1000beb8:stub('file_ctor'),0x1000beb2:stub('open',int(opens),12),0x1000be94:stub('close'),0x1000beac:stub('file_dtor'),0x1000bebe:stub('string_dtor'),0x1000bec4:stub('string_copy',0,4),0x10008f80:stub('read_resource_ids',0,4),0x10008200:stub('prepare_resources',0,8),0x10008b60:stub('resource_path',0,12),0x10007ba0:stub('parse_mrc',int(parses),4),0x10007a50:stub('post_load'),0x1000bf18:stub('save_name',0,4)}
  h.run(0x10007c50,(name,),obj)
  expected=bool(cached and same or opens and parses)
  assert h.u.reg_read(UC_X86_REG_EAX)==int(expected),(calls,expected)
  assert bool(h.get(obj+0x34))==expected
  if cached and same:assert 'open' not in calls
  result(f'load_{cached}_{same}_{opens}_{parses}',dict(ok=expected,calls=calls))
 # Original 2B decoding plus world virtual+94 implementation, including camera origin.
 for x,y,unit_present in [(202,303,True),(-2,-3,True),(202,303,False)]:
  h=Harness('Saver.exe');main=0x30000000;stage=0x30002000;vt=0x30003000;unit=0x30007000;calls=[]
  h.put(main+0xec,stage);h.put(stage,vt);h.put(vt+0x94,0x431870);h.put(vt+0x88,0x300f0100)
  h.put(0x30005004,0x30006000)
  h.u.mem_write(0x30001000,b'\x2b\x01'+struct.pack('<hhhhB',101,x,y,404,1))
  def no_op(clean=0):return lambda:h.ret(clean=clean)
  def capture_state():
   sp=h.u.reg_read(UC_X86_REG_ESP);calls.append([h.get(sp+4+4*i) for i in range(6)]);h.ret()
  h.put(0x446680,0x300f0200);h.put(0x44667c,0x300f0300)
  h.stubs={0x4408a2:lambda:h.ret(0x30005000),0x43afc0:no_op(4),0x42ebf0:no_op(8),0x431820:lambda:h.ret(unit if unit_present else 0),0x300f0100:no_op(4),0x44085a:no_op(),0x440848:no_op(),0x440890:no_op(),0x300f0200:no_op(),0x300f0300:capture_state}
  h.run(0x43c170,(0x30001000,),main)
  got=(struct.unpack('<H',h.u.mem_read(stage+0x78,2))[0],h.get(stage+0x7c),h.get(stage+0x80))
  assert got==(404,x&0xffffffff,y&0xffffffff),got
  if unit_present:
   assert struct.unpack('<h',h.u.mem_read(unit+6,2))[0]==404
   assert struct.unpack('<hh',h.u.mem_read(unit+12,4))==(x,y)
   assert (h.get(stage+0xac),h.get(stage+0xb0))==((x-10)&0xffffffff,(y-7)&0xffffffff)
   assert len(calls)==1 and [v&0xffff for v in calls[0]][::3]==[404,y&0xffff],calls
  else:assert not calls
  result(f'world_position_{x}_{y}_{unit_present}',dict(stage=got,camera=(x-10,y-7) if unit_present else None))
 # Stage absent during receipt: three words retained, then replayed once.
 for x,y in [(202,303),(-2,-3)]:
  h=Harness('Saver.exe');main=0x30000000;stage=0x30002000;vt=0x30003000;seen=[]
  h.put(0x30005004,0x30006000)
  h.u.mem_write(0x30001000,b'\x2b\x01'+struct.pack('<hhhhB',101,x,y,404,2))
  h.stubs={0x4408a2:lambda:h.ret(0x30005000),0x43afc0:lambda:h.ret(clean=4)}
  h.run(0x43c170,(0x30001000,),main)
  assert [struct.unpack('<h',h.u.mem_read(a,2))[0] for a in [0x4532e0,0x4532de,0x4532dc]]==[404,x,y]
  h.put(stage,vt);h.put(vt+0x94,0x300f0100)
  def replay():
   sp=h.u.reg_read(UC_X86_REG_ESP);seen.append([h.get(sp+4+4*i)&0xffff for i in range(3)]);h.ret(clean=12)
  h.stubs[0x300f0100]=replay
  h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.reg_write(UC_X86_REG_ECX,stage)
  h.u.emu_start(0x43c540,0x43c56e,count=1000)
  assert seen==[[404,x&0xffff,y&0xffff]] and bytes(h.u.mem_read(0x4532e0,2))==b'\xff\xff'
  result(f'deferred_position_{x}_{y}',seen)
 # Real parser until disk payload has been read. Later random/UI setup excluded.
 def parse(data,open_ok=True,allocation_ok=True):
  h=Harness('DManage.dll');obj=0x30000000;sp=0x20010000;pos=0;reads=[];status=[]
  h.put(sp,0x300ff000,0x30004000,0);h.u.reg_write(UC_X86_REG_ESP,sp);h.u.reg_write(UC_X86_REG_ECX,obj)
  def read():
   nonlocal pos
   s=h.u.reg_read(UC_X86_REG_ESP);dst,n=h.get(s+4),h.get(s+8);part=data[pos:pos+n];pos+=len(part)
   if part:h.u.mem_write(dst,part)
   reads.append((n,len(part)));h.ret(len(part),8)
  def seek():
   nonlocal pos
   s=h.u.reg_read(UC_X86_REG_ESP);offset=h.get(s+4);origin=h.get(s+8);pos=offset if origin==0 else pos+offset if origin==1 else len(data)+offset;h.ret(pos,8)
  def stop():status.append(True);h.u.emu_stop()
  allocptr=0x300f0400;h.put(0x1000d1b4,allocptr)
  h.stubs={0x1000beb8:lambda:h.ret(),0x1000beb2:lambda:h.ret(int(open_ok),12),0x1000beac:lambda:h.ret(),0x1000be94:lambda:h.ret(),0x1000be9a:read,0x1000bfb4:seek,0x1000bea0:h.alloc,allocptr:h.alloc if allocation_ok else lambda:h.ret(0),0x1000a7ed:stop}
  h.u.emu_start(0x1000a580,0x300ff000,count=100000)
  return h,bool(status),reads
 from analyze_special_skills import ROOT
 maps=sorted((ROOT/'MAP').glob('*.mrc'))
 for p in maps:
  h,ok,reads=parse(p.read_bytes());assert ok,(p.name,reads)
  width,height,n=struct.unpack('<3I',h.u.mem_read(0x30000030,12))
  assert reads[-2:]==[(width*height*5,width*height*5),(n*16,n*16)]
 result('actual_mrc_payloads',dict(files=len(maps),all_payload_reads_reached=True))
 data=(ROOT/'MAP'/'01.mrc').read_bytes()
 for label,payload,opens,alloc,expected in [('open_fail',data,False,True,False),('empty',b'',True,True,False),('truncated_header',data[:20],True,True,False),('truncated_tile',data[:-1],True,True,False),('allocation_fail',data,True,False,False)]:
  h,ok,reads=parse(payload,opens,alloc);assert ok==expected,(label,ok,reads)
  result('mrc_'+label,dict(payload_read=ok,reads=reads))
 write('맵진입_추가_원본명령대조.csv',rows)
 (OUT/'맵진입_추가검증.json').write_text(json.dumps(dict(cases=len(rows),actual_mrc_files=len(maps),mismatches=0,scope='Native wrapper and position code; MRC payload parsing before random setup; filesystem/UI modeled'),ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),actual_mrc_files=len(maps),mismatches=0)))
if __name__=='__main__':main()
