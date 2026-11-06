"""Execute original1.2/late UserUnit tile getter; isolate DManage tile lookup only."""
import hashlib,json,os,struct,subprocess,sys
from pathlib import Path
from native_ai_emulator import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP,pefile
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-tile-attribute')
sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'));from native_pe import PE
class Getter:
 def __init__(self,path,expected):
  p=PE(path);assert p.sha256==expected;at=p.data.index(bytes.fromhex('8a40033cff740625ff000000c3'));self.start=p.va(at)-65;assert p.read(self.start,1)==b'\xa1';self.global_addr=p.u32(self.start+1)
  assert p.read(self.start+55,1)==b'\xe8';self.lookup=self.start+60+struct.unpack('<i',p.read(self.start+56,4))[0]
  image=pefile.PE(data=p.data);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(p.base,(image.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(p.base,image.get_memory_mapped_image())
  for base in [0x0d000000,0x0e000000,0x0f000000]:self.uc.mem_map(base,0x100000)
  self.uc.mem_write(self.global_addr,struct.pack('<I',0x0d001000));self.uc.mem_write(0x0d00104c,struct.pack('<I',0x0d002000));self.missing=False
  self.uc.hook_add(UC_HOOK_CODE,self.hook);self.sha=p.sha256
 def hook(self,uc,address,size,data):
  if address==0x0e000000:uc.emu_stop()
  elif address==self.lookup:
   sp=uc.reg_read(UC_X86_REG_ESP);pc=struct.unpack('<I',uc.mem_read(sp,4))[0];uc.reg_write(UC_X86_REG_EAX,0 if self.missing else 0x0d003000);uc.reg_write(UC_X86_REG_ESP,sp+12);uc.reg_write(UC_X86_REG_EIP,pc)
 def get(self,raw):
  self.uc.mem_write(0x0d003003,bytes([raw]));sp=0x0f080000;self.uc.mem_write(sp,struct.pack('<3I',0x0e000000,5,5));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.emu_start(self.start,0,count=1000);return self.uc.reg_read(UC_X86_REG_EAX)
def main():
 WORK.mkdir(exist_ok=True);variants=[('1.2',ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/userunit.dll','be05066507947df48d9f7beb25d2c3fb2523c4158519a78102e72021ebe57711'),('late',ROOT.parent/'.tools/ds-ghidra/installed/saver/UserUnit.dll','74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b')];results=[]
 for version,path,digest in variants:
  g=Getter(path,digest);values=[g.get(raw)for raw in range(256)];assert values==[i if i!=255 else 0 for i in range(256)];g.missing=True;assert g.get(128)==0;results.append(dict(version=version,sha256=digest,getter=hex(g.start),cases=257,values=values))
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';host=WORK/'Checks.cs';host.write_text('using System;using System.Linq;using System.Text.Json;using DarkSaver.Original.World;class Checks{static void Main(){Console.WriteLine(JsonSerializer.Serialize(Enumerable.Range(0,256).Select(NativeBodyOrder.Attribute).ToArray()));}}')
 refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))];source=ROOT/'Assets/_Original/Runtime/Combat/NativeBodyOrder.cs';exe=WORK/'Checks.dll';rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in ['-nologo','-target:exe','-nostdlib+','-langversion:latest','-out:'+str(exe),*refs,str(source),str(host)]));exe.with_suffix('.runtimeconfig.json').write_bytes(Path('/tmp/ds-original-entry-checks/EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
  p=subprocess.run(cmd,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1200:])
 assert json.loads(p.stdout)==results[0]['values']==results[1]['values']
 sources=[Path(__file__),source,ROOT/'Assets/_Original/Runtime/Tutorial/TutorialScript.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.BodyLayers.cs'];report=dict(nativeCases=514,managedCases=256,mismatches=0,nativeCodeExecuted=True,unityPlay=False,variants=[{k:v for k,v in r.items()if k!='values'}for r in results],scope='Actual old/late tile getter with DManage pointer lookup isolated. All byte values and null tile; actual production attribute normalization. Body scheduling/rendered tree occlusion and historical process/GPU are separate.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in sources});(OUT/'native-tile-attribute-check.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
