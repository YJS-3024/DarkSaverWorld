"""Run the actual local training test authority against an isolated real account service.
Requires check_gateways.py response-file prerequisites (portable runner supported).
"""
import csv,hashlib,json,os,re,shlex,struct,subprocess,sys
from pathlib import Path
from check_picture_initialization import block
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).parent
WORK=Path('/tmp/ds-training-test-authority')
def focus_host_code():
 battle_windows=ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.BattleWindows.cs'
 campaign_root=ROOT/'Assets/_Original/Runtime/Campaign/WorldSceneRoot.Campaign.cs'
 focus=block(battle_windows.read_text(),'private void ReceiveBattleFocus(')
 battle_map=next(line.strip()for line in campaign_root.read_text().splitlines()if line.strip().startswith('private bool BattleMap('))
 code='internal sealed class BattleFocusCheckHost {private LocalServerManager server;private WorldDefinition definition;private object canvasObject=new object();private bool running=true;public bool isActiveAndEnabled=true;private WorldView view;private int battleDialogMap=-1;private string battleDialogSession;private int? battleCameraX,battleCameraY;public int Paints,Resets;public int? Left=>battleCameraX;public int? Top=>battleCameraY;public BattleFocusCheckHost(LocalServerManager s,WorldDefinition d){server=s;definition=d;}public void Accept(NativeBattleFocus value)=>ReceiveBattleFocus(value);public void Stop()=>running=false;public void DestroyCanvas()=>canvasObject=null;public void DropDefinition()=>definition=null;private WorldMapDefinition DisplayMap(int id)=>definition.Map(id);private void ResetBattleDialogs(){Resets++;battleCameraX=battleCameraY=null;battleDialogMap=-1;battleDialogSession=null;}private void Paint(){Paints++;}'+focus+battle_map+'}'
 return code,[battle_windows,campaign_root]
