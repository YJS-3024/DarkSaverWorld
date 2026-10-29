"""Read authoring geometry from saved Unity scenes. Never reads a runtime map JSON.
Compatibility dictionaries are transient tool results, not another geometry file.
"""
from pathlib import Path
from functools import lru_cache
from collections import deque
import copy,hashlib,json,re
import UnityPy,yaml
ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'Assets/_Game/Art/Converted/NonScenarioMaps'
REPORT=ROOT/'Tools/MapChecks/scene_tiles.json'
LOADER=getattr(yaml,'CSafeLoader',yaml.SafeLoader)
def file_sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def _guid(raw):return ''.join(f'{b:02x}'[::-1] for b in raw)
@lru_cache(maxsize=128)
def documents(path):
 path=Path(path);raw=path.read_bytes();result={}
 if raw.startswith(b'%YAML'):
  parts=re.split(r'^--- !u!(\d+) &(-?\d+)\s*\n',raw.decode(),flags=re.M)
  for i in range(1,len(parts),3):
   id=int(parts[i+1]);assert id not in result,(path,id)
   d=yaml.load(parts[i+2],Loader=LOADER);kind=next(iter(d));result[id]=(kind,d[kind])
 else:
  for obj in UnityPy.load(str(path)).objects:
   try:d=obj.read_typetree()
   except (ValueError,TypeError):
    if obj.type.name=='MonoBehaviour':continue
    raise
   def convert(v):
    if isinstance(v,dict):
     if 'm_FileID' in v and 'm_PathID' in v:
      p={'fileID':v['m_PathID']}
      if v['m_FileID']:
       ext=obj.assets_file.externals[v['m_FileID']-1];p.update(guid=_guid(ext.guid),type=ext.type)
      return p
     return {k:convert(x) for k,x in v.items()}
    if isinstance(v,tuple):return dict(zip(['first','second'],map(convert,v)))
    if isinstance(v,list):return list(map(convert,v))
    return v
   result[obj.path_id]=(obj.type.name,convert(d))
 return result
@lru_cache(maxsize=1)
def asset_paths():
 paths={}
 for meta in ASSETS.rglob('*.meta'):
  if meta.name.endswith('.asset.meta') or meta.name.endswith('.png.meta'):
   g=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
   if g:paths[g[1]]=meta.with_suffix('')
 return paths
@lru_cache(maxsize=4096)
def asset_data(guid):
 path=asset_paths()[guid];return next(b for kind,b in documents(str(path)).values() if kind=='MonoBehaviour')
def row_for_resource(folder):
 rel=str(Path(folder).resolve().relative_to(ROOT/'Assets/_Game/Resources'))
 rows=[r for r in json.loads(REPORT.read_text())['maps'] if r['resourceRoot']==rel]
 preferred=('Interior'+rel.split('/')[-1] if rel.startswith('Interiors/') else 'Scenario'+rel.split('/')[1] if rel.startswith('Scenarios/') else rel.split('/')[-1])
 return next((r for r in rows if r['key']==preferred),rows[0] if rows else None)
def read_scene_map(folder):
 folder=Path(folder).resolve();row=row_for_resource(folder)
 if row is None:raise ValueError('No saved scene registered for '+str(folder))
 docs=documents(str(ROOT/row['scene']));gameobjects={id:b for id,(k,b) in docs.items() if k=='GameObject'}
 def component(go,kind):return next(b for k,b in docs.values() if k==kind and b.get('m_GameObject',{}).get('fileID')==go)
 byname={b['m_Name']:id for id,b in gameobjects.items()}
 ground=component(byname['Ground'],'Tilemap');terrain=component(byname['TerrainAttributes'],'Tilemap')
 w,h=ground['m_Size']['x'],ground['m_Size']['y'];attrs=[0]*(w*h);keys=[-1]*(w*h)
 for cell in ground['m_Tiles']:
  pos,t=cell['first'],cell['second'];x,y=pos['x'],h-1-pos['y']
  if 0<=x<w and 0<=y<h:keys[y*w+x]=t['m_TileIndex']
 for cell in terrain['m_Tiles']:
  pos,t=cell['first'],cell['second'];x,y=pos['x'],h-1-pos['y']
  if 0<=x<w and 0<=y<h:
   a=asset_data(terrain['m_TileAssetArray'][t['m_TileIndex']]['m_Data']['guid']);attrs[y*w+x]=int(a['m_Name'].split('_')[-1])
 objects=[]
 for id,go in gameobjects.items():
  m=re.fullmatch(r'Object_(\d+)',go['m_Name'])
  if not m:continue
  tr=component(id,'Transform')['m_LocalPosition'];sr=component(id,'SpriteRenderer');texture=asset_paths()[sr['m_Sprite']['guid']]
  objects.append(dict(index=int(m[1]),x=int(tr['x']),y=int(h-tr['y']),texture='Sprites/'+texture.stem))
 objects.sort(key=lambda o:o['index'])
 # An analysis-only connected reference cell. Runtime spawn coordinates belong to
 # the local server/scene SpawnPoint data, never to this compatibility result.
 blocked={3,6,9} if row['resourceRoot'].startswith(('Fields/','Scenarios/')) else {9}
 free={(x,y) for y in range(h) for x in range(w) if attrs[y*w+x] not in blocked and keys[y*w+x]>=0};parts=[]
 while free:
  first=min(free);free.remove(first);part={first};queue=deque([first])
  while queue:
   x,y=queue.popleft()
   for p in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
    if p in free:free.remove(p);part.add(p);queue.append(p)
  parts.append(part)
 component=max(parts,key=len) if parts else set()
 spawn=min(component,key=lambda p:((p[0]+.5-w*.5)**2+(p[1]+.5-h*.5)**2,p)) if component else (0,0)
 return dict(width=w,height=h,attributes=attrs,floorKeys=keys,objects=objects,spawnX=spawn[0],spawnY=spawn[1],encounters=[],spawnSource='analysis-only connected reference; not runtime spawn')
def geometry_digest(data,folder):
 folder=Path(folder)
 payload=[data['width'],data['height'],data['attributes'],[[o['index'],o['x'],o['y'],file_sha(folder/(o['texture']+'.png'))] for o in data['objects']]]
 return hashlib.sha256(json.dumps(payload,separators=(',',':'),ensure_ascii=False).encode()).hexdigest()
