"""Verify native buttonless message44 and source-derived runtime image without Editor Play."""
import hashlib,json,os,subprocess,itertools,struct,tempfile,re
from pathlib import Path
from build_native_world_message import compose
from native_world_message_emulator import MessageNative
from native_window_placement_emulator import PlacementNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-world-message-checks')
def main():
 WORK.mkdir(exist_ok=True);meta=Path(str(ROOT/'Assets/_Original/Resources/OriginalWorldNotice.bytes')+'.meta');generation=json.loads((OUT/'native-world-message-art.json').read_text());assert 'TextScriptImporter:'in meta.read_text()and re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1]==generation['resourceGuid'];rgba,spec=compose();resource=ROOT/'Assets/_Original/Resources/OriginalWorldNotice.bytes';assert resource.read_bytes()==b'DSN1'+struct.pack('<HH',562,84)+rgba and not spec['buttons'];native=MessageNative();draws=[]
 for paragraph,depth,origin,text in itertools.product([False,True],[8,16],[(0,0),(39,20),(100,88)],['','당신은 저축할 돈이 없습니다.','여기서 팔 수 있는 물건이 아닙니다.','첫 줄\n두 번째 줄']):
  x,y=origin;result=native.draw(text,x,y,paragraph,depth);draws.append(dict(paragraph=paragraph,depth=depth,origin=list(origin),text=text,drawn=result))
 assert native.clear()==dict(result=1,closed=[44],pointer=0)and native.clear(False)==dict(result=0,closed=[],pointer=0)and native.clear(True,True)==dict(result=1,closed=[44],pointer=0)
 placer=PlacementNative();placements=[]
 for x,y in [(0,0),(39,20),(-10,10),(700,500),(-800,-600)]:
  row=placer.resolve(dict(id=44,width=562,height=84,region=[0,0,0,0],minWidth=0,minHeight=0,minRegion=[0,0,0,0],x=x,y=y,center=False));placements.append(dict(inputX=x,inputY=y,**row))
 (WORK/'cases.json').write_text(json.dumps(dict(draws=draws,placements=placements),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeWorldMessageChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeWorldMessageChecks.cs'));names=['UI/NativeWorldMessage.cs','UI/NativeWorldMessageInput.cs','UI/NativeWindowPlacement.cs']
 for name in names:args+='\n"'+str(ROOT/'Assets/_Original/Runtime'/name)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2300:])
 report=json.loads(p.stdout);paths=[Path(__file__),OUT/'build_native_world_message.py',OUT/'native-world-message-art.json',OUT/'native_world_message_emulator.py',OUT/'NativeWorldMessageChecks.cs',resource,ROOT/'Assets/_Original/SourceData/Native/wlib/scene28.lsc.bytes',ROOT/'Assets/_Original/SourceData/Native/wlib/waitlib.lce.bytes',ROOT/'.gitattributes',*[ROOT/'Assets/_Original/Runtime'/n for n in names],*[ROOT/'Assets/_Original/Runtime/World'/n for n in ['WorldSceneRoot.Message.cs','WorldSceneRoot.Facilities.cs','WorldSceneRoot.Bank.cs','WorldSceneRoot.cs','WorldSceneRoot.Utilities.cs']]]
 for source in [ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Bank.cs']:assert'NativeWindow(29'not in source.read_text(),'wrong yes/no message frame remains'
 with tempfile.TemporaryDirectory(prefix='ds-message-checkout-')as tmp:
  subprocess.run(['git','init','-q',tmp],check=True,capture_output=True);Path(tmp,'.gitattributes').write_bytes((ROOT/'.gitattributes').read_bytes())
  blob=subprocess.check_output(['git','hash-object','-w','--no-filters','--stdin'],input=resource.read_bytes(),cwd=tmp).decode().strip()
  filtered=subprocess.check_output(['git','-c','core.autocrlf=true','cat-file','--filters','--path='+str(resource.relative_to(ROOT)),blob],cwd=tmp);assert filtered==resource.read_bytes()
 evidence=ROOT/'분석/Ghidra/Saver_Late/WorldMessage/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  source=ROOT/row['asset'];assert hashlib.sha256(source.read_bytes()).hexdigest()==row['sha256'];paths.append(source)
 report.update(nativeCodeExecuted=True,nativeDrawCases=len(draws),nativeCloseCases=3,nativePlacementCases=len(placements),sourcePixels=len(rgba)//4,sourceButtons=0,mismatches=0,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths});(OUT/'native-world-message-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
