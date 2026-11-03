"""Execute original4347E0 profile restoration; MFC strings/profile and Win32 rect predicates isolated."""
import struct
from check_native_commanders import Native
from native_ai_emulator import UC_X86_REG_ECX
class PlacementNative(Native):
 def __init__(self):
  super().__init__();self.next_string=0x0d060000;self.empty=self.string(b'');self.tokens=[];self.hooks.update({0x479592:'module',0x479598:'empty',0x4795ce:'format-key',0x479904:'profile',0x4795da:'copy',0x4795b6:'assign-text',0x4797ba:'profile-int',0x479586:'destroy-string',0x47958c:'allocate',0x479580:'free',0x0e008000:'id',0x0e008010:'minimize-layout',0x4545a0:'expand-layout',0x479934:'write-profile',0x47993a:'write-profile-int'})
  self.write(0x0d005004,0x0d005800)
 def string(self,data):
  p=self.next_string+12;self.uc.mem_write(self.next_string,struct.pack('<iii',1,len(data),len(data))+data+b'\0');self.next_string=(p+len(data)+16)&~15;return p
 def text(self,p):
  result=b''
  for i in range(512):
   v=bytes(self.uc.mem_read(p+i,1))
   if v==b'\0':return result
   result+=v
  raise AssertionError('unterminated string')
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='module':self.ret(0x0d005000)
  elif name=='empty':p=uc.reg_read(UC_X86_REG_ECX);self.write(p,self.empty);self.ret(p)
  elif name=='copy':p=uc.reg_read(UC_X86_REG_ECX);self.write(p,self.read(self.args(1)[0]));self.ret(p,4)
  elif name=='destroy-string'or name=='free':self.ret()
  elif name=='allocate':assert self.args(1)[0]==200;self.ret(0x0d050000)
  elif name=='id':self.ret(self.window_id)
  elif name=='format-key':
   out,fmt=self.args(2);pattern=self.text(fmt);values=self.args(2+pattern.count(b'%d'))[2:];signed=lambda n:n if n<2147483648 else n-4294967296
   assert pattern in [b'Dlg%d',b'%d %d %d'];self.write(out,self.string(pattern%tuple(map(signed,values))));self.ret()
  elif name=='profile':
   out,section,key,default=self.args(4);self.profile_reads.append(dict(section=self.text(section).decode(),key=self.text(key).decode(),default=self.text(default).decode()if default else None));self.write(out,self.string(self.profile.encode()));self.ret(out,16)
  elif name=='assign-text':
   p=uc.reg_read(UC_X86_REG_ECX);self.write(p,self.string(self.text(self.args(1)[0])));self.ret(p,4)
  elif name=='profile-int':
   section,key,default=self.args(3);assert self.text(key)==b'ChatWindowSize'and default==1;self.ret(self.chat_size,12)
  elif name=='write-profile':
   section,key,value=self.args(3);assert self.text(section)==b'Dlg Information'and self.text(key)==('Dlg'+str(self.window_id)).encode();self.saved=self.text(value).decode();self.ret(1,12)
  elif name=='write-profile-int':
   section,key,value=self.args(3);assert self.text(section)==b'Dlg Information'and self.text(key)==b'ChatWindowSize';self.saved_chat=value;self.ret(1,12)
  elif name=='minimize-layout':
   p=uc.reg_read(UC_X86_REG_ECX);assert p==self.window;self.write(p+0x80,1);self.write(p+0x48,self.case['minWidth']);self.write(p+0x4c,self.case['minHeight']);uc.mem_write(p+0x30,struct.pack('<4i',*self.case['minRegion']));self.ret(1)
  elif name=='expand-layout':
   p=uc.reg_read(UC_X86_REG_ECX);assert p==self.window;self.write(p+0xb4,2);self.write(p+0x4c,401);self.ret(1)
  elif name=='strtok':
   p,delimiters=self.args(2)
   if p:
    value=self.text(p);self.tokens=[];start=None
    for i,b in enumerate(value+b' '):
     if b==32:
      if start is not None:self.tokens.append(p+start);start=None
      self.uc.mem_write(p+i,b'\0')
     elif start is None:start=i
   self.ret(self.tokens.pop(0)if self.tokens else 0)
  elif name=='sscanf':
   token,fmt,out=self.args(3);assert self.text(fmt)==b'%d';self.write(out,int(self.text(token)));self.ret(1)
  elif name=='IsRectEmpty':
   r=struct.unpack('<4i',uc.mem_read(self.args(1)[0],16));self.ret(int(r[2]<=r[0]or r[3]<=r[1]),4)
  elif name=='OffsetRect':
   p,x,y=self.args(3);x=x if x<2147483648 else x-4294967296;y=y if y<2147483648 else y-4294967296;l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));uc.mem_write(p,struct.pack('<4i',l+x,t+y,r+x,b+y));self.ret(1,12)
  elif name=='IntersectRect':
   dst,p,q=self.args(3);p=struct.unpack('<4i',uc.mem_read(p,16));q=struct.unpack('<4i',uc.mem_read(q,16));r=(max(p[0],q[0]),max(p[1],q[1]),min(p[2],q[2]),min(p[3],q[3]));ok=r[0]<r[2]and r[1]<r[3];uc.mem_write(dst,struct.pack('<4i',*(r if ok else (0,0,0,0))));self.ret(int(ok),12)
  else:super().hook(uc,a,size,data)
 def resolve(self,case):
  self.next_string=0x0d060000;self.empty=self.string(b'')
  self.profile_reads=[];self.case=case;self.chat_size=case.get('chatSize',1);self.window_id=case['id'];self.profile=f'{case["x"]} {case["y"]} {case.get("minimized",0)}';self.uc.mem_write(self.window,b'\0'*256);vtable=0x0d004000;self.write(self.window,vtable);self.write(vtable+4,0x0e008000);self.write(vtable+0x44,0x0e008010);self.write(vtable+0x50,0x4346d0 if case['center']else 0x434ed0);self.write(self.window+0xb4,1)
  if case.get('nativeVtable'):
   self.write(self.window,case['nativeVtable']);self.hooks[case['nativeMinimize']]='minimize-layout'
   if case.get('idField')is not None:self.write(self.window+case['idField'],case['id'])
  self.write(self.window+0x48,case['width']);self.write(self.window+0x4c,case['height']);self.uc.mem_write(self.window+0x30,struct.pack('<4i',*case['region']));self.run(0x4347e0,[],self.window)
  signed=lambda n:n if n<2147483648 else n-4294967296
  result=dict(x=signed(self.read(self.window+0x40)),y=signed(self.read(self.window+0x44)),minimized=bool(self.read(self.window+0x80)),expanded=self.read(self.window+0xb4)==2)
  self.saved=None;self.saved_chat=None;self.run(0x434c20,[],self.window);result.update(saved=self.saved,savedChat=self.saved_chat,profileReads=self.profile_reads);assert self.saved is not None;return result
