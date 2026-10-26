"""Independent late-map/gate/campaign asset audit. Does not run Unity or battles."""
import hashlib,json,re,sys
from pathlib import Path
from PIL import Image
import UnityPy
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
import world_asset_baker as b
checks=0

def check(ok,context):
 global checks
 assert ok,context
 checks+=1

def read(path):return json.loads(path.read_text())
def map_check(id,folder,blocked):
 native=b.load_map(id);data=read(folder/'Map.json');w,h=native['width'],native['height']
 check((w,h)==(data['width'],data['height']),('dimensions',id))
 check(data['attributes']==[t[3]&127 for t in native['tiles']],('terrain',id))
 floor=Image.open(folder/'Ground.png').convert('RGBA');check(floor.size==(w*32,h*32),('ground size',id));patterns={}
 for i,(rid,group,tile) in enumerate(zip(native['a'],native['b'],native['tiles'])):
  marker=rid,group,bytes(tile[:3])
  if marker not in patterns:
   image=b.frame(rid,group,0).copy()
   if tile[1]!=255:
    g=b.resource(tile[0])['groups'][tile[1]];image.alpha_composite(b.frame(tile[0],tile[1],g['tiles'][tile[2]]['frames'][0],transparent=True))
   patterns[marker]=image.tobytes()
  check(floor.crop((i%w*32,i//w*32,(i%w+1)*32,(i//w+1)*32)).tobytes()==patterns[marker],('ground pixel',id,i))
 check(len(native['sprites'])==len(data['objects']),('object count',id))
 for i,(obj,source) in enumerate(zip(data['objects'],native['sprites'])):
  check((obj['index'],obj['x'],obj['y'])==(i,source['x'],source['y']),('object coordinate',id,i))
  check(Image.open(folder/(obj['texture']+'.png')).convert('RGBA').tobytes()==b.frame(source['resource'],source['index'],source['frame'],True,True).tobytes(),('sprite pixels',id,i))
 return native,data,b.walkable(native,blocked)

def main():
 report=read(ROOT/'Tools/LateVersionChecks/world_migration_check.json');regions={};town=ROOT/'Assets/_Game/Resources/Towns';fields=ROOT/'Assets/_Game/Resources/Fields';scenarios=ROOT/'Assets/_Game/Resources/Scenarios'
 for key,row in report['areas'].items():
  regions[key]=map_check(row['nativeMap'],(fields if key.startswith('World') else town)/key,{3,6,9} if key.startswith('World') else {9})
  check(hashlib.sha256(b.native_map_path(row['nativeMap']).read_bytes()).hexdigest()==row['sourceSha256'],('native map source hash',key))
 native,data,component=regions['Kaosia'];h=data['height']
 for name,row in report['facilities'].items():
  source=native['sprites'][row['sourceIndex']]
  check((source['resource'],source['index'])==(row['resource'],row['group']),('facility source record',name))
  door,arrival=tuple(row['doorSource']),tuple(row['returnSource'])
  check(door in component and arrival in component and arrival not in b.covered(native),('facility reachable door/clear arrival',name))
  check((door[0]-arrival[0])**2+(door[1]-arrival[1])**2>=9,('facility avoids immediate return',name))
 # The preserved binary town runtime must use the migrated coordinates.
 env=UnityPy.load(str(ROOT/'Assets/_Game/Scenes/Runtime/Town_Runtime.unity'));objects={o.path_id:o for o in env.objects}
 wanted={'Training Entrance':report['facilities']['Training']['portal'],'Spawn FromTraining':report['facilities']['Training']['spawn']}
 r=next(r for r in report['routes'] if r['fromKey']=='Kaosia');wanted.update({'To World Field':[r['portalX'],r['portalY']],'Spawn FromField':[r['spawnX'],r['spawnY']]})
 found=set()
 for obj in env.objects:
  if obj.type.name!='Transform':continue
  t=obj.read_typetree();go=objects[t['m_GameObject']['m_PathID']].read_typetree();name=go['m_Name']
  if name in wanted:check([t['m_LocalPosition']['x'],t['m_LocalPosition']['y']]==wanted[name],('legacy runtime position',name));found.add(name)
 check(found==set(wanted),'all legacy entrances and arrivals found')
 meta=(fields/'WorldField1010/Ground.png.meta').read_text();check('maxTextureSize: 8192' in meta,'5120px field is not downsampled to4096')
 late=read(ROOT/'Tools/LateVersionChecks/new_scenarios_check.json');entries=read(scenarios/'Shared/Entrances.json')['entries'];check(len(entries)==25,'25 campaign gates')
 allroutes=read(ROOT/'Assets/_Game/Resources/World/Routes.json')['routes']
 buildenv=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'));build=next(o.read_typetree()['m_Scenes'] for o in buildenv.objects if o.type.name=='EditorBuildSettings')
 for row in late['scenarios']:
  key=row['key'];native,data,component=map_check(key,scenarios/key/'Map',{3,6,9});definition=read(scenarios/key/'Content.json');unsigned={k:v for k,v in definition.items() if k!='sourceHash'}
  check(hashlib.sha256(json.dumps(unsigned,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()==definition['sourceHash'],('canonical content hash',key))
  check((definition['entryX'],definition['entryY']) in component,('scenario reachable entry',key));occupied=set()
  for unit in definition['units']:
   p=unit['x'],unit['y'];check(p in component and p not in occupied and p!=(definition['entryX'],definition['entryY']),('unit connected unique position',key,unit['id']));occupied.add(p)
   for direction in ['Up','Down','Left','Right']:
    for frame in range(3):check((scenarios/f"Shared/Characters/{unit['job']}_{direction}_{frame}.png").exists(),('unit artwork',key,unit['job']))
  required={int(c['args'][0]) for e in definition['events'] for c in e['conditions'] if c['name']=='CHARDEAD'}
  check(required==set(row['requiredDeaths']) and required<={u['id'] for u in definition['units']},('clear targets exist',key))
  folder=ROOT/f'분석/Saver_Late/decoded/map/{key}set.arc'
  lists=[(definition['intro'],folder/(key+'.deo')),(definition['outro'],folder/(key+'.dee'))]
  for commands,path in lists:
   if not path.exists():check([c['name'] for c in commands]==['END'],('absent script is no-op',key,path.suffix));continue
   source=path.read_text().splitlines()
   for command in commands:
    line=source[command['line']-1].split(';',1)[0].strip();tokens=line.split();check(tokens[0].upper()==command['name'],('source command spelling',key,path.suffix,command['line']))
    if command['name']=='MESSAGE':check(line.split(None,5)[5]==command['text'],('source dialogue',key,command['line']))
  for commands,path in lists+[(sum([e['actions'] for e in definition['events']],[]),folder/(key+'.evt'))]:
   for c in commands:
    if c['name']=='PLAYMIDI':check((scenarios/'Shared/Music'/(Path(c['args'][0].replace('\\','/')).stem.lower()+'.ogg')).exists(),('source BGM clip exists',key))
  gate=next(e for e in entries if e['key']==key);fm=b.load_map(gate['fieldKey'].removeprefix('WorldField'));fc=b.walkable(fm,{3,6,9})
  for x,y in [('portalX','portalY'),('spawnX','spawnY')]:check((int(gate[x]),fm['height']-1-int(gate[y])) in fc,('field gate connected',key,x))
  check((int(gate['returnX']),data['height']-1-int(gate['returnY'])) in component,('scenario return connected',key))
  for runtime in [False,True]:
   name=f'Scenario{key}_Runtime' if runtime else f"Map_{key}_{row['title']}";path=ROOT/f"Assets/_Game/Scenes/{'Runtime' if runtime else 'Maps'}/{name}.unity";rel=str(path.relative_to(ROOT));text=path.read_text()
   check(('spawnId: ScenarioEntry' if runtime else 'townKey: Scenario'+key) in text,('scene loader',key,runtime))
   check(sum(s['path']==rel and s['enabled'] for s in build)==1,('enabled scene unique',key,runtime))
   guid=re.search(r'guid: (\w+)',path.with_name(path.name+'.meta').read_text())[1];entry=next(s for s in build if s['path']==rel)
   check(entry['guid']=={f'data[{i}]':int(guid[i*8:i*8+8][::-1],16) for i in range(4)},('scene GUID',key,runtime))
 janus=read(scenarios/'33/Content.json');boss=next(u for u in janus['units'] if u['id']==700);check(boss['job']==767 and boss['hp']==30000,'explicit late Janus identity and base HP')
 resume=read(ROOT/'Assets/_Game/Resources/World/LateLayoutResume.json')
 for area in resume['maps']:
  key={'Kaosia_Map':'Kaosia','Belpueus_Map':'Belpueus','World1010_Map':'WorldField1010'}[area['mapScene']];native,data,component=regions[key];w,h=data['width'],data['height']
  portals=[(r['portalX'],r['portalY']) for r in allroutes if r['fromKey']==key]+[(e['portalX'],e['portalY']) for e in entries if e['fieldKey']==key]
  if key=='Kaosia':portals += [r['portal'] for r in report['facilities'].values()]
  check(area['safeCells']==sorted(set(area['safeCells'])),'recovery cells sorted unique')
  for v in area['safeCells']:
   p=v%w,v//w;check(p in component,('recovery cell connected',key,v));check(all((p[0]+.5-x)**2+(h-p[1]-.5-y)**2>=4 for x,y in portals),('recovery avoids live portal',key,v))
 result=dict(checks=checks,areas=3,newScenarios=8,newScenePairs=8,registeredScenarios=25,newUnits=sum(r['units'] for r in late['scenarios']),unityPlay=False,scope='Native source pixels/terrain, door/spawn bindings, content hashes, clear targets, actor art, source dialogue/music, build GUIDs, safe resume cells. Not actual combat, rendered timing or Unity import.')
 (ROOT/'Tools/LateVersionChecks/world_campaign_check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
