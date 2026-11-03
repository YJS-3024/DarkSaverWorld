"""Link original GetFace's face/face1/face2/face3 archive precedence and etc/noface.

Only portrait references are changed. Authored map, gateway, placement and script data
are not regenerated. Existing decoded64px images are reused without duplicate copies.
"""
import hashlib,json,re,sys
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT
sys.path.insert(0,str(ROOT/'Tools/OriginalRestart'))
from pixel_texture_policy import normalize
def face_sources():
 rows=[];seen=set();archives=[]
 for index in range(4):
  name='face'+(str(index)if index else '')+'.arc';folder=BASE/'Art/Archives/gameres'/name;paths=sorted((p for p in folder.glob('*.bmp.png')if p.name.split('.')[0].isdigit()),key=lambda p:int(p.name.split('.')[0]))
  archives.append(dict(archive=name,present=(BASE/'SourceData/Native/gameres'/(name+'.bytes')).exists(),entries=len(paths)))
  for path in paths:
   identifier=int(path.name.split('.')[0])
   if identifier in seen:continue
   seen.add(identifier);rows.append((identifier,name,path))
 return rows,archives,BASE/'Art/Archives/gameres/etc.arc/noface.bmp.png'
def build():
 rows,archives,fallback=face_sources();asset=BASE/'Data/Campaign/NativeCampaignArt.asset';text=asset.read_text();guid=lambda p:re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
 # Preserve the original RGB pixels after import too; Unity's default block
 # compression changes these64px UI portraits. Keep existing GUID/sprite data.
 for path in [p for _,_,p in rows]+[fallback]:
  meta=Path(str(path)+'.meta');contents=meta.read_text();contents=normalize(contents);meta.write_text(contents)
 for line in [r'^  faces:\n(?:  - .*\n)*',r'^  faceIds:.*\n',r'^  noFace:.*\n']:text=re.sub(line,'',text,flags=re.M)
 text+='  faces:\n'+''.join('  - {fileID: 2800000, guid: '+guid(path)+', type: 3}\n'for _,_,path in rows)
 text+='  faceIds: '+''.join(identifier.to_bytes(4,'little',signed=True).hex()for identifier,_,_ in rows)+'\n  noFace: {fileID: 2800000, guid: '+guid(fallback)+', type: 3}\n';asset.write_text(text)
 sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();key=asset.relative_to(ROOT).as_posix()
 for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
  p=Path(__file__).with_name(name);report=json.loads(p.read_text())
  if key in report.get('sources',{}):report['sources'][key]=sha(asset)
  for row in report.get('outputs',[]):
   if row['asset']==key:row['sha256']=sha(asset)
  p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 references=[]
 for identifier,archive,path in rows:
  assert Image.open(path).size==(64,64);references.append(dict(id=identifier,archive=archive,asset=path.relative_to(ROOT).as_posix(),sha256=sha(path),importerSha256=sha(Path(str(path)+'.meta')),guid=guid(path)))
 assert Image.open(fallback).size==(64,64)
 inputs=[BASE/'SourceData/Native/gameres'/(row['archive']+'.bytes')for row in archives if row['present']]+[BASE/'SourceData/Native/gameres/etc.arc.bytes']
 report=dict(nativeFile='UserUnit.dll',nativeSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',function='1000F180',archives=archives,faces=references,fallback=dict(asset=fallback.relative_to(ROOT).as_posix(),sha256=sha(fallback),importerSha256=sha(Path(str(fallback)+'.meta')),guid=guid(fallback)),sources={p.relative_to(ROOT).as_posix():sha(p)for p in inputs},outputs=[dict(asset=key,sha256=sha(asset))],unityPlay=False,scope='Original GetFace tries face.arc then face1/2/3.arc; only missing archive/entry across all4 selects etc.arc/noface.bmp. A declared texture/decode failure is not substituted. Negative presentation sentinel remains hidden; not an original raw negative-face-ID rule. Existing GUIDs and exact64px textures retained with point/uncompressed import.')
 Path(__file__).with_name('native-face-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');return report
if __name__=='__main__':
 r=build();print(json.dumps(dict(faces=len(r['faces']),archives=r['archives'],fallback=r['fallback']['asset'])))
