from saved_scene_geometry import read_scene_map
"""Audit generated service NPC placements and removal of shortcut menus, without Unity."""
import json,math
from pathlib import Path
from collections import deque,Counter
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];checks=0

def check(ok,name):
 global checks
 assert ok,name
 checks+=1
content=json.loads((ROOT/'Assets/_Game/Resources/Town/ServiceNpcs.json').read_text());entries=content['entries'];check(content['version']==1 and len(entries)==26,'26 service actors')
world=json.loads((ROOT/'Assets/_Game/Resources/World/Routes.json').read_text())['routes'];facilities=json.loads((ROOT/'Tools/LateVersionChecks/world_migration_check.json').read_text())['facilities'];quests=json.loads((ROOT/'Assets/_Game/Resources/Town/Quests.json').read_text())['rows']
for key in ['Kaosia','Belpueus','Sisirio','Castlelim']:
 data=read_scene_map(ROOT/f'Assets/_Game/Resources/Towns/{key}');w,h=data['width'],data['height'];staff=[n for n in entries if n['townKey']==key]
 check(len(staff)==(8 if key=='Kaosia' else 6),('staff count',key));check(len({r['action'] for r in staff})==(7 if key=='Kaosia' else 6),('roles',key))
 source=(int(staff[0]['x']),h-1-int(staff[0]['y']));seen={source};queue=deque([source])
 while queue:
  x,y=queue.popleft()
  for a,b in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
   if 0<a<w-1 and 0<b<h-1 and data['attributes'][b*w+a]!=9 and (a,b) not in seen:seen.add((a,b));queue.append((a,b))
 obstacles=[]
 for obj in data['objects']:
  tw,th=Image.open(ROOT/f"Assets/_Game/Resources/Towns/{key}/{obj['texture']}.png").size
  obstacles.append((obj['x'],obj['y'],obj['x']+math.ceil(tw/32),obj['y']+math.ceil(th/32)))
 reserved=[(r[x],r[y]) for r in world if r['fromKey']==key for x,y in [('portalX','portalY'),('spawnX','spawnY')]]+[(data['spawnX']+.5,h-data['spawnY']-.5)]
 if key=='Kaosia':reserved += [r[p] for r in facilities.values() for p in ['portal','spawn']]+[(21,21)]
 for row in staff:
  x,y=int(row['x']),h-1-int(row['y']);check((x,y) in seen,('connected terrain',key,row['name']))
  check(not any(a<=x<c and b<=y<d for a,b,c,d in obstacles),('outside decoration',key,row['name']))
  check(all((row['x']-a)**2+(row['y']-b)**2>=9 for a,b in reserved),('portal/spawn/staff separation',key,row['name']));reserved.append((row['x'],row['y']))
  art=ROOT/'Assets/_Game/Resources'/(row['texture']+'.png');check(art.exists() and art.with_name(art.name+'.meta').exists(),('native art reference',row['name']))
  if row['action']=='Quests':
   source=next(q for q in quests if q['id']==row['questId']);check((row['name'],row['appearanceId'])==(source['npcName'],source['npcId']),'original quest actor identity')
hud=(ROOT/'Assets/_Game/Runtime/UI/WorldSessionHud.cs').read_text();check('ui.Button(' not in hud and 'ui.Panel(' not in hud,'no HUD shortcut/menu panel')
shop=(ROOT/'Assets/_Game/Runtime/World/ShopCounter2D.cs').read_text();check('ui.Button(' not in shop and 'OnTriggerEnter2D' not in shop,'shop staff uses actual click, no floating button/automatic dialogue')
panel=(ROOT/'Assets/_Game/Runtime/UI/TownServicesPanel.cs').read_text();check('if(service==TownService.Overview)return false;' in panel and 'town-formation' not in panel and 'town-save"' not in panel,'overview and manual save menu removed')
for path in (ROOT/'Assets/_Game/Runtime/UI').glob('TownServicesPanel*.cs'):
 check('시설 목록' not in path.read_text() and '시설 (임시)' not in path.read_text(),('no overview return',path.name))
report=dict(checks=checks,npcs=len(entries),towns=4,roleCounts=dict(Counter(r['action'] for r in entries)),source='Original native artwork and quest names; user-authorized local positions and staff roles.',unityPlay=False,scope='Connected terrain, decoration/portal/spawn separation, native resource references, quest identities, removed shortcut render paths. Not Unity input, NPC transactions or actual save verification.')
(ROOT/'Tools/MapChecks/service_npcs_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report))
