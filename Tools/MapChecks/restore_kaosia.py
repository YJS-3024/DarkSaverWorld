"""Restore only user-confirmed early Kaosia, preserving later gameplay changes.
Run from the repository root with Pillow and UnityPy installed.
Baseline 7574dbac^ supplies authored doors, not recovered original server routes.
"""
import sys,json,re,subprocess,math,hashlib
from pathlib import Path
sys.path[:0]=['Tools/MapChecks','/tmp/ds-late-deps']
import world_asset_baker as b
import UnityPy
R=Path.cwd(); rev='7574dbac^'
def old(p):return subprocess.check_output(['git','show',rev+':'+str(p)])
def write(p,d):Path(p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
# Explicit mixed-version policy: Kaosia is the user-confirmed early layout.
p=Path('Tools/MapChecks/world_asset_baker.py');s=p.read_text()
if "if str(id)=='1000':return NATIVE" not in s:
 s=s.replace("    late=LATE_MAPS/(id+'.mrc')","    # User-confirmed Kaosia uses the early layout.\n    if str(id)=='1000':return NATIVE/(str(id)+'.mrc')\n    late=LATE_MAPS/(id+'.mrc')")
 p.write_text(s)
# Reload module after policy change, preserving all existing import settings/GUIDs.
import importlib;importlib.reload(b)
orig=b.png
def png(path,image):
 meta=path.with_name(path.name+'.meta');saved=meta.read_bytes() if meta.exists() else None
 orig(path,image)
 if saved is not None:meta.write_bytes(saved)
b.png=png
folder=R/'Assets/_Game/Resources/Towns/Kaosia';d,proof=b.bake('1000',folder,{9});native=b.load_map('1000');component=b.walkable(native,{9});clear=component-b.covered(native)
assert d['width']==43 and len(d['objects'])==225
# Explicit initial player spawn; world position itself remains 21,21 in the game flow.
d['spawnX']=21;d['spawnY']=21;(folder/'Map.json').write_text(json.dumps(d,ensure_ascii=False,separators=(',',':'))+'\n')
for f,typ in [('ShopInteriorCatalog.cs','ShopInteriorDefinition'),('KaosiaFacilityCatalog.cs','KaosiaFacilityDefinition')]:
 p=Path('Assets/_Game/Runtime/Core')/f;prior=old(p).decode();s=p.read_text()
 # Restore only the Kaosia constructor records, preserving later logic changes.
 rows={m[1]:m[0] for m in re.finditer(r'new '+typ+r'\("(Kaosia[^\"]*)"[^\n]*?\)',prior)}
 s=re.sub(r'new '+typ+r'\("(Kaosia[^\"]*)"[^\n]*?\)',lambda m:rows[m[1]],s);p.write_text(s)
p=Path('Assets/_Game/Runtime/Core/TownCatalog.cs');p.write_text(p.read_text().replace('"1000",61,61,true','"1000",43,43,true'))
p=Path('Assets/_Game/Resources/World/Routes.json');routes=json.loads(p.read_text());prior=json.loads(old(p));r=next(r for r in routes['routes'] if r['fromKey']=='Kaosia');r.update(next(r for r in prior['routes'] if r['fromKey']=='Kaosia'));routes['provenance']='User-confirmed early Kaosia layout; other regions retain late layouts and authored reciprocal routing';write(p,routes)
# Four manually serialized gates/return transforms only; keep current components and references.
p=Path('Assets/_Game/Scenes/Runtime/Town_Runtime.unity');before=UnityPy.load(old(p));env=UnityPy.load(str(p));names={'Training Entrance','Spawn FromTraining','To World Field','Spawn FromField'}
def transforms(env):
 result={}
 for o in env.objects:
  if o.type.name!='GameObject':continue
  go=o.read_typetree()
  if go['m_Name'] in names:
   t=next(o.assets_file.objects[c['component']['m_PathID']] for c in go['m_Component'] if o.assets_file.objects[c['component']['m_PathID']].type.name=='Transform');result[go['m_Name']]=t
 return result
positions={k:t.read_typetree()['m_LocalPosition'] for k,t in transforms(before).items()}
for k,t in transforms(env).items():
 td=t.read_typetree();td['m_LocalPosition']=positions[k];t.save_typetree(td)
p.write_bytes(env.file.save())
# Gate/return points are protected from NPC placement and automatic resume.
protected=[(21,21)]
for name,pos in positions.items():protected.append((pos['x'],pos['y']))
for f,typ,skip in [('ShopInteriorCatalog.cs','ShopInteriorDefinition',4),('KaosiaFacilityCatalog.cs','KaosiaFacilityDefinition',6)]:
 for m in re.finditer(r'new '+typ+r'\("Kaosia[^\n]*?\)',(Path('Assets/_Game/Runtime/Core')/f).read_text()):
  tokens=m[0].split('(',1)[1][:-1].split(',');v=[float(x.strip().rstrip('f')) for x in tokens[skip:skip+4]];protected.extend([tuple(v[:2]),tuple(v[2:])])
def world(p):return (p[0]+.5,42.5-p[1])
def away(p,radius):return all((world(p)[0]-q[0])**2+(world(p)[1]-q[1])**2>=radius**2 for q in protected)
assert all((x,y) in component for x in [20,21] for y in [21,22])
p=Path('Assets/_Game/Resources/Town/ServiceNpcs.json');npcs=json.loads(p.read_text());placements=[]
for npc in npcs['entries']:
 if npc['townKey']!='Kaosia':continue
 q=min((p for p in clear if away(p,2)),key=lambda p:((world(p)[0]-npc['x'])**2+(world(p)[1]-npc['y'])**2,p));npc['x'],npc['y']=world(q);protected.append(world(q));placements.append({k:npc[k] for k in ['name','x','y']})
write(p,npcs)
p=Path('Assets/_Game/Resources/World/LateLayoutResume.json');resume=json.loads(p.read_text());entry=next(r for r in resume['maps'] if r['mapScene']=='Kaosia_Map');safe={q for q in clear if away(q,2)};safe|={(20,21),(21,21),(20,22),(21,22)};entry.update(width=43,height=43,safeCells=sorted(y*43+x for x,y in safe));write(p,resume)
# Verify every facility threshold and return is reachable on restored terrain.
for x,y in protected:
 assert (int(x),42-int(y)) in component,(x,y,'unreachable')
write('Tools/LateVersionChecks/kaosia_restore_check.json',dict(policy='User-confirmed early Kaosia only; late character/item/scenario data and other maps preserved',source='Saver_1.2/MAP/1000.mrc',**proof,width=43,height=43,initialWorldPosition=[21,21],npcPlacements=placements,manualGates=positions,reachableProtectedPoints=len(protected),safeResumeCells=len(safe),saveFilesModified=False,verification='Offline source/resource/reachability audit; Unity Play not run'))
print('PASS restored early Kaosia',proof,'reachable gates/NPC/spawn',len(protected))
