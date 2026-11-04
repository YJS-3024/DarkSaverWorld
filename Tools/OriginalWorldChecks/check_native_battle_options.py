"""Original GameCtl option/button/held-scroll execution versus current client routing."""
import hashlib,itertools,json,re,os,subprocess
from pathlib import Path
from native_battle_options_emulator import BattleOptionsNative
from native_battle_briefing_emulator import BattleBriefingNative
from native_battle_status_emulator import BattleStatusNative
from native_battle_menu_emulator import BattleMenuNative
from apply_pixel_import_policy import texture_paths
from pixel_texture_policy import lossless
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-battle-options-checks')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True);native=BattleOptionsNative();options=[];keys=[];scroll=[];compact=[];minimap=[];briefings=[];hovers=[]
 for bits in itertools.product([False,True],repeat=8):
  active,enabled=list(bits[:4]),list(bits[4:])
  for existing in [[],[0,1,3,4,5,6,7,9,24,25,26,27,28]]:
   for control in range(9):options.append(dict(control=control,active=active,enabled=enabled,existing=existing,expected=native.option(control,active,enabled,existing)))
 for bits in itertools.product([False,True],repeat=8):
  active,enabled=list(bits[:4]),list(bits[4:])
  for existing in [[],[0,1,3,4,5,6,7,9,24,25,26,27,28]]:
   for key in range(0x70,0x7c):keys.append(dict(key=key,active=active,enabled=enabled,existing=existing,team=0,expected=native.key(key,active,enabled,existing)))
 for team in [-1,-99]:keys.append(dict(key=0x74,active=[True]*4,enabled=[True]*4,existing=[],team=team,expected=native.key(0x74,[True]*4,[True]*4,[],team)))
 for offset,control,lines,held in itertools.product([-5,-1,0,1,18,57,58,59,100],range(2),[0,1,17,18,19,76,100],[False,True]):scroll.append(dict(offset=offset,control=control,lines=lines,held=held,expected=native.help_scroll(offset,control,lines,held)))
 for clocks in [[1000,1399,1799,1800,2200,2599],[0,399,799,1198],[2147483640,2147483647,2147483648,2147484000,1]]:compact.append(dict(clocks=clocks,expected=native.compact(clocks)))
 for width,height,x,y in itertools.product([10,20,43,100,138],[10,15,43,80,138],[0,10,11,50,111,121],[0,20,21,25,74,125,135]):minimap.append(dict(width=width,height=height,x=x,y=y,expected=native.minimap(x,y,width,height,13,12)))
 for selected,found in itertools.product([-1,0,1,10,401,999],[-1,0,1,10,401,999]):hovers.append(dict(selected=selected,found=found,expected=native.hover(selected,found)))
 original=BattleBriefingNative();briefing_inputs=[]
 for path in sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*set.arc/*.srf.txt')):
  briefing_inputs.append(path)
  for team in [0,1,-1,-99]:
   text=path.read_text();briefings.append(dict(source=text,team=team,expected=original.briefing(text,team)))
 for source in ['V1 처치\nV2\n','v1 lowercase\nv2\n',' V1 ignored\nV1  first \nV1 last\nV2  defeat  ','V1\nV2','V1 foo\nV1 bar\nV2 lose']:
  briefings.append(dict(source=source,team=0,expected=original.briefing(source)))
 for opened,extracted,archive in itertools.product([False,True],repeat=3):
  text='V1 condition\nV2 failure';briefings.append(dict(source=text if opened and extracted and archive else None,team=0,expected=original.briefing(text,opened=opened,extracted=extracted,archive=archive)))
 status_native=BattleStatusNative();statuses=[];glyphs=[];conditions=[]
 for scene,(hp,max_hp),(mp,max_mp),(ap,max_ap),(xp,next_xp),phase in itertools.product([1,9,24],[(0,200),(17,200),(200,200),(2147483647,2147483647)],[(0,0),(3,45),(45,45)],[(0,20),(7,20),(20,20)],[(123,1000),(0,0),(2000,1000),(2**32-1,2**32-1),(2**32+123,1000)],range(3)):
  statuses.append(dict(scene=scene,hp=hp,maxHp=max_hp,mp=mp,maxMp=max_mp,ap=ap,maxAp=max_ap,xp=xp,nextXp=next_xp,phase=phase,expected=status_native.draw(scene,hp,max_hp,mp,max_mp,ap,max_ap,xp,next_xp,phase)))
 for flags in [0,*[1<<i for i in range(32)],0xffff,0xffffffff,0x5555,0xaaaa,0x401,0x2020]:
  result=status_native.draw(1,17,200,3,45,7,20,123,1000,conditions=flags);conditions.append(dict(flags=flags,expected=result['blits'][4:]))
 for text,color in itertools.product(['0/123','2147483647/2147483647','-1',' ?'],[0,1,2]):glyphs.append(dict(text=text,color=color,expected=status_native.glyphs(text,color)))
 menu_native=BattleMenuNative();menu_clocks=[]
 for clock,base_y,disabled in itertools.product([-(2**31),-601,-600,-300,-299,-1,0,1,299,300,301,599,600,899,900,2**31-1,2**31,2**32,2**32+299,2**32+300],[0,32,64,96],[False,True]):
  menu_clocks.append(dict(clock=clock,baseY=base_y,disabled=disabled,expected=menu_native.icon(clock,base_y,disabled)))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(options=options,keys=keys,scroll=scroll,compact=compact,minimap=minimap,briefings=briefings,hovers=hovers,statuses=statuses,glyphs=glyphs,conditions=conditions,menuClocks=menu_clocks)));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'BattleOptionsChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'BattleOptionsChecks.cs'))+'\n"'+str(ROOT/'Assets/_Original/Runtime/UI/NativeBattleOptions.cs')+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1700:])
 result=json.loads(p.stdout);paths=[*briefing_inputs,Path(__file__),OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_status_emulator.py',OUT/'native_battle_menu_emulator.py',OUT/'native_battle_options_emulator.py',OUT/'BattleOptionsChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeBattleOptions.cs',ROOT/'Assets/_Original/Runtime/UI/NativeBattleWindowArt.cs',ROOT/'Assets/_Original/Runtime/UI/NativeBattleWindowPointer.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.BattleWindows.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Training.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Combat.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.BattleMagic.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.TroopOrders.cs',ROOT/'Assets/_Original/Runtime/Tutorial/TutorialSceneRoot.cs',ROOT/'Assets/_Original/Runtime/Combat/TrainingCombat.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs']
 art=json.loads((OUT/'native-battle-window-assets.json').read_text());assets=0
 for path,digest in art['sources'].items():assert sha(ROOT/path)==digest,path;assets+=1
 from metadata_provenance import verify_output
 reconciled=[]
 for row in art['outputs']:
  observed=verify_output(ROOT,row)
  if observed:reconciled.append(observed)
  assets+=1
 result['metadataReconciliations']=reconciled
 paths.extend([OUT/'metadata_provenance.py',OUT/'pixel-import-policy.json',OUT/'pixel-import-check.json'])
 # Resolve every serialized reference including pre-existing source textures/TextAssets.
 asset=ROOT/'Assets/_Original/Resources/OriginalBattleWindows.asset';guid_paths={}
 for meta in (ROOT/'Assets/_Original').rglob('*.meta'):
  match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)
  if match:guid_paths.setdefault(match[1],[]).append(Path(str(meta)[:-5]))
 policy_textures=set(texture_paths())
 for file_id,guid in re.findall(r'fileID: (\d+), guid: ([a-f0-9]{32})',asset.read_text()):
  resolved=guid_paths.get(guid,[]);assert len(resolved)==1,(guid,resolved);path=resolved[0];assert path.is_file();paths.extend([path,Path(str(path)+'.meta')]);assets+=1
  if path.suffix=='.png':assert path in policy_textures and lossless(Path(str(path)+'.meta').read_text(),*Image.open(path).size),'Unverified/lossy battle texture '+str(path)
  assert (file_id=='11500000'and path.suffix=='.cs')or(file_id=='2800000'and path.suffix=='.png')or(file_id=='4900000'and path.suffix in ['.txt','.json']),(file_id,path)
 meta=Path(str(asset)+'.meta');assert 'NativeFormatImporter:'in meta.read_text()and'mainObjectFileID: 11400000'in meta.read_text();assets+=1
 evidence=ROOT/'분석/Ghidra/Saver_Late/BattleWindows/evidence.json';native_evidence=json.loads(evidence.read_text());paths.append(evidence)
 for row in native_evidence['files']+[native_evidence['signatureInspectionScript']]:assert sha(ROOT/row['asset'])==row['sha256'];paths.append(ROOT/row['asset'])
 menu_evidence=ROOT/'분석/Ghidra/Saver_Late/BattleMenus/evidence.json';menu_files=json.loads(menu_evidence.read_text())['files'];paths.append(menu_evidence)
 for row in menu_files:assert sha(ROOT/row['asset'])==row['sha256'];paths.append(ROOT/row['asset'])
 paths.extend([OUT/'native-battle-window-assets.json',OUT/'apply_pixel_import_policy.py',ROOT/'Tools/OriginalRestart/pixel_texture_policy.py']);result.update(nativeOptionCases=len(options),nativeHoverCases=len(hovers),nativeStatusCases=len(statuses),nativeConditionCases=len(conditions),nativeMenuClockCases=len(menu_clocks),nativeGlyphCases=len(glyphs),nativeHelpScrollCases=len(scroll),nativeKeyCases=len(keys),nativeCompactPresses=sum(len(row["clocks"])for row in compact),nativeMinimapCases=len(minimap),nativeBriefingCases=len(briefings),assetChecks=assets,nativeCodeExecuted=True,nativeSha256=native.sha256,logs=str(WORK),scope='Unmodified10004A80 scene4 options through actual100029D0 normal-battle position gateway, all active/enabled4-slot combinations and absent/present queried windows. Actual10004A80 and10005200 help scroll branches. UI/resource source hashes and managed routing compared. Actual10003DA0 and1000F200 parse every current decoded SRF with MFC strings and archive/file results supplied as boundaries; no native decompressor/OS file-loading claim. Actual100054C0/10006830/100063E0 numeric/gauge dispatch and100053F0 glyph loop with supplied runtime records and CString/graphics boundaries. Source status selection hover executes10002D50. Actual1000A800300-clock signed32 phase/disabled-row branches execute with unit/menu/graphics/clock supplied; this does not measure process-clock origin or real-time playback. No complete native window construction/Draw/Unity Play/physical input/other-user alliance/server outcome claim.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in paths});(OUT/'native-battle-options-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
