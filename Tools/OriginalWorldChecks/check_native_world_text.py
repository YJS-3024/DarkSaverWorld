"""Execute original429F70 CString-length/byte request builder, including byte-count wrap."""
import hashlib,json,os,subprocess
from pathlib import Path
from check_native_commanders import Native
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-world-text-checks')
class TextNative(Native):
 def hook(self,uc,address,size,data):
  if self.hooks.get(address)=='send':
   n,p=self.args(2);assert 4<=n<=258;self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret(0,8)
  else:super().hook(uc,address,size,data)
 def packet(self,data,channel):
  self.sent=[];p=0x0d040010;self.write(p-8,len(data));self.uc.mem_write(p,data+b'\0');self.write(0x0d006000,p);self.run(0x429f70,[0x0d006000,channel&0xffffffff]);return self.sent[0]if self.sent else None

def main():
 WORK.mkdir(exist_ok=True);n=TextNative();cases=[]
 for length in [0,1,9,31,127,254,255,256,257,300,511,512,513]:
  data=bytes(65+i%26 for i in range(length))
  for channel in [0,1,3,5,9,255,256,-1]:cases.append(dict(data=data.hex(),channel=channel,packet=n.packet(data,channel)))
 command='/공간이동'.encode('cp949');cases.append(dict(data=command.hex(),channel=1,packet=n.packet(command,1)))
 (WORK/'cases.json').write_text(json.dumps(cases));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'WorldTextChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'WorldTextChecks.cs'))
 for p in [ROOT/'Assets/_Original/Runtime/UI/NativeWorldTextPackets.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.WorldText.cs',OUT.resolve()/'ClientWindowManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 r=json.loads(p.stdout);r.update(nativeCases=len(cases),nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',nativeCodeExecuted=True,unityPlay=False,mismatches=0,logs=str(WORK),scope='Original429F70 serialization and empty/modulo256 behavior; allocator/final send isolated. Production F12 local-manager request emits original7A0109 command only; no assumed server warp permission, destination or success.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_world_text.py',OUT/'WorldTextChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWorldTextPackets.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.WorldText.cs']});(OUT/'native-world-text-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
