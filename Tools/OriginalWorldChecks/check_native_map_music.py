"""Read-only x86 map/script music-state differential; sound device/MFC strings isolated."""
import hashlib,json,os,struct,subprocess
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX,UC_X86_REG_ECX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent;WORK=Path('/tmp/ds-native-map-music-checks')
SHA='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689';TABLE=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/midiset.txt.txt'
class NativeMusic:
 def __init__(self,rows):
  inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());source=next(f['extractedFile']for p in inventory['payloads']for f in p['files']if f.get('sha256')==SHA)
  raw=Path(source).read_bytes();assert hashlib.sha256(raw).hexdigest()==SHA
  pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x400000,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x400000,pe.get_memory_mapped_image())
  self.uc.mem_map(0,0x1000)
  for a in [0x0d000000,0x0e000000,0x0f000000]:self.uc.mem_map(a,0x100000)
  self.hooks={0x0e000000:'end',0x479598:'string-default',0x479586:'string-destroy',0x4795da:'string-copy',0x4795e6:'string-copy'}
  for i,imp in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
   target=0x0e001000+i*16;self.write(imp.address,target);self.hooks[target]=imp.name.decode()if imp.name else '#'+str(imp.ordinal)
  self.next_string=0x0d010000;self.empty=self.string('');self.object=0x0d000100;self.write(self.object+4,1000);self.write(self.object+8,0)
  self.write(self.object+0xc,len(rows));self.write(self.object+0x10,0x0d003000);self.write(self.object+0x14,0x0d004000)
  for i,(key,path) in enumerate(rows):self.write(0x0d003000+i*4,key);self.write(0x0d004000+i*4,self.string(path))
  self.write(0x495320,1);self.write(0x4952a0,0x0d006000);self.write(0x0d006124,0x0d007000)
  self.events=[];self.success=True;self.uc.hook_add(UC_HOOK_CODE,self.hook)
 def write(self,address,value):self.uc.mem_write(address,struct.pack('<I',value&0xffffffff))
 def read(self,address):return struct.unpack('<I',self.uc.mem_read(address,4))[0]
 def string(self,text):
  data=text.encode();ptr=self.next_string+12;self.uc.mem_write(self.next_string,struct.pack('<iii',1,len(data),len(data))+data+b'\0');self.next_string=(ptr+len(data)+16)&~15;return ptr
 def text(self,ptr):
  result=b''
  while (b:=bytes(self.uc.mem_read(ptr,1)))!=b'\0':result+=b;ptr+=1
  return result.decode()
 def args(self,n):return struct.unpack('<'+'I'*n,self.uc.mem_read(self.uc.reg_read(UC_X86_REG_ESP)+4,n*4))
 def ret(self,value=0,cleanup=0):
  sp=self.uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EAX,value);self.uc.reg_write(UC_X86_REG_EIP,pc)
 def hook(self,uc,address,size,data):
  name=self.hooks.get(address)
  if name is None:return
  if name=='end':uc.emu_stop()
  elif name=='string-default':p=uc.reg_read(UC_X86_REG_ECX);self.write(p,self.empty);self.ret(p)
  elif name=='string-destroy':self.ret()
  elif name=='string-copy':p=uc.reg_read(UC_X86_REG_ECX);self.write(p,self.read(self.args(1)[0]));self.ret(p,4)
  elif 'Play@CDirectMusic@@'in name:
   path=self.text(self.args(2)[0]);self.events.append(['play',path]);self.ret(int(self.success),8)
  elif 'Stop@CDirectMusic@@'in name:self.events.append(['stop']);self.ret()
  else:raise RuntimeError('Unimplemented native boundary '+name+' at '+hex(address))
 def step(self,s):
  self.events=[];self.success=s.get('success',True);args=[];address={'map':0x41f310,'enable':0x41f2a0,'play':0x41f8f0,'stop':0x41f980}[s['op']]
  if s['op']in ['map','enable']:args=[s['value']]
  if s['op']=='play':args=[self.string(s['path']),s.get('option',1)]
  sp=0x0f0ff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),0x0e000000,*[a&0xffffffff for a in args]));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.reg_write(UC_X86_REG_ECX,self.object)
  self.uc.emu_start(address,0x0e000001,timeout=1000000,count=100000)
  assert self.uc.reg_read(UC_X86_REG_EIP)==0x0e000000
  map_id=self.read(self.object+4);return dict(map=map_id if map_id<2147483648 else map_id-4294967296,accepted=bool(self.read(self.object+8)),enabled=bool(self.read(0x495320)),events=self.events)
def cases():
 rows=TABLE.read_text().splitlines();n=int(rows[0]);entries=[(int(r.split()[0]),'BGM\\'+r.rsplit(' ',1)[1])for r in rows[1:n+1]]
 result=[]
 for key,_ in entries+[(0,''),(1,''),(26,''),(999,''),(1503,''),(1504,''),(1505,''),(2147483647,''),(-1,'')]:
  for success in [True,False]:result.append([dict(op='map',value=key,success=success),dict(op='map',value=key,success=success),dict(op='enable',value=0),dict(op='map',value=1010),dict(op='enable',value=1,success=success)])
 result += [
  [dict(op='map',value=1000),dict(op='map',value=1004),dict(op='play',path='BGM\\NCO01.MID',option=0),dict(op='map',value=1004),dict(op='stop'),dict(op='map',value=1004)],
  [dict(op='play',path='BGM\\NCO01.MID'),dict(op='enable',value=0),dict(op='play',path='BGM\\missing.mid'),dict(op='map',value=26),dict(op='enable',value=1)],
  [dict(op='map',value=1000),dict(op='map',value=26),dict(op='play',path='BGM\\missing.mid',success=False),dict(op='map',value=26),dict(op='map',value=1000)],
  [dict(op='enable',value=1),dict(op='map',value=1000),dict(op='enable',value=1),dict(op='enable',value=0),dict(op='enable',value=0),dict(op='enable',value=1)]
 ]
 return entries,result

def main():
 WORK.mkdir(exist_ok=True);rows,sequences=cases();expected=[]
 for seq in sequences:
  native=NativeMusic(rows);expected.append([native.step(step)for step in seq])
 (WORK/'cases.json').write_text(json.dumps(sequences));(WORK/'native.json').write_text(json.dumps(expected))
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';old=Path('/tmp/ds-original-campaign-checks');exe=WORK/'MapMusicChecks.dll'
 args=(old/'checks.rsp').read_text().replace(str(old/'CampaignChecks.dll'),str(exe)).replace(str(OUT/'CampaignChecks.cs'),str(OUT/'MapMusicChecks.cs'))
 rsp=WORK/'checks.rsp';rsp.write_text(args);env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json'),str(WORK/'native.json')])]:
  if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'CampaignChecks.runtimeconfig.json').read_bytes())
  r=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
  if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2200:])
 result=json.loads(r.stdout);result.update(nativeSha256=SHA,nativeCodeExecuted=True,unityPlay=False,sequences=len(sequences),nativeTransitions=sum(map(len,sequences)),mismatches=0,logs=str(WORK),scope='Actual Saver0041F310/0041F820/0041F2A0/0041F400/0041F8F0 control and lookup. Native MFC CString storage and DirectMusic device calls are isolated. Not real audio playback.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [TABLE,ROOT/'Assets/_Original/Runtime/Campaign/CampaignModel.cs',OUT/'MapMusicChecks.cs',OUT/'check_native_map_music.py']})
 (OUT/'map-music-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
