"""Rebake changed live field layouts only when existing routes/encounters remain valid.
Stage neither save changes nor guessed routes across resized native towns.
"""
import json,sys,shutil,math,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
import world_asset_baker as b
original=b.NATIVE;later=Path(sys.argv[1])/'Saver_Files/saver/MAP'
routes=json.loads((ROOT/'Assets/_Game/Resources/World/Routes.json').read_text())['routes']
manifest_path=ROOT/'Tools/MapChecks/world_fields_resources.json';manifest=json.loads(manifest_path.read_text());reports=[];stage=ROOT/'Tools/LateVersionChecks/.map-stage'
try:
 for id in ['1000','1002','1010','1012','1030','1060']:
  old=b.read_mrc(original/(id+'.mrc'));new=b.read_mrc(later/(id+'.mrc'));key='WorldField'+id
  row={'map':id,'oldSize':[old['width'],old['height']],'lateSize':[new['width'],new['height']],'lateSourceSha256':hashlib.sha256((later/(id+'.mrc')).read_bytes()).hexdigest()}
  folder=ROOT/'Assets/_Game/Resources/Fields'/key
  if not (folder/'Map.json').exists() or row['oldSize']!=row['lateSize']:
   row.update(applied=False,reason='Resized town or legacy scene requires doorway/NPC/route migration; existing live map retained');reports.append(row);continue
  data=json.loads((folder/'Map.json').read_text());component=b.walkable(new,{3,6,9});points=[]
  for r in routes:
   if r['fromKey']==key:points.extend([(int(r['portalX']),new['height']-1-int(r['portalY'])),(int(r['spawnX']),new['height']-1-int(r['spawnY']))])
  points.extend((r['x'],r['y']) for r in data.get('encounters',[]));points.append((data['spawnX'],data['spawnY']))
  if not all(p in component for p in points):
   row.update(applied=False,reason='Existing route/spawn/encounter disconnected in late layout',invalid=[p for p in points if p not in component]);reports.append(row);continue
  # Keep changed GRS scoped to migrated maps; legacy maps retain their own resources.
  resource_ids=set(new['a'])|{t[0] for t in new['tiles'] if t[1]!=255}|{s['resource'] for s in new['sprites']}
  for rid in resource_ids:
   name=f'mdsr{rid}.grs'
   if not (original/name).exists() or (original/name).read_bytes()!=(later/name).read_bytes():
    b.LATE_MAPS.mkdir(parents=True,exist_ok=True);(b.LATE_MAPS/name).write_bytes((later/name).read_bytes())
  dest=stage/key
  override=b.LATE_MAPS/(id+'.mrc');override.parent.mkdir(parents=True,exist_ok=True);override.write_bytes((later/(id+'.mrc')).read_bytes())
  result,proof=b.bake(id,dest,{3,6,9});assert all((e['x'],e['y']) not in b.covered(new) for e in data.get('encounters',[]))
  for name,value in data.items():
   if name not in ('width','height','attributes','floorKeys','objects'):result[name]=value
  (dest/'Map.json').write_text(json.dumps(result,ensure_ascii=False,separators=(',',':'))+'\n')
  for p in dest.rglob('*'):
   if not p.is_file() or p.suffix=='.meta':continue
   target=folder/p.relative_to(dest);target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,target)
   meta=target.with_name(target.name+'.meta')
   if not meta.exists():shutil.copyfile(p.with_name(p.name+'.meta'),meta)
  record=next(r for r in manifest if r['key']==key);record.update(proof);record['sourceVersion']='Saver_Late';reports.append(dict(row,applied=True,checkedRouteSpawnEncounterPoints=len(points),**proof))
 manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
 (ROOT/'Tools/LateVersionChecks/map_migration_check.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2)+'\n');print(json.dumps(reports,ensure_ascii=False))
finally:
 if stage.exists():shutil.rmtree(stage)
 meta=stage.with_name(stage.name+'.meta')
 if meta.exists():meta.unlink()
