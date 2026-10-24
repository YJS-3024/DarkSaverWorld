"""Offline native field geometry/pixels, graph reachability and scene/resource audit."""
import csv,hashlib,json,re
from collections import defaultdict,deque
from pathlib import Path
from PIL import Image
import UnityPy,yaml
from world_asset_baker import ROOT,NATIVE,load_map,walkable,covered,frame,resource
from check_four_towns import UnityYaml

def main():
    manifest=json.loads((ROOT/'Tools/MapChecks/world_fields_resources.json').read_text());assert len(manifest)==17
    route_data=json.loads((ROOT/'Assets/_Game/Resources/World/Routes.json').read_text());routes=route_data['routes'];assert len(routes)==42
    regions={}
    for id,title,w,h in re.findall(r'Field\("(\d+)","([^"]+)",(\d+),(\d+)\)',(ROOT/'Assets/_Game/Runtime/Core/FieldCatalog.cs').read_text()):regions['WorldField'+id]=(id,int(w),int(h),{3,6,9})
    for key,title,map,runtime,id,w,h in re.findall(r'new TownDefinition\("([^"]+)","([^"]+)","([^"]+)","([^"]+)","([^"]+)",(\d+),(\d+),',(ROOT/'Assets/_Game/Runtime/Core/TownCatalog.cs').read_text()):regions[key]=(id,int(w),int(h),{9})
    assert len(regions)==22
    components={key:walkable(load_map(id),blocked) for key,(id,w,h,blocked) in regions.items()}
    adjacency=defaultdict(list)
    for route in routes:
        key,to=route['fromKey'],route['toKey'];id,w,h,blocked=regions[key]
        portal=(int(route['portalX']),h-1-int(route['portalY']));spawn=(int(route['spawnX']),h-1-int(route['spawnY']))
        assert portal in components[key] and spawn in components[key],(key,to,'not connected')
        reciprocal=next(r for r in routes if r['fromKey']==to and r['toKey']==key)
        assert route['targetSpawnId']==reciprocal['spawnId']
        if not route['legacy']:
            assert (route['portalX']-route['spawnX'])**2+(route['portalY']-route['spawnY'])**2>=9
            for other in routes:
                if other['fromKey']==key:assert (route['spawnX']-other['portalX'])**2+(route['spawnY']-other['portalY'])**2>=2.25,(key,'arrival overlaps another gate')
        adjacency[key].append(to)
    visited={'Kaosia'};queue=deque(visited)
    while queue:
        for target in adjacency[queue.popleft()]:
            if target not in visited:visited.add(target);queue.append(target)
    assert visited==set(regions)
    settings=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'));build=next(o.read_typetree()['m_Scenes'] for o in settings.objects if o.type.name=='EditorBuildSettings')
    monster_rows={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/monsters.csv').open(encoding='utf-8-sig'))}
    cells=objects=enemies=0
    for row in manifest:
        key=row['key'];id=row['nativeMap'];original=load_map(id);folder=ROOT/'Assets/_Game/Resources/Fields'/key
        assert hashlib.sha256((NATIVE/(id+'.mrc')).read_bytes()).hexdigest()==row['sourceSha256']
        data=json.loads((folder/'Map.json').read_text());w,h=data['width'],data['height'];floor=Image.open(folder/'Ground.png').convert('RGBA')
        assert floor.size==(w*32,h*32) and max(floor.size)<=4096
        assert data['attributes']==[t[3]&127 for t in original['tiles']]
        assert len(data['floorKeys'])==w*h and len(set(data['floorKeys']))==row['uniqueFloorTiles']
        patterns={};shared={}
        for i,(rid,group,tile) in enumerate(zip(original['a'],original['b'],original['tiles'])):
            marker=rid,group,bytes(tile[:3])
            if marker not in patterns:
                image=frame(rid,group,0).copy()
                if tile[1]!=255:
                    g=resource(tile[0])['groups'][tile[1]];image.alpha_composite(frame(tile[0],tile[1],g['tiles'][tile[2]]['frames'][0],transparent=True))
                patterns[marker]=image.tobytes()
            expected=patterns[marker];x,y=i%w,i//w
            assert floor.crop((x*32,y*32,(x+1)*32,(y+1)*32)).tobytes()==expected
            k=data['floorKeys'][i]
            if k in shared:assert shared[k]==expected
            else:shared[k]=expected
        assert len(data['objects'])==len(original['sprites'])
        for i,(actual,s) in enumerate(zip(data['objects'],original['sprites'])):
            assert (actual['index'],actual['x'],actual['y'])==(i,s['x'],s['y'])
            texture=folder/(actual['texture']+'.png');assert Image.open(texture).convert('RGBA').tobytes()==frame(s['resource'],s['index'],s['frame'],True,True).tobytes()
        assert len(data['encounters'])==4
        for enemy in data['encounters']:
            assert (enemy['x'],enemy['y']) in components[key] and (enemy['x'],enemy['y']) not in covered(original)
            native=monster_rows[enemy['job']]
            for field,column in [('hp','체력수'),('mp','마력수'),('attack','공력수'),('defense','방력수'),('magicAttack','마공수'),('magicDefense','마방수'),('experience','획득경')]:assert enemy[field]==int(native[column])
            assert (ROOT/'Assets/_Game/Resources/World/Monsters'/(str(enemy['job'])+'.png')).exists()
            for route in routes:
                if route['fromKey']==key:
                    ex,ey=enemy['x']+.5,h-1-enemy['y']+.5
                    assert (ex-route['portalX'])**2+(ey-route['portalY'])**2>=16
        for runtime in [False,True]:
            name='Field'+id+'_Runtime' if runtime else 'World'+id+'_Map';path='Assets/_Game/Scenes/'+('Runtime' if runtime else 'Maps')+'/'+name+'.unity';file=ROOT/path
            text=file.read_text().replace('%TAG !u! tag:unity3d.com,2011:\n','').replace('!u!','!<tag:unity3d.com,2011:');text=re.sub(r'!<tag:unity3d.com,2011:(\d+)',r'!<tag:unity3d.com,2011:\1>',text)
            docs=list(yaml.load_all(text,Loader=UnityYaml));component=next(d['MonoBehaviour'] for d in docs if 'MonoBehaviour' in d)
            assert component['townKey']==key and bool(component['runtimeOnly'])==runtime
            entry=next(e for e in build if e['path']==path);assert entry['enabled'];guid=re.search(r'guid: (\w+)',file.with_name(file.name+'.meta').read_text())[1]
            assert entry['guid']=={f'data[{i}]':int(guid[i*8:i*8+8][::-1],16) for i in range(4)}
        cells+=w*h;objects+=len(data['objects']);enemies+=len(data['encounters'])
    ui=(ROOT/'Assets/_Game/Runtime/UI/TownServicesPanel.cs').read_text();assert 'TownService.Travel' not in ui and '마을 이동' not in ui
    assert not (ROOT/'Assets/_Game/Runtime/UI/TownServicesPanel.Travel.cs').exists()
    report=dict(ordinary_fields=18,towns=4,new_field_maps=17,new_scenes=34,directed_routes=len(routes),fully_reachable_regions=len(visited),native_ground_cells=cells,native_decorations=objects,authored_encounters=enemies,
        source_scope='Exact native geometry/base-stat/artwork resources; first frame ground rendering. Field aliases, topology and encounter placements are authored.',
        verification='Offline source pixels, collision components, reciprocal spawns, graph coverage, scene/build references, base-stat matches and menu removal. Not Unity import or Play.',
        excluded='Scenario and dungeon-interior maps; original server spawn/route tables unavailable')
    (ROOT/'Tools/MapChecks/world_fields_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
if __name__=='__main__':main()
