"""Late original settings callbacks and checkbox initialization vs managed client settings."""
import hashlib,json,os,subprocess
from pathlib import Path
from check_native_commanders import Native
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-settings-checks')
class Settings(Native):
 def __init__(self,state):
  super().__init__();self.write(0x4953b8,self.manager);self.write(self.window+0x54,0x0d006000);self.events=[]
  for i in range(8):self.write(0x0d006000+i*4,0x0d007000+i*32)
  for field,address in [('music',0x495320),('sound',0x495324),('globalChat',0x490ae0),('whisper',0x490ae4),('viewMenu',0x490ae8)]:self.write(address,int(state[field]))
  self.hooks[0x41f2a0]='music';self.hooks[0x41f2e0]='sound'
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name in ['music','sound']:
   v=self.args(1)[0];self.events.append([name,v]);self.write(0x495320 if name=='music'else 0x495324,v);self.ret(0,4)
  elif name=='send':n,p=self.args(2);assert n==3;self.sent.append(bytes(uc.mem_read(p,n)).hex());self.ret(0,8)
  else:super().hook(uc,a,size,data)
 def state(self):return {field:bool(self.read(address))for field,address in [('music',0x495320),('sound',0x495324),('globalChat',0x490ae0),('whisper',0x490ae4),('viewMenu',0x490ae8)]}
 def panel(self):
  self.run(0x45a130,[],self.window)
  return [self.read(0x0d007000+i*32+12)==2 for i in range(8)]
 def toggle(self,control,value):
  self.write(0x0d007000+control*32+12,int(value)+1);self.run(0x459ef0,[control],self.window)
  return dict(state=self.state(),packet=self.sent[0]if self.sent else None,controlChecked=self.read(0x0d007000+control*32+12)==2)
def main():
 WORK.mkdir(exist_ok=True);cases=[]
 for bits in range(32):
  state={k:bool(bits&(1<<i))for i,k in enumerate(['music','sound','globalChat','whisper','viewMenu'])}
  for control in range(1,7):
   for value in [False,True]:
    native=Settings(state);panel=native.panel();cases.append(dict(initial=state,control=control,value=value,panel=panel,expected=native.toggle(control,value)))
 (WORK/'cases.json').write_text(json.dumps(cases));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ClientSettingsChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'ClientSettingsChecks.cs'))+'\n"'+str(ROOT/'Assets/_Original/Runtime/UI/OriginalClientSettings.cs')+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1700:])
 r=json.loads(p.stdout);r.update(nativeCases=len(cases),nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',nativeCodeExecuted=True,unityPlay=False,mismatches=0,logs=str(WORK),scope='Original459EF0 checkbox callback,45A130 initialization,45A1C0 whisper request3byte builder. Native music/sound setter calls isolated; their internal policies have separate original audits. Not OS preference storage or actual window/audio Play.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_settings.py',OUT/'ClientSettingsChecks.cs',ROOT/'Assets/_Original/Runtime/UI/OriginalClientSettings.cs']});(OUT/'native-settings-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
