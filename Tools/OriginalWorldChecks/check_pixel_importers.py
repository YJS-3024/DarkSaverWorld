"""Check Unity platform metadata, lossless source pixels and Windows checkout policy.
The Editor importer is API-compiled separately; Unity asset import/GPU is not run.
"""
import hashlib,json,re,subprocess,sys,io,yaml
from pathlib import Path
from PIL import Image
from apply_pixel_import_policy import texture_paths
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'Tools/OriginalRestart'))
from pixel_texture_policy import normalize,lossless,platforms

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 checks=0
 def check(ok,label):
  nonlocal checks;checks+=1
  if not ok:raise AssertionError(label)
 report=json.loads((OUT/'pixel-import-policy.json').read_text());rows=report['textures'];check({ROOT/r['asset']for r in rows}==set(texture_paths()),'all manifest textures included')
 inputs=[Path(__file__),OUT/'apply_pixel_import_policy.py',ROOT/'Tools/OriginalRestart/pixel_texture_policy.py',ROOT/'Tools/OriginalRestart/reset_assets.py',ROOT/'.gitattributes',ROOT/'Assets/_Original/Editor/OriginalPixelTextureImporter.cs'];observed={};largest=0
 check(re.search(r'^guid: [a-f0-9]{32}$',(ROOT/'Assets/_Original/Editor/OriginalPixelTextureImporter.cs.meta').read_text(),re.M)is not None,'Editor importer script GUID survives harmless metadata formatting')
 for row in rows:
  image=ROOT/row['asset'];meta=Path(str(image)+'.meta');text=meta.read_text();size=Image.open(image).size;largest=max(largest,max(size))
  check(sha(image)==row['sourceSha256'],'unchanged native-derived PNG bytes '+row['asset']);check(re.search(r'^guid: ([a-f0-9]{32})$',text,re.M)[1]==row['guid'],'retained GUID '+row['asset'])
  check(lossless(text,*size),'effective point/uncompressed/full-size '+row['asset']);canonical=normalize(text);check(normalize(canonical)==canonical,'idempotent metadata normalization '+row['asset']);observed[str(meta.relative_to(ROOT))]=sha(meta)
 # Compare the captured pre-change metadata against the actual base-commit blobs.
 # Strip only policy scalars/blocks; all GUID, sprite, pivot, readable, alpha and
 # unknown metadata values must survive. Unity may add defaults or rewrite YAML
 # formatting/serialization versions during a legitimate Editor import.
 tracked_rows=[row for row in rows if row.get('beforeCommit',report['baseCommit'])];request=''.join(row.get('beforeCommit',report['baseCommit'])+':'+row['asset']+'.meta\n'for row in tracked_rows).encode();batch=io.BytesIO(subprocess.check_output(['git','cat-file','--batch'],input=request,cwd=ROOT));preserved=0
 def other_metadata(text):
  text=re.sub(r'^  platformSettings:\n(?:(?:  -|    )[^\n]*\n)*','',text,flags=re.M)
  return re.sub(r'^(?:  (?:textureCompression|nPOTScale|maxTextureSize)|    (?:filterMode|enableMipMap)):[^\n]*\n','',text,flags=re.M)
 def platform_payload(text):
  match=re.search(r'^  platformSettings:\n(?:(?:  -|    )[^\n]*\n)*',text,re.M);result={}
  if match:
   for block in re.split(r'(?=^  - serializedVersion:)',match[0].split('\n',1)[1],flags=re.M):
    target=re.search(r'^    buildTarget: (.+)$',block,re.M)
    if target:result[target[1]]=re.sub(r'^    (?:maxTextureSize|resizeAlgorithm|textureFormat|textureCompression|crunchedCompression):[^\n]*\n','',block,flags=re.M)
  return result
 def retained(before,after):
  if isinstance(before,dict):return isinstance(after,dict)and all(key=='serializedVersion'or key in after and retained(value,after[key])for key,value in before.items())
  if isinstance(before,list):return isinstance(after,list)and len(before)==len(after)and all(retained(a,b)for a,b in zip(before,after))
  return before==after
 for row in tracked_rows:
  header=batch.readline().decode().split();check(len(header)==3 and header[1]=='blob','pre-change metadata tracked');raw=batch.read(int(header[2]));check(batch.read(1)==b'\n','Git metadata framing');check(hashlib.sha256(raw).hexdigest()==row['beforeImporterSha256'],'pre-change observation matches base commit')
  before=raw.decode().replace('\r\n','\n');after=Path(str(ROOT/row['asset'])+'.meta').read_text();check(retained(yaml.safe_load(other_metadata(before)),yaml.safe_load(other_metadata(after))),'all non-policy metadata values preserved')
  old=platform_payload(before);new=platform_payload(after);check(all(key in new and retained(yaml.safe_load(value),yaml.safe_load(new[key]))for key,value in old.items())and all(key in old or key=='DefaultTexturePlatform'or re.search(r'^    overridden: 0$',new[key],re.M)for key in new),'existing platform identity/unknown fields preserved; added inactive defaults allowed');preserved+=1
 sample=(ROOT/'Assets/_Original/Art/World/CommanderRoster3.png.meta').read_text();check(lossless(sample,3200,32),'large original atlas survives default platform maximum')
 for key,value in [('textureCompression','1'),('maxTextureSize','2048'),('textureFormat','10'),('crunchedCompression','1')]:
  broken=re.sub(r'^(    '+key+r':) [^\n]+',lambda m:m[1]+' '+value,sample,flags=re.M);check(not lossless(broken,3200,32),'reject effective pixel loss: '+key)
 check(not lossless(re.sub(r'^(    textureFormat:) [^\n]+',r'\1 3',sample,flags=re.M),3200,32),'RGB24 cannot preserve native color-key alpha')
 check(lossless(re.sub(r'^(    textureFormat:) [^\n]+',r'\1 4',sample,flags=re.M),3200,32),'explicit uncompressed RGBA32 is lossless too')
 inactive=sample+'''  unusedTestField: ignored
''';check(lossless(inactive,3200,32),'harmless unknown serialized fields do not break provenance')
 inactive=sample.replace('  userData:', '''  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 1
    overridden: 0
  userData:''');check(lossless(inactive,3200,32),'inactive generated platform compression is not the effective policy');check(not lossless(inactive.replace('    overridden: 0\n  userData:','    overridden: 1\n  userData:'),3200,32),'active platform compression is rejected')
 check(normalize(inactive)==inactive,'Unity-created inactive defaults need no normalization')
 semantic=yaml.safe_load(other_metadata(sample));added=yaml.safe_load(other_metadata(sample+'  newlySerializedDefault: 0\n'));check(retained(semantic,added),'Unity additive metadata is not destructive')
 changed=yaml.safe_load(other_metadata(sample));changed['TextureImporter']['isReadable']=1-int(changed['TextureImporter']['isReadable']);check(not retained(semantic,changed),'readability changes are not hidden by formatting normalization')
 # Windows CRLF simulation runs Git's actual checkout filters, without touching index/files.
 cases=['Assets/_Original/Data/World/CommanderRoster3.json','Assets/_Original/Runtime/UI/NativeWindowPlacement.cs','Assets/_Original/SourceData/Native/map/1500.mrc.bytes','Assets/_Original/Data/Tables/items.csv'];checkout=[]
 for path in cases:
  raw=subprocess.check_output(['git','show','HEAD:'+path],cwd=ROOT);filtered=subprocess.check_output(['git','-c','core.autocrlf=true','cat-file','--filters','--path='+path,'HEAD:'+path],cwd=ROOT)
  check(raw==filtered,'Windows checkout stable bytes '+path);checkout.append(dict(path=path,stable=True))
 # Source of the editor import hook is audited; this is not execution of Unity callbacks.
 source=(ROOT/'Assets/_Original/Editor/OriginalPixelTextureImporter.cs').read_text();check('GetDefaultPlatformTextureSettings()'in source and 'TextureImporterCompression.Uncompressed'in source and 'if(!settings.overridden)continue;'in source,'actual Unity platform API adapter')
 result=dict(assertions=checks,textures=len(rows),nonPolicyMetadataPreserved=preserved,baseCommit=report['baseCommit'],largestSourceDimension=largest,unityImport=False,unityPlay=False,mismatches=0,windowsCheckout=checkout,scope='Literal PNG/GUID preservation, effective default/active platform compression/format/full source size, idempotence and Git CRLF checkout filters. Editor AssetPostprocessor is API-compiled only, not executed. Original bytes/tables are binary-preserved; no map/gateway/placement/player-save files are rewritten.',sources={**{str(p.relative_to(ROOT)):sha(p)for p in inputs},**observed},generationReportSha256=sha(OUT/'pixel-import-policy.json'))
 (OUT/'pixel-import-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
