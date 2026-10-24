"""Native flood/gradient path functions with controlled tile-provider fixtures."""
import hashlib,heapq,json,random,struct,subprocess
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EAX
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write
GRID=0x30010000
DIRECTIONS={1:(1,0),2:(-1,0),3:(0,1),4:(0,-1)}
def signed(h,a):return struct.unpack('<i',h.u.mem_read(a,4))[0]
def build(mode,origin,budget,costs,occupants,unit=12):
 h=Harness('userunit.dll');h.put(0x100e3298,8,8);h.put(0x1010a3d8,mode)
 h.u.mem_write(0x100e32a0,b'\xff'*160000)
 for (x,y),v in occupants.items():h.put(0x100e32a0+4*(200*x+y),v&0xffffffff)
 h.run(0x10004ea0,(GRID,));h.put(0x10012150,0x300f0000)
 # Keep native flood, occupancy lookup and arithmetic. Only map tile/table providers modeled.
 def tile():
  sp=h.u.reg_read(UC_X86_REG_ESP);x=h.get(sp+4);y=h.get(sp+8);h.ret(8*x+y)
 def attr():
  sp=h.u.reg_read(UC_X86_REG_ESP);ident=h.get(sp+8);x,y=divmod(ident,8)
  h.put(0x30004008,costs.get((x,y),1)&0xffffffff);h.ret(0x30004000)
 h.stubs={0x10004f30:tile,0x300f0000:attr}
 h.run(0x10004c40,(*origin,budget,unit,0,GRID))
 return h

