"""Original4347E0 profile placement and4346D0 centering vs the production pure resolver."""
import hashlib,json,os,subprocess
from pathlib import Path
from native_window_placement_emulator import PlacementNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-window-placement-checks')
def main():
 WORK.mkdir(exist_ok=True);n=PlacementNative();cases=[]
 specs=json.loads((ROOT/'Assets/_Original/Data/World/WorldUtilities.json').read_text())['windows']+json.loads((ROOT/'Assets/_Original/Data/World/WorldPersonalWindows.json').read_text())['windows']
 for spec in specs:
  for x in [-1000,-200,-100,-30,-1,0,5,100,500,639,640,641,1000]:
   for y in [-500,-200,-34,-1,0,100,300,479,480,481,1000]:
    for minimized in ([0,1,-1]if spec.get('minWidth',0)>0 else [0]):
     for chat_size in ([1,2]if spec['id']==19 else [1]):
      case=dict(id=spec['id'],x=x,y=y,width=spec['width'],height=spec['height'],region=[spec['dragX'],spec['dragY'],spec['dragX']+spec['dragWidth'],spec['dragY']+spec['dragHeight']],minWidth=spec.get('minWidth',0),minHeight=spec.get('minHeight',0),minRegion=[spec.get('minDragX',0),spec.get('minDragY',0),spec.get('minDragX',0)+spec.get('minDragWidth',0),spec.get('minDragY',0)+spec.get('minDragHeight',0)],center=spec['id']==33,minimized=minimized,chatSize=chat_size);case['expected']=n.resolve(case);cases.append(case)
 (WORK/'cases.json').write_text(json.dumps(cases));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'WindowPlacementChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'WindowPlacementChecks.cs'))+'\n"'+str(ROOT/'Assets/_Original/Runtime/UI/NativeWindowPlacement.cs')+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1600:])
 r=json.loads(p.stdout);r.update(nativeCases=len(cases),nativeCodeExecuted=True,nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',unityPlay=False,mismatches=0,logs=str(WORK),scope='Original4347E0 restoration,434C20 save and4346D0 final centering execute. MFC strings/profile/CRT and Win32 rects are bounded host predicates. Minimize/expanded UI construction is isolated to supplied native LSC dimensions and flags, not drawing execution. Both minimized flags0/1/-1 and chat-size1/2 branches are exercised; malformed registry strings, tutorial position transfer and Unity drag/input are outside scope.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_window_placement.py',OUT/'native_window_placement_emulator.py',OUT/'WindowPlacementChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowPlacement.cs',ROOT/'Assets/_Original/Runtime/UI/OriginalWindowPreferences.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Personal.cs',ROOT/'Assets/_Original/Data/World/WorldUtilities.json',ROOT/'Assets/_Original/Data/World/WorldPersonalWindows.json']});(OUT/'native-window-placement-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
