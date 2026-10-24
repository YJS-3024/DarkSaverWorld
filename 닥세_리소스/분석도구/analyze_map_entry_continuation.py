"""Native download callback, follow-up request, and optional map-resource branches."""
import struct,json
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 rows=[]
 def record(case,detail):rows.append(dict(case=case,detail=str(detail),match=True))
 # Full follow-up request builder; exact bytes and local busy flag gates.
 for mode,first,second in [(-1,False,False),(-1,True,False),(-1,False,True),(0,False,False),(2,False,False)]:
  h=Harness('Saver.exe');stage=0x30000000;h.u.mem_write(stage+0x78,struct.pack('<h',404));h.put(0x4533e0,0x30008000)
  h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:lambda:h.send(True),0x432b70:lambda:h.ret(int(first),8),0x432bc0:lambda:h.ret(int(second),8)}
  h.run(0x431a20,(202,303,mode&0xffffffff),stage)
  assert h.sent==[b'\x2c\x25'+struct.pack('<hhhh',404,202,303,mode)]
  busy=mode!=-1 or first or second;assert h.get(stage+0x148)==int(busy)
  record(f'request_{mode}_{first}_{second}',dict(payload=h.sent[0].hex(),busy=busy))
 # Whole download callback. Model file/UI services; execute actual branch and request code.
 for active,moved,mode in [(False,False,1),(True,True,1),(True,False,1),(True,False,0)]:
  h=Harness('Saver.exe');stage=0x30000000;main=0x30008000;dialog=0x30009000;vt=0x3000a000;position=0x3000b000;seen=[]
  h.put(0x451294,stage if active else 0);h.put(0x4533e0,main);h.put(stage+0xd8,dialog);h.put(dialog,vt);h.put(vt+4,0x300f0100)
  h.put(stage+0x11c,202,303,0x3000c000,mode);h.u.mem_write(stage+0x78,struct.pack('<h',404));h.put(position,203 if moved else 202,303)
  def mark(label,clean):
   def run():seen.append(label);h.ret(clean=clean)
   return run
  h.stubs={0x4408d2:lambda:h.ret(clean=4),0x440848:lambda:h.ret(),0x427600:mark('notice',8),0x431770:lambda:h.ret(position,8),0x426f40:mark('remove_2c',4),0x300f0100:mark('ui_17',8),0x427270:lambda:h.ret(0,4),0x440884:mark('save_name',4),0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:lambda:h.send(True)}
  h.run(0x4321d0)
  assert h.get(0x451294)==0
  expected=[b'\x2c\x25'+struct.pack('<hhhh',404,202,303,-1)] if active and not moved and mode else []
  assert h.sent==expected,(seen,h.sent)
  assert ('save_name' in seen)==(active and not moved and not mode)
  record(f'callback_{active}_{moved}_{mode}',dict(calls=seen,payloads=[p.hex() for p in h.sent]))
 # Full native Load wrapper: resource load failure does not itself force MRC failure.
 for already_loaded,resource_ok in [(False,False),(False,True),(True,False)]:
  h=Harness('DManage.dll');obj=0x30000000;name=0x30004000;ids=0x30005000;table=0x30006000;calls=[]
  h.u.mem_write(name,b'Map\\01.mrc\0');h.put(obj+0x5c,1,ids);h.put(ids,5);h.put(obj+0x3c,table);h.put(table+20,0x30007000 if already_loaded else 0)
  def stub(label,value=0,clean=0):
   def run():calls.append(label);h.ret(value,clean)
   return run
  h.stubs={0x1000beb8:stub('file_ctor'),0x1000beb2:stub('open',1,12),0x1000be94:stub('close'),0x1000beac:stub('file_dtor'),0x1000bebe:stub('string_dtor'),0x1000bec4:stub('string_copy',0,4),0x10008f80:stub('resource_ids',0,4),0x10008200:stub('prepare',0,8),0x10008b60:stub('path',0,12),0x10007aa0:stub('load_resource',int(resource_ok),4),0x10007e30:stub('build_resource',0,4),0x10007ba0:stub('parse_mrc',1,4),0x10007a50:stub('post_load'),0x1000bf18:stub('save_name',0,4)}
  h.run(0x10007c50,(name,),obj)
  assert h.u.reg_read(UC_X86_REG_EAX)==1 and h.get(obj+0x34)==1
  assert ('load_resource' in calls)==(not already_loaded)
  assert ('build_resource' in calls)==(not already_loaded and resource_ok)
  record(f'resource_{already_loaded}_{resource_ok}',dict(load_ok=True,calls=calls))
 write('맵진입_후속처리_원본명령대조.csv',rows)
 (OUT/'맵진입_후속처리검증.json').write_text(json.dumps(dict(cases=len(rows),mismatches=0,scope='Native request/callback/Load control; file, resource and UI effects modeled'),indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),mismatches=0)))
if __name__=='__main__':main()
