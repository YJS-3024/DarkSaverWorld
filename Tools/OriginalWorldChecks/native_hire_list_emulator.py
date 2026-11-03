"""Execute unmodified late setunit.GetSoldierList; initialize source table memory.

No game/API callbacks or output algorithm are stubbed. Table-loader parsing and
the full Saver UI caller are outside this isolated execution.
"""
import hashlib,struct
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_X86_REG_ESP,UC_X86_REG_EIP
from native_shop_details_emulator import item_record
from build_native_shop_details import rows
class HireListNative:
 def __init__(self,catalog):
  raw=Path('/workspace/.tools/ds-ghidra/installed/saver/setunit.dll').read_bytes();self.sha256=hashlib.sha256(raw).hexdigest();assert self.sha256=='71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca'
  pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x10000000,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x10000000,pe.get_memory_mapped_image());self.uc.mem_map(0x0d000000,0x100000);self.uc.mem_map(0x0f000000,0x100000);self.stop=0x0d000000
  for i in range(300):self.write(0x10005500+i*388,-1)
  for i,skill in enumerate(catalog['skills']):
   at=0x10005500+i*388;self.write(at,skill['id']);self.write(at+260,len(skill['jobs']));self.write(at+384,skill['maximum'])
   for j,job in enumerate(skill['jobs']):self.write(at+264+j*4,job)
  for row in rows():self.uc.mem_write(0x1007bde0+int(row['번호'])*664,item_record(row))
  # Explicit synthetic catalogs may override a source equipment record so every
  # source-proven modifier branch can be compared, without editing game data.
  for item in catalog['equipment']:
   at=0x1007bde0+item['id']*664;self.write(at,item['id']);self.write(at+48,item['skill']);self.write(at+112,item['boost'])
 def write(self,address,value):self.uc.mem_write(address,struct.pack('<I',value&0xffffffff))
 def resolve(self,worn,learned):
  for at,values in [(0x0d010000,worn),(0x0d010100,learned)]:
   values=list(values[:10]);values+= [-1]*(10-len(values));self.uc.mem_write(at,struct.pack('<10i',*values))
  sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<3I',self.stop,0x0d010000,0x0d010100));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.emu_start(0x10002850,self.stop,timeout=1000000,count=1000000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop
  count=struct.unpack('<i',self.uc.mem_read(0x100052b0,4))[0];assert 0<=count<=100
  return [dict(zip(['job','maximum'],struct.unpack('<2i',self.uc.mem_read(0x100052b4+i*8,8))))for i in range(count)]
