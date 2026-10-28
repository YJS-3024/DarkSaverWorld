"""Read-only local building route/scene/terrain audit, not original routing proof or Unity Play."""
from pathlib import Path
import json
from collections import deque
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
table=json.loads((ROOT/'Assets/_Game/Resources/World/BuildingRoutes.json').read_text())
checks=0
def check(ok,name):
 global checks
 if not ok:raise AssertionError(name)
 checks+=1
native={'KaosiaWeaponShop':'2000','KaosiaArmorShop':'2100','KaosiaAccessoryShop':'2200','KaosiaMagicTools':'2300','KaosiaBank':'2400','KaosiaWarehouse':'2510','KaosiaTemple':'2600'}
def load(key):
 rel='Towns/Kaosia' if key=='Kaosia' else 'Interiors/'+native[key]
 return json.loads((ROOT/'Assets/_Game/Resources'/rel/'Map.json').read_text())
def cell(d,x,y):return int(x),d['height']-1-int(y)
def walkable(d,p):
 x,y=p;return 0<=x<d['width'] and 0<=y<d['height'] and d['attributes'][y*d['width']+x]!=9
check(table['version']==1 and len(table['routes'])==14,'14 authorized building directions')
rows=table['routes'];build=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'))
paths=set()
for o in build.objects:
 if o.type.name=='EditorBuildSettings':paths={Path(x['path']).stem for x in o.read_typetree()['m_Scenes'] if x['enabled']}
for r in rows:
 src,dst=load(r['fromKey']),load(r['toKey'])
 check((r['width'],r['height'])==(dst['width'],dst['height']),'destination size '+r['toKey'])
 check(r['map'] in paths and r['runtime'] in paths,'registered scene pair '+r['toKey'])
 check(walkable(src,cell(src,r['doorX'],r['doorY'])),'door terrain '+r['fromKey'])
 check(walkable(dst,cell(dst,r['x'],r['y'])),'arrival terrain '+r['toKey'])
 start=(src['spawnX'],src['spawnY']);target=cell(src,r['doorX'],r['doorY']);seen={start};q=deque([start])
 while q:
  x,y=q.popleft()
  for p in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
   if p not in seen and walkable(src,p):seen.add(p);q.append(p)
 check(target in seen,'door reachable from region spawn '+r['fromKey'])
 back=[b for b in rows if b['fromKey']==r['toKey'] and b['toKey']==r['fromKey']]
 check(len(back)==1,'exact return route '+r['toKey'])
 check((r['x']-back[0]['doorX'])**2+(r['y']-back[0]['doorY'])**2>1.5**2,'arrival avoids immediate bounce '+r['toKey'])
report=dict(checks=checks,directions=len(rows),buildings=len(native),sceneTargets=len({r[k] for r in rows for k in ['map','runtime']}),unityPlay=False,scope='Current converted geometry and enabled build scenes; local authored routes, not recovered original server door coordinates.')
(ROOT/'Tools/MapChecks/building_routes_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('PASS',checks,'local building resource checks; NOT Unity Play')
