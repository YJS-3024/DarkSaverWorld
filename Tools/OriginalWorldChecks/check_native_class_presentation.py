"""Original class text/metrics and actual MFC selection calls versus current UI helpers."""
import hashlib,itertools,json,os,random,subprocess
from pathlib import Path
from native_class_presentation_emulator import ClassMagicNative,ClassBarsNative,ClassHeaderNative,ClassSelectionNative,ClassIconNative,ClassGhostNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-class-presentation-checks')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True);magic=[];bars=[];headers=[];selections=[];m=ClassMagicNative();b=ClassBarsNative();h=ClassHeaderNative();s=ClassSelectionNative();catalog=ROOT/'Assets/_Original/Data/Combat/MagicMenu.json'
 for item in json.loads(catalog.read_text())['entries']:magic.append(dict(item=item,expected=m.description(item)))
 magic.append(dict(item=None,expected=m.description(None)))
 for mp,distance,range_,team in itertools.product([-1,0,1],[0,1],[0,1],[0,34]):
  item=dict(id=101,name='경계 마법',mp=mp,distance=distance,range=range_,team=team);magic.append(dict(item=item,expected=m.description(item)))
 for stat,baseline,before,limit in itertools.product(range(6),[-1,0,10,100,32767],[-1,0,11,99,32767],[-32768,-1,0,1,99,32767]):bars.append(dict(stat=stat,baseline=baseline,before=before,after=13,limit=limit,expected=b.metric(stat,baseline,before,13,limit)))
 for level,xp,origin in itertools.product([-32768,-1,0,1,32767],[0,1,2**31-1,2**31,2**32-1,2**64-1],[(0,0),(35,50)]):
  x,y=origin;headers.append(dict(level=level,xp=xp,x=x,y=y,expected=h.header(level,xp,x,y)))
 for list_kind,index,slot,origin in itertools.product([0,1],[-1,0,7],[-1,0,1,2],[(0,0),(35,50)]):
  x,y=origin;selections.append(dict(list=list_kind,index=index,slot=slot,x=x,y=y,expected=s.selection(list_kind,index,slot,x,y)))
 icons=[];icon_native=ClassIconNative()
 for item in json.loads(catalog.read_text())['entries']:
  for list_kind in [0,1]:icons.append(dict(item=item,list=list_kind,index=0,drag=False,surface=True,matching=True,x=0,y=0,expected=icon_native.icons(item,list_kind)))
 item=json.loads(catalog.read_text())['entries'][0]
 for list_kind,index,drag,surface,matching in itertools.product([0,1],[0,7],[False,True],[False,True],[False,True]):
  icons.append(dict(item=item,list=list_kind,index=index,drag=drag,surface=surface,matching=matching,x=35,y=50,expected=icon_native.icons(item,list_kind,index,drag,surface,matching,35,50)))
 ghosts=[];ghost_native=ClassGhostNative()
 for list_kind in [0,1]:
  for opposite,(x,y),origin in itertools.product([0,7,8]if list_kind==0 else[0,15,16],[(207,121),(208,121),(239,152),(240,121),(241,121),(471,152),(472,152),(208,47),(239,78),(240,47),(471,111),(472,111),(224,63),(224,137)],[(0,0),(35,50)]):ghosts.append(dict(list=list_kind,opposite=opposite,x=x,y=y,origin=list(origin),expected=ghost_native.ghost(list_kind,opposite,x,y,origin)))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(magic=magic,bars=bars,headers=headers,selections=selections,icons=icons,ghosts=ghosts),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ClassPresentationChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'ClassPresentationChecks.cs'))+'\n"'+str(ROOT/'Assets/_Original/Runtime/UI/NativeClassPresentation.cs')+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1900:])
 result=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_class_presentation_emulator.py',OUT/'native_class_adjustments_emulator.py',OUT/'ClassPresentationChecks.cs',catalog,ROOT/'Assets/_Original/Runtime/UI/NativeClassPresentation.cs',ROOT/'Assets/_Original/Runtime/Growth/NativeClassMagicClient.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.StatAdjustment.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.MagicLoadout.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.ClassChange.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',OUT/'native-class-presentation-art.json'];art=json.loads(paths[-1].read_text());assert sha(ROOT/art['resource'])==art['sha256'];paths.extend(ROOT/name for name in art['sources']);paths.append(ROOT/art['resource'])
 for row in art['outputs']:assert sha(ROOT/row['asset'])==row['sha256'];paths.append(ROOT/row['asset'])
 assert art['embeddedBitmap']['id']==154 and art['embeddedBitmap']['dibSha256']=='6e840a0dfab43f2ae17abf51c27cbc081ff74fdc93c745e13a65556a9469b0e4'
 from PIL import Image
 image=Image.open(ROOT/art['outputs'][0]['asset']).convert('RGBA');assert image.size==(32,32)and hashlib.sha256(image.tobytes()).hexdigest()==art['embeddedBitmap']['rgbaSha256'];assert all(a==(0 if r==g==b==0 else 255)for r,g,b,a in image.get_flattened_data())
 evidence=ROOT/'분석/Ghidra/Saver_Late/ClassMagicDrag/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  q=ROOT/row['asset'];assert sha(q)==row['sha256'];paths.append(q)
 result.update(nativeMagicCases=len(magic),nativeBarCases=len(bars),nativeHeaderCases=len(headers),nativeSelectionCases=len(selections),nativeIconCases=len(icons),nativeGhostCases=len(ghosts),nativeCodeExecuted=True,logs=str(WORK),scope='Actual464380 ghost/forbidden overlay and4642E0/464330 drop-cell predicates;4635C0/463720 raw magic icon coordinates, identity/surface/drag branches without invented animation;462740 magic strings,464150 six-attribute field/width/min logic with Win32 MulDiv as a host boundary,462C50 header record reads/software-font calls, and463CE0 with actual MFC42D stock-object slot plus GDI rectangles/fills captured. Current presentation helpers/resource source integrity compared. Source8-point Gulim stat text uses11 logical pixels at96DPI in Unity; no original GDI glyph pixels/DPI/raster equivalence, Unity UI hierarchy/drag/EventSystem/Play claim.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in paths});(OUT/'native-class-presentation-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
