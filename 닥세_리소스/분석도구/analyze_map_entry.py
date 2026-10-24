"""Map-entry native branch fixtures; external UI/filesystem operations are modeled."""
import json, struct
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT, write

def main():
 rows=[]
 def record(case, detail): rows.append(dict(case=case, detail=detail, match=True))
 h=Harness('Saver.exe')
 for address,msg,handler in [(0x448a50,0x403,0x43c5d0),(0x448a68,0x404,0x43ca60),(0x448b58,0x4cd,0x43c5f0)]:
  assert h.get(address)==msg and h.get(address+20)==handler
  record('message_'+hex(msg),hex(handler))
 # WM404: transfer lifecycle and stored callback, rather than direct scene finish.
 for present,queue in [(False,False),(True,False),(True,True)]:
  h=Harness('Saver.exe'); main=0x30000000; obj=0x30002000; vt=0x30003000; cb=0x300f0100; dtor=0x300f0200; seen=[]
  h.put(main+0x148,obj if present else 0);h.put(main+0x14c,cb)
  h.put(obj,vt);h.put(vt,dtor)
  if queue:
   h.put(main+0x150,0x30004000);h.put(0x30004000,vt,2,2)
  def destroy():seen.append('destroy');h.ret(clean=4)
  def callback():seen.append('callback');h.ret()
  h.stubs={dtor:destroy,cb:callback}
  h.run(0x43ca60,(0,0),main)
  assert seen==([] if not present else ['destroy']*(2 if queue else 1)+['callback'])
  if present: assert h.get(main+0x148)==h.get(main+0x14c)==h.get(main+0x150)==0
  record(f'transfer_completion_{present}_{queue}',','.join(seen))
 # Original map viewport assignments: start after successful Load(), before CString cleanup.
 for enabled,bounds,expected in [(0,(3,4,12,13),(0,0,40,50)),(1,(3,4,12,13),(3,4,12,13)),(1,(-3,-4,12,13),(0,0,12,13))]:
  h=Harness('Saver.exe'); main=0x30000000; manager=0x30002000; meta=0x30003000
  h.put(0x451290,manager);h.put(0x4533e0,main);h.put(manager+0x4c,meta);h.put(meta+0x30,40,50)
  h.put(main+0x164,enabled,*[v&0xffffffff for v in bounds])
  h.u.reg_write(UC_X86_REG_ESP,0x20010000)
  h.u.emu_start(0x429859,0x4298df,count=1000)
  got=struct.unpack('<4I',h.u.mem_read(manager+0x80,16));assert got==expected,(got,expected)
  record('viewport_'+str(enabled)+'_'+str(bounds),str(got))
 # Native SetCurrentScenario: explicit stub boundary for allocation and script processing.
 h=Harness('GameCtl.dll');seen=[]
 def mark(label,clean=0):
  def run():seen.append(label);h.ret(clean=clean)
  return run
 h.stubs={0x100146a0:mark('assign_name',4),0x10007e60:mark('reset_objects'),0x1000bcf0:mark('process_scenario')}
 h.run(0x10008040,(0x30001000,));assert seen==['assign_name','reset_objects','process_scenario'] and h.get(0x1006c084)==1
 record('set_scenario_order',','.join(seen))
 # Native branch fragment: extracted script empty/nonempty and DEO filename.
 for nonempty,deo,script_ok,expected in [(False,True,False,0x1000bd30),(True,True,False,0x1000fbf8),(True,True,True,0x1000bd30),(False,False,False,0x1000fbf8)]:
  h=Harness('GameCtl.dll');sp=0x20010000; string=0x30002008;compare=0x300f0300;hit=[]
  h.put(sp+0x10,string);h.put(string-8,int(nonempty));h.put(sp+0x60,0x30004000)
  for reg,val in [(UC_X86_REG_ESP,sp),(UC_X86_REG_ESI,0),(UC_X86_REG_EBX,compare),(UC_X86_REG_EBP,0x30005000)]: h.u.reg_write(reg,val)
  h.stubs[compare]=lambda:h.ret(0 if deo else 1)
  h.stubs[0x1000f6c0]=lambda:h.ret(int(script_ok))
  def stop(address):
   def run():hit.append(address);h.u.emu_stop()
   return run
  for address in [0x1000bd30,0x1000fbf8,0x1000fd48]:h.stubs[address]=stop(address)
  h.u.emu_start(0x1000fb73,0x1000fe97,count=1000)
  assert hit==[expected],(hit,expected)
  record(f'script_branch_{nonempty}_{deo}_{script_ok}',hex(expected))
 write('맵진입_원본명령대조.csv',rows)
 (OUT/'맵진입_검증.json').write_text(json.dumps(dict(cases=len(rows),mismatches=0,scope='Isolated native fragments; external callbacks modeled; no full Windows session'),ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),mismatches=0)))
if __name__=='__main__':main()
