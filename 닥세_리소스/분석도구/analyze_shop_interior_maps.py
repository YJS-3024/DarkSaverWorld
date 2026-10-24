"""Native interior-map inventory and visual evidence; does not infer town routing."""
import collections,hashlib,json,sys
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
from world_asset_baker import load_map,frame,resource,NATIVE
OUT=ROOT/'닥세_리소스/분석결과'
# Labels below were read directly from native reconstructed pixels, not filenames.
LABELS={'1500':'무기·방패 벽면 실내; 시설 미확정','2000':'무기점','2010':'주거/사무형 실내; 역할 미확정','2100':'방어구점','2110':'용병상점','2120':'무기점','2130':'방어구점','2140':'주점형 실내','2150':'무기점','2160':'방어구점','2170':'장신구점','2180':'소모품점','2190':'장비 진열 실내; 간판 없음','2200':'장신구점','2300':'소모품점','2400':'은행','2510':'창고','2600':'교회형 실내','2700':'주점형 실내','2800':'서가·마법 문양 실내; 역할 미확정','2900':'용병상점','3000':'복합 사무/주거형 실내; 역할 미확정'}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def render(d):
 w,h=d['width'],d['height'];im=Image.new('RGBA',(w*32,h*32),(0,0,0,255))
 for i,(rid,g,t) in enumerate(zip(d['a'],d['b'],d['tiles'])):
  pos=(i%w*32,i//w*32);im.alpha_composite(frame(rid,g,0),pos)
  if t[1]!=255:
   gr=resource(t[0])['groups'][t[1]];im.alpha_composite(frame(t[0],t[1],gr['tiles'][t[2]]['frames'][0],transparent=True),pos)
 for s in d['sprites']:im.alpha_composite(frame(s['resource'],s['index'],s['frame'],True,True),(s['x']*32,s['y']*32))
 return im

def main():
 inventory=[];groups=collections.defaultdict(list)
 for p in sorted(NATIVE.glob('*.mrc')):
  d=load_map(p.stem);digest=sha(p);groups[digest].append(p.name)
  inventory.append(dict(file=p.name,width=d['width'],height=d['height'],decorations=len(d['sprites']),resources=d['ids'],sha256=digest))
 rows=[];sheet=Image.new('RGB',(1600,1350),'#333333');draw=ImageDraw.Draw(sheet)
 for n,(key,label) in enumerate(LABELS.items()):
  p=NATIVE/(key+'.mrc');d=load_map(key);im=render(d);original_hash=hashlib.sha256(im.tobytes()).hexdigest()
  copies=[]
  for installation in ('Saver_Files','NetGame_Files','Shared_DLLs'):
   for suffix in ('','t'):
    f=ROOT/'닥세_리소스/설치본_추출'/installation/'Saver/MAP'/(key+suffix+'.mrc')
    if f.exists():copies.append(dict(file=str(f.relative_to(ROOT)),sha256=sha(f)))
  attrs=collections.Counter(t[3]&127 for t in d['tiles'])
  rows.append(dict(key=key,visualIdentification=label,identificationBasis='native first-frame reconstruction visually reviewed 2026-10-24',width=d['width'],height=d['height'],decorations=len(d['sprites']),resources=d['ids'],sha256=sha(p),identicalMapNames=groups[sha(p)],copies=copies,allCopiesIdentical=all(c['sha256']==sha(p) for c in copies),attributeCounts=dict(attrs),bottomRowAttributes=[t[3]&127 for t in d['tiles'][-d['width']:]],rgbaSha256=original_hash,kaosiaAssociationConfirmed=False,serverEntranceCoordinatesRecovered=False))
  if key in ('2000','2100'):im.save(OUT/('원본_실내_'+key+'.png'))
  im.thumbnail((310,240));x=n%5*320;y=n//5*270;sheet.paste(im,(x,y+25));draw.text((x+5,y+4),key,fill='white')
 sheet.save(OUT/'원본_실내맵_대조.png')
 report=dict(mapFiles=len(inventory),uniqueMapContents=len(groups),reviewedInteriorCandidates=len(rows),rows=rows,inventory=inventory,scope='Native MRC/GRS first-frame pixels and exact file inventory. No original client execution, NPC placement, server route recovery or Unity Play.',correction='Native shop interiors exist. Previous authored 14x12 Unity interiors are temporary and do not represent the original maps.',recommendedWeaponCandidate='2000',recommendedArmorCandidate='2100',recommendationBasis='Explicit native shop signs and complete shop layout; exact Kaosia-to-interior routing not recovered.')
 (OUT/'상점_실내맵_분석.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 assert len(rows)==22 and all(r['allCopiesIdentical'] for r in rows)
 print(f'PASS {len(inventory)} MRC files, {len(groups)} unique contents, {len(rows)} visually reviewed interiors; source copies identical')
if __name__=='__main__':main()
