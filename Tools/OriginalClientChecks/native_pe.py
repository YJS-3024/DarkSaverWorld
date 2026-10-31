"""Small read-only PE32 reader used by original-client evidence checks."""
import hashlib,struct,subprocess
from pathlib import Path
class PE:
 def __init__(self,path):
  self.path=Path(path);self.data=self.path.read_bytes();self.sha256=hashlib.sha256(self.data).hexdigest();b=self.data
  self.pe=struct.unpack_from('<I',b,60)[0];self.opt=self.pe+24;assert struct.unpack_from('<H',b,self.opt)[0]==0x10b
  self.base=struct.unpack_from('<I',b,self.opt+28)[0];count=struct.unpack_from('<H',b,self.pe+6)[0];start=self.opt+struct.unpack_from('<H',b,self.pe+20)[0];self.sections=[]
  for i in range(count):
   o=start+40*i;vs,rva,rs,raw=struct.unpack_from('<4I',b,o+8);flags=struct.unpack_from('<I',b,o+36)[0]
   self.sections.append(dict(name=b[o:o+8].split(b'\0')[0].decode(),rva=rva,virtualSize=vs,rawSize=rs,raw=raw,flags=flags))
 def offset(self,va):
  for s in self.sections:
   if s['rva']<=va-self.base<s['rva']+s['rawSize']:return s['raw']+va-self.base-s['rva']
  raise ValueError(hex(va))
 def va(self,offset):
  for s in self.sections:
   if s['raw']<=offset<s['raw']+s['rawSize']:return self.base+s['rva']+offset-s['raw']
  raise ValueError(offset)
 def read(self,va,n):return self.data[self.offset(va):self.offset(va)+n]
 def u32(self,va):return struct.unpack('<I',self.read(va,4))[0]
 def u16(self,va):return struct.unpack('<H',self.read(va,2))[0]
 def string(self,va):
  o=self.offset(va);return self.data[o:self.data.index(b'\0',o)].decode('cp949',errors='replace')
 def exports(self):
  rva,size=struct.unpack_from('<2I',self.data,self.opt+96)
  if not rva:return {}
  at=self.base+rva;n=self.u32(at+24);func=self.base+self.u32(at+28);names=self.base+self.u32(at+32);ords=self.base+self.u32(at+36)
  return {self.string(self.base+self.u32(names+i*4)):self.base+self.u32(func+self.u16(ords+i*2)*4) for i in range(n)}
 def imports(self):
  rva,size=struct.unpack_from('<2I',self.data,self.opt+104);result={}
  if not rva:return result
  at=self.base+rva
  while self.u32(at+12):
   original=self.u32(at);iat=self.u32(at+16);name=self.string(self.base+self.u32(at+12));i=0
   while (v:=self.u32(self.base+(original or iat)+4*i)):
    symbol=f'ordinal:{v&65535}' if v&0x80000000 else self.string(self.base+v+2)
    result[self.base+iat+i*4]=(name,symbol);i+=1
   at+=20
  return result
 def disasm(self,start,end):
  return subprocess.check_output(['objdump','-d','-Mintel',f'--start-address={start}',f'--stop-address={end}',str(self.path)],text=True).replace(str(self.path),self.path.name+' [SHA256 '+self.sha256+']')
 def expect(self,va,hexbytes):assert self.read(va,len(bytes.fromhex(hexbytes)))==bytes.fromhex(hexbytes),hex(va)
 def executable(self,va):return any(s['flags']&0x20000000 and self.base+s['rva']<=va<self.base+s['rva']+s['virtualSize'] for s in self.sections)
 def thunk(self,va):
  seen=set()
  while self.executable(va) and va not in seen and self.read(va,1)==b'\xe9':
   seen.add(va);va+=5+struct.unpack('<i',self.read(va+1,4))[0]
  return va
