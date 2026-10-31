import importlib.util,json,hashlib,argparse
from pathlib import Path
def mod(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
parser=argparse.ArgumentParser();parser.add_argument('native_root',type=Path);args=parser.parse_args()
b=mod('catalog','닥세_리소스/분석도구/build_late_saver_catalog.py');d=mod('decoder','닥세_리소스/분석도구/analyze_client.py');rows=[]
for category in ['gameres','MAP']:
 p=args.native_root/category/'tutorial.arc'
 for name,method,size,packed in b.arc_entries(p):
  raw=(d.lzh_decode if method==0 else d.lzss_decode)(packed,size);assert len(raw)==size
  out=Path('분석/Saver_Late/decoded')/category.lower()/'tutorial.arc'/name.lower();out.parent.mkdir(parents=True,exist_ok=True)
  if out.exists():assert out.read_text().replace('\r','')==raw.decode('cp949').replace('\r','')
  else:out.write_text(raw.decode('cp949'))
  rows.append(dict(archive=category.lower()+'/tutorial.arc',name=name,size=size,sha256=hashlib.sha256(raw).hexdigest(),output=str(out)))
Path('분석/Saver_Late/tutorial_archive_inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n');print(rows)
