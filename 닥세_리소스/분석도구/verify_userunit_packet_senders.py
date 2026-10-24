"""userunit.dll's separate WM52C packet bridge and all four direct generators."""
import struct,json
from analyze_packet_requests import Harness
from analyze_special_skills import OUT,write

def main():
 rows=[]
 for enabled in [0,1,2]:
  for entry,body,payload in [(0x10005010,struct.pack('<11h',*range(1,12)),b'\x5c\x43'+struct.pack('<11h',*range(1,12))),(0x10005200,b'',b'\x5c\x0b'+struct.pack('<h',77)),(0x10005260,struct.pack('<4h',1,-2,300,400),b'\x5c\x82'+struct.pack('<4h',1,-2,300,400))]:
   h=Harness('userunit.dll');h.put(0x1010a3d8,enabled);h.u.mem_write(0x30001000,body or b'\0');h.stubs={0x100117e6:h.alloc,0x10004f90:lambda:(h.send(),h.u.emu_stop())}
   # Disabled generators return without touching CRT/API; enabled ones stop at the bridge.
   if enabled:
    from unicorn.x86_const import UC_X86_REG_ESP
    h.put(0x20010000,0x300ff000,77 if entry==0x10005200 else 0x30001000);h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.emu_start(entry,0x300ff000,count=100000)
   else:h.run(entry,(77 if entry==0x10005200 else 0x30001000,))
   assert h.sent==([payload] if enabled else []),(hex(entry),h.sent)
   rows.append(dict(entry=hex(entry),enabled=enabled,skill='',flag='',targets='',payloads=';'.join(x.hex() for x in h.sent),match=True))
 for enabled in [0,1,2]:
  for skill in [-1,9999,10000]:
   for flag in [0,1]:
    for targets in [(),(7,-1,300)]:
     h=Harness('userunit.dll');h.put(0x1010a3d8,enabled);h.u.mem_write(0x30001000,struct.pack('<hhhhIB3xI',skill,11,-2,33,0,len(targets),0x30002000))
     if targets:h.u.mem_write(0x30002000,struct.pack('<'+'h'*len(targets),*targets))
     h.put(0x30003000,3,0,2,4,6,8);h.put(0x30003170,10);h.put(0x1001215c,0x300f0000)
     h.stubs={0x100117e6:h.alloc,0x10004f90:lambda:(h.send(),h.u.emu_stop()),0x300f0000:lambda:h.ret(0x30003000)}
     if enabled:
      from unicorn.x86_const import UC_X86_REG_ESP
      h.put(0x20010000,0x300ff000,0x30001000,flag);h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.emu_start(0x100050b0,0x300ff000,count=100000)
     else:h.run(0x100050b0,(0x30001000,flag))
     expected=b'\x5c'+bytes([0x89 if skill<10000 else 0x92,1 if flag==0 else 2])+struct.pack('<hhhhIB',skill,11,-2,33,90,len(targets))+struct.pack('<'+'h'*len(targets),*targets)
     assert h.sent==([expected] if enabled else []),(skill,flag,targets,h.sent)
     rows.append(dict(entry='0x100050b0',enabled=enabled,skill=skill,flag=flag,targets=str(targets),payloads=';'.join(x.hex() for x in h.sent),match=True))
 # Native bridge -> modeled SendMessageA: hwnd,msg,wParam=length,lParam=data.
 h=Harness('userunit.dll');h.put(0x1010a3e8,123);h.put(0x10012134,0x300f0000);observed=[]
 def window_send():
  from unicorn.x86_const import UC_X86_REG_ESP
  sp=h.u.reg_read(UC_X86_REG_ESP);observed.extend(h.get(sp+4+4*i) for i in range(4));h.ret(clean=16)
 h.stubs[0x300f0000]=window_send;h.run(0x10004f90,(0x30001000,24));assert observed==[123,0x52c,24,0x30001000]
 rows.append(dict(entry='0x10004f90',enabled='',skill='',flag='',targets='',payloads=str(observed),match=True))
 write('패킷_userunit_송신_원본명령대조.csv',rows)
 (OUT/'패킷_userunit_송신검증.json').write_text(json.dumps(dict(cases=len(rows),functions=4,mismatches=0,scope='Native generators and window-message bridge, modeled allocation/table/API'),indent=2)+'\n')
 print(json.dumps(dict(cases=len(rows),functions=4,mismatches=0)))
if __name__=='__main__':main()
