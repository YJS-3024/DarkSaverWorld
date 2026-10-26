"""User-authorized local service NPCs replacing shortcut menus, not original placements."""
import json,sys,uuid
from pathlib import Path
from collections import deque
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];out=ROOT/'Assets/_Game/Resources/Town/ServiceNpcs.json'
regions={'Kaosia':(61,61),'Belpueus':(65,65),'Sisirio':(30,30),'Castlelim':(60,60)}
roles=[('소지품 안내인','Inventory',0,805),('자기정보 안내인','CharacterInfo',0,807),('전직관','ClassChange',0,809),('용병 모집관','Mercenary',0,810),('부대 관리인','Formation',0,816),('기록관','Save',0,817)]
world=json.loads((ROOT/'Assets/_Game/Resources/World/Routes.json').read_text())['routes'];entries=json.loads((ROOT/'Assets/_Game/Resources/Scenarios/Shared/Entrances.json').read_text())['entries'];facilities=json.loads((ROOT/'Tools/LateVersionChecks/world_migration_check.json').read_text())['facilities'];quests=json.loads((ROOT/'Assets/_Game/Resources/Town/Quests.json').read_text())['rows'];result=[]
for key,(w,h) in regions.items():
 folder=ROOT/'Assets/_Game/Resources/Towns'/key;data=json.loads((folder/'Map.json').read_text());assert (w,h)==(data['width'],data['height'])
 free={(x,y) for y in range(1,h-1) for x in range(1,w-1) if data['attributes'][y*w+x]!=9};remaining=set(free);parts=[]
 while remaining:
  p=min(remaining);remaining.remove(p);component={p};queue=deque([p])
  while queue:
   x,y=queue.popleft()
   for q in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
    if q in remaining:remaining.remove(q);component.add(q);queue.append(q)
  parts.append(component)
 component=max(parts,key=len);covered=set()
 for obj in data['objects']:
  iw,ih=Image.open(folder/(obj['texture']+'.png')).size
  covered.update((obj['x']+dx,obj['y']+dy) for dx in range((iw+31)//32) for dy in range((ih+31)//32))
 reserved=[(r[x],r[y]) for r in world if r['fromKey']==key for x,y in [('portalX','portalY'),('spawnX','spawnY')]]+[(data['spawnX']+.5,h-data['spawnY']-.5)]
 if key=='Kaosia':reserved += [r[p] for r in facilities.values() for p in ['portal','spawn']]+[(21,21)]
 specs=list(roles)
 if key=='Kaosia':specs += [(q['npcName'],'Quests',q['id'],q['npcId']) for q in quests]
 for name,action,quest,appearance in specs:
  # Generic staff use existing native indoor frames; quest NPC805/806 reuse exact existing scenario frames.
  texture=f'Scenarios/Shared/Characters/{appearance}_Down_1' if quest else f'Interiors/Npcs/{appearance}'
  assert (ROOT/'Assets/_Game/Resources'/(texture+'.png')).exists(),texture
  options=[(x,y) for x,y in component-covered if all((x+.5-rx)**2+(h-y-.5-ry)**2>=9 for rx,ry in reserved)]
  assert options,key
  x,y=min(options,key=lambda p:((p[0]-w/2)**2+(p[1]-h/2)**2,p));wx,wy=x+.5,h-y-.5;reserved.append((wx,wy))
  result.append(dict(townKey=key,name=name,action=action,questId=quest,appearanceId=appearance,texture=texture,x=wx,y=wy))
content=dict(version=1,provenance='User-authorized single-player NPC roles and placements; exact existing native artwork; no recovered original coordinates claimed',entries=result)
out.write_text(json.dumps(content,ensure_ascii=False,indent=2)+'\n');out.with_name(out.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,str(out.relative_to(ROOT))).hex+'\n')
report=dict(npcs=len(result),towns=len(regions),roles=sorted({r['action'] for r in result}),walkableConnected=True,decorationAndPortalOverlap=False,unityPlay=False)
(ROOT/'Tools/MapChecks/service_npcs_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report))
