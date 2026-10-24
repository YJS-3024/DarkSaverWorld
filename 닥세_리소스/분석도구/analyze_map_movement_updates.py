"""Real MRC tile samples, native tile lookup, movement-step and pixel offsets."""
import collections,hashlib,json,struct,subprocess
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,PE,write
from decode_visuals import read_mrc
MOVE={1:(-1,0),2:(1,0),3:(0,-1),4:(0,1),5:(-1,-1),6:(-1,1),7:(1,-1),8:(1,1)}
PIXELS={1:(-16,0),2:(16,0),3:(0,-16),4:(0,16),5:(-10,-10),6:(-10,10),7:(10,-10),8:(10,10),9:(0,0)}
def fixture():
 h=Harness('userunit.dll');h.put(0x1010a3a0,0x30008000);u=0x30008000+3*736
 h.put(u,1,1);h.put(u+0x70,4,4);h.put(u+0x100,1);h.put(u+0x120,0xffffffff);h.put(u+0x168,77)
 return h,u

def movement(direction,phase,enabled=1,next_direction=1):
 h,u=fixture();h.put(u+0x11c,direction);h.put(u+0x170,phase);h.put(0x1017fa10,enabled)
 h.u.mem_write(0x100e32a0,b'\xff'*160000);h.put(0x100e32a0+4*(200*4+4),3)
 dx,dy=MOVE[direction];h.put(0x100e32a0+4*(200*(4+dx)+4+dy),88)
 h.stubs={0x1000f870:lambda:h.ret(next_direction),0x10004b80:lambda:h.ret(),0x1001184c:lambda:h.ret(),0x10002330:lambda:h.ret()}
 def stop(uc,a,n,d):
  if a==0x100105d8:uc.emu_stop()
 h.u.hook_add(UC_HOOK_CODE,stop);h.put(0x20010000,0x300ff000,3);h.u.reg_write(UC_X86_REG_ESP,0x20010000)
 h.u.emu_start(0x100101f0,0x300ff000,count=100000);assert h.u.reg_read(UC_X86_REG_EIP)==0x100105d8
 return h,u

