"""Result record labels and AI area scalar; isolated original x86 probes."""
import struct,json,hashlib,subprocess
from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EDI,UC_X86_REG_EIP
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,PE,write

def main():
 checks=[];fields=[];p=PE(ROOT/'GameCtl.dll')
 for code in [0,1,2,3,65535]:
  h=Harness('GameCtl.dll');h.u.mem_write(0x30001006,struct.pack('<H',code));h.u.reg_write(UC_X86_REG_EDI,0x30001004)
  targets={0x1000794a,0x10007925,0x100078e4}
  def stop(u,a,n,d):
   if a in targets:u.emu_stop()
  h.u.hook_add(UC_HOOK_CODE,stop);h.u.emu_start(0x100078da,0x10007990,count=100)
  expected=0x1000794a if code==1 else 0x10007925 if code==2 else 0x100078e4
  assert h.u.reg_read(UC_X86_REG_EIP)==expected
  checks.append(dict(case='growth_code_'+str(code),result='levelup' if code==1 else 'classchange' if code==2 else 'experience text'))
 for value in [0,123456,2147483647,4294967295]:
  h=Harness('GameCtl.dll');h.put(0x3000101c,value);h.u.reg_write(UC_X86_REG_EDI,0x30001004);h.u.reg_write(UC_X86_REG_ESP,0x20010000)
  h.u.emu_start(0x100078e4,0x100078f2,count=100)
  sp=h.u.reg_read(UC_X86_REG_ESP);assert h.get(sp+8)==value and h.get(sp+4)==0x1001a2e4
  checks.append(dict(case='experience_'+str(value),result='record dword28 forwarded unchanged to experience %d formatter'))
 # SetArea scalar produces a diamond of scalar+1 tiles, fixed 200-row stride.
 for scalar in [0,1,2,4]:
  h=Harness('userunit.dll');h.put(0x100e3298,20);h.put(0x100e329c,20);table=0x30010000;h.u.mem_write(table,b'\xff'*160000)
  h.run(0x10004ba0,(10,10,scalar,table))
  for x in range(20):
   for y in range(20):
    v=h.get(table+(x*200+y)*4);distance=abs(x-10)+abs(y-10)
    assert (v!=0xffffffff)==(distance<=scalar+1),(scalar,x,y,v)
    if distance<=scalar+1:assert v==scalar+1-distance
  checks.append(dict(case='area_scalar_'+str(scalar),result='diamond radius scalar+1; values radius-Manhattan distance'))
 # Completion branch uses stored result code without consulting result-received flag.
 for code in [1,2,3,4]:
  for received in [0,1]:
   h=Harness('GameCtl.dll');h.u.mem_write(0x1006a740,struct.pack('<H',code));h.put(0x1006a890,received)
   def stop_completion(u,a,n,d):
    if a in [0x1000d003,0x1000d03f]:u.emu_stop()
   h.u.hook_add(UC_HOOK_CODE,stop_completion);h.u.reg_write(UC_X86_REG_ESP,0x20010000)
   h.u.emu_start(0x1000cfe0,0x1000d070,count=100)
   assert h.u.reg_read(UC_X86_REG_EIP)==(0x1000d003 if code==1 else 0x1000d03f)
   checks.append(dict(case=f'completion_result_{code}_received_{received}',result='victory branch' if code==1 else 'non-victory branch'))
 for offset,name,a in [(6,'growth display code',0x100078da),(28,'experience gained display',0x100078e4),(2,'ability ID used for character surface',0x10007679),(4,'item-display block enabled when value2; product category name unresolved',0x100076e2),(8,'secondary ability ID if positive',0x100077f4),(10,'secondary surface repeat count',0x10007831),(12,'overlay repeat count',0x1000788e),(14,'seven item IDs via GetItemTable; negative skipped',0x1000772d)]:
  fields.append(dict(message='5C54',record_offset=offset,type='int32_le' if offset==28 else 'int16_le' if offset!=14 else 'int16_le[7]',meaning=name,consumer_va=hex(a),scope='record base is payload14+32*i; display semantics, server computation unknown'))
 write('패킷_결과레코드_필드.csv',fields);write('패킷_결과필드_원본명령대조.csv',checks)
 labels=[]
 for a in [0x1001a2cc,0x1001a2d4,0x1001a2e4]:labels.append(dict(va=hex(a),text=p.data(a,64).split(b'\0')[0].decode('cp949')))
 write('패킷_결과표시_문자열.csv',labels)
 evidence=[];asm=[]
 for name,a,b in [('GameCtl.dll',0x10007679,0x10007690),('GameCtl.dll',0x10007723,0x10007975),('userunit.dll',0x1000314b,0x1000317e),('userunit.dll',0x1000389f,0x100038d8),('userunit.dll',0x10003c3f,0x10003c80),('userunit.dll',0x10004ba0,0x10004c40)]:
  path=ROOT/name;evidence.append(dict(file=name,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(path)],text=True))
 write('패킷_결과필드_근거.csv',evidence);(OUT/'패킷_결과필드_코드.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(checks),mismatches=0,scope='isolated original result branches/argument forwarding and SetArea; no full AI, UI or server execution',scalar='runtime30 -> candidate-search area parameter; SetArea radius input+1; product stat name unresolved')
 (OUT/'패킷_결과필드검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
