"""Native F4 held-item caption and inherited double-click callback evidence."""
import hashlib,json,os,subprocess,sys
from pathlib import Path
from native_item_caption_emulator import ItemCaptionNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-item-caption-checks')
def main():
 WORK.mkdir(exist_ok=True);native=ItemCaptionNative();cases=[];clicks=[]
 for name in [None,'','대거','가죽갑옷','a_b','이름 없음','무기 %d']:
  for uses in [-2147483648,-1,0,1,5000,2147483647]:
   for mode in [0,1,2,4]:cases.append(dict(name=name,uses=uses,mode=mode,expected=native.caption(name,uses,mode)))
 for delta in [-1,0,1,249,250,251,1000]:
  for minimized in [False,True]:
   result=native.double_click(delta,minimized);assert result['packets']==[]and result['noop']==int(delta<250 and not minimized)and result['restore']==int(delta<250 and minimized)and result['localX']==113 and result['localY']==25;clicks.append(dict(delta=delta,minimized=minimized,result=result))
 # Check the exact late F4 slot, inherited no-op and main-world/lesson mode getters.
 sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'));from native_pe import PE
 pe=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');assert pe.thunk(pe.u32(0x486108+0x28))==0x433e20;pe.expect(0x433e20,'c20800');pe.expect(0x42a5b0,'b801000000c3');pe.expect(0x435130,'b804000000c3')
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(captions=cases,clicks=clicks),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ItemCaptionChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'ItemCaptionChecks.cs'))
 for name in ['NativeItemCaption.cs','NativeWindowPlacement.cs']:args+='\n"'+str(ROOT/'Assets/_Original/Runtime/UI'/name)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1500:])
 r=json.loads(p.stdout);r.update(nativeCodeExecuted=True,nativeSha256=pe.sha256,nativeCaptionCases=len(cases),nativeInheritedClickCases=clicks,logs=str(WORK),scope='Actual46BF20 formatting,433D00 clock dispatch and actual F4+28 inherited433E20 RET8. Shared signed32bit strict<250 clock compared with native14inputs. MFC/clock/allocator and minimized UI restore isolated. Full inventory down/up/drop handlers, Unity pointer/drag clock phase are outside execution scope. Item caption anchor230,192/TA_CENTER6 and gold104,193/TA_RIGHT2 from46B860 static source; unknown uses remain unknown.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_item_caption.py',OUT/'native_item_caption_emulator.py',OUT/'ItemCaptionChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeItemCaption.cs',ROOT/'Assets/_Original/Runtime/UI/NativeItemPress.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowPlacement.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWindowClickInput.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Personal.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Commanders.cs']});(OUT/'native-item-caption-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k not in ['sources','nativeInheritedClickCases']}))
if __name__=='__main__':main()
