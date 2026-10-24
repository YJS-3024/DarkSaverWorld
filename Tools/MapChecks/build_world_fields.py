"""Bake all18 ordinary world-region layouts and an explicitly authored reciprocal network.
Dungeon interiors1500+ and scenario arenas remain separate. No original route table is inferred.
"""
import csv,json,re
from collections import defaultdict,deque
from pathlib import Path
from PIL import Image
from world_asset_baker import ROOT,NATIVE,directory,load_map,walkable,covered,bake,png,text_asset,scene,register


def main():
    catalog=(ROOT/'Assets/_Game/Runtime/Core/FieldCatalog.cs').read_text()
    fields=[dict(key='WorldField'+id,title=title,nativeMap=id,width=int(w),height=int(h))
            for id,title,w,h in re.findall(r'Field\("(\d+)","([^"]+)",(\d+),(\d+)\)',catalog)]
    assert len(fields)==18
    towns=[dict(key=key,title=title,nativeMap=id,width=int(w),height=int(h)) for key,title,map,runtime,id,w,h in
           re.findall(r'new TownDefinition\("([^"]+)","([^"]+)","([^"]+)","([^"]+)","([^"]+)",(\d+),(\d+),',
                      (ROOT/'Assets/_Game/Runtime/Core/TownCatalog.cs').read_text())]
    assert len(towns)==4
    nodes={r['key']:r for r in towns+fields}
    def field(id):return 'WorldField'+id
    chains=[['1010','1020','1030','1040','1050','1060','1110','1111'],['1010','1011','1012'],['1060','1070','1080','1090'],['1110','1100'],['1111','1200','1300','1400','1410']]
    pairs=[(field(a),field(b)) for chain in chains for a,b in zip(chain,chain[1:])]
    pairs += [('Belpueus',field('1010')),('Sisirio',field('1010')),('Castlelim',field('1020'))]
    adjacent=defaultdict(list)
    for a,b in pairs:adjacent[a].append(b);adjacent[b].append(a)
    world=ROOT/'Assets/_Game/Resources/World';directory(world);directory(world/'Monsters')
    # Confirmed base records/artwork, authored encounter placements. Fields rearm
    # existing combat encounters; no invented permanent kill/loot server state.
    rows=list(csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/monsters.csv').open(encoding='utf-8-sig')))
    monsters=[]
    for job in [301,302,303,304]:
        source=next(r for r in rows if int(r['직업'])==job)
        images=list((ROOT/'분석/Saver_1.2/decoded/all_arc/gameres').glob('char*/'+str(job)+'R.bmp'));assert len(images)==1
        image=Image.open(images[0]).convert('RGB');size=image.width//3
        im=image.crop((0,size,size,size*2)).convert('RGBA')
        # Native colorkey is exact RGB black, not luminance rounding.
        im.putdata([(*pixel[:3],0 if pixel[:3]==(0,0,0) else 255) for pixel in im.getdata()])
        png(world/'Monsters'/(str(job)+'.png'),im)
        monsters.append(dict(job=job,name=source['이름'],hp=int(source['체력수']),mp=int(source['마력수']),attack=int(source['공력수']),defense=int(source['방력수']),
            magicAttack=int(source['마공수']),magicDefense=int(source['마방수']),experience=int(source['획득경']),wave=0))
    directory(ROOT/'Assets/_Game/Resources/Fields')
    mapdata={};components={};clear={};scenes=[];manifest=[]
    for key,node in nodes.items():
        data=load_map(node['nativeMap']);node['data']=data
        component=walkable(data,{3,6,9} if key.startswith('WorldField') else {9});components[key]=component;clear[key]=component-covered(data)
        if key=='WorldField1010':assert (23,125) in component
        if key.startswith('WorldField') and key!='WorldField1010':
            folder=ROOT/'Assets/_Game/Resources/Fields'/key;result,report=bake(node['nativeMap'],folder,{3,6,9});mapdata[key]=result
            node.update(report)
            for runtime in [False,True]:
                name=('Field'+node['nativeMap']+'_Runtime') if runtime else ('World'+node['nativeMap']+'_Map')
                path=ROOT/'Assets/_Game/Scenes'/('Runtime' if runtime else 'Maps')/(name+'.unity');scene(path,key,runtime);scenes.append(path)
    endpoints={};protected=defaultdict(list)
    protected['WorldField1010']=[(25,125),(25,118),(29,121),(20,121),(23,125)]
    for key,neighbors in adjacent.items():
        node=nodes[key];w,h=node['width'],node['height'];component=components[key]
        candidates=[p for p in clear[key] if all((p[0]+dx,p[1]+dy) in component for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)])]
        assert candidates,key
        anchors=[(w/2,0),(w-1,h/2),(w/2,h-1),(0,h/2)]
        for index,to in enumerate(sorted(neighbors)):
            anchor=anchors[index%4];reserved=protected[key]
            possible=[p for p in candidates if all((p[0]-q[0])**2+(p[1]-q[1])**2>=16 for q in reserved)]
            assert possible,(key,to,'no portal space')
            found=None
            for p in sorted(possible,key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p)):
                distance={p:0};queue=deque([p]);arrivals=[]
                while queue:
                    q=queue.popleft();depth=distance[q]
                    if depth>=3 and (q[0]-p[0])**2+(q[1]-p[1])**2>=10 and q in clear[key] and all((q[0]-r[0])**2+(q[1]-r[1])**2>=9 for r in reserved):arrivals.append(q)
                    if depth>=7:continue
                    for r in [(q[0]-1,q[1]),(q[0]+1,q[1]),(q[0],q[1]-1),(q[0],q[1]+1)]:
                        if r in component and r not in distance:distance[r]=depth+1;queue.append(r)
                if arrivals:
                    arrival=min(arrivals,key=lambda p:((p[0]-w/2)**2+(p[1]-h/2)**2,p));found=p,arrival;break
            assert found,(key,to,'no safe arrival');portal,arrival=found
            protected[key].extend(found);endpoints[key,to]=dict(fromKey=key,toKey=to,portalX=portal[0]+.5,portalY=h-1-portal[1]+.5,
                spawnX=arrival[0]+.5,spawnY=h-1-arrival[1]+.5,spawnId='Route_'+to,targetSpawnId='Route_'+key,legacy=False)
    routes=list(endpoints.values())
    # Preserve the actual existing Kaosia/1010 access points, rather than mapping
    # its four doors to unrelated towns.
    routes.extend([dict(fromKey='Kaosia',toKey='WorldField1010',portalX=21.5,portalY=41.5,spawnX=21.5,spawnY=39,spawnId='FromField',targetSpawnId='FromKaosia',legacy=True),
                   dict(fromKey='WorldField1010',toKey='Kaosia',portalX=25,portalY=34.5,spawnX=23,spawnY=34,spawnId='FromKaosia',targetSpawnId='FromField',legacy=True)])
    for key,data in mapdata.items():
        node=nodes[key];w,h=node['width'],node['height'];used=protected[key];choices=list(clear[key]);encounters=[]
        for index,monster in enumerate(monsters):
            options=[p for p in choices if all((p[0]-q[0])**2+(p[1]-q[1])**2>=16 for q in used)]
            assert options,(key,'no encounter space')
            anchor=[(w*.3,h*.3),(w*.7,h*.3),(w*.3,h*.7),(w*.7,h*.7)][index]
            p=min(options,key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p));used.append(p)
            encounters.append(dict(monster,id=400+index,x=p[0],y=p[1]))
        data['encounters']=encounters;text_asset(ROOT/'Assets/_Game/Resources/Fields'/key/'Map.json',data)
        manifest.append({k:v for k,v in node.items() if k!='data'}|dict(encounters=len(encounters),identity='native geometry, authored region title/routes/encounters',groundAnimation='native first frame'))
    text_asset(world/'Routes.json',dict(version=1,provenance='authored reciprocal routes over native walkable layouts; existing Kaosia route retained',routes=routes))
    register(scenes)
    (ROOT/'Tools/MapChecks/world_fields_resources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    print('Baked',len(manifest),'additional fields,',len(scenes),'scenes,',len(routes),'directed route records and',sum(r['encounters'] for r in manifest),'authored source-stat encounters.')
if __name__=='__main__':main()
