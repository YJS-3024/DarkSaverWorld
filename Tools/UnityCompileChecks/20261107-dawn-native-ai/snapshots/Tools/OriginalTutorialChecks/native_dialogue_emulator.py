"""Execute original waiting layout/draw coordinates and battle frame tile calls.
DirectDraw surfaces, text rasterization and SetRect/OffsetRect are isolated;
no Windows process, GPU framebuffer or Unity render/input is executed.
"""
import sys,struct,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/OriginalWorldChecks'))
from check_native_commanders import Native
from native_ai_emulator import pefile,UC_X86_REG_ECX
class Dialogue(Native):
 def __init__(self):
  super().__init__();self.faces=[];self.texts=[];self.blits=[]
  r=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());self.gamectlSha='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0';source=next(f['extractedFile']for payload in r['payloads']for f in payload['files']if f.get('sha256')==self.gamectlSha);raw=Path(source).read_bytes();assert hashlib.sha256(raw).hexdigest()==self.gamectlSha;p=pefile.PE(data=raw);self.uc.mem_map(0x10000000,(p.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x10000000,p.get_memory_mapped_image());self.write(0x100161d4,0x0e008a00);self.hooks[0x0e008a00]='SetRect';self.hooks[0x10014714]='frame-blit';self.hooks[0x43e390]='paragraph';self.hooks[0x43ea00]='paragraph'
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='SetRect':
   ptr,*v=self.args(5);uc.mem_write(ptr,struct.pack('<4I',*v));self.ret(1,20)
  elif name=='OffsetRect':
   ptr,x,y=self.args(3);l,t,r,b=struct.unpack('<4I',uc.mem_read(ptr,16));uc.mem_write(ptr,struct.pack('<4I',l+x,t+y,r+x,b+y));self.ret(1,12)
  elif name and name.startswith('?BltFast@CDDSurface'):
   self.faces.append(self.args(4));self.ret(1,16)
  elif name and name.startswith('?GetDescription@CDDSurface'):
   ptr=self.args(1)[0];uc.mem_write(ptr,bytes(124));self.ret(1,4)
  elif name and name.startswith('?Lock@CDDSurface'):self.ret(0x0d090000)
  elif name and name.startswith('?Unlock@CDDSurface'):self.ret()
  elif name=='paragraph':self.texts.append(self.args(6));self.ret(0,168)
  elif name=='frame-blit':
   x,y,src,r,flags=self.args(5);self.blits.append((x,y,src,struct.unpack('<4i',uc.mem_read(r,16)),flags));self.ret(1,20)
  else:super().hook(uc,a,size,data)
 def waiting(self):
  self.write(self.window+0x48,562);self.write(self.window+0x4c,84);self.run(0x472640,[],self.window);at=(self.read(self.window+0x40),self.read(self.window+0x44));self.write(self.window+0xbc,0x0d001800);self.write(self.window+0xac,0x0d001000);self.write(0x0d001000,0);self.write(self.window+0xb8,1);self.write(self.window+0xb4,0x0d001400);self.run(0x472670,[0x0d001c00],self.window);return at,self.faces,self.texts
 def battle(self):
  self.write(0x1006a704,0x0d005000);self.write(0x1001c470,0x0d005400);self.run(0x10008e10,[40,50,601,154]);return self.blits
