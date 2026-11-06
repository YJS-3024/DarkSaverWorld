"""Execute late Saver roster toggle/drag/swap code; isolate UI and final send only."""
import hashlib,json,os,struct,subprocess,random
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX,UC_X86_REG_ECX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-commander-checks');SHA='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
class Native:
 def __init__(self):
  r=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());source=next(f['extractedFile']for p in r['payloads']for f in p['files']if f.get('sha256')==SHA);raw=Path(source).read_bytes();assert hashlib.sha256(raw).hexdigest()==SHA
  pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x400000,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x400000,pe.get_memory_mapped_image())
  self.uc.mem_map(0,0x1000)
  for a in [0x0d000000,0x0e000000,0x0f000000]:self.uc.mem_map(a,0x100000)
  self.stop=0x0e000000;self.manager=0x0d000100;self.window=0x0d002000;self.records=0x0d010000;self.sent=[]
  self.hooks={self.stop:'end',0x4132d0:'send',0x427d10:'temporary-destructor',0x465680:'refresh-toggles',0x465790:'refresh-selection',0x433d00:'base-mouseup'}
  for i,imp in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
   target=0x0e001000+i*16;self.write(imp.address,target);self.hooks[target]=imp.name.decode()if imp.name else '#'+str(imp.ordinal)
  self.write(0x4952a0,self.manager);self.uc.hook_add(UC_HOOK_CODE,self.hook)
 def write(self,a,v):self.uc.mem_write(a,struct.pack('<I',v&0xffffffff))
 def read(self,a):return struct.unpack('<I',self.uc.mem_read(a,4))[0]
 def args(self,n):return struct.unpack('<'+'I'*n,self.uc.mem_read(self.uc.reg_read(UC_X86_REG_ESP)+4,n*4))
 def ret(self,value=0,cleanup=0):
  sp=self.uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EAX,value);self.uc.reg_write(UC_X86_REG_EIP,pc)
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name is None:return
  if name=='end':uc.emu_stop()
  elif name=='send':n,p=self.args(2);assert n==5;self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret(0,8)
  elif name=='PtInRect':
   rect,x,y=self.args(3);l,t,r,b=struct.unpack('<4i',uc.mem_read(rect,16));self.ret(int(l<=x<r and t<=y<b),12)
  elif name.startswith('??2@')or name=='#823':self.ret(0x0d080000)
  elif name.startswith('??3@')or name=='#825':self.ret()
  elif name in ['temporary-destructor']:self.ret()
  elif name=='refresh-toggles':self.ret()
  elif name=='refresh-selection':self.ret(0,4)
  elif name=='base-mouseup':self.ret(0,8)
  else:raise RuntimeError('Unimplemented boundary '+name+' '+hex(a))
 def run(self,address,args,ecx=0):
  sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.stop,*args));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.reg_write(UC_X86_REG_ECX,ecx);self.uc.emu_start(address,self.stop+1,timeout=1000000,count=200000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop
 def toggle(self,record,value):self.sent=[];self.run(0x465700,[record,value]);return self.sent[0]
 def swap(self,count,source,target):
  self.sent=[];rng=random.Random(count*100+source*10+target);rows=[]
  for i in range(count):
   b=bytearray(rng.randbytes(732));struct.pack_into('<I',b,0,0x484264);struct.pack_into('<4H',b,4,i,1+i%3,1+i%2,[100,101,103][i%3]);struct.pack_into('<I',b,32,i+100);rows.append(bytes(b))
  self.uc.mem_write(self.records,b''.join(rows));self.write(self.manager+0x38,count);self.write(self.manager+0x3c,self.records)
  self.write(self.window+0xb0,self.manager);self.write(self.window+0x78,1);self.write(self.window+0x154,1);self.write(self.window+0x158,source);self.write(self.window+0x164,0)
  self.write(self.window+0x10,target*40+16);self.write(self.window+0x14,16)
  for i in range(count):self.uc.mem_write(self.window+0xb4+i*16,struct.pack('<4i',i*40,0,i*40+32,32))
  self.run(0x4652a0,[0,0],self.window)
  result=[bytes(self.uc.mem_read(self.records+i*732,732))for i in range(count)]
  if source!=target:
   for i in range(count):
    old=rows[target if i==source else source if i==target else i];expected=bytearray(old)
    if i in [source,target]:struct.pack_into('<H',expected,4,i)
    assert result[i]==expected,'native opaque record preservation'
  else:assert rows==result and not self.sent
  project=lambda b:dict(id=str(struct.unpack_from('<I',b,32)[0]),nativeRecord=struct.unpack_from('<H',b,4)[0],nativeStatus=struct.unpack_from('<H',b,6)[0],deployment=struct.unpack_from('<H',b,8)[0],job=struct.unpack_from('<H',b,10)[0])
  return dict(source=source,target=target,rows=list(map(project,rows)),expected=list(map(project,result)),packet=self.sent[0]if self.sent else None)
def main():
 WORK.mkdir(exist_ok=True);native=Native();toggles=[dict(record=r,value=v,packet=native.toggle(r,v))for r in [0,1,255,256,65534]for v in [1,2]];swaps=[native.swap(n,s,t)for n in range(1,11)for s in range(n)for t in range(n)]
 (WORK/'cases.json').write_text(json.dumps(dict(toggles=toggles,swaps=swaps)));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeCommanderChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'NativeCommanderChecks.cs'));rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 result=json.loads(p.stdout);result.update(nativeSha256=SHA,nativeCodeExecuted=True,unityPlay=False,nativeToggleCases=len(toggles),nativeDragCases=len(swaps),mismatches=0,logs=str(WORK),scope='Actual Saver465700 byte/word serialization;4652A0 drag builder +427BC0 native732byte exchange. Send, temporary destructor and UI refresh are isolated; no host network. Managed typed roster projection compared; no claim of complete raw record receiver.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_commanders.py',OUT/'NativeCommanderChecks.cs',ROOT/'Assets/_Original/Runtime/Growth/CommanderRoster.cs']})
 (OUT/'native-commander-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
