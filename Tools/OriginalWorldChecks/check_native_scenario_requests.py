"""Execute original late GameCtl request builders; isolate alloc/free/final send only."""
import hashlib,json,struct,subprocess,os
from pathlib import Path
from native_ai_emulator import pefile,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent;WORK=Path('/tmp/ds-native-scenario-request-checks')
SHA='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
class Requests:
 def __init__(self):
  inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
  row=next(f for p in inventory['payloads']for f in p['files']if f.get('sha256')==SHA);raw=Path(row['extractedFile']).read_bytes();assert hashlib.sha256(raw).hexdigest()==SHA
  self.pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0x10000000,(self.pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x10000000,self.pe.get_memory_mapped_image())
  self.uc.mem_map(0x01000000,0x100000);self.uc.mem_map(0,0x1000);self.uc.mem_map(0x02000000,0x100000);self.hooks={0x01001000:'stop',0x1000e2c0:'send'}
  for i,imp in enumerate(i for d in self.pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
   address=0x02001000+i*16;self.uc.mem_write(imp.address,struct.pack('<I',address));self.hooks[address]=imp.name.decode()if imp.name else '#'+str(imp.ordinal)
  self.uc.hook_add(UC_HOOK_CODE,self.hook)
 def ret(self,value=0):
  sp=self.uc.reg_read(UC_X86_REG_ESP);pc=struct.unpack('<I',self.uc.mem_read(sp,4))[0];self.uc.reg_write(UC_X86_REG_ESP,sp+4);self.uc.reg_write(UC_X86_REG_EAX,value);self.uc.reg_write(UC_X86_REG_EIP,pc)
 def arg(self,n):return struct.unpack('<'+'I'*n,self.uc.mem_read(self.uc.reg_read(UC_X86_REG_ESP)+4,4*n))
 def hook(self,uc,address,size,data):
  name=self.hooks.get(address)
  if name is None:return
  if name=='stop':uc.emu_stop()
  elif name=='send':
   p,n=self.arg(2);assert n in [3,5];self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret()
  elif name.startswith('??2@') or name=='#823':
   n=self.arg(1)[0];assert n in [3,5];uc.mem_write(0x02080000,b'\0'*n);self.ret(0x02080000)
  elif name.startswith('??3@') or name=='#825':self.ret()
  else:raise RuntimeError('Unimplemented native boundary '+name+' at '+hex(address))
 def run(self,address,value):
  self.sent=[];sp=0x010ff000;self.uc.mem_write(sp,struct.pack('<II',0x01001000,value&0xffffffff));self.uc.reg_write(UC_X86_REG_ESP,sp)
  self.uc.emu_start(address,0x01001001,timeout=1000000,count=100000)
  assert self.uc.reg_read(UC_X86_REG_EIP)==0x01001000 and len(self.sent)==1;return self.sent[0]
def main():
 WORK.mkdir(exist_ok=True);native=Requests();cases=[]
 for token,address in [('GETGENERAL',0x1000ea30),('GOLD',0x1000e420),('USEITEM',0x1000e470)]:
  for value in [-2147483648,-65537,-32769,-1,0,1,101,398,65535,65536,2147483647]:cases.append(dict(command=token+' '+str(value),packet=native.run(address,value)))
 for arg,mode in [('',1),('ALONE',2)]:cases.append(dict(command='SCENECLEAR '+arg,packet=native.run(0x1000eab0,mode)))
 (WORK/'native.json').write_text(json.dumps(cases))
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';old=Path('/tmp/ds-original-campaign-checks');exe=WORK/'NativeScenarioPackets.dll';source=WORK/'NativeScenarioPackets.cs'
 source.write_text('using System;using System.IO;using System.Text.Json;using DarkSaver.Original.Campaign;class NativeScenarioPackets{static void Main(string[]a){using var d=JsonDocument.Parse(File.ReadAllText(a[0]));foreach(var r in d.RootElement.EnumerateArray()){var c=r.GetProperty("command").GetString();var managed=Convert.ToHexString(OriginalCampaignScript.Packet(new CampaignCommand(c,1))).ToLowerInvariant();if(managed!=r.GetProperty("packet").GetString())throw new Exception(c+" "+managed);}Console.WriteLine(d.RootElement.GetArrayLength());}}')
 args=(old/'checks.rsp').read_text().replace(str(old/'CampaignChecks.dll'),str(exe)).replace(str(OUT/'CampaignChecks.cs'),str(source));rsp=WORK/'checks.rsp';rsp.write_text(args)
 env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(WORK/'native.json')])]:
  if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'CampaignChecks.runtimeconfig.json').read_bytes())
  r=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
  if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
 result=dict(cases=len(cases),mismatches=0,nativeCodeExecuted=True,unityPlay=False,nativeSha256=SHA,logs=str(WORK),scope='Original GameCtl1000EA30/1000E420/1000E470/1000EAB0 and byte/WORD/DWORD appenders execute. Bounded allocation/free/final send boundary only; no host network/server grants.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_scenario_requests.py',ROOT/'Assets/_Original/Runtime/Campaign/CampaignModel.cs']})
 (OUT/'native-scenario-request-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
