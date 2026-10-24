"""Native scenario response dispatch and rendering pointer preconditions, no OS execution."""
import struct,json
from unicorn import UcError,UC_ERR_READ_UNMAPPED
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 rows=[]
 def record(case,detail):rows.append(dict(case=case,detail=str(detail),match=True))
 for command,ready,choice in [(0x20,False,0),(0x20,True,0),(0x20,True,1),(0x20,True,2),(0x21,False,0),(0x21,True,0)]:
  h=Harness('Saver.exe');stage=0x30000000;main=0x30008000;dialog=0x30009000;vt=0x3000a000;seen=[]
  name=b'01';data=b'\x2c\x24'+struct.pack('<H',command)+bytes([len(name)])+name+struct.pack('<hh',0,choice)
  h.u.mem_write(0x30001000,data);h.put(0x4533e0,main);h.put(stage+0xd8,dialog);h.put(dialog,vt);h.put(vt+4,0x300f0100);h.put(stage+0x148,1)
  def preflight():
   sp=h.u.reg_read(UC_X86_REG_ESP);idx=h.get(sp+8);h.put(idx,h.get(idx)+2);seen.append(('preflight',h.get(sp+16)));h.ret(int(ready),16)
  def start():
   sp=h.u.reg_read(UC_X86_REG_ESP);seen.append(('request52',[h.get(sp+4+i*4) for i in range(4)]));h.ret()
  def ui():
   sp=h.u.reg_read(UC_X86_REG_ESP);seen.append(('ui',h.get(sp+4)));h.ret(1,8)
  h.stubs={0x44085a:lambda:h.ret(),0x440848:lambda:h.ret(),0x440728:h.alloc,0x44072e:lambda:h.ret(),0x431eb0:preflight,0x43d680:start,0x300f0100:ui,0x440872:lambda:h.ret(clean=4),0x427270:lambda:h.ret(0,4)}
  h.run(0x42f550,(0x30001000,len(data)),stage)
  assert h.u.reg_read(UC_X86_REG_EAX)==1 and h.get(stage+0x148)==0
  assert any(x[0]=='request52' for x in seen)==(command==0x20 and ready and choice!=1),seen
  if command==0x20 and ready and choice!=1:assert next(x[1] for x in seen if x[0]=='request52')[0:2]==[1,0]
  assert ('ui',0x37) in seen if command==0x20 and ready and choice==1 else ('ui',0x37) not in seen
  assert ('ui',0x17) in seen if command==0x21 and ready else ('ui',0x17) not in seen
  record(f'response_{command:x}_{ready}_{choice}',seen)
 # The base world dispatcher does not equate the outbound opcode with a response handler.
 h=Harness('Saver.exe');h.u.mem_write(0x30001000,b'\x2c\x25'+b'\0'*8)
 h.run(0x42f550,(0x30001000,10),0x30000000);assert h.u.reg_read(UC_X86_REG_EAX)==0
 record('world_2c25_unhandled',0)
 # Rendering fragment; remove harness low-page mapping to model Windows null dereferences.
 for missing in ['group','tile','surface_array','surface','none']:
  h=Harness('DManage.dll');h.u.mem_unmap(0,4096);manager=0x30000000;meta=0x30002000;plane=0x30003000;groups=0x30004000;group=0x30005000;tiles=0x30006000;tile=0x30007000;surfaces=0x30008000
  h.put(manager+0x4c,meta);h.put(meta+0x30,1);h.put(meta+0x10,plane);h.put(meta+0xc,plane);h.put(manager+0xc,groups)
  h.put(groups,0 if missing=='group' else group);h.put(group+4,tiles);h.put(tiles,0 if missing=='tile' else tile);h.put(tile+4,0 if missing=='surface_array' else surfaces);h.put(surfaces,0 if missing=='surface' else 0x30009000)
  sp=0x20010000;h.put(sp+0x10,0);h.u.reg_write(UC_X86_REG_ESP,sp);h.u.reg_write(UC_X86_REG_EBX,manager);h.u.reg_write(UC_X86_REG_EDI,0);h.u.reg_write(UC_X86_REG_EBP,0)
  hit=[]
  def stop(label):
   def run():hit.append(label);h.u.emu_stop()
   return run
  h.stubs={0x100085fd:stop('skip_null_surface'),0x1000a8b0:stop('surface_ready')}
  fault=None
  try:h.u.emu_start(0x10008558,0x10008633,count=1000)
  except UcError as e:
   assert e.errno==UC_ERR_READ_UNMAPPED;fault=hex(h.u.reg_read(UC_X86_REG_EIP))
  expected_fault={'group':'0x1000857a','tile':'0x10008583','surface_array':'0x10008586'}
  if missing in expected_fault:assert fault==expected_fault[missing],(missing,fault,hit)
  else:assert hit==['skip_null_surface' if missing=='surface' else 'surface_ready'] and fault is None
  record('render_'+missing,dict(fault_instruction=fault,endpoint=hit))
 write('맵진입_응답그래픽_원본명령대조.csv',rows)
 (OUT/'맵진입_응답그래픽검증.json').write_text(json.dumps(dict(cases=len(rows),mismatches=0,scope='Native response dispatch with preflight/UI/request boundary stubs; synthetic rendering pointer graph'),indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),mismatches=0)))
if __name__=='__main__':main()
