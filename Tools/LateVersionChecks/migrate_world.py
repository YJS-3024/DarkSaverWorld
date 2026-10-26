"""Migrate three late layouts while retaining authored region/service identities.
All gates/returns are checked against native collision components. This does not
claim a recovered original server route/NPC table or edit any save file.
"""
import hashlib,json,re,sys,shutil
from pathlib import Path
from collections import deque
import UnityPy
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
import world_asset_baker as b
from build_campaign_entrances import choose
# Historical whole-world migration must not overwrite the confirmed early Kaosia.
if (ROOT/'Tools/LateVersionChecks/kaosia_restore_check.json').exists():
 raise SystemExit('Kaosia is pinned to the user-confirmed early layout; do not rerun the historical three-map migration.')
source=Path(sys.argv[1])/'Saver_Files/saver/MAP';oldroot=b.NATIVE;overrides=b.LATE_MAPS
b.NATIVE=b.LATE_MAPS=source
report={'sourceVersion':'Saver_Late','routeProvenance':'Retained single-player identities and topology; native geometry, reviewed building silhouettes, authored thresholds','areas':{},'facilities':{}}
# Preserve all existing asset GUIDs/settings except the required large map limit.
original_png=b.png
def png(path,image):
 meta=path.with_name(path.name+'.meta');saved=meta.read_text() if meta.exists() else None
 original_png(path,image)
 if saved is not None:meta.write_text(saved)
 if max(image.size)>4096:meta.write_text(re.sub(r'maxTextureSize: \d+','maxTextureSize: 8192',meta.read_text()))
b.png=png
meta_safe_scene=lambda path,key,runtime:write_scene(path,key,runtime)
def write_scene(path,key,runtime):
 meta=path.with_name(path.name+'.meta');saved=meta.read_text() if meta.exists() else None
 b.scene(path,key,runtime)
 if saved is not None:meta.write_text(saved)
raw={};data={};components={};clear={};protected={};folders={};proofs={}
for key,id,kind,blocked in [('Kaosia','1000','Towns',{9}),('Belpueus','1002','Towns',{9}),('WorldField1010','1010','Fields',{3,6,9})]:
 folder=ROOT/f'Assets/_Game/Resources/{kind}/{key}';result,proof=b.bake(id,folder,blocked);native=b.load_map(id)
 raw[key]=native;data[key]=result;proofs[key]=proof;folders[key]=folder;components[key]=b.walkable(native,blocked);clear[key]=components[key]-b.covered(native);protected[key]=[]
 report['areas'][key]=dict(nativeMap=id,width=native['width'],height=native['height'],**proof)
 # Cache only changed/new map resources so other legacy map resources do not mix versions.
 overrides.mkdir(parents=True,exist_ok=True);shutil.copyfile(source/(id+'.mrc'),overrides/(id+'.mrc'))
 resources=set(native['a'])|{t[0] for t in native['tiles'] if t[1]!=255}|{s['resource'] for s in native['sprites']}
 for rid in resources:
  name=f'mdsr{rid}.grs'
  if not (oldroot/name).exists() or (oldroot/name).read_bytes()!=(source/name).read_bytes():shutil.copyfile(source/name,overrides/name)
# Keep the explicitly requested initial town coordinate exactly; test all touching tiles.
assert all((x,y) in components['Kaosia'] for x in [20,21] for y in [39,40])
protected['Kaosia'].append((21,39))
# Reviewed weapon/armor/temple silhouettes; bank/warehouse identity remains authored.
roles=[('KaosiaWeaponShop',108,(3,9)),('KaosiaArmorShop',191,(6,9)),('KaosiaAccessoryShop',247,(10,6)),('KaosiaMagicTools',224,(3,6)),('KaosiaTemple',135,(13,11)),('KaosiaBank',240,(6,7)),('KaosiaWarehouse',197,(8,10)),('Training',149,(6,8))]
for key,index,offset in roles:
 s=raw['Kaosia']['sprites'][index];anchor=(s['x']+offset[0],s['y']+offset[1]);component=components['Kaosia'];candidates=component-set(protected['Kaosia'])
 portal=min((p for p in candidates if all((p[0]-q[0])**2+(p[1]-q[1])**2>=9 for q in protected['Kaosia'])),key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p))
 # Reach a clear return position outside the image footprint, not an adjacent auto-return gate.
 dist={portal:0};queue=deque([portal]);returns=[]
 while queue:
  p=queue.popleft();depth=dist[p]
  if depth>=3 and (p[0]-portal[0])**2+(p[1]-portal[1])**2>=9 and p in clear['Kaosia'] and all((p[0]-q[0])**2+(p[1]-q[1])**2>=9 for q in protected['Kaosia']):returns.append(p)
  if depth>=16:continue
  for q in [(p[0]-1,p[1]),(p[0]+1,p[1]),(p[0],p[1]-1),(p[0],p[1]+1)]:
   if q in component and q not in dist:dist[q]=depth+1;queue.append(q)
 assert returns,(key,'no reachable outdoor return')
 arrival=min(returns,key=lambda p:(dist[p],(p[0]-21)**2+(p[1]-39)**2,p));protected['Kaosia'] += [portal,arrival]
 def world(p,h=61):return [p[0]+.5,h-p[1]-.5]
 report['facilities'][key]=dict(sourceIndex=index,resource=s['resource'],group=s['index'],sourcePosition=[s['x'],s['y']],portal=world(portal),spawn=world(arrival),doorSource=list(portal),returnSource=list(arrival),role='authored unsigned-building assignment' if key in ['KaosiaBank','KaosiaWarehouse'] else 'reviewed silhouette; threshold authored')