def main():
 rows=[];maps=[];seen=set();p=PE(ROOT/'DManage.dll');lookup=p.data(0x1000aa00,0x3c)
 # Getter has only local relative branches, safe to copy verbatim; no rewritten arithmetic.
 for path in sorted((ROOT/'MAP').iterdir()):
  if path.suffix.lower() not in ('.mrc','.mrtc'):continue
  digest=hashlib.sha256(path.read_bytes()).hexdigest()
  if digest in seen:continue
  seen.add(digest);data=read_mrc(path);w,ht=data['width'],data['height'];raw=b''.join(data['tiles']);counts=collections.Counter(t[3] for t in data['tiles'])
  maps.append(dict(file=path.name,sha256=digest,width=w,height=ht,tile_count=len(data['tiles']),attribute_byte_counts=json.dumps(dict(sorted(counts.items()))),raw_attribute_255=counts[255]))
  h=Harness('userunit.dll');h.u.mem_write(0x300e0000,lookup);h.u.mem_write(0x10011768,b'\xe9'+struct.pack('<i',0x300e0000-(0x10011768+5)))
  h.put(0x1010a3e4,0x30000000);h.put(0x3000004c,0x30001000);h.put(0x30001014,0x30020000);h.put(0x30001030,w,ht)
  assert len(raw)<0xb0000;h.u.mem_write(0x30020000,raw)
  samples={(0,0),(w-1,ht-1),(w//2,ht//2),(-1,0),(w,0),(0,ht)}
  for x,y in samples:
   h.run(0x10004f30,(x&0xffffffff,y&0xffffffff))
   got=h.u.reg_read(UC_X86_REG_EAX)
   expect=data['tiles'][y*w+x][3] if 0<=x<w and 0<=y<ht else 0
   if expect==255:expect=0
   assert got==expect,(path.name,x,y,got,expect)
   rows.append(dict(case=f'map_{path.name}_{x}_{y}',result='original userunit attribute call + copied native DManage getter match MRC byte3',match=True))
  h.put(0x30000080,1,1);h.run(0x10004f30,(0,0));expect=data['tiles'][w+1][3] if w>1 and ht>1 else 0
  assert h.u.reg_read(UC_X86_REG_EAX)==(0 if expect==255 else expect)
  rows.append(dict(case='map_offset_'+path.name,result='scene+80/+84 offsets applied before row-major lookup',match=True))
 # Execute native path postprocessor and list primitives; replace only pool allocation/free.
 for source,expected in [([9,1,3],[5,9]),([9,3,1],[5,9]),([9,2,4],[8,9]),([9,2,3],[7,9]),([9,1,4],[6,9]),([9,1,1],[1,1,9])]:
  h,u=fixture();lst=u+0x12c;h.put(lst+0x18,10)
  def pool():
   sp=h.u.reg_read(UC_X86_REG_ESP);ptr,n,size=[h.get(sp+i) for i in (4,8,12)]
   block=h.heap;h.heap+=4+n*size+32;h.put(block,h.get(ptr));h.put(ptr,block);h.ret(block,clean=12)
  h.stubs={0x1001185e:pool,0x1001184c:lambda:h.ret()}
  for direction in source:h.run(0x1000f7f0,(lst,direction))
  h.run(0x1000d720,(lst,));got=[]
  while h.get(lst+0xc):
   h.run(0x1000f870,(lst,));got.append(h.u.reg_read(UC_X86_REG_EAX))
  assert got==expected,(source,got,expected)
  rows.append(dict(case='postprocess_'+str(source),result='native list traversal/order and diagonal merge -> '+str(got),match=True))
 for direction in range(1,9):
  for phase in (0,1,2):
   h,u=movement(direction,phase);threshold=2 if direction>=5 else 1;completed=phase>=threshold
   dx,dy=MOVE[direction];expected=(4+dx,4+dy) if completed else (4,4)
   assert (h.get(u+0x70),h.get(u+0x74))==expected
   assert h.get(u+0x170)==(0 if completed else phase+1)
   old=h.get(0x100e32a0+4*(200*4+4));new=h.get(0x100e32a0+4*(200*(4+dx)+4+dy))
   assert (old,new)==((77,3) if completed else (3,88))
   if completed:assert h.get(u+0x168)==88 and h.get(u+0x148)==direction
   rows.append(dict(case=f'step_{direction}_{phase}',result='logical position, phase, old occupant restoration/new occupant backup matched',match=True))
  h,u=movement(direction,2,enabled=0);assert (h.get(u+0x70),h.get(u+0x74),h.get(u+0x170))==(4,4,2)
  rows.append(dict(case='paused_'+str(direction),result='global stage gate preserves movement',match=True))
 for direction in range(1,10):
  for phase in (0,1,2):
   h,u=fixture();h.put(u+0x11c,direction);h.put(u+0x170,phase);h.stubs[0x1000f8e0]=lambda:h.ret(1)
   h.run(0x1000f300,(3,0x30001000,0x30001004,0x30001008))
   got=struct.unpack('<ii',h.u.mem_read(0x30001000,8));scale=PIXELS[direction]
   assert got==(scale[0]*phase,scale[1]*phase) and h.get(0x30001008)==1
   rows.append(dict(case=f'offset_{direction}_{phase}',result='native screen displacement matches phase coefficient',match=True))
 for direction in (1,5):
  h,u=movement(direction,2,next_direction=9)
  assert h.get(u+0x100)==0x10 and h.get(u+0x170)==0 and h.get(u+0x16c)==0
  rows.append(dict(case='finish_'+str(direction),result='sentinel9 clears moving/20 flags, sets10, resets phase/time',match=True))
 write('이동_실제맵_속성분포.csv',maps);write('이동_맵연결_보간_원본명령대조.csv',rows)
 evidence=[]
 for name,a,b in [('DManage.dll',0x1000aa00,0x1000aa3c),('userunit.dll',0x10004f30,0x10004f80),('userunit.dll',0x100101f0,0x100105d8),('userunit.dll',0x1000f300,0x1000f431),('userunit.dll',0x1000f4f0,0x1000f8d6),('userunit.dll',0x1000d720,0x1000dbac)]:
  evidence.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/name)],text=True))
 (OUT/'이동_맵연결_보간_근거.asm.txt').write_text('\n'.join(evidence))
 result=dict(cases=len(rows),distinct_maps=len(maps),tiles=sum(m['tile_count'] for m in maps),mismatches=0,binary_sha256={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in ('userunit.dll','DManage.dll')},scope='MRC records parsed, native tile getter copied without modifications; original attribute lookup/position/occupancy/phase/screen offset instructions; native path postprocess/list primitives with pool allocation/free modeled; step fixtures model next-direction pop, UI, visibility; stop before unrelated action branches',limits='not original map loader execution or full render; actual wall-clock update interval not established',movement_directions=MOVE,pixel_coefficients=PIXELS)
 (OUT/'이동_맵연결_보간검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k not in ('movement_directions','pixel_coefficients','binary_sha256')},ensure_ascii=False))
if __name__=='__main__':main()