def reference(mode,origin,budget,costs,occupants,unit=12):
 out={};x,y=origin
 if not ((0<x<7 and 0<y<7) if mode else (0<=x<=8 and 0<=y<=8)):return out
 out[origin]=budget+1;queue=[(-budget,*p) for p in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]];heapq.heapify(queue)
 while queue:
  negative,x,y=heapq.heappop(queue);v=-negative
  if not ((0<x<7 and 0<y<7) if mode else (0<=x<8 and 0<=y<8)):continue
  if v<=0 or v<=out.get((x,y),-1):continue
  cost=costs.get((x,y),1);occupant=occupants.get((x,y),-1)
  if cost in (99,-99) or (occupant>=0 and occupant//50!=unit//50):continue
  out[x,y]=v
  if v-cost>0:
   for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:heapq.heappush(queue,(-(v-cost),nx,ny))
 return out

def main():
 rows=[];examples=[]
 rng=random.Random(7331)
 cases=[]
 for mode in (0,1):
  for origin in ((4,4),(0,0),(7,7)):
   for budget in (0,1,4):cases.append((mode,origin,budget,{},{}))
  for cost in (0,1,2,3,99,-99):cases.append((mode,(4,4),4,{(3,4):cost},{}))
  for occupant in (-1,0,12,49,50,99,100):cases.append((mode,(4,4),4,{}, {(3,4):occupant}))
  for i in range(16):
   costs={(x,y):rng.choice([1,1,2,3,99,-99]) for x in range(8) for y in range(8)}
   occupied={(rng.randrange(8),rng.randrange(8)):rng.choice([0,49,50,100]) for _ in range(4)}
   cases.append((mode,(rng.randrange(1,7),rng.randrange(1,7)),rng.randrange(1,6),costs,occupied))
 for i,(mode,origin,budget,costs,occupants) in enumerate(cases):
  h=build(mode,origin,budget,costs,occupants);expected=reference(mode,origin,budget,costs,occupants)
  for x in range(8):
   for y in range(8):assert signed(h,GRID+4*(200*x+y))==expected.get((x,y),-1),(i,x,y)
  rows.append(dict(case='flood_'+str(i),mode=mode,origin=str(origin),budget=budget,result='64 cells match independent weighted flood',match=True))
 # Native setunit getter: normalize encoded tile attribute, retain 180-byte row stride.
 for category in (0,3,9):
  for tile_id in (-1,0,1,27,28,127,128,129,155,156,256):
   h=Harness('setunit.dll');h.run(0x10002b50,(category,tile_id&0xffffffff))
   normalized=tile_id-128 if tile_id>127 else tile_id
   if normalized<0 or normalized>27:normalized=0
   assert h.u.reg_read(UC_X86_REG_EAX)==0x10265200+180*normalized+12*category
   rows.append(dict(case=f'attribute_pointer_{category}_{tile_id}',mode='n/a',origin='n/a',budget='n/a',result='native tile normalization and category triple address matched',match=True))
 # Source->goal reconstruction over the native generated field. List plumbing intercepted only.
 for phase in (0,1):
  for start in ((1,4),(4,1),(6,4),(4,6),(2,2),(4,4)):
   h=build(0,(4,4),6,{},{});h.put(0x1010a3a0,0x30008000);h.put(0x1001516c,phase);steps=[]
   def append():steps.append(h.get(h.u.reg_read(UC_X86_REG_ESP)+8));h.ret()
   h.stubs[0x1001184c]=lambda:h.ret();h.stubs[0x1000f7f0]=append;h.put(0x30007000,0)
   h.run(0x1000d2c0,(*start,0,4,4,GRID,0x30007000))
   assert h.u.reg_read(UC_X86_REG_EAX)==1 and steps[0]==9
   x,y=start
   for d in steps[1:]:dx,dy=DIRECTIONS[d];x+=dx;y+=dy
   assert (x,y)==(4,4) and len(steps)-1==abs(start[0]-4)+abs(start[1]-4)
   assert h.get(0x30007000)==len(steps)-1
   examples.append(dict(initial_phase=phase,start=str(start),goal='(4, 4)',directions=','.join(map(str,steps)),step_count=len(steps)-1))
   rows.append(dict(case=f'path_{phase}_{start}',mode=0,origin=str(start),budget=6,result='native reconstruction reaches goal; cardinal minimum steps on uniform field',match=True))
 for start in ((-1,4),(8,4),(1,1)):
  h=build(0,(4,4),1,{},{});h.put(0x1010a3a0,0x30008000);steps=[]
  def append_fail():steps.append(h.get(h.u.reg_read(UC_X86_REG_ESP)+8));h.ret()
  h.stubs[0x1001184c]=lambda:h.ret();h.stubs[0x1000f7f0]=append_fail
  h.run(0x1000d2c0,(*[v&0xffffffff for v in start],0,4,4,GRID,0x30007000))
  assert h.u.reg_read(UC_X86_REG_EAX)==0 and steps==[9]
  rows.append(dict(case='unreachable_'+str(start),mode=0,origin=str(start),budget=1,result='out-of-range or unreachable rejected after path reset/sentinel9',match=True))
 write('이동_경로탐색_원본명령대조.csv',rows)
 write('이동_경로탐색_방향표본.csv',examples)
 ranges=[(0x10004c40,0x10004f80),(0x1000d2c0,0x1000d720),(0x1000f7f0,0x1000f86f)]
 (OUT/'이동_경로탐색_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'userunit.dll')],text=True) for a,b in ranges))
 (OUT/'이동_경로탐색_근거.asm.txt').write_text((OUT/'이동_경로탐색_근거.asm.txt').read_text()+subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel','--start-address=0x10002b50','--stop-address=0x10002b80',str(ROOT/'setunit.dll')],text=True))
 result=dict(cases=len(rows),flood_cases=len(cases),flood_cells_checked=64*len(cases),mismatches=0,binary_sha256={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('userunit.dll','setunit.dll')},scope='native reset/recursive weighted flood/occupancy lookup/gradient path; map tile and attribute providers, list append/free modeled; no full Windows runtime',directions=DIRECTIONS,blocked_costs=[99,-99],occupancy='nonnegative occupied ID /50 must match moving ID /50; do not infer faction name',limitations='synthetic nonnegative costs except blocked -99; not all production maps or negative cost behavior; uniform-cost path optimality only')
 (OUT/'이동_경로탐색검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
