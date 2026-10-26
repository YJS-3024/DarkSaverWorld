from pathlib import Path
import importlib.util,struct,hashlib,json,sys
# Usage from repository root: python 닥세_리소스/분석도구/analyze_added_saver.py <unshield-output> <report-dir>
if len(sys.argv)!=3: raise SystemExit("Expected extraction root and report directory")
extracted=Path(sys.argv[1]); output=Path(sys.argv[2]);output.mkdir(parents=True,exist_ok=True)
sp=importlib.util.spec_from_file_location('decoder','닥세_리소스/분석도구/analyze_client.py');m=importlib.util.module_from_spec(sp);sp.loader.exec_module(m)
r=extracted/'Saver_Files/saver';old=Path('닥세_리소스/설치본_추출/Saver_Files/Saver');oi={str(p.relative_to(old)).lower():p for p in old.rglob('*') if p.is_file()}
def entries(p):
 b=p.read_bytes();i=b[0]+1;out={}
 while i+36<=len(b):
  rec=b[i:i+36];i+=36;n=rec[1:1+rec[0]].decode('cp949')
  if n=='HEADEND':break
  sz,packed,st,en=struct.unpack_from('<4I',rec,20);out[n.lower()]=(rec[15],sz,b[st:en+1])
 return out
report=[]
for p in r.rglob('*.arc'):
 key=str(p.relative_to(r)).lower();q=oi.get(key)
 if q and p.read_bytes()==q.read_bytes():continue
 a=entries(p);b=entries(q) if q else {};changes=[]
 for n,(method,sz,data) in a.items():
  if n in b and a[n]==b[n]:continue
  row={'entry':n,'status':'added' if n not in b else 'changed','size':sz}
  if p.name.lower()=='set.arc' or p.parent.name.lower()=='map':
   
   try:d=(m.lzh_decode if method==0 else m.lzss_decode)(data,sz);assert len(d)==sz
   except Exception as ex:
    row['decodeError']=str(ex);row['method']=method;changes.append(row);continue
   dest=output/'decoded'/key/n;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(d)
   row['sha256']=hashlib.sha256(d).hexdigest()
   if n in b:
    om,os,od=b[n];before=(m.lzh_decode if om==0 else m.lzss_decode)(od,os)
    if before==d:row['status']='repacked-identical'
    else:
     dest.with_suffix(dest.suffix+'.old').write_bytes(before)
  changes.append(row)
 report.append({'archive':key,'entries':len(a),'differences':changes,'removed':sorted(b.keys()-a.keys())})
 print(key,len(a),'differences',len(changes),flush=True)
(output/'archives.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))

# Compare actual installed game files; omit prior derived ARC extractions.
def index(root): return {str(p.relative_to(root)).lower():p for p in root.rglob('*') if p.is_file()}
a,b=index(r),index(old)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
files=[]
for key,p in sorted(a.items()):
 q=b.get(key);digest=sha(p)
 files.append(dict(path=key,size=p.stat().st_size,sha256=digest,status='added' if q is None else 'same' if digest==sha(q) else 'changed'))
(output/'files.json').write_text(json.dumps(files,ensure_ascii=False,indent=2)+'\n')
tables=[]
for p in sorted((output/'decoded/gameres/set.arc').glob('*.atr')):
 def rows(path):
  if not path.exists(): return {}
  data={}
  for line in path.read_bytes().decode('cp949',errors='replace').splitlines():
   tokens=line.split()
   if tokens and tokens[0].lstrip('-').isdigit():data[':'.join(tokens[:2]) if p.name=='levelabl.atr' else tokens[0]]=tokens
  return data
 x,y=rows(p),rows(p.with_suffix(p.suffix+'.old'))
 if not y: continue
 added=sorted(x.keys()-y.keys(),key=lambda k:tuple(map(int,k.split(":"))));removed=sorted(y.keys()-x.keys(),key=lambda k:tuple(map(int,k.split(":"))))
 changed=[dict(id=k,fields=[dict(column=i,before=old,after=new) for i,(old,new) in enumerate(zip(y[k],x[k])) if old!=new],before=y[k],after=x[k]) for k in sorted(x.keys()&y.keys(),key=lambda k:tuple(map(int,k.split(":")))) if x[k]!=y[k]]
 tables.append(dict(table=p.name,newRows=len(x),oldRows=len(y),added=[x[k] for k in added],removed=removed,changed=changed))
(output/'tables.json').write_text(json.dumps(tables,ensure_ascii=False,indent=2)+'\n')
