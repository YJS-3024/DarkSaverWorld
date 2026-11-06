"""Fix generated/imported pixel metadata only; never rebuild maps or source textures."""
import hashlib,json,re,sys,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'Tools/OriginalRestart'))
from pixel_texture_policy import normalize

def texture_paths():
 paths=set()
 for directory in ['Tools/OriginalEntryChecks','Tools/OriginalTutorialChecks','Tools/OriginalWorldChecks']:
  for manifest in list((ROOT/directory).glob('*assets.json'))+list((ROOT/directory).glob('*-art.json')):
   report=json.loads(manifest.read_text());rows=report.get('outputs',[])
   if not isinstance(rows,list):continue
   # Battle windows reuse hmaps/cursor images that are inputs, not generated outputs.
   if manifest.name=='native-battle-window-assets.json':
    paths.update(ROOT/name for name in report.get('sources',{})if name.endswith('.png')and(ROOT/name).is_relative_to(BASE)and(ROOT/name).is_file())
   for row in rows:
    if not isinstance(row,dict):continue
    path=ROOT/row.get('asset','')
    if path.suffix=='.png'and path.is_relative_to(BASE)and path.is_file():paths.add(path)
 faces=json.loads((OUT/'native-face-assets.json').read_text())
 paths.update(ROOT/row['asset']for row in faces['faces']+[faces['fallback']]);return sorted(paths)

def apply():
 sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();rows=[];changed=0
 previous_path=OUT/'pixel-import-policy.json';previous=json.loads(previous_path.read_text())if previous_path.exists()else{};prior={r['asset']:r for r in previous.get('textures',[])};head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
 for texture in texture_paths():
  meta=Path(str(texture)+'.meta');before=meta.read_text();guid=re.search(r'^guid: ([a-f0-9]{32})$',before,re.M)[1];after=normalize(before)
  assert normalize(after)==after,'Metadata normalization must be idempotent';assert re.search(r'^guid: ([a-f0-9]{32})$',after,re.M)[1]==guid
  old=sha(meta);asset=texture.relative_to(ROOT).as_posix();image_sha=sha(texture);earlier=prior.get(asset)
  if before!=after:meta.write_text(after);changed+=1
  if before==after and earlier and earlier['appliedImporterSha256']==old and earlier['guid']==guid and earlier['sourceSha256']==image_sha:
   row=dict(earlier);row.setdefault('beforeCommit',previous.get('baseCommit',head))
  else:
   tracked=subprocess.run(['git','cat-file','-e',head+':'+asset+'.meta'],cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
   row=dict(asset=asset,sourceSha256=image_sha,guid=guid,beforeImporterSha256=old,appliedImporterSha256=sha(meta),changed=before!=after,beforeCommit=head if tracked else None)
  rows.append(row)
 # These are generation observations, not a rewritten verification result.
 manifest=OUT/'native-face-assets.json';faces=json.loads(manifest.read_text())
 for row in faces['faces']+[faces['fallback']]:row['importerSha256']=sha(Path(str(ROOT/row['asset'])+'.meta'))
 manifest.write_text(json.dumps(faces,ensure_ascii=False,indent=2)+'\n')
 report=dict(baseCommit=previous.get('baseCommit',head),changedThisRun=changed,scope='Existing generated PNG metadata only, preserving PNG bytes/GUID/sprite payload and all non-policy metadata. Default and explicit platform settings retain point/uncompressed/full-size source pixels. No Unity import is performed.',unityImport=False,textures=rows,sources={str(p.relative_to(ROOT)):sha(p)for p in [Path(__file__),ROOT/'Tools/OriginalRestart/pixel_texture_policy.py',ROOT/'Tools/OriginalRestart/reset_assets.py',ROOT/'Assets/_Original/Editor/OriginalPixelTextureImporter.cs']})
 (OUT/'pixel-import-policy.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(textures=len(rows),changed=changed,unityImport=False)))
if __name__=='__main__':apply()
