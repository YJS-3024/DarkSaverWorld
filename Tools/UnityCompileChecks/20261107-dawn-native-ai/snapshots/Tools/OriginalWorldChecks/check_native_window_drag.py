"""Source title regions/unbounded native drag versus the runtime state and catalog."""
import hashlib,json,os,random,subprocess
from pathlib import Path
from native_window_drag_emulator import WindowDragNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-window-drag-checks')
def main():
 WORK.mkdir(exist_ok=True);native=WindowDragNative();specs=json.loads((ROOT/'Assets/_Original/Resources/OriginalFacilityWindows.json').read_text())['windows'];cases=[];rng=random.Random(4330)
 for spec in specs:
  regions=[[spec['dragX'],spec['dragY'],spec['dragX']+spec['dragWidth'],spec['dragY']+spec['dragHeight']]]
  if spec.get('minWidth'):regions.append([spec['minDragX'],spec['minDragY'],spec['minDragX']+spec['minDragWidth'],spec['minDragY']+spec['minDragHeight']])
  for region in regions:
   left,top,right,bottom=region
   for x,y in [(0,0),(33,75),(-200,450),(600,-80)]:
    for dx,dy in [(left,top),(right-1,bottom-1),(right,top),(left,bottom),(left-1,top),(left,top-1)]:
     for enabled in [False,True]:
      steps=[[px,py,enable,lock]for px,py,enable,lock in [(-100,-250,True,False),(900,700,True,False),(150,160,False,False),(500,80,True,True),(-450,120,True,False)]]
      down=[x+dx,y+dy];expected=native.sequence(x,y,region,down,steps,enabled);cases.append(dict(x=x,y=y,region=region,down=down,enabled=enabled,steps=steps,expected=expected))
 for i in range(240):
  x,y=rng.randint(-500,900),rng.randint(-500,900);region=[11,11,318,73];down=[x+rng.randint(0,330),y+rng.randint(0,80)];steps=[[rng.randint(-1000,1400),rng.randint(-1000,1400),bool(rng.randrange(2)),bool(rng.randrange(2))]for _ in range(8)];expected=native.sequence(x,y,region,down,steps);cases.append(dict(x=x,y=y,region=region,down=down,enabled=True,steps=steps,expected=expected))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(cases));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'WindowDragChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'WindowDragChecks.cs'))
 for name in ['NativeWindowDrag.cs','NativeWindowPlacement.cs','NativeFacilityWindows.cs']:args+='\n"'+str(ROOT/'Assets/_Original/Runtime/UI'/name)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 result=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_window_drag_emulator.py',OUT/'WindowDragChecks.cs',OUT/'native-facility-windows-art.json',ROOT/'Assets/_Original/Resources/OriginalFacilityWindows.json',ROOT/'Assets/_Original/Runtime/UI/NativeWindowDrag.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowDragInput.cs',ROOT/'Assets/_Original/Runtime/UI/NativeFacilityWindows.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowPlacement.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Personal.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.ClassChange.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Chat.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.PrivateChat.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Utilities.cs']
 evidence=ROOT/'분석/Ghidra/Saver_Late/FacilityWindows/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];paths.append(path)
 result.update(nativeCodeExecuted=True,nativeTitlePressCases=len(cases),nativeMotionCases=sum(len(row['steps'])for row in cases),logs=str(WORK),scope='Actual433BE0 with button count0 (title-only) and433EC0 on LSC full/mini regions, enabled/locked/offscreen motion. Production state, source catalog identity and boundary-hit rules compared. Current uGUI pointer-down offset/no threshold/cancel/foreground behavior has separate46 lifecycle assertions, not OS/EventSystem/real window pixels. No button-priority native manager or full min/restore rendering claim.',sources={str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest()for path in paths});(OUT/'native-window-drag-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
