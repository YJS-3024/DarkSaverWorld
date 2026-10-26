"""Check the serialized scenario02 bridge and saved source-event changes. Not Unity Play."""
from pathlib import Path
from collections import deque
import hashlib,json,re
import UnityPy
ROOT=Path(__file__).resolve().parents[2]

def main():
    checks=0
    def check(ok,message):
        nonlocal checks
        assert ok,message
        checks+=1
    base=ROOT/'Assets/_Game/Resources/Scenarios/02'
    content=json.loads((base/'Content.json').read_text())
    visuals=json.loads((base/'Visuals.json').read_text())
    scene=ROOT/'Assets/_Game/Scenes/Maps/Map_02_자모라 요새.unity'
    env=UnityPy.load(str(scene));file=env.file
    maps={};names=set()
    for obj in env.objects:
        if obj.type.name=='GameObject':names.add(obj.read_typetree()['m_Name'])
        if obj.type.name=='Tilemap':
            data=obj.read_typetree();name=file.objects[data['m_GameObject']['m_PathID']].read_typetree()['m_Name'];maps[name]=data
    tile_assets={}
    for path in (ROOT/'Assets/_Game/Art/Converted/RestoredMaps/Tiles').glob('Terrain_*.asset'):
        guid=re.search(r'guid: ([0-9a-f]+)',path.with_name(path.name+'.meta').read_text())[1]
        asset=UnityPy.load(str(path));data=next(o.read_typetree() for o in asset.objects if o.type.name=='MonoBehaviour')
        tile_assets[guid]=data['m_ColliderType']
    ground={(p['x'],content['height']-1-p['y']) for p,t in maps['Ground']['m_Tiles']}
    terrain={}
    data=maps['TerrainAttributes']
    for p,t in data['m_Tiles']:
        ref=data['m_TileAssetArray'][t['m_TileIndex']]['m_Data']
        raw=file.externals[ref['m_FileID']-1].guid.hex()
        guid=''.join(raw[i+1]+raw[i] for i in range(0,len(raw),2))
        terrain[p['x'],content['height']-1-p['y']]=tile_assets[guid]
    operations={o['line']:o for o in visuals['operations']}
    def apply(event,tiles):
        tiles=dict(tiles)
        for command in event['actions']:
            if command['name']!='CHANGESPRITE':continue
            op=operations[command['line']]
            check(op['sourceIndex']>=0 and 'Object_'+str(op['sourceIndex']).zfill(3) in names,'native replacement target exists')
            check((base/(op['texture']+'.png')).exists(),'replacement artwork exists')
            for x in range(op['x'],op['x']+op['widthTiles']):
                for y in range(op['y'],op['y']+op['heightTiles']):tiles[x,y]=2 if op['attribute'] in (3,6,9) else 0
        return tiles
    def reachable(tiles,start):
        seen={start};queue=deque(seen)
        while queue:
            x,y=queue.popleft()
            for point in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
                if point in ground and tiles.get(point,0)==0 and point not in seen:seen.add(point);queue.append(point)
        return seen
    first=next(e for e in content['events'] if e['id']==50)
    second=next(e for e in content['events'] if e['id']==51)
    check([(c['args']) for c in second['conditions'] if c['name']=='CHARPOS']==[['35','19'],['35','20'],['35','21']],'source east-bank predicates preserved')
    before=apply(first,terrain)
    check(all(before.get((34,y),0)==2 for y in (19,20,21)),'serialized attribute24 blocks second bridge')
    check(all((35,y) not in reachable(before,(33,20)) for y in (19,20,21)),'reported isolation reproduced from serialized scene')
    after=apply(second,before)
    for y in (19,20,21):
        check(all((x,y) in ground and after.get((x,y),0)==0 for x in (33,34,35)),'all bridge rows become walkable')
        check((35,y) in reachable(after,(33,20)),'west bank reaches original east predicate after replacement')
    check(apply(second,after)==after,'restored map changes are idempotent')
    report=dict(checks=checks,passed=True,scope='serialized scene and original map-change footprints; no Unity physics/Play',
        sourceHash=content['sourceHash'],sceneSha256=hashlib.sha256(scene.read_bytes()).hexdigest(),
        sourcePredicateX=35,localAdditionalProbeX=33,prerequisiteEvent=50,bomb=398,
        unityPlayExecuted=False)
    (ROOT/'Tools/MapChecks/scenario02_bridge_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PASS',checks,'serialized bridge checks; NOT Unity Play')

if __name__=='__main__':main()
