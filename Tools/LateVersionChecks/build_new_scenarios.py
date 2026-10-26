"""Build eight late scenarios with explicit local-roster provenance.
Original scripts/AI centers are evidence; reused reviewed supplements are authored
single-player content, not a recovered server deployment or original unlock path.
"""
import sys,json,re,hashlib,io,copy
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapChecks'));sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
import world_asset_baker as b
from build_scenario01 import guid,directory
from build_campaign_entrances import choose
from build_late_saver_catalog import arc_entries
from analyze_client import lzh_decode,lzss_decode
native=Path(sys.argv[1])/'Saver_Files/saver';legacy=b.NATIVE;overrides=b.LATE_MAPS;b.NATIVE=b.LATE_MAPS=native/'MAP'
config=[('33','야누스','32','WorldField1410','17'),('330','추가 전장 330','301','WorldField1010','17'),('331','추가 전장 331','302','WorldField1020','330'),('332','추가 전장 332','303','WorldField1030','331'),('333','추가 전장 333','304','WorldField1040','332'),('404','추가 전장 404','401','WorldField1060','17'),('405','추가 전장 405','402','WorldField1110','404'),('406','추가 전장 406','403','WorldField1111','405')]
plan=json.loads((ROOT/'닥세_리소스/분석결과/싱글판_배치보완안.json').read_text());tables=json.loads((ROOT/'분석/Saver_Late/tables.json').read_text());rows={int(r['values'][0]):r['values'] for r in tables['ability.atr']['rows']}
profile_hash=hashlib.sha256(('late-side-campaign-v1:'+plan['profile_hash']).encode()).hexdigest();out=ROOT/'Assets/_Game/Resources/Scenarios';reports=[];scenes=[];jobs=set();faces=set();music=set()
original_png=b.png
def png(path,image):
 meta=path.with_name(path.name+'.meta');saved=meta.read_text() if meta.exists() else None;original_png(path,image)
 if saved is not None:meta.write_text(saved)
b.png=png
# Parse UTF8-transcoded source, preserving source line numbers and original command spelling.
def commands(path):
 result=[]
 if not path.exists():return result
 for number,line in enumerate(path.read_text().splitlines(),1):
  text=line.split(';',1)[0].strip();tokens=text.split()
  if not tokens:continue
  group=int(tokens.pop(0)) if tokens[0].isdigit() and len(tokens)>1 else 0
  name=tokens.pop(0).upper();body=text.split(None,5)[5] if name=='MESSAGE' else '';args=tokens[:4] if name=='MESSAGE' else tokens
  result.append(dict(name=name,args=args,line=number,text=body,group=group))
 return result
end=[dict(name='END',args=[],line=0,text='',group=0)]
def stats(job,id,x,y):
 v=rows[job];assert int(v[2])>0
 return dict(id=id,job=job,name=v[35],hp=int(v[2]),mp=max(0,int(v[4])),attack=int(v[6]),defense=int(v[8]),magicAttack=max(0,int(v[10])),magicDefense=max(0,int(v[12])),experience=max(0,int(v[32])),x=x,y=y,wave=0)