def main():
 WORK.mkdir(exist_ok=True)
 old=Path('/tmp/ds-npc-gateway-checks')
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 runtime=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.TrainingTest.cs'
 harness=OUT/'TrainingTestAuthorityChecks.cs'
 policy=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Training.cs'
 constant=' '.join(line.strip()for line in policy.read_text().splitlines()if line.strip().startswith('public const '))
 host=WORK/'Policy.cs'
 manager=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.cs'
 disable=block(manager.read_text(),'private void OnDisable(')
 editor=ROOT/'Assets/_Original/Editor/LocalGrowthTestWindow.cs'
 recovery=block(editor.read_text(),'private void RecoverForTest(')
 host.write_text('using System;using System.Linq;using DarkSaver.Original.World;using DarkSaver.Original.Entry;\nnamespace DarkSaver.Original.Entry {public sealed partial class LocalServerManager {'+constant+disable+'}}\n'+
  'internal sealed class RecoveryCheckHost {private LocalServerManager server;private OriginalCombatCatalog combatCatalog;private string message; public RecoveryCheckHost(LocalServerManager s,OriginalCombatCatalog c){server=s;combatCatalog=c;} public string Run(){RecoverForTest();return message;}'+recovery+'}')
 world_manager=ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs'
 root_source=ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs'
 entry=block(world_manager.read_text(),'public WorldReply Request_EnterTraining(')
 world_request=block(world_manager.read_text(),'private WorldReply WorldRequest(')
 world_request+=block(world_manager.read_text(),'private bool IsCurrentWorldReply(')
 dispatch=block(policy.read_text(),'private WorldReply TrainingResult(')+block(policy.read_text(),'private WorldReply BattleResult(')
 dispatch+=block(policy.read_text(),'private bool IsCurrentBattleReply(')
 regroup=ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Regroup.cs'
 dispatch+=block(regroup.read_text(),'private void PumpBattleRegroup(')
 late=block(root_source.read_text(),'private void LateUpdate(')
 refresh=block(root_source.read_text(),'private bool TryRefreshWorldView(')
 handle=block(root_source.read_text(),'private void Handle(')
 extra='\nnamespace DarkSaver.Original.Entry {public sealed partial class LocalServerManager {'+entry+world_request+dispatch+'}}\n'
 extra+='internal sealed class WorldFrameCheckHost {private LocalServerManager server;private WorldDefinition definition;private EntryCharacter character;private UnityEngine.GameObject canvasObject=new UnityEngine.GameObject();private bool running=true;private WorldView view;private string portalInFlight;private WorldFailure previousFailure; public int Paints;public WorldView View=>view;public bool Running=>running;public bool Active=>canvasObject.Active;public WorldFrameCheckHost(LocalServerManager s,WorldDefinition d){server=s;definition=d;character=new EntryCharacter{id=s.WorldSnapshot.characterId};} public void Frame()=>LateUpdate(); private WorldMapDefinition DisplayMap(int id)=>definition.Map(id);private void MaintainCurrencyState(){}private void MaintainRevivalQuote(){}private void MaintainWarehouseState(){}private void ShowFailure(WorldFailure f){}private void Paint(){Paints++;}'+late+refresh+handle+'}'
 focus_code,focus_sources=focus_host_code();extra+=focus_code
 battle_windows,campaign_root=focus_sources
 host.write_text(host.read_text()+extra)
 exe=WORK/'checks.dll'
 args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe))
 args+='\n-main:TrainingTestAuthorityChecks\n"'+str(harness)+'"\n"'+str(runtime)+'"\n'
 args+='\n\"'+str(host)+'\"\n'
 args+='\n\"'+str(ROOT/'Assets/_Original/Runtime/Entry/TrainingTestLayoutCatalog.cs')+'\"\n'
 args+='\n\"'+str(ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Growth.cs')+'\"\n'
 (WORK/'checks.rsp').write_text(args)
 exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(WORK/'checks.rsp')]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),text=True,capture_output=True)
  (WORK/(name+'.log')).write_text(proc.stdout+proc.stderr)
  if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-2400:])
 result=json.loads(proc.stdout)
 catalog=ROOT/'Assets/_Original/Data/Combat/OriginalCombat.json'
 table=ROOT/'Assets/_Original/Data/Tables/units_all.csv'
 fields=dict(hp='체력수',mp='마력수',attack='공력수',defense='방력수',magicAttack='마공수',magicDefense='마방수',speed='속도',movement='이동력',accuracy='명중률',evasion='회피율',critical='크릭율',magicAccuracy='마명율',magicEvasion='마회율',hpLimit='체력한',mpLimit='마력한',attackLimit='공력한',defenseLimit='방력한',magicAttackLimit='마공한',magicDefenseLimit='마방한',commandAttack='지공',commandDefense='지방')
 original={int(row['직업']):row for row in csv.DictReader(table.open(encoding='utf-8-sig'))}
 jobs=json.loads(catalog.read_text())['jobs']
 for job in jobs:
  assert job['baseStats']=={key:int(original[job['job']][col])for key,col in fields.items()},job['job']
 # Independently re-extract the selected late installer's ability table, not just two generated files.
 canonical=ROOT/'분석/Saver_Late/unity_catalogs/units_all.csv'
 assert canonical.read_bytes()==table.read_bytes(),'Runtime table differs from late-version canonical CSV'
 archive=ROOT/'Assets/_Original/SourceData/Native/gameres/set.arc.bytes'
 inventory=ROOT/'분석/Saver_Late/files.json'
 expected=next(row['sha256']for row in json.loads(inventory.read_text())if row['path'].lower()=='gameres/set.arc')
 assert hashlib.sha256(archive.read_bytes()).hexdigest()==expected,'Native archive version changed'
 sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
 from analyze_client import lzh_decode,lzss_decode
 from build_late_saver_catalog import arc_entries
 entry=next((method,size,packed)for name,method,size,packed in arc_entries(archive)if name.lower()=='ability.atr')
 method,size,packed=entry;assert method in (0,1)
 raw=(lzh_decode if method==0 else lzss_decode)(packed,size)
 assert len(raw)==size
 decoded=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/ability.atr.txt'
 assert raw.decode('cp949').encode('utf-8')==decoded.read_bytes(),'Decoded ability text differs from native archive'
 header=next(line.lstrip(';').split()for line in decoded.read_text().splitlines()if line.startswith(';직업'))
 native={}
 for line in decoded.read_text().splitlines():
  parts=line.split()
  if len(parts)<36 or not parts[0].isdigit():continue
  assert int(parts[0])not in native,'Duplicate native ability ID'
  native[int(parts[0])]=dict(zip(header[:36],parts[:36]))
  native[int(parts[0])]['설명']=' '.join(parts[36:])
 assert set(native)==set(original),'Source unit ID coverage differs'
 for job,row in original.items():
  assert row['설명']==native[job]['설명'],(job,'source description')
  for field in fields.values():assert int(row[field])==int(native[job][field]),(job,field)
 result['sourceBaseStatRows']=len(jobs)
 result['nativeAbilityRows']=len(native)
 growth=ROOT/'Assets/_Original/Data/Growth/CharacterGrowth.json'
 stage_jobs=json.loads(growth.read_text())['jobs']
 for job in stage_jobs:
  expected_stage=int(re.search(r'(?:제\s*)?(\d+)\s*단계',original[job['id']]['설명'].replace('_',' '))[1])
  assert job['stage']==expected_stage,job['id']
 result['sourceClassStages']=len(stage_jobs)
 result['nativeArchiveMatched']=True
 registry=ROOT/'Assets/_Original/Resources/TrainingTestLayoutCatalog.asset'
 registry_text=registry.read_text()
 packed=bytes.fromhex(re.search(r'^  maps: ([0-9a-f]+)$',registry_text,re.M)[1])
 registered=list(struct.unpack('<'+'i'*(len(packed)//4),packed))
 refs=re.findall(r'fileID: 4900000, guid: ([0-9a-f]{32})',registry_text)
 assert len(registered)==len(refs) and len(set(registered))==len(registered)
 for map_id,guid in zip(registered,refs):
  assert 101<=map_id<=107
  meta=ROOT/f'Assets/_Original/Resources/TrainingTestLayouts/{map_id}.json.meta'
  assert 'guid: '+guid in meta.read_text(),f'wrong training resource reference: {map_id}'
 result['registeredTrainingMaps']=registered
 sources=[runtime,policy,manager,editor,world_manager,root_source,battle_windows,campaign_root,regroup,harness,Path(__file__),OUT/'check_picture_initialization.py',catalog,table,growth,ROOT/'Assets/_Original/Runtime/Growth/CharacterGrowth.cs',ROOT/'Assets/_Original/Runtime/Campaign/ScenarioGatewaySettings.cs',ROOT/'Assets/_Original/Runtime/Campaign/CampaignResultService.cs',ROOT/'Assets/_Original/Runtime/World/WorldService.cs',canonical,archive,inventory,decoded,ROOT/'닥세_리소스/분석도구/analyze_client.py',ROOT/'닥세_리소스/분석도구/build_late_saver_catalog.py',ROOT/'Assets/_Original/Runtime/Entry/TrainingTestLayoutCatalog.cs',ROOT/'Assets/_Original/Resources/TrainingTestLayoutCatalog.asset',ROOT/'Tools/OriginalWorldChecks/build_combat_assets.py',ROOT/'Assets/_Original/Runtime/Combat/TrainingCombat.cs']
 for n in (101,102):
  source=ROOT/f'Assets/_Original/Editor/ScenarioLayouts/{n}.json'
  exported=ROOT/f'Assets/_Original/Resources/TrainingTestLayouts/{n}.json'
  assert source.read_bytes()==exported.read_bytes(),f'{n}: authored placement changed during export'
  sources.extend([source,exported])
 # Include the actual compiled file store, account/model/service dependencies and fixture sources.
 sources.append(OUT/'check_picture_initialization.py')
 for line in args.splitlines():
  for token in shlex.split(line,posix=False):
   p=Path(token.strip('"'))
   if p.suffix=='.cs'and p.is_file()and ROOT in p.parents:sources.append(p)
 result.update(sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in dict.fromkeys(sources)},scope='Production local training test adapter and real isolated services; memory storage plus one temporary physical account/profile roundtrip. Actual file store/serialization and source metadata included; Unity/resource APIs stubbed. No Unity Play or tutorial UI execution.')
 (OUT/'training-test-authority-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps(result))
if __name__=='__main__':main()