# Rebind all reciprocal region routes on the changed layouts; retain the graph and IDs.
path=ROOT/'Assets/_Game/Resources/World/Routes.json';routes=json.loads(path.read_text())
for row in routes['routes']:
 key=row['fromKey']
 if key not in data:continue
 h=data[key]['height'];anchor=(int(row['portalX']),h-1-int(row['portalY']))
 if key in ['Kaosia','Belpueus']:anchor=(data[key]['width']//2,1)
 if key=='WorldField1010' and row['toKey']=='Kaosia':anchor=(20,124) # source town silhouette588 near the legacy entrance neighborhood
 portal,arrival=choose(components[key],clear[key],anchor,protected[key],[])
 protected[key]+=[portal,arrival];row.update(portalX=portal[0]+.5,portalY=h-portal[1]-.5,spawnX=arrival[0]+.5,spawnY=h-arrival[1]-.5)
 if key=='WorldField1010':row['legacy']=False
routes['provenance']='Later native layouts; existing authored reciprocal graph retained; gates and returns revalidated'
b.text_asset(path,routes)
# Rebind the original Scenario01 gate on the changed field without resetting progress.
p=ROOT/'Assets/_Game/Resources/Scenarios/Shared/Entrances.json';entries=json.loads(p.read_text())
for row in entries['entries']:
 if row['fieldKey']!='WorldField1010':continue
 portal,arrival=choose(components['WorldField1010'],clear['WorldField1010'],(int(row['portalX']),159-int(row['portalY'])),protected['WorldField1010'],[]);protected['WorldField1010'] += [portal,arrival]
 row.update(portalX=portal[0]+.5,portalY=159.5-portal[1],spawnX=arrival[0]+.5,spawnY=159.5-arrival[1])
b.text_asset(p,entries)
# Four standard authored field encounters, using unchanged native base rows.
units=json.loads((ROOT/'분석/Saver_Late/tables.json').read_text())['ability.atr']['rows'];by={int(r['values'][0]):r['values'] for r in units};encounters=[]
for index,job in enumerate([301,302,303,304]):
 v=by[job];anchor=[(40,40),(100,40),(40,100),(100,100)][index]
 p=min((p for p in clear['WorldField1010'] if all((p[0]-q[0])**2+(p[1]-q[1])**2>=16 for q in protected['WorldField1010'])),key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p));protected['WorldField1010'].append(p)
 encounters.append(dict(id=400+index,job=job,name=v[35],hp=int(v[2]),mp=max(0,int(v[4])),attack=int(v[6]),defense=int(v[8]),magicAttack=max(0,int(v[10])),magicDefense=max(0,int(v[12])),experience=int(v[32]),x=p[0],y=p[1],wave=0))
