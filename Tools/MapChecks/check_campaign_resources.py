"""Offline 01-17 resource/provenance audit; no Unity import or Play execution."""
import hashlib,json,re
from PIL import Image
import UnityPy
from build_scenario01 import SOURCE
from world_asset_baker import ROOT,load_map,walkable,frame,resource

def read(path):return json.loads(path.read_text())
def main():
    base=ROOT/'Assets/_Game/Resources/Scenarios';manifest=read(ROOT/'Tools/MapChecks/scenarios_02_17_resources.json')
    plan=read(ROOT/'닥세_리소스/분석결과/싱글판_배치보완안.json')
    entrances=read(base/'Shared/Entrances.json')['entries'];assert len(entrances)==17
    build_env=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'))
    build=next(o.read_typetree()['m_Scenes'] for o in build_env.objects if o.type.name=='EditorBuildSettings')
    units=operations=frames=portraits=0
    for n in range(1,18):
        key=f'{n:02}';data=read(base/key/'Content.json');canonical=dict(data);canonical.pop('sourceHash')
        assert hashlib.sha256(json.dumps(canonical,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()==data['sourceHash'],key
        if n==1:
            assert read(base/key/'Visuals.json')['contentHash']==data['sourceHash']
        else:
            row=manifest['scenarios'][n-2];assert row['sourceHash']==data['sourceHash']
            native=load_map(key);w,h=data['width'],data['height'];component=walkable(native,{3,6,9})
            assert component=={(v%w,v//w) for v in data['traversalCells']}
            source_units={r['unit']:r for r in plan['units'] if r['scenario']==key}
            assert set(source_units)=={u['id'] for u in data['units']}
            occupied=set()
            for u in data['units']:
                src=source_units[u['id']];assert (u['job'],u['wave'])==(src['ability_id'],src['spawn_wave'])
                assert (u['x'],u['y']) in component and (u['wave'],u['x'],u['y']) not in occupied
                occupied.add((u['wave'],u['x'],u['y']));units+=1
            for s in data['statSentinels']:
                u=next(u for u in data['units'] if u['id']==s['unit']);assert (u['mp'],u['magicAttack'])==(0,0)
            endings=data['endings'];assert [e['tag'] for e in endings]==(['A','B'] if n in (2,8,12) else [])
            if endings:assert data['defaultEnding']=='A' and data['outro']==endings[0]['commands']
            commands=data['intro']+data['outro']+[c for e in endings for c in e['commands']]+[c for e in data['events'] for c in e['actions']]
            for c in commands:
                if c['name']=='MAGIC':assert len(c['args'])>=4 and all(a.lstrip('-').isdigit() for a in c['args'])
                if c['name']=='ATTACK':assert len(c['args'])>=2 and all(a.lstrip('-').isdigit() for a in c['args'])
            visual=read(base/key/'Visuals.json');assert visual['contentHash']==data['sourceHash']
            actions={c['line']:c for e in data['events'] for c in e['actions'] if c['name'] in ('CHANGESPRITE','CHANGETILE2')}
            assert set(actions)=={c['line'] for c in visual['operations']}
            for op in visual['operations']:
                c=actions[op['line']];a=list(map(int,c['args']));x,y,rid,group,index=a[:5]
                if c['name']=='CHANGESPRITE':
                    expected=frame(rid,group,index,True,True);source_index=next((i for i,s in enumerate(native['sprites']) if (s['x'],s['y'])==(x,y)),-1)
                    assert op['sourceIndex']==source_index and op['attribute']==(a[5]&127)
                else:
                    first=resource(rid)['groups'][group]['tiles'][index]['frames'][0];expected=frame(rid,group,first,transparent=True)
                    attr=a[6]&127;assert op['attribute']==(attr if attr<=27 else 0)
                assert Image.open(base/key/(op['texture']+'.png')).convert('RGBA').tobytes()==expected.tobytes();operations+=1
            map_scene=next((ROOT/'Assets/_Game/Scenes/Maps').glob('Map_'+key+'_*.unity'))
            runtime=ROOT/'Assets/_Game/Scenes/Runtime'/('Scenario'+key+'_Runtime.unity')
            scene_text=runtime.read_text();assert 'spawnId: ScenarioEntry' in scene_text
            assert f'x: {data["entryX"]+.5}, y: {h-1-data["entryY"]+.5}, z: 0' in scene_text
            for path in (map_scene,runtime):
                row=next(r for r in build if r['path']==str(path.relative_to(ROOT)));assert row['enabled']
                guid=re.search(r'guid: (\w+)',path.with_name(path.name+'.meta').read_text())[1]
                assert row['guid']=={f'data[{i}]':int(guid[i*8:i*8+8][::-1],16) for i in range(4)}
        entrance=next(e for e in entrances if e['key']==key);field=load_map(entrance['fieldKey'].replace('WorldField',''));fh=field['height']
        fc=walkable(field,{3,6,9})
        assert (int(entrance['portalX']),fh-1-int(entrance['portalY'])) in fc
        assert (int(entrance['spawnX']),fh-1-int(entrance['spawnY'])) in fc
        assert (entrance['portalX']-entrance['spawnX'])**2+(entrance['portalY']-entrance['spawnY'])**2>=10
        assert entrance['entryX']==data['entryX']+.5 and entrance['entryY']==data['height']-1-data['entryY']+.5
        back=(int(entrance['returnX']),data['height']-1-int(entrance['returnY']))
        assert back[1]*data['width']+back[0] in data['traversalCells']
        assert back not in {(u['x'],u['y']) for u in data['units']}
    for job in manifest['characterJobs']:
        source=next(p for p in (SOURCE/'gameres').glob('char*/*') if p.name.lower()==str(job)+'r.bmp');image=Image.open(source).convert('RGBA');size=image.width//3
        for row,direction in enumerate(('Up','Down','Left','Right')):
            for i in range(3):
                expected=image.crop((i*size,row*size,(i+1)*size,(row+1)*size));expected.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else 255) for r,g,b,a in expected.getdata()])
                assert Image.open(base/'Shared/Characters'/f'{job}_{direction}_{i}.png').convert('RGBA').tobytes()==expected.tobytes();frames+=1
    for face in set(manifest['portraitIds'])|{0}:
        source=next((p for folder in ('face','face1','face2','face3') for p in (SOURCE/'gameres'/folder).glob('*') if p.name.lower()==str(face)+'.bmp'),SOURCE/'gameres/etc/noface.bmp')
        expected=Image.open(source).convert('RGBA');expected.putalpha(255)
        assert Image.open(base/'Shared/Portraits'/f'{face}.png').convert('RGBA').tobytes()==expected.tobytes();portraits+=1
    music=read(ROOT/'Tools/MapChecks/campaign_music_check.json')
    assert set(music['rendered'])|set(music['missingOriginal'])==set(manifest['music'])
    for name in music['rendered']:assert (base/'Shared/Music'/(name[:-4]+'.ogg')).stat().st_size>1000
    result=dict(scenarios=17,supplementalUnits02To17=units,nativeCharacterFrames=frames,nativePortraitsIncludingFallback=portraits,nativeMapChangeImages=operations,physicalEntrancePairs=17,renderedMidi=len(music['rendered']),missingOriginalMidi=music['missingOriginal'],unityImport=False,unityPlay=False)
    (ROOT/'Tools/MapChecks/campaign_resource_check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
