"""Convert all reviewed native interiors; keep Kaosia aliases for existing saves."""
import json,re,hashlib
from PIL import Image
from world_asset_baker import ROOT,directory,bake,scene,register,guid,png,text_asset,load_map,walkable

def main():
    audit=json.loads((ROOT/'닥세_리소스/분석결과/상점_실내맵_분석.json').read_text())
    out=ROOT/'Assets/_Game/Resources/Interiors';directory(out);rows=[];scenes=[]
    bootstrap_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/_Game/Runtime/World/AdditiveSceneBootstrap.cs.meta').read_text())[1]
    template=(ROOT/'Assets/_Game/Scenes/Maps/Belpueus_Map.unity').read_text()
    for row in audit['rows']:
        key=row['key'];data,proof=bake(key,out/key,{9})
        region='Interior'+key
        paths=[]
        for runtime in [False,True]:
            path=ROOT/'Assets/_Game/Scenes'/('Runtime' if runtime else 'Maps')/(region+('_Runtime' if runtime else '_Map')+'.unity')
            scene(path,region,runtime);paths.append(str(path.relative_to(ROOT)));scenes.append(path)
        entry=ROOT/'Assets/_Game/Scenes/Entry'/(region+'_Play.unity')
        text=re.sub(r'm_Script: .*',f'm_Script: {{fileID: 11500000, guid: {bootstrap_guid}, type: 3}}',template)
        text=text.replace('DarkSaver.World.NativeTownScene','DarkSaver.World.AdditiveSceneBootstrap').replace('m_Name: Belpueus native region loader','m_Name: '+region+' entry')
        text=re.sub(r'  townKey: Belpueus\n  runtimeOnly: 0',f'  coreScene: GameCore\n  mapScene: {region}_Map\n  runtimeScene: {region}_Runtime\n  useEntryPosition: 1\n  entryPosition: {{x: {data["spawnX"]+.5}, y: {data["height"]-data["spawnY"]-.5}}}',text)
        entry.write_text(text);entry.with_name(entry.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(entry)+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');scenes.append(entry)
        rows.append(dict(playScene=str(entry.relative_to(ROOT)),key=region,nativeMap=key,title=row['visualIdentification'],width=data['width'],height=data['height'],spawnNative=[data['spawnX'],data['spawnY']],scenes=paths,**proof))
    staff=[('2000','무기점 주인',809),('2100','방어구점 주인',810),('2200','장신구점 주인',805),('2300','마법도구점 주인',816),('2400','은행 직원',807),('2510','창고 직원',827),('2600','신전 관리인',817)]
    directory(out/'Npcs');npc_rows=[]
    source=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres'
    for native,name,appearance in staff:
        path=next(p for p in source.glob('char*/*') if p.name.lower()==f'{appearance}r.bmp')
        sheet=Image.open(path).convert('RGBA');size=sheet.width//3
        image=sheet.crop((0,0,size,size));image.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in image.getdata()])
        png(out/'Npcs'/(str(appearance)+'.png'),image)
        map_path=out/native/'Map.json';data=json.loads(map_path.read_text());m=load_map(native)
        x,y=min(walkable(m,{9}),key=lambda p:((p[0]+.5-m['width']/2)**2+(p[1]+.5-m['height']*.45)**2,p))
        data['npc']=dict(name=name,texture=str(appearance),appearanceId=appearance,x=x,y=y);text_asset(map_path,data)
        npc_rows.append(dict(nativeMap=native,**data['npc'],source=str(path.relative_to(ROOT)),sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),placementPolicy='authored one staff member per connected interior; original identity/placement not claimed'))
    script_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/_Game/Runtime/World/ShopInteriorScene.cs.meta').read_text())[1]
    template=(ROOT/'Assets/_Game/Scenes/Maps/Belpueus_Map.unity').read_text()
    for key in ['KaosiaWeaponShop','KaosiaArmorShop']:
        for runtime in [False,True]:
            path=ROOT/'Assets/_Game/Scenes'/('Runtime' if runtime else 'Maps')/(key+('_Runtime' if runtime else '_Map')+'.unity')
            text=template.replace('m_Name: Belpueus native region loader','m_Name: '+key+' native interior loader')
            text=re.sub(r'm_Script: .*',f'm_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}',text)
            text=text.replace('DarkSaver.World.NativeTownScene','DarkSaver.World.ShopInteriorScene').replace('townKey: Belpueus','shopKey: '+key).replace('runtimeOnly: 0','runtimeOnly: '+str(int(runtime)))
            path.write_text(text);path.with_name(path.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');scenes.append(path)
    facilities=[]
    catalog=(ROOT/'Assets/_Game/Runtime/Core/KaosiaFacilityCatalog.cs').read_text()
    pattern=r'new KaosiaFacilityDefinition\("([^"]+)","([^"]+)","(\d+)",(\d+),(\d+),(\d+),([\d.]+)f,([\d.]+)f,([\d.]+)f,([\d.]+)f\)'
    for key,title,native,height,index,group,dx,dy,rx,ry in re.findall(pattern,catalog):
        paths=[]
        for runtime in (False,True):
            path=ROOT/'Assets/_Game/Scenes'/('Runtime' if runtime else 'Maps')/(key+('_Runtime' if runtime else '_Map')+'.unity')
            scene(path,key,runtime);scenes.append(path);paths.append(str(path.relative_to(ROOT)))
        facilities.append(dict(key=key,title=title,nativeMap=native,height=int(height),buildingIndex=int(index),buildingGroup=int(group),door=[float(dx),float(dy)],returnPoint=[float(rx),float(ry)],scenes=paths,association='authored single-player routing; native building and interior artwork'))
    assert len(facilities)==5
    register(scenes)
    report=dict(authoredIndoorNpcs=npc_rows,kaosiaFacilities=facilities,nativeInteriorMaps=rows,scenes=[str(p.relative_to(ROOT)) for p in scenes],nativeGeometryAndArtwork=True,kaosiaBindings={'KaosiaWeaponShop':'2000','KaosiaArmorShop':'2100'},bindingPolicy='Authored town association; original interior geometry/artwork. Five more facilities connected by authored policy; remaining interiors have no invented town entrances.',merchantNpcAdded=True,unityPlay=False)
    (ROOT/'Tools/MapChecks/shop_interiors_resources.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(f'Built {len(rows)} native interiors, {len(scenes)} registered scenes including two existing Kaosia aliases.')
if __name__=='__main__':main()
