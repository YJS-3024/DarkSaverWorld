"""Read-only audit of installer containers and InstallShield payloads; never run EXEs."""
import argparse,collections,datetime,hashlib,json,struct,subprocess,zipfile
from pathlib import Path
from read_split_egg import extract as extract_egg
ROOT=Path(__file__).resolve().parents[2]
def sha(b):return hashlib.sha256(b).hexdigest()
def pe_date(b):
 try:
  at=struct.unpack_from('<I',b,60)[0]
  if b[at:at+4]!=b'PE\0\0':return None
  stamp=struct.unpack_from('<I',b,at+8)[0];return datetime.datetime.fromtimestamp(stamp,datetime.timezone.utc).isoformat()
 except (ValueError,struct.error,OverflowError):return None

def main():
 p=argparse.ArgumentParser();p.add_argument('--work',type=Path,default=Path('/tmp/ds-installers'));p.add_argument('--unshield',default='/tmp/ds-unshield');a=p.parse_args();a.work.mkdir(exist_ok=True)
 sources=ROOT/'닥세_리소스/설치파일';files=sorted(x for x in sources.iterdir() if x.is_file());containers=[];payloads={};duplicates=collections.defaultdict(list)
 for path in files:
  raw=path.read_bytes();digest=sha(raw);duplicates[digest].append(path.name);row={'file':path.name,'bytes':len(raw),'sha256':digest};containers.append(row)
  if path.suffix.lower()=='.egg':row['kind']='split EGG volume';continue
  dest=a.work/'containers'/digest[:16];dest.mkdir(parents=True,exist_ok=True)
  try:
   entries=[]
   with zipfile.ZipFile(path) as z:
    for info in z.infolist():
     if info.is_dir():continue
     name=Path(info.filename.replace('\\','/'));assert not name.is_absolute() and '..' not in name.parts
     blob=z.read(info);target=dest/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(blob)
     entries.append({'file':info.filename,'bytes':len(blob),'sha256':sha(blob),'zipDate':list(info.date_time)})
   row['kind']='ZIP/SFX container';row['entries']=entries
   cabs=[x for x in dest.rglob('*') if x.name.lower()=='data1.cab'];assert len(cabs)==1
   cab=cabs[0];cabhash=sha(cab.read_bytes());row['payloadCabSha256']=cabhash
   if cabhash not in payloads:
    out=a.work/'payloads'/cabhash[:16];out.mkdir(parents=True,exist_ok=True)
    proc=subprocess.run([a.unshield,'-L','-d',str(out),'x',str(cab)],capture_output=True,text=True)
    (a.work/(cabhash[:16]+'.log')).write_text(proc.stdout+proc.stderr)
    if proc.returncode:raise RuntimeError('unshield failed; see '+str(a.work/(cabhash[:16]+'.log')))
    payloads[cabhash]={'extracted':str(out),'sources':[]}
   payloads[cabhash]['sources'].append(path.name)
  except Exception as e:row['error']=str(e)
 volumes=sorted(sources.glob('*.egg'))
 if volumes:
  eggout=a.work/'egg';eggout.mkdir(exist_ok=True);egg=extract_egg(volumes,eggout)
  eggid='egg-'+sha(''.join(egg['volumeHashes']).encode())
  payloads[eggid]={'extracted':str(eggout),'sources':[v.name for v in volumes]}
  (a.work/'egg-manifest.json').write_text(json.dumps(egg,ensure_ascii=False,indent=2)+'\n')
 # Compare whole installed file maps, not just installer names/dates.
 late={r['path'].lower():r for r in json.loads((ROOT/'분석/Saver_Late/files.json').read_text())};versions=[]
 for cabhash,payload in payloads.items():
  out=Path(payload['extracted']);entries=[]
  for f in sorted(x for x in out.rglob('*') if x.is_file()):
   raw=f.read_bytes();rel=f.relative_to(out).as_posix();parts=rel.split('/');at=next((i for i,v in enumerate(parts) if v.lower()=='saver'),None);key='/'.join(parts[at+1:]).lower() if at is not None else rel.lower()
   h=sha(raw);old=late.get(key);entries.append({'file':key,'bytes':len(raw),'sha256':h,'peTimestamp':pe_date(raw),'lateComparison':'same' if old and old['sha256']==h else 'changed' if old else 'not matched','extractedFile':str(f)})
  versions.append({'payloadId':cabhash,'kind':'split EGG installed files' if cabhash.startswith('egg-') else 'InstallShield CAB','cabSha256':None if cabhash.startswith('egg-') else cabhash,'containers':payload['sources'],'fileCount':len(entries),'comparison':dict(collections.Counter(e['lateComparison'] for e in entries)),'files':entries})
 report={'scope':'All supplied root containers and unique InstallShield payloads; PE timestamps and filenames are not official release versions','containers':containers,'duplicateContainerGroups':[n for n in duplicates.values() if len(n)>1],'payloads':versions}
 target=ROOT/'분석/InstallerVersions';target.mkdir(exist_ok=True);(target/'installer_inventory.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print('containers',len(containers),'unique payloads',len(versions),'errors',[(c['file'],c['error']) for c in containers if 'error' in c])
 for v in versions:print(v['containers'],v['fileCount'],v['comparison'])
if __name__=='__main__':main()
