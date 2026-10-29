from saved_scene_geometry import read_scene_map
"""Source pixels, authored bindings, spawn reachability, scenes and build GUIDs.
Does not run the Unity loader or claim original town identity/routing recovery.
"""
import hashlib,json,re,sys
from pathlib import Path
from PIL import Image
import UnityPy,yaml
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_mrc,read_grs,grs_image
from world_asset_baker import native_map_path,load_map,resource
class UnityYaml(yaml.SafeLoader):pass
UnityYaml.add_multi_constructor('tag:unity3d.com,2011:',lambda loader,suffix,node:loader.construct_mapping(node,deep=True))

def main():
    manifest=json.loads((ROOT/'Tools/MapChecks/four_towns_resources.json').read_text())
    native=ROOT/'닥세_리소스/설치본_추출/Saver_Files/Saver/MAP'
    env=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'))
    build=next(o.read_typetree()['m_Scenes'] for o in env.objects if o.type.name=='EditorBuildSettings')
    resources={};results=[]
    def grs(id):
        return resource(id)
    script=(ROOT/'Assets/_Game/Runtime/World/NativeTownScene.cs.meta').read_text();script_guid=re.search(r'guid: (\w+)',script)[1]
    for row in manifest:
        key=row['key'];folder=ROOT/'Assets/_Game/Resources/Towns'/key;source=native_map_path(row['nativeMap'])
        assert hashlib.sha256(source.read_bytes()).hexdigest()==row['sourceSha256']
        original=load_map(row['nativeMap']);data=read_scene_map(folder);w,h=data['width'],data['height']
        assert (w,h)==(original['width'],original['height'])
        assert data['attributes']==[t[3]&127 for t in original['tiles']]
        assert len(data['objects'])==len(original['sprites'])
        for i,(actual,expected) in enumerate(zip(data['objects'],original['sprites'])):
            assert (actual['index'],actual['x'],actual['y'])==(i,expected['x'],expected['y'])
            assert actual['texture']==f"Sprites/{expected['resource']}_{expected['index']}_{expected['frame']}"
            g=grs(expected['resource'])['sprites'][expected['index']]
            expected_image=grs_image(g['palette'],g['images'][expected['frame']],True)
            assert Image.open(folder/(actual['texture']+'.png')).convert('RGBA').tobytes()==expected_image.tobytes()
        floor=Image.open(folder/'Ground.png').convert('RGBA');assert floor.size==(w*32,h*32)
        # Compare every native background/overlay cell, using decoder caches only.
        cache={}
        for i,(rid,group,tile) in enumerate(zip(original['a'],original['b'],original['tiles'])):
            marker=(rid,group,bytes(tile[:3]))
            if marker not in cache:
                g=grs(rid)['groups'][group];image=grs_image(g['palette'],g['images'][0])
                if tile[1]!=255:
                    g=grs(tile[0])['groups'][tile[1]];frame=g['tiles'][tile[2]]['frames'][0]
                    image.alpha_composite(grs_image(g['palette'],g['images'][frame],True))
                cache[marker]=image.tobytes()
            x,y=i%w,i//w
            assert floor.crop((x*32,y*32,(x+1)*32,(y+1)*32)).tobytes()==cache[marker]
        x,y=data['spawnX'],data['spawnY'];assert data['attributes'][y*w+x]!=9
        for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)]:assert data['attributes'][(y+dy)*w+x+dx]!=9
        for runtime in [False,True]:
            path='Assets/_Game/Scenes/'+('Runtime' if runtime else 'Maps')+'/'+key+('_Runtime' if runtime else '_Map')+'.unity'
            file=ROOT/path
            # Unity YAML scopes TAG directives to a document; expand tags before standard parsing.
            text=file.read_text().replace('%TAG !u! tag:unity3d.com,2011:\n','').replace('!u!','!<tag:unity3d.com,2011:')
            text=re.sub(r'!<tag:unity3d.com,2011:(\d+)',r'!<tag:unity3d.com,2011:\1>',text)
            docs=list(yaml.load_all(text,Loader=UnityYaml));component=next(d['MonoBehaviour'] for d in docs if 'MonoBehaviour' in d)
            assert component['m_Script']['guid']==script_guid and component['townKey']==key and bool(component['runtimeOnly'])==runtime
            entry=next(e for e in build if e['path']==path);assert entry['enabled']
            guid=re.search(r'guid: (\w+)',file.with_name(file.name+'.meta').read_text())[1]
            assert entry['guid']=={f'data[{i}]':int(guid[i*8:i*8+8][::-1],16) for i in range(4)}
        results.append(dict(key=key,nativeMap=row['nativeMap'],cells=w*h,objects=len(data['objects']),spawn=row['spawnWorld'],sourcePixels='exact',identityBinding='authored'))
    report=dict(towns=results,additional_map_scenes=3,additional_runtime_scenes=3,build_entries_checked=6,
        scope='Offline native pixels/geometry, walkable spawns, Unity YAML references and build GUIDs. Not Unity import/Play.',
        remaining=['Original map/name identities for three towns','Original field routes and NPC locations','Original non-Kaosia stock','Ground native animation: first-frame presentation only'])
    (ROOT/'Tools/MapChecks/four_towns_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(f"PASS {len(results)} native town layouts, {sum(r['objects'] for r in results)} decoration records, {sum(r['cells'] for r in results)} ground cells, six scenes/build GUIDs and walkable spawns.")
if __name__=='__main__':main()
