"""Close scenario UI 0x37 and 0x17 event-to-request links with original code."""
import json,struct
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 rows=[]
 for entry in [0x405b60,0x432760,0x418c00,0x418cd0]:
  for variant in [0,1]:
   h=Harness('Saver.exe');obj=0x30000000;dialog=0x30002000;item=0x30003000;vt=0x30004000;record=0x30005000;arr=0x30006000;main=0x30007000;name=0x30008000;calls=[]
   h.put(obj,vt);h.put(obj+0xa0,dialog);h.put(dialog,vt);h.put(vt+4,0x300f0100);h.put(vt+0x10,0x300f0200);h.put(obj+0xd0,arr);h.put(arr,record);h.put(obj+0xe0,0)
   h.u.mem_write(record+4,struct.pack('<hh',2 if variant else 1,91));h.put(0x4533e0,main);h.put(main+0x160,name);h.put(name-8,2);h.u.mem_write(name,b'01\0');h.put(0x451230,obj);h.put(0x451298,obj)
   def copy_string():
    sp=h.u.reg_read(UC_X86_REG_ESP);src=h.get(sp+4);h.put(h.u.reg_read(UC_X86_REG_ECX),h.get(src));h.ret(clean=4)
   def constructor():h.put(h.u.reg_read(UC_X86_REG_ECX),name);h.ret()
   def request(n):
    def run():
     sp=h.u.reg_read(UC_X86_REG_ESP);calls.append((n,[h.get(sp+4+4*i) for i in range(4)]));h.ret()
    return run
   h.stubs={0x44088a:copy_string,0x44085a:constructor,0x440848:lambda:h.ret(),0x43d680:request('52'),0x43d810:request('54'),0x300f0100:lambda:h.ret(0x37,8 if h.u.reg_read(UC_X86_REG_ECX)==dialog else 0),0x300f0200:lambda:h.ret(clean=4),0x427270:lambda:h.ret(item,4),0x426f40:lambda:h.ret(clean=4)}
   args=(variant,) if entry==0x405b60 else (variant,name) if entry in [0x432760,0x418cd0] else ()
   h.run(entry,args,obj)
   if entry==0x405b60:
    if variant:assert calls==[('52',[1,0,name,name])],calls
    else:assert not calls and h.get(item+0xd0)==0x432760
   elif entry==0x432760:
    assert calls==([('52',[2,name,name,name])] if variant else []),calls
   elif entry==0x418c00:
    if variant:assert not calls and h.get(item+0xd0)==0x418cd0
    else:assert calls==[('54',[91,1,1,name])],calls
   else:assert calls==([('54',[91,2,1,name])] if variant else []),calls
   rows.append(dict(entry=hex(entry),variant=variant,request_arguments=str(calls),registered_callback=hex(h.get(item+0xd0)),match=True))
 write('패킷_시나리오UI_원본명령대조.csv',rows)
 (OUT/'패킷_시나리오UI검증.json').write_text(json.dumps(dict(cases=len(rows),mismatches=0,scope='Native UI handlers; string/window effects and request entry boundaries modeled'),indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),mismatches=0)))
if __name__=='__main__':main()
