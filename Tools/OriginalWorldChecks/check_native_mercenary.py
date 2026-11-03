"""Original late mercenary window labels/wire/private preview versus managed bridge."""
import hashlib,itertools,json,os,subprocess
from pathlib import Path
from native_mercenary_window_emulator import MercenaryNative
from build_native_mercenary_catalog import data
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-mercenary-checks')
def main():
 WORK.mkdir(exist_ok=True);native=MercenaryNative();actions=[];labels=[]
 for commander,hire,maximum,hired,count,selected in itertools.product([0,37,65534],[False,True],[0,1,2,32768,-1],[-1,200,201],[0,1,2,32767],[-1,0,1,2]):
  offers=[(200,maximum),(217,3)];selectedjob=offers[selected][0]if 0<=selected<len(offers)else -1;expected=native.request(hire,commander,hired,count,offers,selected)
  actions.append(dict(commander=commander,hire=hire,hired=hired,count=count,offers=offers,selectedJob=selectedjob,expected=expected))
 units=data()['units']
 for i,unit in enumerate(units):
  for depth in [8,16]:
   x,y=[(0,0),(113,20),(-25,-30)][i%3];count=i%7;maximum=i%6+1;gold=[0,1,2**32-1,2**32+17,2**64-1][i%5]
   expected=native.labels(unit,unit['job']if i%2 else-1,count,[(unit['job'],maximum)],0,gold,x,y,depth);assert len(expected)==3
   labels.append(dict(unit=unit,count=count,limit=maximum,gold=gold,x=x,y=y,depth=depth,expected=expected))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(actions=actions,labels=labels),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeMercenaryChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeMercenaryChecks.cs'))
 names=['UI/NativeMercenaryClient.cs','UI/NativeCurrencyText.cs','Entry/LocalServerManager.NativeMercenary.cs','World/LocalServerManager.Mercenary.cs']
 for path in [*[ROOT/'Assets/_Original/Runtime'/n for n in names],OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs']:args+='\n"'+str(path)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2200:])
 result=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_mercenary_window_emulator.py',OUT/'native_shop_details_emulator.py',OUT/'NativeMercenaryChecks.cs',OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs',*[ROOT/'Assets/_Original/Runtime'/n for n in names],ROOT/'Assets/_Original/Runtime/Growth/NativeMercenaryCatalog.cs',ROOT/'Assets/_Original/Runtime/World/MercenaryTransferService.cs',ROOT/'Assets/_Original/Runtime/World/WorldModel.cs',ROOT/'Assets/_Original/Runtime/World/WorldService.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs',ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs',ROOT/'Assets/_Original/Resources/OriginalMercenarySkills.json']
 evidence=ROOT/'분석/Ghidra/Saver_Late/Mercenary/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];paths.append(path)
 result.update(nativeCodeExecuted=True,nativeActionCases=len(actions),nativeUnitLabelCases=len(labels),nativeLabelCalls=sum(len(row['expected'])for row in labels),logs=str(WORK),scope='Unmodified late470070/4701F0 packets and private count mutations; full46F7F0 text/coordinates through isolated MFC/surface/font/table boundaries. All92 unit source base/price rows across8/16bit. Managed real service/store/manager integration protects identity, duplicates, approval/save failure/close. No client-invented ID, financial approval/refund or saved prediction. GDI selection pixels/Unity pointer/actual animation/audio/Play not executed.',sources={str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest()for path in paths});(OUT/'native-mercenary-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
