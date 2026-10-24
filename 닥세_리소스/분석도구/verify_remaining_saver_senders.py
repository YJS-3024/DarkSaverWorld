"""Original Saver serializers through first application-send boundary, modeled UI inputs."""
import json,struct
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 # Entry, exact opcode prefix, exact wire length, fixed scalar fixture arguments.
 specs=[(0x4061a0,'93',1),(0x406620,'9d',1),(0x409140,'d8',1),(0x40cc20,'d6',3),(0x40cc90,'d9',1),(0x40fab0,'c3',3),(0x40fd50,'ac',5),(0x4119d0,'58',3),(0x413e10,'af',5),(0x414500,'e7',5),(0x414970,'7d',3),(0x415230,'a0',3),(0x416d50,'2c53',6),(0x417500,'a7',3),(0x418b80,'50',6),(0x418fd0,'9e',3),(0x41a080,'83',5),(0x41a210,'86',3),(0x41a640,'88',3),(0x41a6a0,'89',3),(0x41a7d0,'8a',3),(0x41c9c0,'42',5),(0x41f4f0,'ad',5),(0x420800,'40',11),(0x420930,'a2',7),(0x4209b0,'2c26',6),(0x4247f0,'97',3),(0x424d60,'90',1),(0x42d990,'2c22',8),(0x431af0,'2c27',2),(0x431b40,'2c40',2),(0x43acc0,'25',1),(0x43b2f0,'5c61',2),(0x42d640,'2c20',2)]
 rows=[]
 for entry,prefix,length in specs:
  for variant in [0,1]:
   h=Harness('Saver.exe');obj=0x30000000;data=0x30004000;string=0x30008000;sp=0x20010000
   h.u.mem_write(obj,struct.pack('<I',data)*1024);h.u.mem_write(data,struct.pack('<I',data)*1024)
   h.put(string-8,4);h.u.mem_write(string,b'A\x09GB\0');h.put(obj+0x3fc,variant);h.put(obj+0xb8,variant)
   if entry==0x418b80:h.put(obj+0xc8,string)
   h.put(0x4533e0,0x3000a000);h.heap=0x30020000
   # Some methods consume an input record pointer while others consume scalar arguments.
   args=(data,variant,5,6,7)
   h.put(data,3+variant,4+variant,5+variant,6+variant,7+variant)
   if entry==0x42d990:h.put(obj+0x60,0)
   if entry in [0x414500,0x41a080,0x420800,0x420930,0x4209b0]:args=(variant,4,5,6,7)
   def send():h.send(True);h.u.emu_stop()
   h.stubs={0x440728:h.alloc,0x44072e:lambda:h.ret(),0x43b840:send}
   h.put(sp,0x300ff000,*args);h.u.reg_write(UC_X86_REG_ESP,sp);h.u.reg_write(UC_X86_REG_ECX,obj)
   try:h.u.emu_start(entry,0x300ff000,count=100000)
   except Exception:
    print('fixture_failure',hex(entry),variant,hex(h.u.reg_read(UC_X86_REG_EIP)));raise
   assert len(h.sent)==1,(hex(entry),h.sent)
   packet=h.sent[0];assert packet.startswith(bytes.fromhex(prefix)) and len(packet)==length,(hex(entry),packet.hex(),length)
   rows.append(dict(entry_va=hex(entry),variant=variant,prefix=prefix,wire_length=length,payload=packet.hex(),match=True))
 write('패킷_Saver_확장송신_원본명령대조.csv',rows)
 (OUT/'패킷_Saver_확장송신검증.json').write_text(json.dumps(dict(cases=len(rows),functions=len(specs),mismatches=0,scope='Original functions through first send; synthetic record/UI inputs, no Windows session'),indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),functions=len(specs),mismatches=0)))
if __name__=='__main__':main()
