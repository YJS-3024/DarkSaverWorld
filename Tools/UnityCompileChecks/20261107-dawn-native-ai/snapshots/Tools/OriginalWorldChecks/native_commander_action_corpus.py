"""Actual042E0 with real target queries/CheckMagicTarget and original wire builders.
Only metadata/map/team/rand/allocator/free/final-send boundaries are supplied.
"""
import copy,struct
from check_native_magic_target_catalog import CatalogTargets,UNITS
from native_ai_emulator import UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP
class NativeCommanderActionCorpus(CatalogTargets):
 def __init__(self,row,variant='ready'):
  units=copy.deepcopy(UNITS);actor=next(u for u in units if u['id']==400);actor.update(hp=99,maxHp=100,nativeState=16)
  if variant=='no-target':units=[actor]
  actor.update(hp=29 if variant=='hp29'else 30 if variant=='hp30'else 50 if variant=='rest-hurt'else 100 if variant=='rest-full'else 99,nativeState=17 if variant=='moving'else 0 if variant=='no-ready'else 16,nativeActive=variant!='inactive')
  case=dict(width=24,height=24,source=400,metadata=row,units=units,random=[42,0,2]);super().__init__(case);self.sent=[];self.input=[];self.alloc=0;self.trace=[]
  for address in [0x10004f70,0x1001188a,0x10005090,0x100051e0,0x10005240,self.exports['CheckMagicTarget'],0x10003860,0x10003a40,0x10003c00]:
   if address not in self.hooks:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=address,end=address)
  p=self.RUNTIME+400*0x2e0;self.write(p+0x28,row['mp']-1 if variant=='mp-short'else row['mp']);self.write(p+0x104,128 if variant=='status80'else 0);self.write(self.DATA+0x20004,row['mp'])
  for i in range(100):self.write(0x100d99a0+i*0x110,999)
  for at,v in [(0,400),(4,2),(0x1c,2),(0x20,100),(0x24,2),(0x28,row['id']),(0x2c,0),(0x30,1),(0x34,-1)]:self.write(0x100d99a0+at,v)
  if variant=='no-row':self.write(0x100d99a0,999)
  if variant in ['rest-hurt','rest-full']:self.write(0x100d99a0+0x24,0)
  if variant=='invalid-magic':self.write(0x100d99a0+0x28,-1)
  if variant in ['zero-rest0','zero-rest1']:
   self.case['random'][0]=0 if variant=='zero-rest0'else 1
   for at,value in [(0x1c,3),(0x20,0),(0x24,0),(0x2c,100),(0x30,2),(0x34,row['id']),(0x38,0),(0x3c,1)]:self.write(0x100d99a0+at,value)
 def _dispatch(self,uc,address,size,data):
  if address==0x10011896:
   count=self.args(1)[0];assert 0<count<65536;pointer=self.DATA+0x30000+self.alloc;self.alloc+=(count+15)//16*16;assert self.alloc<0x30000;self.ret(pointer);return
  if address==0x1001188a:self.ret();return
  if address==0x10004f70:
   p,count=self.args(2);self.sent.append(bytes(uc.mem_read(p,count)).hex());self.ret();return
  if address==0x10005090:
   p,mode=self.args(2);magic,source,x,y=struct.unpack('<4h',uc.mem_read(p,8));count=uc.mem_read(p+12,1)[0];target=self.read(p+16);targets=list(struct.unpack('<'+'h'*count,uc.mem_read(target,count*2)))if count else[]
   self.input.append(dict(kind='magic',magic=magic,source=source,x=x,y=y,mode=mode+1,targets=targets));return
  if address==0x100051e0:self.input.append(dict(kind='rest',source=self.args(1)[0]));return
  if address==0x10005240:
   p=self.args(1)[0];source,target,a,b=struct.unpack('<4h',uc.mem_read(p,8));self.input.append(dict(kind='attack',source=source,target=target,sourceTerrain=a&65535,targetTerrain=b&65535));return
  if address in [self.exports['CheckMagicTarget'],0x10003860,0x10003a40,0x10003c00]:self.trace.append([hex(address),self.args(5 if address==self.exports['CheckMagicTarget'] else 4)]);return
  return super()._dispatch(uc,address,size,data)
 def run(self):
  before={u['id']:bytes(self.uc.mem_read(self.RUNTIME+u['id']*0x2e0,0x2e0))for u in self.case['units']}
  sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<3I',self.STOP,400,0));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.emu_start(0x100042e0,self.STOP+1,timeout=5000000,count=5000000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and self.uc.reg_read(UC_X86_REG_ESP)==sp+4
  result=dict(inputs=self.input,packets=self.sent,order=self.read(self.RUNTIME+400*0x2e0+0x108),ap=self.read(self.RUNTIME+400*0x2e0+0x60),state=self.read(self.RUNTIME+400*0x2e0+0x100),randomCalls=self.random_calls)
  mutations={}
  for unit,raw in before.items():
   after=bytes(self.uc.mem_read(self.RUNTIME+unit*0x2e0,0x2e0));changed=[i for i,(a,b)in enumerate(zip(raw,after))if a!=b]
   if changed:mutations[str(unit)]=changed
   assert not changed or unit==400 and all(i in range(0x60,0x64)or i in range(0x100,0x104)or i in range(0x108,0x10c)for i in changed),(unit,changed)
  result['unitByteMutations']=mutations;self.close();return result
