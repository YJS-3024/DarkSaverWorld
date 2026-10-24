"""Bake source02-17 scripts, reviewed supplemental rosters and native presentation bindings."""
import csv,hashlib,json,re,sys,uuid
from pathlib import Path
from PIL import Image
from build_scenario01 import ROOT,SOURCE,commands,directory,guid
from world_asset_baker import load_map,walkable,covered,resource,frame,png,text_asset,register


def main():
    out=ROOT/'Assets/_Game/Resources/Scenarios';directory(out/'Shared');directory(out/'Shared/Characters');directory(out/'Shared/Portraits')
    plan=json.loads((ROOT/'닥세_리소스/분석결과/싱글판_배치보완안.json').read_text());stats={}
    for filename in ['jobs.csv','monsters.csv','npcs_and_special.csv']:
        for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs'/filename).open(encoding='utf-8-sig')):
            id=int(r['직업']);stats[id]=dict(id=id,job=id,name=r['이름'],hp=int(r['체력수']),mp=int(r['마력수']),attack=int(r['공력수']),defense=int(r['방력수']),magicAttack=int(r['마공수']),magicDefense=int(r['마방수']),experience=int(r['획득경']),x=0,y=0,wave=0)
    titles={r['mapKey']:r['title'] for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/maps.csv').open(encoding='utf-8-sig'))}
    metadata=[];report=[];jobs=set();faces=set();music=set();scenes=[]
    inert={'EN5','ENDDD','MESSGE','MASSAGE','CAHRDIR'}
    def source_commands(path):
        rows=commands(path)
        for c in rows:
            if c['name'] in inert:c['sourceName']=c['name'];c['name']='NATIVE_INERT'
        return rows
    for n in range(2,18):
        key=f'{n:02}';folder=SOURCE/'MAP'/(key+'set');files={p.suffix.lower():p for p in folder.iterdir() if p.stem.lower()==key and p.suffix.lower() in ['.deo','.dee','.evt']}
        endings=[dict(tag=p.stem[-1].upper(),commands=source_commands(p),source=p.name) for p in sorted(folder.iterdir()) if p.suffix.lower()=='.dee' and p.stem.lower() in [key+'a',key+'b']]
        intro=source_commands(files['.deo']);outro=source_commands(files['.dee']) if '.dee' in files else endings[0]['commands'];events={};active=None
        for c in source_commands(files['.evt']):
            name=c['name']
            if name=='EVENT':active=dict(id=int(c['args'][0]),scope='',mode='',conditions=[],actions=[]);events[active['id']]=active
            elif name in ['ALL','CHAR','COMMANDER']:active['scope']=name
            elif name in ['ONCE','CHARONCE','INF']:active['mode']=name
            elif c['group']:active['conditions'].append(c)
            elif name!='END':active['actions'].append(c)
        events=list(events.values());native=load_map(key);w,h=native['width'],native['height'];component=walkable(native,{3,6,9})
        entry=next(r for r in plan['scenarios'] if r['scenario']==key);anchor=(entry['entry_x'],entry['entry_y']);entry_cell=min(component,key=lambda p:(abs(p[0]-anchor[0])+abs(p[1]-anchor[1]),p))
        reserved={entry_cell};reserved.update((int(c['args'][0]),int(c['args'][1])) for e in events for c in e['conditions'] if c['name'] in ['CHARPOS','CHARPOS3'])
        units=[];adjustments=[];sentinels=[]
        for r in [r for r in plan['units'] if r['scenario']==key]:
            u=dict(stats[r['ability_id']],id=r['unit'],x=r['x'],y=r['y'],wave=r['spawn_wave'])
            if u['mp']==-1 and u['magicAttack']==-1:
                sentinels.append(dict(unit=u['id'],job=u['job'],rawMp=-1,rawMagicAttack=-1,runtimeMp=0,runtimeMagicAttack=0,policy='no-magic sentinel adapted only for local combat view'))
                u['mp']=u['magicAttack']=0
            if any(u[f]<0 for f in ['mp','attack','defense','magicAttack','magicDefense','experience']) or u['hp']<=0:raise ValueError((key,u,'invalid source stats'))
            old=(u['x'],u['y']);used={(a['x'],a['y']) for a in units if a['wave']==u['wave']}
            if old not in component or old in used or old in reserved:
                options=component-reserved-used
                pos=min(options,key=lambda p:(abs(p[0]-old[0])+abs(p[1]-old[1]),p))
                adjustments.append(dict(unit=u['id'],fromX=old[0],fromY=old[1],toX=pos[0],toY=pos[1],reason='authored placement adjusted to actual Unity collision/component'))
                u['x'],u['y']=pos
            units.append(u)
        all_outros=outro+[c for ending in endings for c in ending['commands']]
        allcmd=intro+all_outros+[c for e in events for c in e['actions']]
        character_jobs={u['job'] for u in units}|{int(c['args'][0]) for c in intro+all_outros if c['name']=='CHARPOS' and c['args'][0].isdigit()}
        jobs.update(character_jobs)
        for c in allcmd:
            if c['name']=='MESSAGE' and c['args'][1].isdigit():faces.add(int(c['args'][1]))
            if c['name']=='PLAYMIDI':music.add(Path(c['args'][0].replace('\\','/')).name.lower())
        definition=dict(version=2,scenarioKey=key,width=w,height=h,entryX=entry_cell[0],entryY=entry_cell[1],profileHash=plan['profile_hash'],intro=intro,outro=outro,endings=endings,defaultEnding=endings[0]['tag'] if endings else '',events=events,units=units,
                        characters=[stats[j] for j in sorted(character_jobs) if j in stats],traversalCells=sorted(y*w+x for x,y in component),placementAdjustments=adjustments,statSentinels=sentinels)
        definition['sourceHash']=hashlib.sha256(json.dumps(definition,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
        destination=out/key;directory(destination);directory(destination/'Changes');text_asset(destination/'Content.json',definition)
        changes=[]
        for e in events:
            for c in e['actions']:
                if c['name'] not in ['CHANGESPRITE','CHANGETILE2']:continue
                a=list(map(int,c['args']));x,y,rid,group,index=a[:5]
                if c['name']=='CHANGESPRITE':
                    g=resource(rid)['sprites'][group];image=frame(rid,group,index,True,True);source_index=next((i for i,s in enumerate(native['sprites']) if (s['x'],s['y'])==(x,y)),-1);attribute=a[5]&127
                    width,height=g['width']//32,g['height']//32
                else:
                    g=resource(rid)['groups'][group];first=g['tiles'][index]['frames'][0];image=frame(rid,group,first,transparent=True);source_index=-1;attribute=a[6]&127;attribute=attribute if attribute<=27 else 0;width=height=1
                file=destination/'Changes'/(str(c['line'])+'.png');png(file,image)
                changes.append(dict(line=c['line'],kind=c['name'],x=x,y=y,sourceIndex=source_index,attribute=attribute,widthTiles=width,heightTiles=height,texture='Changes/'+str(c['line'])))
        text_asset(destination/'Visuals.json',dict(contentHash=definition['sourceHash'],operations=changes))
        map_path=next(p for p in (ROOT/'Assets/_Game/Scenes/Maps').glob('Map_'+key+'_*') if p.suffix=='.unity');map_scene=map_path.stem
        runtime='Scenario'+key+'_Runtime';scene_path=ROOT/'Assets/_Game/Scenes/Runtime'/(runtime+'.unity')
        # Reuse an existing minimal additive schema with a SceneSpawnPoint component.
        template=(ROOT/'Assets/_Game/Scenes/Maps/Belpueus_Map.unity').read_text();script_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/_Game/Runtime/World/SceneSpawnPoint.cs.meta').read_text())[1]
        template=re.sub(r'm_Name: .*native (?:town|region) loader','m_Name: Spawn ScenarioEntry',template)
        template=re.sub(r'm_LocalPosition: \{x: 0, y: 0, z: 0\}',f'm_LocalPosition: {{x: {entry_cell[0]+.5}, y: {h-1-entry_cell[1]+.5}, z: 0}}',template)
        template=re.sub(r'm_Script: .*',f'm_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}',template)
        template=template.replace('DarkSaver.World.NativeTownScene','DarkSaver.World.SceneSpawnPoint');template=re.sub(r'  townKey: .*\n  runtimeOnly: .*\n','  spawnId: ScenarioEntry\n',template)
        scene_path.write_text(template);scene_path.with_name(scene_path.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(scene_path)+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');scenes.extend([scene_path,map_path])
        metadata.append((key,titles[key],map_scene,runtime,w,h))
        report.append(dict(key=key,title=titles[key],units=len(units),waves=max(u['wave'] for u in units)+1,intro=len(intro),outro=len(outro),endingVariants=[e['tag'] for e in endings],events=len(events),mapChanges=len(changes),adjustedPlacements=len(adjustments),sourceHash=definition['sourceHash']))
    # Native character sheets: source numeric actors plus reviewed roster jobs.
    char_template=(ROOT/'Assets/_Game/Art/Converted/Monsters/Kisra701_Down_1.png.meta').read_text();missing=[]
    for job in sorted(jobs):
        files=[p for p in (SOURCE/'gameres').glob('char*/*') if p.name.lower()==str(job)+'r.bmp']
        if len(files)!=1:missing.append(job);continue
        image=Image.open(files[0]).convert('RGBA');size=image.width//3
        for row,direction in enumerate(['Up','Down','Left','Right']):
            for i in range(3):
                crop=image.crop((i*size,row*size,(i+1)*size,(row+1)*size));crop.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else 255) for r,g,b,a in crop.getdata()])
                path=out/'Shared/Characters'/f'{job}_{direction}_{i}.png';crop.save(path)
                meta=re.sub(r'guid: [0-9a-f]{32}','guid: '+guid(path),char_template,count=1);meta=re.sub(r'spriteID: [0-9a-f]{32}','spriteID: '+guid(path),meta)
                path.with_name(path.name+'.meta').write_text(meta)
    if missing:raise ValueError(('Missing source character sheets',missing))
    for face in sorted(faces|{0}):
        file=next((p for folder in ['face','face1','face2','face3'] for p in (SOURCE/'gameres'/folder).glob('*') if p.name.lower()==str(face)+'.bmp'),SOURCE/'gameres/etc/noface.bmp')
        im=Image.open(file).convert('RGBA');im.putalpha(255);png(out/'Shared/Portraits'/(str(face)+'.png'),im)
    entries=['new ScenarioDefinition("01","브르고스궁","Brugos01_Map","Brugos01_Runtime",40,40)']
    entries += [f'new ScenarioDefinition({json.dumps(k,ensure_ascii=False)},{json.dumps(title,ensure_ascii=False)},{json.dumps(map,ensure_ascii=False)},{json.dumps(runtime)}, {w},{h})' for k,title,map,runtime,w,h in metadata]
    code='''using System;\nusing System.Collections.Generic;\nnamespace DarkSaver.Core\n{\n    public sealed class ScenarioDefinition\n    {\n        public readonly string Key,Title,MapScene,RuntimeScene;public readonly int Width,Height;\n        public ScenarioDefinition(string key,string title,string map,string runtime,int width,int height){Key=key;Title=title;MapScene=map;RuntimeScene=runtime;Width=width;Height=height;}\n    }\n    public static class ScenarioCatalog\n    {\n        public static readonly IReadOnlyList<ScenarioDefinition> All=Array.AsReadOnly(new[]{\n            '''+',\n            '.join(entries)+'''\n        });\n        public static bool TryGet(string key,out ScenarioDefinition scenario){foreach(var row in All)if(row.Key==key || "Scenario"+row.Key==key){scenario=row;return true;}scenario=null;return false;}\n        public static bool IsPair(string map,string runtime){foreach(var row in All)if(row.MapScene==map && row.RuntimeScene==runtime)return true;return false;}\n        public static bool IsMap(string map){foreach(var row in All)if(row.MapScene==map)return true;return false;}\n    }\n}\n'''
    file=ROOT/'Assets/_Game/Runtime/Core/ScenarioCatalog.cs';file.write_text(code);file.with_name(file.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,str(file.relative_to(ROOT))).hex+'\n')
    register(scenes)
    text_asset(out/'Shared/MusicSources.json',dict(files=sorted(music)))
    (ROOT/'Tools/MapChecks/scenarios_02_17_resources.json').write_text(json.dumps(dict(scenarios=report,characterJobs=sorted(jobs),portraitIds=sorted(faces),music=sorted(music)),ensure_ascii=False,indent=2)+'\n')
    print('Baked16 scenario definitions;',sum(r['units'] for r in report),'units;',len(jobs),'character jobs;',len(faces),'portraits;',len(music),'MIDI references.')
if __name__=='__main__':main()