data['WorldField1010']['encounters']=encounters
for key,result in data.items():b.text_asset(folders[key]/'Map.json',result)
# Generic map scenes preserve scene GUIDs; the original town runtime retains its training catalog.
write_scene(ROOT/'Assets/_Game/Scenes/Maps/Kaosia_Map.unity','Kaosia',False)
write_scene(ROOT/'Assets/_Game/Scenes/Maps/World1010_Map.unity','WorldField1010',False)
write_scene(ROOT/'Assets/_Game/Scenes/Runtime/Field_Runtime.unity','WorldField1010',True)
# Update only transforms of existing manual training/legacy town gates.
path=ROOT/'Assets/_Game/Scenes/Runtime/Town_Runtime.unity';env=UnityPy.load(str(path));file=env.objects[0].assets_file
kaosia_route=next(r for r in routes['routes'] if r['fromKey']=='Kaosia')
positions={'Training Entrance':report['facilities']['Training']['portal'],'Spawn FromTraining':report['facilities']['Training']['spawn'],'To World Field':[kaosia_route['portalX'],kaosia_route['portalY']],'Spawn FromField':[kaosia_route['spawnX'],kaosia_route['spawnY']]}
for obj in env.objects:
 if obj.type.name!='GameObject':continue
 go=obj.read_typetree()
 if go['m_Name'] not in positions:continue
 t=next(file.objects[c['component']['m_PathID']] for c in go['m_Component'] if file.objects[c['component']['m_PathID']].type.name=='Transform');d=t.read_typetree();x,y=positions[go['m_Name']];d['m_LocalPosition'].update(x=x,y=y);t.save_typetree(d)
path.write_bytes(env.file.save())
# Update native layout sizes and shop/facility gate data without changing interior scenes.
p=ROOT/'Assets/_Game/Runtime/Core/TownCatalog.cs';s=p.read_text().replace('"1000",43,43,true','"1000",61,61,true').replace('"1002",40,40,false','"1002",65,65,false');p.write_text(s)
for filename,kind in [('ShopInteriorCatalog.cs','ShopInteriorDefinition'),('KaosiaFacilityCatalog.cs','KaosiaFacilityDefinition')]:
 p=ROOT/'Assets/_Game/Runtime/Core'/filename;s=p.read_text()
 def replace(match):
  tokens=match[1].split(',');key=tokens[0].strip('"')
  if key not in report['facilities']:return match[0]
  r=report['facilities'][key];coords=[str(v)+'f' for v in r['portal']+r['spawn']]
  if kind=='ShopInteriorDefinition':tokens[3:8]=[str(r['sourceIndex'])]+coords
  else:tokens[4:10]=[str(r['sourceIndex']),str(r['group'])]+coords
  return 'new '+kind+'('+','.join(tokens)+')'
 s=re.sub(r'new '+kind+r'\(([^\n]*?)\)',replace,s);p.write_text(s)
# Restore only blocked legacy positions in memory; never rewrite a save on load.
resume=[]
for key,id in [('Kaosia','1000'),('Belpueus','1002'),('WorldField1010','1010')]:
 h=data[key]['height'];valid=clear[key]
 for q in protected[key]:valid={p for p in valid if (p[0]-q[0])**2+(p[1]-q[1])**2>=4}
 if key=='Kaosia':valid.add((21,39))
 scene='Kaosia_Map' if key=='Kaosia' else 'Belpueus_Map' if key=='Belpueus' else 'World1010_Map'
 resume.append(dict(mapScene=scene,width=data[key]['width'],height=h,safeCells=sorted(y*data[key]['width']+x for x,y in valid)))
b.text_asset(ROOT/'Assets/_Game/Resources/World/LateLayoutResume.json',dict(version=1,maps=resume))
# Existing source-resource audit manifests now include the migrated field.
p=ROOT/'Tools/MapChecks/world_fields_resources.json';manifest=json.loads(p.read_text());row=next((r for r in manifest if r['key']=='WorldField1010'),None)
if row is None:
 row=dict(key='WorldField1010',nativeMap='1010',title='카오시아 외곽',width=160,height=160,sourceVersion='Saver_Late',mapScene='World1010_Map',runtimeScene='Field_Runtime',encounters=4);manifest.append(row)
row.update(proofs['WorldField1010']);row['sourceVersion']='Saver_Late';p.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
p=ROOT/'Tools/MapChecks/four_towns_resources.json';manifest=json.loads(p.read_text())
for row in manifest:
 if row['key']=='Belpueus':row.update(width=65,height=65,sourceSha256=proofs['Belpueus']['sourceSha256'],sourceObjects=153,reachableCells=len(components['Belpueus']),spawnSource=[data['Belpueus']['spawnX'],data['Belpueus']['spawnY']],spawnWorld=[data['Belpueus']['spawnX']+.5,64.5-data['Belpueus']['spawnY']],sourceVersion='Saver_Late')
p.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
report['routes']=[r for r in routes['routes'] if r['fromKey'] in data];report['initialTownPosition']=[21,21];report['saveFilesModified']=False
(ROOT/'Tools/LateVersionChecks/world_migration_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'areas':report['areas'],'facilities':{k:r['portal'] for k,r in report['facilities'].items()}},ensure_ascii=False))
