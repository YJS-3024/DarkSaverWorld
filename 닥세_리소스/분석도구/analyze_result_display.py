"""Isolated item-icon consumer and result display gates; no UI/server execution."""
import struct,json,hashlib,subprocess
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EDI
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write

def branch(entry,targets,setup):
 h=Harness('GameCtl.dll');h.u.reg_write(UC_X86_REG_ESP,0x20010000);setup(h)
 def stop(u,a,n,d):
  if a in targets:u.emu_stop()
 h.u.hook_add(UC_HOOK_CODE,stop);h.u.emu_start(entry,max(targets)+1,count=1000)
 pc=h.u.reg_read(UC_X86_REG_EIP);assert pc in targets
 return pc

def main():
 rows=[];rects=[]
 for icon in [0,1,99,100,101,299,700,1234]:
  h=Harness('GameCtl.dll');h.put(0x30003000,0,icon);requested=[]
  def getitem():
   sp=h.u.reg_read(UC_X86_REG_ESP);requested.append(h.get(sp+4));h.ret(0x30003000)
  h.put(0x100162d0,0x300f0000);h.stubs={0x300f0000:getitem};h.run(0x10007240,(0x30002000,321))
  got=struct.unpack('<4i',h.u.mem_read(0x30002000,16));expected=((icon%100)*32,(icon//100)*32,(icon%100)*32+32,(icon//100)*32+32)
  assert requested==[321] and got==expected
  rects.append(dict(item_id=321,synthetic_icon=icon,left=got[0],top=got[1],right=got[2],bottom=got[3],scope='GetItemTable stub supplies icon; source rectangle original instructions'))
  rows.append(dict(case='item_icon_'+str(icon),result='item lookup and 32px rectangle matched'))
 for value in [0,1,2,3,65535]:
  def setup(h):h.u.mem_write(0x30001004,struct.pack('<H',value));h.u.reg_write(UC_X86_REG_EDI,0x30001004)
  pc=branch(0x100076e2,{0x100076ec,0x100077f4},setup);assert pc==(0x100076ec if value==2 else 0x100077f4)
  rows.append(dict(case='item_display_code_'+str(value),result='item block enabled' if value==2 else 'item block skipped'))
 for value in [-32768,-1,0,1,32767]:
  def setup(h):h.put(0x20010018,0x30001000);h.u.mem_write(0x30001000,struct.pack('<h',value))
  pc=branch(0x10007742,{0x10007752,0x100077da},setup);assert pc==(0x10007752 if value>=0 else 0x100077da)
  rows.append(dict(case='item_id_'+str(value),result='draw lookup' if value>=0 else 'skip negative ID'))
 for active in [0,1]:
  for received in [0,1]:
   def setup(h):
    h.put(0x1001c434,active);h.put(0x1001c430,1);h.put(0x1006ad4c,0);h.put(0x1006a890,received);h.u.reg_write(UC_X86_REG_EDI,0)
   pc=branch(0x100072f3,{0x10007302,0x1000753d},setup);assert pc==(0x1000753d if active else 0x10007302)
   rows.append(dict(case=f'ui_active_{active}_received_{received}',result='pass activation gate' if active else 'return before result rendering'))
 for received in [0,1]:
  def setup(h):h.put(0x1006a890,received);h.u.reg_write(UC_X86_REG_EDI,0)
  pc=branch(0x1000756e,{0x10007990,0x1000757a},setup);assert pc==(0x1000757a if received else 0x10007990)
  rows.append(dict(case='ui_result_gate_'+str(received),result='draw result section' if received else 'skip result section'))
 write('패킷_결과표시_원본명령대조.csv',rows);write('패킷_결과아이템_아이콘대조.csv',rects)
 states=[dict(active=a,result_received=r,display='result section eligible' if a and r else 'result section gated',scope='two original UI branches; other UI/resource preconditions not covered') for a in [0,1] for r in [0,1]]
 write('패킷_결과UI_상태조건.csv',states)
 evidence=[];asm=[]
 for a,b in [(0x10007240,0x10007292),(0x100072f3,0x1000736a),(0x1000756e,0x1000758e),(0x100076e2,0x100077f4)]:
  p=ROOT/'GameCtl.dll';evidence.append(dict(file=p.name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True))
 write('패킷_결과표시_근거.csv',evidence);(OUT/'패킷_결과표시_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,scope='isolated original item helper and branch gates; item table mocked; no complete UI or server session',item_ids='record offsets14..26 are item IDs used by GetItemTable and GetItemSurface',category='record offset4 ==2 enables item-display block; original category name unresolved',order='activation and result data are separate display prerequisites; no unique wire ordering established')
 (OUT/'패킷_결과표시검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
