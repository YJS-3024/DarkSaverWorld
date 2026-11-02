"""Bind existing native town MRC/GRS files without rebuilding authored routes/NPCs."""
import hashlib,importlib.util,json,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_mrc
spec=importlib.util.spec_from_file_location('animation_world_builder',OUT/'build_world_assets.py');world=importlib.util.module_from_spec(spec);spec.loader.exec_module(world)
def build():
    rows=json.loads((BASE/'Data/World/KaosiaWorld.json').read_text())['maps'];maps=[];late={};old={};sources={}
    def source(path):sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest();return path
    for row in rows:
        folder=BASE/'SourceData'/('KaosiaLegacy'if row['sourceVersion']=='Saver_1.2'else 'Native/map')
        path=source(folder/(str(row['id'])+'.mrc.bytes'));maps.append(path)
        for key in read_mrc(path)['ids']:(old if row['sourceVersion']=='Saver_1.2'else late)[key]=source(folder/f'mdsr{key}.grs.bytes')
    fields={'nativeMapIds':[r['id']for r in rows],'nativeMaps':maps,'nativeGraphicIds':sorted(late),'nativeGraphics':[late[k]for k in sorted(late)],'legacyGraphicIds':sorted(old),'legacyGraphics':[old[k]for k in sorted(old)]}
    asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text()
    for field,values in fields.items():
        text=re.sub(r'^  '+field+r':[^\n]*\n(?:  - [^\n]*\n)*','',text,flags=re.M)
        if field.endswith('Ids'):text+='  '+field+': '+''.join(n.to_bytes(4,'little',signed=True).hex()for n in values)+'\n'
        else:text+='  '+field+':\n'+''.join('  - {fileID: 4900000, guid: '+world.guid(p)+', type: 3}\n'for p in values)
    asset.write_text(text)
    for name in ['Runtime/Maps/NativeMapAnimation.cs','Runtime/World/WorldSceneRoot.MapAnimation.cs']:
        path=BASE/name
        if not Path(str(path)+'.meta').exists():world.metadata.meta(path,ROOT/'Assets')
    for name in ['world-assets.json','campaign-assets.json','gateway-assets.json']:
        path=OUT/name;report=json.loads(path.read_text());key=asset.relative_to(ROOT).as_posix();digest=hashlib.sha256(asset.read_bytes()).hexdigest()
        if key in report.get('sources',{}):report['sources'][key]=digest
        for item in report.get('outputs',[]):
            if item['asset']==key:item['sha256']=digest
        path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    result=dict(maps=len(maps),nativeGraphics=len(late),legacyGraphics=len(old),sources=sources,outputs={asset.relative_to(ROOT).as_posix():hashlib.sha256(asset.read_bytes()).hexdigest()},operatingDefaultsGenerated=False)
    (OUT/'map-animation-assets.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','outputs']}))
if __name__=='__main__':build()
