"""Shared native region resources and minimal additive Unity scene writer."""
import hashlib,json,re,sys
from collections import deque
from PIL import Image
from build_scenario01 import ROOT,directory,guid
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_mrc,read_grs,grs_image
NATIVE=ROOT/'닥세_리소스/설치본_추출/Saver_Files/Saver/MAP'
LATE_MAPS=ROOT/'분석/Saver_Late/native_overrides/MAP'
def native_map_path(id):
    # User-confirmed Kaosia layout is the early 43x43 map, even with late overrides.
    if str(id)=='1000':return NATIVE/(str(id)+'.mrc')
    late=LATE_MAPS/(id+'.mrc')
    return late if late.exists() else NATIVE/(id+'.mrc')
GRS={};FRAMES={}
ACTIVE_LATE=False
def resource(id):
    name='mdsr'+str(id)+'.grs'
    path=LATE_MAPS/name if ACTIVE_LATE and (LATE_MAPS/name).exists() else NATIVE/name
    key=str(path)
    if key not in GRS:GRS[key]=read_grs(path)
    return GRS[key]
def frame(rid,group,index,sprite=False,transparent=False):
    key=ACTIVE_LATE,rid,group,index,sprite,transparent
    if key not in FRAMES:
        g=resource(rid)['sprites' if sprite else 'groups'][group];FRAMES[key]=grs_image(g['palette'],g['images'][index],transparent)
    return FRAMES[key]
def png(path,image):
    template=(ROOT/'Assets/_Game/Resources/Scenarios/01/Portraits/NoFace.png.meta').read_text()
    template=template.replace('textureType: 8','textureType: 0').replace('spriteMode: 2','spriteMode: 0').replace('textureCompression: 1','textureCompression: 0').replace('maxTextureSize: 2048','maxTextureSize: 4096')
    if max(image.size)>4096:template=template.replace('maxTextureSize: 4096','maxTextureSize: 8192')
    image.save(path);path.with_name(path.name+'.meta').write_text(re.sub(r'guid: [0-9a-f]{32}','guid: '+guid(path),template,count=1))
def text_asset(path,data):
    path.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n')
    path.with_name(path.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
def load_map(id):
    # Tool calls are sequential. Keep changed GRS resources scoped to upgraded maps.
    global ACTIVE_LATE
    path=native_map_path(id);ACTIVE_LATE=path.parent==LATE_MAPS
    return read_mrc(path)
def walkable(data,blocked):
    w,h=data['width'],data['height'];remaining={(x,y) for y in range(1,h-1) for x in range(1,w-1) if data['tiles'][y*w+x][3]&127 not in blocked};components=[]
    while remaining:
        first=min(remaining);remaining.remove(first);component={first};queue=deque([first])
        while queue:
            x,y=queue.popleft()
            for point in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
                if point in remaining:remaining.remove(point);component.add(point);queue.append(point)
        components.append(component)
    return max(components,key=len)
def covered(data):
    cells=set()
    for s in data['sprites']:
        g=resource(s['resource'])['sprites'][s['index']]
        cells.update((s['x']+dx,s['y']+dy) for dx in range((g['width']+31)//32) for dy in range((g['height']+31)//32))
    return cells
def bake(id,folder,blocked):
    directory(folder);directory(folder/'Sprites');data=load_map(id);w,h=data['width'],data['height'];canvas=Image.new('RGBA',(w*32,h*32),(0,0,0,255));tile_ids={};keys=[]
    for i,(rid,group,tile) in enumerate(zip(data['a'],data['b'],data['tiles'])):
        canvas.alpha_composite(frame(rid,group,0),(i%w*32,i//w*32))
        if tile[1]!=255:
            g=resource(tile[0])['groups'][tile[1]];canvas.alpha_composite(frame(tile[0],tile[1],g['tiles'][tile[2]]['frames'][0],transparent=True),(i%w*32,i//w*32))
        key=rid,group,bytes(tile[:3]);keys.append(tile_ids.setdefault(key,len(tile_ids)))
    png(folder/'Ground.png',canvas);objects=[];written=set()
    for i,s in enumerate(data['sprites']):
        name=f"Sprites/{s['resource']}_{s['index']}_{s['frame']}";path=folder/(name+'.png')
        if name not in written:png(path,frame(s['resource'],s['index'],s['frame'],True,True));written.add(name)
        objects.append(dict(index=i,x=s['x'],y=s['y'],texture=name))
    component=walkable(data,blocked);clear=component-covered(data)
    candidates=[p for p in clear if all((p[0]+dx,p[1]+dy) in component for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)])] or list(component)
    x,y=min(candidates,key=lambda p:((p[0]+.5-w/2)**2+(p[1]+.5-h/2)**2,p))
    result=dict(width=w,height=h,spawnX=x,spawnY=y,attributes=[t[3]&127 for t in data['tiles']],floorKeys=keys,objects=objects)
    text_asset(folder/'Map.json',result)
    return result,dict(sourceSha256=hashlib.sha256(native_map_path(id).read_bytes()).hexdigest(),cells=w*h,objects=len(objects),uniqueFloorTiles=len(tile_ids),reachableCells=len(component))
def scene(path,key,runtime):
    script=ROOT/'Assets/_Game/Runtime/World/NativeTownScene.cs.meta';script_guid=re.search(r'guid: (\w+)',script.read_text())[1]
    # Same small loader scene schema as existing additional town scenes.
    template=(ROOT/'Assets/_Game/Scenes/Maps/Belpueus_Map.unity').read_text()
    template=re.sub(r'm_Name: Belpueus native (?:town|region) loader','m_Name: '+key+' native region loader',template).replace('townKey: Belpueus','townKey: '+key).replace('runtimeOnly: 0','runtimeOnly: '+str(int(runtime)))
    assert 'guid: '+script_guid in template
    path.write_text(template);path.with_name(path.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
def register(scenes):
    import UnityPy
    path=ROOT/'ProjectSettings/EditorBuildSettings.asset';env=UnityPy.load(str(path));obj=next(o for o in env.objects if o.type.name=='EditorBuildSettings');data=obj.read_typetree()
    for scene in scenes:
        p=str(scene.relative_to(ROOT));g=re.search(r'guid: (\w+)',scene.with_name(scene.name+'.meta').read_text())[1];row=next((r for r in data['m_Scenes'] if r['path']==p),None)
        scene_guid={f'data[{i}]':int(g[i*8:i*8+8][::-1],16) for i in range(4)}
        if row:row['enabled']=True;row['guid']=scene_guid
        else:data['m_Scenes'].append(dict(enabled=True,path=p,guid=scene_guid))
    obj.save_typetree(data);path.write_bytes(env.file.save())
