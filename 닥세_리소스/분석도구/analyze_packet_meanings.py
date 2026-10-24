"""Resolve packet labels through UI consumers; isolated branches, no game/OS execution."""
import struct,json,hashlib,subprocess
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EAX,UC_X86_REG_ESP,UC_X86_REG_EIP
from analyze_packet_requests import Harness
from analyze_world_packets import decode_record
from analyze_special_skills import ROOT,OUT,PE,write

def stop_at(h,addresses):
 def hook(u,a,n,d):
  if a in addresses:u.emu_stop()
 h.u.hook_add(UC_HOOK_CODE,hook)

def main():
 checks=[];labels=[];p=PE(ROOT/'GameCtl.dll')
 for value,target,string in [(1,0x1000758e,0x1001a314),(2,0x1000759c,0x1001a30c),(3,0x100075aa,0x1001a304),(4,0x100075b8,0x1001a2fc)]:
  label=p.data(string,40).split(b'\0')[0].decode('cp949')
  h=Harness('GameCtl.dll');h.u.mem_write(0x1006a740,struct.pack('<H',value));stop_at(h,{target});h.u.emu_start(0x1000757a,target+1,count=100)
  assert h.u.reg_read(UC_X86_REG_EIP)==target
  labels.append(dict(message='5C54',field='word0',value=value,meaning=label,consumer_va=hex(target),string_va=hex(string),scope='result UI label; does not establish global session order'))
  checks.append(dict(case='result_label_'+str(value),result=label))
 for value in [9999,10000,19999,20000,29999,30000,31999,32000]:
  h=Harness('GameCtl.dll');h.put(0x30001000,value);h.u.reg_write(UC_X86_REG_EAX,0x30001000);stop_at(h,{0x100096d1,0x1000974f})
  h.u.emu_start(0x100096af,0x10009750,count=100)
  accepted=h.u.reg_read(UC_X86_REG_EIP)==0x100096d1
  assert accepted==(10000<=value<20000 or 30000<=value<32000)
  checks.append(dict(case='skill_menu_range_'+str(value),result='included' if accepted else 'excluded'))
 for label,a in [('magic',0x1001a3ac),('skill',0x1001a3c4)]:
  msg=p.data(a,80).split(b'\0')[0].decode('cp949');labels.append(dict(message='5C51',field='short22..29' if label=='magic' else 'short30..37',value='up to8; -1 termination',meaning=msg,consumer_va='0x10009460' if label=='magic' else '0x10009600',string_va=hex(a),scope='character list consumed by magic/skill menu'))
 # Last seven transmitted shorts are consumed by reader but absent from captured output.
 prefix=struct.pack('<38H',*range(100,138));tail=struct.pack('<4I',10000,20000,30000,40000)+b'\x03ABC\x03XYZ'
 _,baseline,names=decode_record(0x51,prefix+bytes(14)+tail)
 for fill in [1,9,71,32767,65535]:
  _,record,strings=decode_record(0x51,prefix+struct.pack('<7H',*[fill]*7)+tail)
  assert record==baseline and strings==names
  checks.append(dict(case='third_array_'+str(fill),result='resetcharacter 0xB0-byte argument and CString assignments unchanged'))
 write('패킷_명칭_근거.csv',labels);write('패킷_명칭_원본명령대조.csv',checks)
 ranges=[('GameCtl.dll',0x10009460,0x100095f4),('GameCtl.dll',0x10009600,0x100097c0),('GameCtl.dll',0x1000756e,0x10007666),('GameCtl.dll',0x1000ccdf,0x1000cd44)]
 evidence=[];asm=[]
 for name,a,b in ranges:
  path=ROOT/name;evidence.append(dict(file=name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(path)],text=True))
 write('패킷_명칭_코드근거.csv',evidence);(OUT/'패킷_명칭_코드.asm.txt').write_text('\n'.join(asm))
 summary=dict(cases=len(checks),mismatches=0,result54_labels={1:'승리',2:'도망',3:'패배',4:'종료'},list1='magic menu IDs',list2='skill menu IDs; filtered ranges [10000,20000),[30000,32000)',third_list='7 shorts parsed; five differential fixtures leave captured resetcharacter argument and CString assignments unchanged',scope='isolated UI branches and differential receive-handler fixtures, not live gameplay or full server ordering')
 (OUT/'패킷_명칭검증.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
