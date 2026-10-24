"""Authored walkable scenario gates; no original overworld entrance claim."""
import json,re
from collections import deque
from pathlib import Path
from world_asset_baker import ROOT,load_map,walkable,covered,text_asset

def choose(component,clear,anchor,reserved,creatures):
    possible=[p for p in clear if all((p[0]-q[0])**2+(p[1]-q[1])**2>=9 for q in reserved) and all((p[0]-q[0])**2+(p[1]-q[1])**2>=4 for q in creatures)]
    for portal in sorted(possible,key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p)):
        distance={portal:0};queue=deque([portal]);arrivals=[]
        while queue:
            point=queue.popleft();depth=distance[point]
            if depth>=3 and (point[0]-portal[0])**2+(point[1]-portal[1])**2>=10 and point in clear and all((point[0]-q[0])**2+(point[1]-q[1])**2>=9 for q in reserved) and all((point[0]-q[0])**2+(point[1]-q[1])**2>=4 for q in creatures):arrivals.append(point)
            if depth>=7:continue
            for next in [(point[0]-1,point[1]),(point[0]+1,point[1]),(point[0],point[1]-1),(point[0],point[1]+1)]:
                if next in component and next not in distance:distance[next]=depth+1;queue.append(next)
        if arrivals:return portal,min(arrivals,key=lambda p:((p[0]-anchor[0])**2+(p[1]-anchor[1])**2,p))
    raise ValueError('No safe gate/return pair')

def main():
    fields=re.findall(r'Field\("(\d+)","([^"]+)",(\d+),(\d+)\)',(ROOT/'Assets/_Game/Runtime/Core/FieldCatalog.cs').read_text());world=json.loads((ROOT/'Assets/_Game/Resources/World/Routes.json').read_text());entries=[]
    for index in range(17):
        key=f'{index+1:02}';id,title,width,height=fields[index];field_key='WorldField'+id;w,h=int(width),int(height)
        native=load_map(id);component=walkable(native,{3,6,9});clear=component-covered(native)
        reserved=[(int(r['portalX']),h-1-int(r['portalY'])) for r in world['routes'] if r['fromKey']==field_key]+[(int(r['spawnX']),h-1-int(r['spawnY'])) for r in world['routes'] if r['fromKey']==field_key]
        creatures=[] if id=='1010' else [(e['x'],e['y']) for e in json.loads((ROOT/'Assets/_Game/Resources/Fields'/field_key/'Map.json').read_text())['encounters']]
        try:portal,arrival=choose(component,clear,(w/2,h/2),reserved,creatures)
        except ValueError as error:raise ValueError((key,field_key,len(component),len(clear),len(reserved))) from error
        data=json.loads((ROOT/'Assets/_Game/Resources/Scenarios'/key/'Content.json').read_text());sw,sh=data['width'],data['height'];entry=(data['entryX'],data['entryY']);native_scenario=load_map(key)
        sc={(v%sw,v//sw) for v in data['traversalCells']};reserved_units={(u['x'],u['y']) for u in data['units']};reserved_events={(int(c['args'][0]),int(c['args'][1])) for e in data['events'] for c in e['conditions'] if c['name'] in ['CHARPOS','CHARPOS3']}
        candidates=[p for p in sc-covered(native_scenario)-reserved_units-reserved_events if (p[0]-entry[0])**2+(p[1]-entry[1])**2>=16]
        assert candidates,key
        back=min(candidates,key=lambda p:((p[0]-entry[0])**2+(p[1]-entry[1])**2,p))
        entries.append(dict(key=key,fieldKey=field_key,portalX=portal[0]+.5,portalY=h-1-portal[1]+.5,spawnX=arrival[0]+.5,spawnY=h-1-arrival[1]+.5,
            entryX=entry[0]+.5,entryY=sh-1-entry[1]+.5,returnX=back[0]+.5,returnY=sh-1-back[1]+.5))
    text_asset(ROOT/'Assets/_Game/Resources/Scenarios/Shared/Entrances.json',dict(version=1,provenance='authored gates over native collision components, unlock after previous clear',entries=entries))
    print('Baked17 walkable scenario entrances and return pairs.')
if __name__=='__main__':main()
