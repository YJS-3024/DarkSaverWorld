"""Audit interior NPC evidence; native decoder probes use modeled reset/UI effects."""
import csv,hashlib,json,struct,sys,subprocess
from pathlib import Path
from PIL import Image,ImageDraw
from analyze_world_packets import decode_record
from analyze_special_skills import ROOT as NATIVE
from decode_visuals import read_mrc
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'닥세_리소스/분석결과'
sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
from world_asset_baker import frame
MAPS={'2000':'무기점','2100':'방어구점','2200':'장신구점','2300':'소모품/마법도구점','2400':'은행','2510':'창고','2600':'신전형'}
DECODED=ROOT/'분석/Saver_1.2/decoded/all_arc'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 rows=[];groups=set()
 for key,title in MAPS.items():
  path=NATIVE/'MAP'/(key+'.mrc');m=read_mrc(path)
  exact=16+4*len(m['ids'])+7*m['width']*m['height']+16*len(m['sprites'])
  assert exact==path.stat().st_size
  refs=sorted({(s['resource'],s['index'],s['frame']) for s in m['sprites']});groups.update(refs)
  placements=[str(p.relative_to(DECODED)) for p in (DECODED/'MAP').rglob('*') if p.is_file() and p.stem.lower() in (key,key+'t') and p.suffix.lower() in ('.set','.pos','.evt','.deo','.dee','.srf')]
  rows.append(dict(map=key,role=title,sha256=sha(path),width=m['width'],height=m['height'],decorations=len(m['sprites']),exactParsedEOF=True,placementFiles=placements,staticSpriteReferences=refs,npcCount=None,npcNames=None,npcAppearanceIds=None,npcCoordinates=None,reason='No map-linked actor roster recovered; static image references reviewed separately.'))
 sheet=Image.new('RGB',(1000,((len(groups)+7)//8)*140),'#555555');draw=ImageDraw.Draw(sheet)
 for n,(rid,g,f) in enumerate(sorted(groups)):
  image=frame(rid,g,f,True,True).copy();size=image.size;image.thumbnail((118,112));x=n%8*125;y=n//8*140
  sheet.paste(image,(x,y+22),image);draw.text((x,y),f'{rid}/{g}/{f} {size}',fill='white')
 sheet.save(OUT/'실내NPC_정적장식대조.png')
 abilities=list(csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/npcs_and_special.csv').open(encoding='utf-8-sig')))
 candidates=[];sheet=Image.new('RGB',(800,5*140),'#555555');draw=ImageDraw.Draw(sheet)
 for n,row in enumerate(r for r in abilities if 804<=int(r['직업'])<=828):
  aid=row['직그림'];paths=[p for p in (DECODED/'gameres').glob('char*/*') if p.name.lower()==aid+'r.bmp'];assert len(paths)<=1
  if not paths:
   candidates.append(dict(abilityId=int(row['직업']),appearanceId=int(aid),tableName=row['이름'],art=None,mapAssociation=None,merchantRoleConfirmed=False));continue
  path=paths[0];im=Image.open(path).convert('RGBA');w,h=im.size;assert w%3==0 and h==w//3*4
  # Full directional sheet, not a guess at the merchant's canonical facing.
  im.thumbnail((150,112));x=n%5*160;y=n//5*140;sheet.paste(im,(x,y+22));draw.text((x,y),aid,fill='white')
  candidates.append(dict(abilityId=int(row['직업']),appearanceId=int(aid),tableName=row['이름'],art=str(path.relative_to(ROOT)),sha256=sha(path),mapAssociation=None,merchantRoleConfirmed=False))
 sheet.save(OUT/'실내NPC_마을사람외형후보.png')
 keywords=['은행','창고','상인','은행원','창고지기','무기점','방어구점','장신구점','소모품점','신전'];hits=[];scanned=0
 for path in sorted(DECODED.rglob('*')):
  if not path.is_file() or path.suffix.lower() not in ('.txt','.evt','.deo','.dee','.srf','.set','.pos'):continue
  scanned+=1
  for line,text in enumerate(path.read_text(encoding='cp949',errors='replace').splitlines(),1):
   if any(word in text for word in keywords):hits.append(dict(file=str(path.relative_to(DECODED)),line=line,text=text))
 binary_hits=[]
 for name in ['Saver.exe','userunit.dll','GameCtl.dll']:
  path=NATIVE/name;raw=path.read_bytes()
  for word in keywords:
   needle=word.encode('cp949');start=0
   while (offset:=raw.find(needle,start))>=0:
    a=raw.rfind(b'\0',0,offset)+1;z=raw.find(b'\0',offset)
    binary_hits.append(dict(binary=name,sha256=sha(path),fileOffset=hex(offset),keyword=word,text=raw[a:z].decode('cp949',errors='replace')));start=offset+len(needle)
 probes=[]
 for slot,ability,x,y in [(40,804,8,4),(41,805,11,4),(42,820,9,6),(43,828,12,6)]:
  words=[0]*16;words[0]=slot;words[2]=ability;words[3]=ability;words[14]=x;words[15]=y
  body=struct.pack('<16H2I',*words,0,0)+b'\x03NPC'
  trace,record,strings=decode_record(0x50,body)
  values=[struct.unpack_from('<I',record,o)[0] for o in (0,4,8,0x48,0x4c)]
  assert values==[slot,ability,ability,x,y] and strings==['4e5043']
  probes.append(dict(slot=slot,syntheticAbility=ability,x=x,y=y,capturedUnitinfoValues=values,match=True))
 asm=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel','--start-address=0x1000c700','--stop-address=0x1000cac0',str(NATIVE/'GameCtl.dll')],text=True)
 (OUT/'실내NPC_수신생성근거.asm.txt').write_text(asm)
 report=dict(scope='Native resources + isolated original x86 receive decoder; resetcharacter and CString effects modeled. No original Windows/server session.',interiors=rows,reviewedStaticSpriteReferences=len(groups),staticReview='No separate human figure identified among these seven interiors static decoration references; does not establish total live NPC count.',candidateAppearances=candidates,abilityRows=len(candidates),availableDirectionalSheets=sum(c.get('art') is not None for c in candidates),scannedTextFiles=scanned,textRoleHits=hits,binaryRoleHits=binary_hits,decoderProbes=probes,decoder=dict(binary='GameCtl.dll',sha256=sha(NATIVE/'GameCtl.dll'),entry='0x1000C700',subtype='5C50',conclusion='Received per-unit records carry slot, ability/display fields and coordinates independently of the MRC; synthetic fixtures do not identify historical merchants or actual NPC count.'),conclusion='Original per-interior NPC count/name/appearance/location remain unconfirmed; 25 generic townsperson ability rows exist (not all directional sheets available), but no shop assignment is established.',unityNpcAdded=False)
 (OUT/'실내상점_NPC_원본분석.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(f'PASS {len(rows)} interiors exact EOF, {len(groups)} static references, {len(candidates)} ability rows/{sum(c.get('art') is not None for c in candidates)} directional sheets, {scanned} text files, {len(probes)} original x86 decoder probes')
if __name__=='__main__':main()
