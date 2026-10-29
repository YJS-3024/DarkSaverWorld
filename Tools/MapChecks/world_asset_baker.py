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
    if path.name.lower()=="map.json":raise RuntimeError("Runtime geometry JSON is retired; save the map scene instead.")
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
def bake(*args,**kwargs):
    raise RuntimeError("Runtime geometry generation is retired. Author saved scene tiles; validate with check_scene_tiles.py.")
def scene(path,key,runtime):
    if path.exists():return  # Never replace authored maps or NPC/runtime scene edits.
    raise RuntimeError("Create a saved Unity map/runtime scene in the editor; loader-only scene generation is retired.")
def register(scenes):
    import UnityPy
    path=ROOT/'ProjectSettings/EditorBuildSettings.asset';env=UnityPy.load(str(path));obj=next(o for o in env.objects if o.type.name=='EditorBuildSettings');data=obj.read_typetree()
    for scene in scenes:
        p=str(scene.relative_to(ROOT));g=re.search(r'guid: (\w+)',scene.with_name(scene.name+'.meta').read_text())[1];row=next((r for r in data['m_Scenes'] if r['path']==p),None)
        scene_guid={f'data[{i}]':int(g[i*8:i*8+8][::-1],16) for i in range(4)}
        if row:row['enabled']=True;row['guid']=scene_guid
        else:data['m_Scenes'].append(dict(enabled=True,path=p,guid=scene_guid))
    obj.save_typetree(data);path.write_bytes(env.file.save())
