"""Verify exact outputs or bounded Unity-import metadata evolution.
Never rewrite source assets or historical generation hashes.
"""
import hashlib,json,re,subprocess
from pathlib import Path
import yaml

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def retained(before,after):
 if isinstance(before,dict):return isinstance(after,dict)and all(k=='serializedVersion'or k in after and retained(v,after[k])for k,v in before.items())
 if isinstance(before,list):return isinstance(after,list)and len(before)==len(after)and all(retained(a,b)for a,b in zip(before,after))
 return before==after
def nonpolicy(text):
 text=re.sub(r'^  platformSettings:\n(?:(?:  -|    )[^\n]*\n)*','',text,flags=re.M)
 value=yaml.safe_load(re.sub(r'^(?:  (?:textureCompression|nPOTScale|maxTextureSize)|    (?:filterMode|enableMipMap)):[^\n]*\n','',text,flags=re.M))
 importer=value.get('TextureImporter',{})
 if 'mipmaps'in importer and importer['mipmaps']is None:importer['mipmaps']={} # Empty after removing the verified policy scalar.
 return value
def platform_payload(text):
 found=re.search(r'^  platformSettings:\n(?:(?:  -|    )[^\n]*\n)*',text,re.M);result={}
 if found:
  for block in re.split(r'(?=^  - serializedVersion:)',found[0].split('\n',1)[1],flags=re.M):
   name=re.search(r'^    buildTarget: (.+)$',block,re.M)
   if name:
    value=yaml.safe_load(re.sub(r'^    (?:maxTextureSize|resizeAlgorithm|textureFormat|textureCompression|crunchedCompression):[^\n]*\n','',block,flags=re.M))
    result[name[1]]=value[0]if isinstance(value,list)else value
 return result
def verify_output(root,row):
 path=root/row['asset'];actual=sha(path)
 if actual==row['sha256']:return None
 assert row['asset'].endswith('.png.meta')and path.resolve().is_relative_to((root/'Assets').resolve()),'Changed non-metadata output: '+row['asset']
 png=Path(str(path)[:-5]);policy_path=root/'Tools/OriginalWorldChecks/pixel-import-policy.json';check_path=root/'Tools/OriginalWorldChecks/pixel-import-check.json';policy=json.loads(policy_path.read_text());check=json.loads(check_path.read_text())
 assert check['mismatches']==0 and check['generationReportSha256']==sha(policy_path),'Current pixel policy verification required'
 entry=next((r for r in policy['textures']if r['asset']==png.relative_to(root).as_posix()),None)
 assert entry is not None and entry['sourceSha256']==sha(png)and entry['appliedImporterSha256']==actual,'PNG/current importer observation mismatch'
 assert check['sources'].get(row['asset'])==actual,'Pixel check does not cover current importer'
 before=None;revision=None
 for rev in ['HEAD','491e8535','a738e846','4aeebc1f','88587912']:
  p=subprocess.run(['git','show',rev+':'+row['asset']],cwd=root,capture_output=True)
  if p.returncode==0 and hashlib.sha256(p.stdout).hexdigest()==row['sha256']:before=p.stdout.decode().replace('\r\n','\n');revision=rev;break
 assert before is not None,'No independently verified historical metadata blob: '+row['asset']
 after=path.read_text();assert retained(nonpolicy(before),nonpolicy(after)),'Existing non-policy field changed: '+row['asset']
 old,new=platform_payload(before),platform_payload(after)
 assert all(k in new and retained(v,new[k])for k,v in old.items()),'Existing platform payload changed'
 assert all(k in old or k=='DefaultTexturePlatform'or new[k].get('overridden')==0 for k in new),'New active platform override'
 return dict(asset=row['asset'],historicalSha256=row['sha256'],currentSha256=actual,verifiedHistoricalCommit=revision,pngSha256=entry['sourceSha256'],scope='Existing metadata values retained; additive Unity fields/serialization versions and verified pixel-policy scalars allowed. Historical hash preserved; no Unity/GPU execution claim.')
