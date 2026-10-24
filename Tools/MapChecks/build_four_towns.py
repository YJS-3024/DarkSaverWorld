"""Bake three native town layouts. Name/map bindings are authored, not recovered routes."""
import json
from world_asset_baker import ROOT,directory,bake,scene,register
TOWNS=[('Belpueus','벨퓌어스','1002'),('Sisirio','시시리오','1003'),('Castlelim','캐슬림','1004')]

def main():
    out=ROOT/'Assets/_Game/Resources/Towns';directory(out);scenes=[];manifest=[]
    for key,title,native in TOWNS:
        data,report=bake(native,out/key,{9})
        for runtime in [False,True]:
            path=ROOT/'Assets/_Game/Scenes'/('Runtime' if runtime else 'Maps')/(key+('_Runtime' if runtime else '_Map')+'.unity');scene(path,key,runtime);scenes.append(path)
        manifest.append(dict(key=key,title=title,nativeMap=native,width=data['width'],height=data['height'],sourceSha256=report['sourceSha256'],
            spawnSource=[data['spawnX'],data['spawnY']],spawnWorld=[data['spawnX']+.5,data['height']-1-data['spawnY']+.5],
            reachableCells=report['reachableCells'],sourceObjects=len(data['objects']),identityBinding='authored, original map-name identity unconfirmed',groundAnimation='native first-frame snapshot'))
    register(scenes)
    (ROOT/'Tools/MapChecks/four_towns_resources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    print('Baked three native town layouts using the common world resource converter.')
if __name__=='__main__':main()