def safe_scene(path,text):
 path.write_text(text)
 if not path.with_name(path.name+'.meta').exists():path.with_name(path.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
for key,title,template,field,prereq in config:
 folder=ROOT/f'분석/Saver_Late/decoded/map/{key}set.arc';intro=commands(folder/(key+'.deo')) or copy.deepcopy(end);outro=commands(folder/(key+'.dee')) or copy.deepcopy(end)
 events={};event=None
 for c in commands(folder/(key+'.evt')):
  if c['name']=='EVENT':event=dict(id=int(c['args'][0]),scope='',mode='',conditions=[],actions=[]);events[event['id']]=event
  elif c['name'] in ['ALL','CHAR','COMMANDER']:event['scope']=c['name']
  elif c['name'] in ['ONCE','CHARONCE','INF']:event['mode']=c['name']
  elif c['group']:event['conditions'].append(c)
  elif c['name']!='END':event['actions'].append(c)
 native_map=b.load_map(key);w,h=native_map['width'],native_map['height'];component=b.walkable(native_map,{3,6,9});covered=b.covered(native_map);clear=component-covered
 party=next((c for c in intro if c['name']=='CHARPOS' and c['args'][0].upper().startswith('COM')),None)
 old_entry=next(r for r in plan['scenarios'] if r['scenario']==template);anchor=tuple(map(int,party['args'][2:4])) if party else (old_entry['entry_x'],old_entry['entry_y'])
 entry=min(clear or component,key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p));reserved={entry};units=[];placement=[]
 seeds=[r for r in plan['units'] if r['scenario']==template];seed_by_id={r['unit']:r for r in seeds}
 ai={int(m[1]):(int(m[2]),int(m[3])) for m in re.finditer(r'(?m)^\s*(\d+)\s+\w+\s+AREA\s*\(\s*(-?\d+)\s*,\s*(-?\d+)',(folder/(key+'.ai')).read_text())}
 source_actors={int(c['args'][1]):(int(c['args'][0]),int(c['args'][2]),int(c['args'][3])) for c in intro+outro if c['name']=='CHARPOS' and c['args'][0].isdigit() and int(c['args'][1])>=400}
 if key=='33':
  chosen=[]
  for id,center in sorted(ai.items()):
   if id==700:chosen.append((700,767,source_actors[700][1:], 'explicit CHARPOS767/700; cutscene coordinate used as initial local candidate'));continue
   for delta in [0,1,2]:
    seed=seed_by_id.get(id+delta)
    if seed is None:continue
    chosen.append((id+delta,seed['ability_id'],(center[0]+delta%2,center[1]+delta//2),'reviewed scenario32 unit profile; AI AREA center used as authored placement anchor'))
 else:chosen=[(r['unit'],r['ability_id'],(r['x'],r['y']),f'reviewed scenario{template} supplement reused; original initial roster absent') for r in seeds]
 for id,job,point,provenance in chosen:
  options=component-reserved;pos=min(options,key=lambda p:((p[0]-point[0])**2+(p[1]-point[1])**2,p));reserved.add(pos);units.append(stats(job,id,*pos));placement.append(dict(unit=id,job=job,anchor=list(point),actual=list(pos),provenance=provenance))
 required={int(c['args'][0]) for e in events.values() for c in e['conditions'] if c['name']=='CHARDEAD'};assert required<={u['id'] for u in units},(key,required)
 character_jobs={u['job'] for u in units}|{v[0] for v in source_actors.values()};jobs.update(character_jobs)
 cmds=intro+outro+[c for e in events.values() for c in e['actions']]
 for c in cmds:
  if c['name']=='MESSAGE' and c['args'][1].isdigit():faces.add(int(c['args'][1]))
  if c['name']=='PLAYMIDI':music.add(Path(c['args'][0].replace('\\','/')).name.lower())
 definition=dict(version=2,scenarioKey=key,width=w,height=h,entryX=entry[0],entryY=entry[1],profileHash=profile_hash,intro=intro,outro=outro,endings=[],defaultEnding='',events=list(events.values()),units=units,characters=[stats(job,job,0,0) for job in sorted(character_jobs) if job in rows],traversalCells=sorted(y*w+x for x,y in component),placementAdjustments=placement,statSentinels=[],provenance=dict(sourceArchive=key+'set.arc',template=template,unlock='authored prerequisite '+prereq,missingIntro=not (folder/(key+'.deo')).exists(),missingOutro=not (folder/(key+'.dee')).exists()))
 definition['sourceHash']=hashlib.sha256(json.dumps(definition,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 dest=out/key;directory(dest);directory(dest/'Map');b.bake(key,dest/'Map',{3,6,9});b.text_asset(dest/'Content.json',definition);b.text_asset(dest/'Visuals.json',dict(contentHash=definition['sourceHash'],operations=[]))
 # Generic native tilemap loader for map-only, explicit ScenarioEntry marker for runtime.
 map_path=ROOT/f'Assets/_Game/Scenes/Maps/Map_{key}_{title}.unity';text=(ROOT/'Assets/_Game/Scenes/Maps/Belpueus_Map.unity').read_text().replace('Belpueus native region loader',f'Scenario{key} native region loader').replace('townKey: Belpueus','townKey: Scenario'+key);safe_scene(map_path,text)
 runtime=ROOT/f'Assets/_Game/Scenes/Runtime/Scenario{key}_Runtime.unity';spawn_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/_Game/Runtime/World/SceneSpawnPoint.cs.meta').read_text())[1]
 text=text.replace('NativeTownScene','SceneSpawnPoint').replace(f'Scenario{key} native region loader','Spawn ScenarioEntry');text=re.sub(r'm_Script: .*',f'm_Script: {{fileID: 11500000, guid: {spawn_guid}, type: 3}}',text);text=re.sub(r'  townKey: .*\n  runtimeOnly: .*\n','  spawnId: ScenarioEntry\n',text);text=text.replace('m_LocalPosition: {x: 0, y: 0, z: 0}',f'm_LocalPosition: {{x: {entry[0]+.5}, y: {h-entry[1]-.5}, z: 0}}');safe_scene(runtime,text);scenes.extend([map_path,runtime])
 # Cache new scenario MRC and only changed referenced native resources for future checks.
 overrides.mkdir(parents=True,exist_ok=True);(overrides/(key+'.mrc')).write_bytes((native/'MAP'/(key+'.mrc')).read_bytes())
 ids=set(native_map['a'])|{t[0] for t in native_map['tiles'] if t[1]!=255}|{s['resource'] for s in native_map['sprites']}
 for rid in ids:
  name=f'mdsr{rid}.grs'
  if not (legacy/name).exists() or (legacy/name).read_bytes()!=(native/'MAP'/name).read_bytes():(overrides/name).write_bytes((native/'MAP'/name).read_bytes())
 reports.append(dict(key=key,title=title,width=w,height=h,template=template,fieldKey=field,prerequisite=prereq,sourceHash=definition['sourceHash'],units=len(units),requiredDeaths=sorted(required),entry=list(entry)))
# Native job aliases are resolved through the late ability table, not the numeric job filename.
sheets={};portrait_sources={}
for arc in (native/'gameres').glob('*.arc'):
 if not re.fullmatch(r'(char\d+|face\d*)',arc.stem):continue
 for name,method,size,packed in arc_entries(arc):
  if re.fullmatch(r'\d+r\.bmp',name,re.I):sheets[int(name[:-5])]=(method,size,packed)
  elif arc.stem.startswith('face') and Path(name).stem.isdigit():portrait_sources[int(Path(name).stem)]=(method,size,packed)
for job in sorted(jobs):
 sheet=int(rows[job][1]);method,size,packed=sheets[sheet];raw=(lzh_decode if method==0 else lzss_decode)(packed,size);im=Image.open(io.BytesIO(raw)).convert('RGBA');cell=im.width//3
 for y,direction in enumerate(['Up','Down','Left','Right']):
  for x in range(3):
   crop=im.crop((x*cell,y*cell,(x+1)*cell,(y+1)*cell));crop.putdata([(*p[:3],0 if p[:3]==(0,0,0) else 255) for p in crop.getdata()]);path=out/f'Shared/Characters/{job}_{direction}_{x}.png'
   if not path.exists():
    crop.save(path);template=(ROOT/'Assets/_Game/Art/Converted/Monsters/Kisra701_Down_1.png.meta').read_text();path.with_name(path.name+'.meta').write_text(re.sub(r'guid: [0-9a-f]{32}','guid: '+guid(path),template,count=1))
for face in sorted(faces):
 path=out/f'Shared/Portraits/{face}.png'
 if path.exists():continue
 if face in portrait_sources:
  method,size,packed=portrait_sources[face];raw=(lzh_decode if method==0 else lzss_decode)(packed,size);image=Image.open(io.BytesIO(raw)).convert('RGBA');image.putalpha(255)
 else:image=Image.open(out/'01/Portraits/NoFace.png').convert('RGBA')
 b.png(path,image)
# Append physically separated field gates; original 17 entry/state definitions remain unchanged.
p=out/'Shared/Entrances.json';content=json.loads(p.read_text());existing={e['key']:e for e in content['entries']};world=json.loads((ROOT/'Assets/_Game/Resources/World/Routes.json').read_text())['routes']
for row in reports:
 key=row['key'];field=row['fieldKey'];fid=field.removeprefix('WorldField');fm=b.load_map(fid);component=b.walkable(fm,{3,6,9});clear=component-b.covered(fm);fw,fh=fm['width'],fm['height']
 reserve=[]
 for r in world:
  if r['fromKey']==field:reserve.extend([(int(r['portalX']),fh-1-int(r['portalY'])),(int(r['spawnX']),fh-1-int(r['spawnY']))])
 for e in existing.values():
  if e['fieldKey']==field and e['key']!=key:reserve.extend([(int(e['portalX']),fh-1-int(e['portalY'])),(int(e['spawnX']),fh-1-int(e['spawnY']))])
 field_data=json.loads((ROOT/f'Assets/_Game/Resources/Fields/{field}/Map.json').read_text());creatures=[(u['x'],u['y']) for u in field_data.get('encounters',[])];portal,arrival=choose(component,clear,(fw/2,fh/2),reserve,creatures)
 scenario=b.load_map(key);sc=b.walkable(scenario,{3,6,9});definition=json.loads((out/key/'Content.json').read_text());occupied={(u['x'],u['y']) for u in definition['units']};entry=tuple(row['entry']);back=min((p for p in sc-b.covered(scenario)-occupied if (p[0]-entry[0])**2+(p[1]-entry[1])**2>=9),key=lambda p:((p[0]-entry[0])**2+(p[1]-entry[1])**2,p));h=row['height']
 existing[key]=dict(key=key,fieldKey=field,portalX=portal[0]+.5,portalY=fh-portal[1]-.5,spawnX=arrival[0]+.5,spawnY=fh-arrival[1]-.5,entryX=entry[0]+.5,entryY=h-entry[1]-.5,returnX=back[0]+.5,returnY=h-back[1]-.5)
content['entries']=sorted(existing.values(),key=lambda e:int(e['key']));content['provenance']='Authored gates on reviewed native collision components; original campaign plus explicit later side branches';b.text_asset(p,content);b.register(scenes)
# Recovery cells must also avoid the newly added live field portals.
resume_path=ROOT/'Assets/_Game/Resources/World/LateLayoutResume.json'
resume=json.loads(resume_path.read_text())
for area in resume['maps']:
 if area['mapScene']!='World1010_Map':continue
 portals=[(e['portalX'],e['portalY']) for e in content['entries'] if e['fieldKey']=='WorldField1010']
 area['safeCells']=[v for v in area['safeCells'] if all((v%area['width']+.5-x)**2+(area['height']-v//area['width']-.5-y)**2>=4 for x,y in portals)]
b.text_asset(resume_path,resume)
(ROOT/'Tools/LateVersionChecks/new_scenarios_check.json').write_text(json.dumps(dict(scenarios=reports,characterJobs=sorted(jobs),portraitIds=sorted(faces),musicSources=sorted(music),sourceRosterMissing=True,unityPlay=False),ensure_ascii=False,indent=2)+'\n');print(json.dumps(reports,ensure_ascii=False))
