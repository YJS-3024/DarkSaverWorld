"""Bind original47-row map MIDI table and literal scenario requests to GM scores."""
import hashlib,importlib.util,json,re,struct,subprocess,sys,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';OUT=Path(__file__).resolve().parent

def timeline(b):
 _,tracks,division=struct.unpack_from('>3H',b,8);assert not division&0x8000
 pos=8+int.from_bytes(b[4:8],'big');events=[];ends=[]
 def vlq(data,i):
  value=0
  while True:
   byte=data[i];i+=1;value=value<<7|byte&127
   if byte<128:return value,i
 for _ in range(tracks):
  assert b[pos:pos+4]==b'MTrk';n=int.from_bytes(b[pos+4:pos+8],'big');d=b[pos+8:pos+8+n];pos+=8+n;i=tick=0;running=None
  while i<len(d):
   dt,i=vlq(d,i);tick+=dt;status=d[i]
   if status&128:i+=1
   else:assert running is not None;status=running
   if status==255:
    kind=d[i];i+=1;length,i=vlq(d,i);payload=d[i:i+length];i+=length
    if kind==81:assert length==3;events.append((tick,int.from_bytes(payload,'big')))
    if kind==47:break
   elif status in (240,247):length,i=vlq(d,i);i+=length;running=None
   else:assert 128<=status<240;running=status;i+=1 if status>>4 in (12,13)else 2
  ends.append(tick)
 assert pos==len(b);last=0;tempo=500000;seconds=0
 for tick,new in sorted(events):seconds+=(tick-last)*tempo/division/1e6;last=tick;tempo=new
 return seconds+(max(ends)-last)*tempo/division/1e6

def build():
 folder=BASE/'Art/Campaign/Music';folder.mkdir(exist_ok=True)
 spec=importlib.util.spec_from_file_location('scenario_tutorial_assets',ROOT/'Tools/OriginalTutorialChecks/build_tutorial_assets.py');tutorial=importlib.util.module_from_spec(spec);spec.loader.exec_module(tutorial)
 if not Path(str(folder)+'.meta').exists():tutorial.meta.meta(folder,BASE,True)
 catalog=json.loads((BASE/'Data/Campaign/CampaignCatalog.json').read_text());report=json.loads((OUT/'campaign-assets.json').read_text());scripts=report['scripts'];names=set();sources={};outputs=[];refs=[];missing=[]
 for m in catalog['maps']:
  for field in ['introIndex','outroIndex']:
   if m[field]<0:continue
   path=ROOT/scripts[m[field]];sources[str(path.relative_to(ROOT))]=hashlib.sha256(path.read_bytes()).hexdigest()
   names.update(s.replace('\\','/').lower()for s in re.findall(r'^PLAYMIDI\s+(\S+)',path.read_text(),re.M))
 table=BASE/'SourceData/Decoded/gameres/set.arc/midiset.txt.txt';text=table.read_text();lines=text.splitlines();declared=int(lines[0]);assert len(lines)>declared
 from build_late_saver_catalog import arc_entries
 from analyze_client import lzh_decode,lzss_decode
 archive=BASE/'SourceData/Native/gameres/set.arc.bytes'
 member=next((method,size,data)for name,method,size,data in arc_entries(archive)if name.lower()=='midiset.txt')
 method,size,packed=member;decoded=lzh_decode(packed,size)if method==0 else lzss_decode(packed,size)
 assert decoded.decode('cp949').replace('\r\n','\n')==text
 sources[str(archive.relative_to(ROOT))]=hashlib.sha256(archive.read_bytes()).hexdigest()

 sources[str(table.relative_to(ROOT))]=hashlib.sha256(table.read_bytes()).hexdigest()
 map_rows=[dict(map=int(row.split()[0]),path='bgm/'+row.rsplit(' ',1)[1].lower())for row in lines[1:declared+1]]
 names.update(row['path']for row in map_rows)
 native={p.relative_to(BASE/'SourceData/Native').as_posix().removesuffix('.bytes').lower():p for p in (BASE/'SourceData/Native/bgm').glob('*.mid.bytes')}
 template=Path(str(BASE/'Art/Tutorial/Nco01.ogg')+'.meta').read_text()
 for name in sorted(names):
  if name not in native:missing.append(name);continue
  source=native[name];seconds=timeline(source.read_bytes());assert 0<seconds<900
  target=folder/(Path(name).stem+'.ogg');sources[str(source.relative_to(ROOT))]=hashlib.sha256(source.read_bytes()).hexdigest()
  if not target.exists():tutorial.render_music(target,source,seconds)
  guid=uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+target.relative_to(BASE).as_posix()).hex
  importer=re.sub(r'^guid: .*$', 'guid: '+guid,template,flags=re.M)
  importer=re.sub(r'^    loadType: .*$', '    loadType: 2',importer,flags=re.M)
  importer=re.sub(r'^    preloadAudioData: .*\n','',importer,flags=re.M)
  importer=importer.replace('  forceToMono:', '  preloadAudioData: 0\n  forceToMono:')
  importer=re.sub(r'^  loadInBackground: .*$', '  loadInBackground: 1',importer,flags=re.M)
  Path(str(target)+'.meta').write_text(importer);refs.append('{fileID: 8300000, guid: '+guid+', type: 3}')
  outputs.append(dict(asset=str(target.relative_to(ROOT)),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),native=name,nominalSeconds=seconds,importerSha256=hashlib.sha256(Path(str(target)+'.meta').read_bytes()).hexdigest()))
 asset=BASE/'Data/Campaign/NativeCampaignArt.asset';text=asset.read_text()
 for field in ['musicPaths','musicClips']:text=re.sub('  '+field+r':\n(?:  - .*\n)*','',text)
 table_guid=tutorial.guid(table)
 text=re.sub(r'  mapMusicTable: .*\n','',text)
 asset.write_text(text+'  mapMusicTable: {fileID: 4900000, guid: '+table_guid+', type: 3}\n'+'  musicPaths:\n'+''.join('  - '+name+'\n'for name in [o['native']for o in outputs])+'  musicClips:\n'+''.join('  - '+ref+'\n'for ref in refs))
 for name in ['campaign-assets.json','gateway-assets.json']:
  p=OUT/name;r=json.loads(p.read_text());key=str(asset.relative_to(ROOT));digest=hashlib.sha256(asset.read_bytes()).hexdigest()
  if key in r.get('sources',{}):r['sources'][key]=digest
  for row in r.get('outputs',[]):
   if row['asset']==key:row['sha256']=digest
  p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
 sf=Path('/usr/share/sounds/sf2/TimGM6mb.sf2');sources['external:TimGM6mb.sf2']=hashlib.sha256(sf.read_bytes()).hexdigest()
 (OUT/'scenario-music-assets.json').write_text(json.dumps(dict(sources=sources,outputs=outputs,mapRows=map_rows,declaredMapRows=declared,ignoredTrailingRows=lines[declared+1:],missingNativePaths=missing,unityPlay=False,policy='Original declared47 map rows and literal PLAYMIDI paths; trailing rows are excluded; no fallback for unmapped maps. Original embedded tempo/end-of-track duration, synthesized with FluidSynth/TimGM6mb. Native Windows device timbre and audible timing are not measured.'),ensure_ascii=False,indent=2)+'\n');return len(outputs)
if __name__=='__main__':print(build())
