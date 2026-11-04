"""Register original training102..107 geometry and native resource references without spawn/economy defaults."""
import json,re,sys,copy,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'));from decode_visuals import read_mrc
def main():
    base=ROOT/'Assets/_Original';p=base/'Data/World/KaosiaWorld.json';original_world_hash=hashlib.sha256(p.read_bytes()).hexdigest();d=json.loads(p.read_text());template=next(m for m in d['maps']if m['id']==101)
    asset=base/'Data/World/NativeWorldArt.asset';text=asset.read_text();original_art_hash=hashlib.sha256(asset.read_bytes()).hexdigest();ids=set();new=[]
    for i in range(102,108):
     raw=base/f'SourceData/Native/map/{i}.mrc.bytes';m=read_mrc(raw);ids.update(m['ids']);r=copy.deepcopy(template);r.update(id=i,title=f'수련장 {i-100}단',nativeKey=str(i),decorations=[],portals=[],npcs=[])
     r['map'].update(width=m['width'],height=m['height'],attributes=[0 if (t[3]&127)>27 else t[3]&127 for t in m['tiles']],renderAttributes=[t[3]for t in m['tiles']])
     if not any(x['id']==i for x in d['maps']):d['maps'].append(r)
     new.append((i,raw))
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
    def guid(p):return re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
    for idfield,arrayfield,items in [('nativeMapIds','nativeMaps',new),('nativeGraphicIds','nativeGraphics',[(i,base/f'SourceData/Native/map/mdsr{i}.grs.bytes')for i in sorted(ids)])]:
     match=re.search(r'^  '+idfield+r': ([0-9a-f]+)$',text,re.M);v=bytes.fromhex(match[1]);known=[int.from_bytes(v[j:j+4],'little')for j in range(0,len(v),4)]
     added=[(i,p)for i,p in items if i not in known];known+=[i for i,p in added];text=text[:match.start(1)]+b''.join(i.to_bytes(4,'little')for i in known).hex()+text[match.end(1):]
     match=re.search(r'^  '+arrayfield+r':\n(?:  - .*\n)*',text,re.M);text=text[:match.end()]+''.join('  - {fileID: 4900000, guid: '+guid(p)+', type: 3}\n'for i,p in added)+text[match.end():]
    asset.write_text(text);print('Registered training maps102..107, original graphics',sorted(ids))

    sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
    outputs={str(p.relative_to(ROOT)):sha(p),str(asset.relative_to(ROOT)):sha(asset)}
    sources=[Path(__file__),*[file for i,file in new],*[base/f'SourceData/Native/map/mdsr{i}.grs.bytes'for i in sorted(ids)]]
    report=dict(trainingMaps=list(range(101,108)),maps=len(d['maps']),sources={str(file.relative_to(ROOT)):sha(file)for file in sources},before={str(p.relative_to(ROOT)):original_world_hash,str(asset.relative_to(ROOT)):original_art_hash},outputs=outputs,unityPlay=False,policy='Original geometry/resource registration only. No monster roster, HP/AP/economy or authored gateway edits.')
    report_path=Path(__file__).with_name('training-map-registration.json');report_path.write_text(json.dumps(report,indent=2)+'\n')
    # These generation manifests consume the transformed world/art. Record this actual
    # postprocess rather than leaving their old base-generation hashes as current.
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
     manifest=Path(__file__).with_name(name)
     if not manifest.exists():continue
     data=json.loads(manifest.read_text())
     for key,digest in outputs.items():
      if key in data.get('sources',{}):data['sources'][key]=digest
      for row in data.get('outputs',[]):
       if row['asset']==key:row['sha256']=digest
     data['trainingGeometryPostprocess']=str(report_path.relative_to(ROOT))
     if name=='world-assets.json':data['maps']=len(d['maps'])
     manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')

if __name__=='__main__':main()
