"""Single-player relevant packet consumers: absolute stats, movement rollback, death and status flags."""
import struct,json
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write,ROOT
import subprocess,hashlib

def main():
 rows=[];maps=[]
 def fixture(active=1):
  h=Harness('userunit.dll');base=0x30000000;unit=base+3*736;h.put(0x1010a3a0,base);h.put(unit,active);return h,unit
 def record(label,detail):rows.append(dict(case=label,detail=str(detail),match=True))
 for entry,offset,kind in [(0x1000cd80,0x20,0),(0x1000ce60,0x28,1)]:
  for old,new in [(100,75),(75,100),(100,0),(100,150),(100,-1),(0,32767)]:
   h,u=fixture();h.put(u+offset,old);h.put(u+0x100,0x1000);h.put(u+0x15c,99);h.put(u+0x2ac,99)
   h.run(entry,(struct.unpack('<I',struct.pack('<hh',3,new))[0],))
   assert h.get(u+offset)==new&0xffffffff
   assert h.get(u+0x160)==(new-old)&0xffffffff
   assert h.get(u+0x15c)==0 and h.get(u+0x100)==0x1040 and h.get(u+0x2ac)==kind
   record(f'stat_{kind}_{old}_{new}',dict(value=new,display_delta=new-old,type=kind))
 # These display updates share one delta/type slot; they are not a retained event queue.
 h,u=fixture();h.put(u+0x20,100);h.put(u+0x28,50)
 h.run(0x1000cd80,(struct.unpack('<I',struct.pack('<hh',3,75))[0],))
 h.run(0x1000ce60,(struct.unpack('<I',struct.pack('<hh',3,40))[0],))
 assert h.get(u+0x20)==75 and h.get(u+0x28)==40 and h.get(u+0x160)==0xfffffff6 and h.get(u+0x2ac)==1
 h.run(0x1000ce10,(3,));assert h.get(u+0x2ac)==2 and h.get(u+0x160)==0xfffffff6
 record('hp_mp_fail_sequence',dict(hp=75,mp=40,last_delta=-10,last_display_type=2))
 for value in [0,50,100000,0xffffffff]:
  h,u=fixture();h.put(u+8,12345);h.run(0x1000cdf0,(3,value));assert h.get(u+8)==value
  record('absolute_exp_'+str(value),value)
 for old,new in [((7,8),(9,10)),((7,8),(7,8)),((0,0),(1,0))]:
  h,u=fixture();h.put(u+0x70,*old);h.put(u+0x100,0x81);h.put(u+0x10c,0x10)
  def cell(p):return 0x100e32a0+4*(200*p[0]+p[1])
  h.put(cell(old),3);h.put(cell(new),3 if old==new else 0xffffffff)
  h.run(0x1000dc70,(3,*new))
  assert (h.get(u+0x70),h.get(u+0x74))==new
  assert h.get(cell(new))==3
  if old!=new:assert h.get(cell(old))==0xffffffff
  assert h.get(u+0x100)==0xa0 and h.get(u+0x10c)==0x11
  record('move_fail_'+str(old)+'_'+str(new),dict(position=new,state_flags='A0',path_flags='11'))
 for protected in [False,True]:
  h,u=fixture();h.put(u+0x70,7,8);h.put(u+0x100,0x80 if protected else 0)
  cell=0x100e32a0+4*(200*7+8);h.put(cell,3);h.run(0x1000efe0,(3,))
  assert h.get(u)==int(protected) and h.get(cell)==(3 if protected else 0xffffffff)
  record('remove_unit_protected_'+str(protected),dict(active=h.get(u),occupancy=h.get(cell)))
 h,u=fixture();h.put(u+0x100,0x20);h.run(0x1000f030,(3,));assert h.get(u)==1 and h.get(u+0x100)==0xa0
 h.run(0x1000efe0,(3,));assert h.get(u)==1
 h.put(u+0x100,h.get(u+0x100)&~0x80);h.put(u+0x70,7,8);h.run(0x1000efe0,(3,));assert h.get(u)==0
 record('mark_then_remove_then_clear',dict(mark_does_not_remove=True,protected_remove_ignored=True,clear_then_remove=True))
 # Exact wire status-bit permutation. Input bits16..31 are not consumed here.
 bit_map=[0x40,0x20,0x4,0x80,0x10,0x8,0x1,0x100,0x200,0x400,0x800,0x1000,0x2000,0x4000,0x2,0x8000]
 values=list(range(256))+[1<<b for b in range(8,32)]+[0xffff,0x55aa,0xaa55,0xffffffff]
 for value in values:
  h,u=fixture();h.put(u+0x104,0xdeadbeef);h.run(0x1000c930,(3,value))
  expected=sum(out for bit,out in enumerate(bit_map) if value&(1<<bit));assert h.get(u+0x104)==expected
  record('status_'+hex(value),hex(expected))
 for bit,out in enumerate(bit_map):maps.append(dict(wire_mask=hex(1<<bit),runtime_offset='0x104',runtime_mask=hex(out),operation='replace mapped state; upper wire bits ignored'))
 h,u=fixture(0);h.put(u+0x104,0xdeadbeef);h.run(0x1000c930,(3,0xffff));assert h.get(u+0x104)==0xdeadbeef
 record('status_inactive_unit',dict(unchanged=True))
 for entry in [0x1000efe0,0x1000f030]:
  h,u=fixture();h.run(entry,(0xffffffff,));assert h.get(u)==1
  record('negative_unit_guard_'+hex(entry),dict(unchanged=True))
 # 5C52 is both an action-mask replacement and a clamped tick update.
 tick_map=[0x2,0x10,0x8,0x20,0x1,0x4,0x40,0x80]
 for flags in range(256):
  tick=[-5,0,4,20][flags%4];h,u=fixture();h.put(u+0x5c,10,99);h.put(u+0x100,0x4004);h.put(u+0x10c,0xffffffff);h.put(u+0x64,1)
  h.run(0x1000c720,(struct.unpack('<I',struct.pack('<hh',3,tick))[0],flags))
  expected=sum(out for bit,out in enumerate(tick_map) if flags&(1<<bit))
  expected_state=0x4004|(0x10 if flags&1 else 0)|(0x20 if flags&0x10 else 0)
  assert h.get(u+0x10c)==expected and h.get(u+0x100)==expected_state and h.get(u+0x60)==min(max(tick,0),10)
  record('tick_menu_'+str(flags),dict(mask=hex(expected),tick=min(max(tick,0),10)))
 h,u=fixture(0);h.put(u+0x10c,0x1234);h.put(u+0x60,7);h.run(0x1000c720,(struct.unpack('<I',struct.pack('<hh',3,9))[0],0xff));assert h.get(u+0x10c)==0x1234 and h.get(u+0x60)==7
 record('tick_menu_inactive',dict(unchanged=True))
 write('싱글플레이_행동비트_대응.csv',[dict(wire_mask=hex(1<<bit),runtime_offset='0x10C',runtime_mask=hex(out),state_offset='0x100',state_mask=hex(0x10 if bit==0 else 0x20) if bit in [0,4] else '') for bit,out in enumerate(tick_map)])
 # Whole-unit transient reset and server-driven tick updates over all 3000 slots.
 h,u=fixture();h.u.mem_map(0x30100000,0x200000)
 last=0x30000000+2999*736
 for at in [u,last]:h.put(at,1);h.put(at+0x20,77);h.put(at+0x70,7,8);h.put(at+0x100,0xa1,0xffff)
 h.run(0x1000b070)
 for at in [u,last]:assert h.get(at)==1 and h.get(at+0x20)==77 and (h.get(at+0x70),h.get(at+0x74))==(7,8) and h.get(at+0x100)==h.get(at+0x104)==0
 record('initvalue_transient_only',dict(slots=3000,active_hp_position_preserved=True,flags_cleared=True))
 h,u=fixture();h.u.mem_map(0x30100000,0x200000)
 ticks=[(3,1,1,10,9,10),(4,1,0,10,9,9),(5,0,1,10,9,9),(6,1,1,10,20,10)]
 for idx,active,eligible,cap,old,expected in ticks:
  at=0x30000000+idx*736;h.put(at,active,eligible);h.put(at+0x5c,cap,old)
 h.run(0x1000c6b0)
 for idx,active,eligible,cap,old,expected in ticks:assert h.get(0x30000000+idx*736+0x60)==expected
 record('inctick_eligibility_cap',dict(slots=3000,cases=ticks))
 # Movement command branching; path-search and preprocessing boundaries are explicitly modeled.
 for active,busy,direction,target,path_ok,blocked in [(1,1,1,(9,10),False,False),(1,0,0,(9,10),False,False),(1,0,0,(9,10),True,False),(1,0,0,(9,10),False,True),(0,0,0,(9,10),False,False),(1,0,0,(0,0),False,False)]:
  h,u=fixture(active);h.put(u+0x70,7,8);h.put(u+0x100,busy);h.put(u+0x11c,direction);h.put(u+0x264,99)
  h.put(0x100e32a0+4*(200*7+8),3)
  h.put(0x101314ec+4*(200*target[0]+target[1]),0xffffffff if blocked else 0)
  h.stubs={0x10004ea0:lambda:h.ret(),0x10004c40:lambda:h.ret(),0x1000d2c0:lambda:h.ret(int(path_ok)),0x1000d720:lambda:h.ret()}
  h.run(0x1000d930,(3,*target))
  position=(h.get(u+0x70),h.get(u+0x74))
  if not active or target==(0,0):assert position==(7,8) and h.get(u+0x264)==99
  elif busy and direction:
   assert position==(7,8) and (h.get(u+0x120),h.get(u+0x124),h.get(u+0x128))==(3,18,20)
  elif path_ok and not blocked:assert position==(7,8) and h.get(u+0x100)&1
  else:assert position==target and h.get(0x100e32a0+4*(200*target[0]+target[1]))==3
  record(f'move_{active}_{busy}_{direction}_{target}_{path_ok}_{blocked}',dict(position=position,pathfinder_modeled=True))
 write('싱글플레이_상태갱신_원본명령대조.csv',rows);write('싱글플레이_상태비트_대응.csv',maps)
 ranges=[(0x1000cd80,0x1000ced2),(0x1000dc70,0x1000dd03),(0x1000efe0,0x1000f05d),(0x1000c930,0x1000cbb0),(0x10004ef0,0x10004f29),(0x1000b070,0x1000b09b),(0x1000c6b0,0x1000c930),(0x1000d930,0x1000dc44)]
 evidence=['userunit.dll sha256='+hashlib.sha256((ROOT/'userunit.dll').read_bytes()).hexdigest()]
 for a,b in ranges:evidence.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'userunit.dll')],text=True))
 (OUT/'싱글플레이_상태갱신_코드.asm.txt').write_text('\n'.join(evidence))
 result=dict(cases=len(rows),mismatches=0,scope='Original stat/status/remove/rollback/reset/tick consumers and occupancy writes; movement pathfinder/preprocessing modeled; no OS, renderer or server decisions')
 (OUT/'싱글플레이_상태갱신검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
