"""Offline doorway/collision, native texture, category, loader/build and reference audit."""
import csv,hashlib,json,re
from collections import deque
from PIL import Image
import UnityPy
from world_asset_baker import ROOT,NATIVE,load_map,walkable,frame

def main():
    catalog=(ROOT/'Assets/_Game/Runtime/Core/ShopInteriorCatalog.cs').read_text()
    pattern=r'new ShopInteriorDefinition\("([^"]+)","([^"]+)",(true|false),(\d+),([\d.]+)f,([\d.]+)f,([\d.]+)f,([\d.]+)f,"([^"]+)"\)'
    rows=re.findall(pattern,catalog);assert len(rows)==2
    native=load_map('1000');component=walkable(native,{9});settings=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'))
    build=next(o.read_typetree()['m_Scenes'] for o in settings.objects if o.type.name=='EditorBuildSettings')
    loader_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/_Game/Runtime/World/ShopInteriorScene.cs.meta').read_text())[1]
    manifest=json.loads((ROOT/'Tools/MapChecks/shop_interiors_resources.json').read_text())
    import sys
    sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
    from analyze_shop_interior_maps import render
    native_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/_Game/Runtime/World/NativeTownScene.cs.meta').read_text())[1]
    checked_cells=0;checked_objects=0
    assert len(manifest['nativeInteriorMaps'])==22 and len(manifest['scenes'])==80
    for region in manifest['nativeInteriorMaps']:
        key=region['nativeMap'];source=load_map(key);folder=ROOT/'Assets/_Game/Resources/Interiors'/key
        data=json.loads((folder/'Map.json').read_text());assert (data['width'],data['height'])==(source['width'],source['height'])
        assert data['attributes']==[t[3]&127 for t in source['tiles']]
        expected=render({**source,'sprites':[]});assert Image.open(folder/'Ground.png').convert('RGBA').tobytes()==expected.tobytes()
        assert len(data['objects'])==len(source['sprites'])
        for actual,original in zip(data['objects'],source['sprites']):
            assert (actual['x'],actual['y'])==(original['x'],original['y'])
            pixels=frame(original['resource'],original['index'],original['frame'],True,True)
            assert Image.open(folder/(actual['texture']+'.png')).convert('RGBA').tobytes()==pixels.tobytes()
        assert (data['spawnX'],data['spawnY']) in walkable(source,{9})
        assert hashlib.sha256((NATIVE/(key+'.mrc')).read_bytes()).hexdigest()==region['sourceSha256']
        for path in region['scenes']:
            text=(ROOT/path).read_text();assert 'guid: '+native_guid in text and 'townKey: '+region['key'] in text
        entry=(ROOT/region['playScene']).read_text()
        assert 'DarkSaver.World.AdditiveSceneBootstrap' in entry and 'mapScene: '+region['key']+'_Map' in entry
        assert 'runtimeScene: '+region['key']+'_Runtime' in entry and 'useEntryPosition: 1' in entry
        assert f'entryPosition: {{x: {data["spawnX"]+.5}, y: {data["height"]-data["spawnY"]-.5}}}' in entry
        checked_cells+=data['width']*data['height'];checked_objects+=len(data['objects'])
    npcs=manifest['authoredIndoorNpcs'];assert len(npcs)==7 and len({n['nativeMap'] for n in npcs})==7
    for npc in npcs:
        folder=ROOT/'Assets/_Game/Resources/Interiors'/npc['nativeMap'];data=json.loads((folder/'Map.json').read_text())
        assert data['npc']=={k:npc[k] for k in ('name','texture','appearanceId','x','y')}
        assert (npc['x'],npc['y']) in walkable(load_map(npc['nativeMap']),{9})
        assert abs(npc['x']+.5-10.5)+abs(data['height']-npc['y']-.5-3.5)>2
        source=ROOT/npc['source'];assert hashlib.sha256(source.read_bytes()).hexdigest()==npc['sourceSha256']
        sheet=Image.open(source).convert('RGBA');size=sheet.width//3;expected=sheet.crop((0,0,size,size))
        expected.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in expected.getdata()])
        assert Image.open(ROOT/'Assets/_Game/Resources/Interiors/Npcs'/(npc['texture']+'.png')).convert('RGBA').tobytes()==expected.tobytes()
    facilities=manifest['kaosiaFacilities'];assert len(facilities)==5
    for facility in facilities:
        source=native['sprites'][facility['buildingIndex']]
        assert (source['resource'],source['index'])==(20,facility['buildingGroup'])
        dx,dy=facility['door'];rx,ry=facility['returnPoint']
        assert (int(dx),42-int(dy)) in component and (int(rx),42-int(ry)) in component
        assert (dx-rx)**2+(dy-ry)**2>=4
        indoor=load_map(facility['nativeMap']);h=indoor['height'];w=indoor['width']
        start=(10,h-4);target=(10,h-1);reachable={start};queue=deque([start])
        while queue:
            x,y=queue.popleft()
            for point in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                a,b=point
                if 0<=a<w and 0<=b<h and indoor['tiles'][b*w+a][3]&127!=9 and point not in reachable:
                    reachable.add(point);queue.append(point)
        assert target in reachable
        for name in facility['scenes']:
            text=(ROOT/name).read_text();assert 'guid: '+native_guid in text and 'townKey: '+facility['key'] in text
    for name in manifest['scenes']:
        path=ROOT/name;meta=re.search(r'guid: (\w+)',path.with_name(path.name+'.meta').read_text())[1]
        entry=next(r for r in build if r['path']==name);assert entry['enabled']
        assert entry['guid']=={f'data[{i}]':int(meta[8*i:8*i+8][::-1],16) for i in range(4)}
    for key in ('2000','2100'):
        data=json.loads((ROOT/'Assets/_Game/Resources/Interiors'/key/'Map.json').read_text())
        source=load_map(key);start=(10,11);reachable={start};queue=deque([start])
        while queue:
            x,y=queue.popleft()
            for point in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
                a,b=point
                if 0<=a<20 and 0<=b<15 and source['tiles'][b*20+a][3]&127!=9 and point not in reachable:
                    reachable.add(point);queue.append(point)
        assert all((x,14-y) in reachable for x,y in [(10,3),(10,0),(10,9)])
    items=list(csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/items.csv').open(encoding='utf-8-sig')))
    stock=[r for r in items if int(r['번호'])>0 and int(r['장착위']) in [1,2,3,4,5,8] and 0<=int(r['장착렙'])<=30
           and r['가격'].isdigit() and int(r['가격'])>0 and all(r[k] in ['0','1'] for k in ['보','비','기','수','창','궁','승','신','마','사'])
           and any(r[k]=='1' for k in ['보','비','기','수','창','궁','승','신','마','사'])]
    assert len(stock)==202;shops=[]
    for key,title,weapons,index,dx,dy,rx,ry,spawn in rows:
        dx,dy,rx,ry=map(float,(dx,dy,rx,ry));index=int(index)
        source=native['sprites'][index];assert (source['resource'],source['index'])==(20,9 if weapons=='true' else 2)
        door=(int(dx),42-int(dy));back=(int(rx),42-int(ry));assert door in component and back in component
        assert (dx-rx)**2+(dy-ry)**2>=9
        for runtime in (False,True):
            path=ROOT/'Assets/_Game/Scenes'/('Runtime' if runtime else 'Maps')/(key+('_Runtime' if runtime else '_Map')+'.unity')
            text=path.read_text();assert 'DarkSaver.World.ShopInteriorScene' in text and 'guid: '+loader_guid in text
            assert 'shopKey: '+key in text and 'runtimeOnly: '+str(int(runtime)) in text
            meta=re.search(r'guid: (\w+)',path.with_name(path.name+'.meta').read_text())[1]
            row=next(r for r in build if r['path']==str(path.relative_to(ROOT)));assert row['enabled']
            assert row['guid']=={f'data[{i}]':int(meta[8*i:8*i+8][::-1],16) for i in range(4)}
        categories={1,2} if weapons=='true' else {3,4,5,8};ids=[int(r['번호']) for r in stock if int(r['장착위']) in categories]
        assert ids
        shops.append(dict(key=key,title=title,exteriorNativeBuildingIndex=index,doorWorld=[dx,dy],returnWorld=[rx,ry],authoredEntranceThreshold=True,
                          defaultStockCount=len(ids),stockIds=ids,roomWidth=20,roomHeight=15,roomLayout='native2000' if weapons=='true' else 'native2100',merchantNpcAdded=True))
    assert sum(s['defaultStockCount'] for s in shops)==202 and not set(shops[0]['stockIds'])&set(shops[1]['stockIds'])
    result=dict(authoredIndoorNpcs=npcs,kaosiaFacilities=facilities,shops=shops,nativeInteriorMaps=22,loaderScenes=58,playEntryScenes=22,nativeFloorPixelsMatch=True,nativeGroundCells=checked_cells,nativeDecorations=checked_objects,scope='Offline source geometry/artwork, scene/build GUIDs, default stock separation. Runtime routing/save policy checked separately in linked C#. Not Unity Play.',unityPlay=False)
    (ROOT/'Tools/MapChecks/shop_interiors_check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(f'PASS 22 native interiors, 80 scenes/build GUIDs; five more Kaosia facilities, {checked_cells} exact floor cells, {checked_objects} decorations; Kaosia portals, seven authored NPCs and stock separation')
if __name__=='__main__':main()
