"""Version-specific script/table/art comparison from extracted original installers."""
import collections,hashlib,importlib.util,json,re,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'분석/InstallerVersions'
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def main():
 c=load('catalog',ROOT/'닥세_리소스/분석도구/build_late_saver_catalog.py');d=load('decoder',ROOT/'닥세_리소스/분석도구/analyze_client.py');versions=json.loads((OUT/'installer_inventory.json').read_text())['payloads'];rows=[]
 # Reuse equal archive bytes across versions, retaining version-scoped entry records.
 cache={}
 for version in versions:
  entries=[];errors=[];pictures=[]
  for f in version['files']:
   key=f['file'];istext=key.lower().endswith('.arc') and (key.startswith('map/') or key in ['gameres/set.arc','gameres/tutorial.arc']);isart=key in ['gameres/char0.arc','gameres/char1.arc','gameres/char2.arc','gameres/char3.arc','gameres/char4.arc']
   if not istext and not isart:continue
   if f['sha256'] in cache:
    cached=cache[f['sha256']]
    # Equal bytes can live at different paths (gameres/tutorial.arc and map/tutorial.arc).
    entries.extend(dict(r,archive=key) for r in cached[0])
    errors.extend(dict(r,archive=key) for r in cached[1])
    pictures.extend(dict(r,archive=key) for r in cached[2]);continue
   local=[];bad=[];art=[]
   try:
    for name,method,size,packed in c.arc_entries(Path(f['extractedFile'])):
     suffix=Path(name).suffix.lower();image=isart and name.lower() in ['301r.bmp','401r.bmp','305r.bmp','701r.bmp']
     if not image and (not istext or suffix not in ['.deo','.dee','.evt','.ai','.set','.pos','.atr','.txt','.srf','.at','.a']):continue
     try:
      raw=(d.lzh_decode if method==0 else d.lzss_decode)(packed,size);assert len(raw)==size
      digest=hashlib.sha256(raw).hexdigest();record={'archive':key,'entry':name,'bytes':size,'sha256':digest}
      if image:
       assert raw[:2]==b'BM';record.update(width=struct.unpack_from('<i',raw,18)[0],height=struct.unpack_from('<i',raw,22)[0]);art.append(record);continue
      text=raw.decode('cp949');lines=text.splitlines();record['lines']=len(lines)
      if suffix=='.deo':record['commands']=sum(bool(t.strip()) and not t.lstrip().startswith(';') for t in lines)
      local.append(record)
      # Content-addressed snapshots avoid writing identical source texts repeatedly.
      if 'tutorial' in key or key.endswith(('/01set.arc','/302set.arc')) or suffix in ['.set','.pos'] or key=='gameres/set.arc':
       target=OUT/'decoded'/digest[:16]/name.lower();target.parent.mkdir(parents=True,exist_ok=True)
       if not target.exists():target.write_text(text)
       record['decoded']=str(target.relative_to(ROOT))
     except Exception as e:bad.append({'archive':key,'entry':name,'error':type(e).__name__+': '+str(e)})
   except Exception as e:bad.append({'archive':key,'error':str(e)})
   cache[f['sha256']]=(local,bad,art);entries.extend(local);errors.extend(bad);pictures.extend(art)
  tutorial=[r for r in entries if r['entry'].lower()=='tutorial.deo'];rosters=[r for r in entries if Path(r['entry']).suffix.lower() in ['.set','.pos']]
  rows.append({'containers':version['containers'],'payloadId':version['payloadId'],'scriptsAndTables':entries,'decodeErrors':errors,'tutorials':tutorial,'nativeRosterFiles':rosters,'brugosArt':pictures})
 (OUT/'payload_data_comparison.json').write_text(json.dumps({'scope':'Original installed tables/scripts and four wolf-family artwork IDs; partial patches are not full clients','versions':rows},ensure_ascii=False,indent=2)+'\n')
 for v in rows:print(v['containers'],'text',len(v['scriptsAndTables']),'tutorial hashes',sorted({t['sha256'][:12] for t in v['tutorials']}),'SET/POS',len(v['nativeRosterFiles']),'errors',len(v['decodeErrors']))
if __name__=='__main__':main()
