"""Update verified effect source rectangles from the later 999x1098 Fx sheet.
Sprite rectangles/IDs, runtime timing and geometry stay unchanged. Every crop is
checked against the original converter output before replacement; unmatched or
removed pattern definitions are retained with an explicit report.
"""
import sys,io,json,re,hashlib
from pathlib import Path
import yaml
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from build_late_saver_catalog import arc_entries
from analyze_client import lzh_decode,lzss_decode
for name,method,size,packed in arc_entries(Path(sys.argv[1])/'Saver_Files/saver/gameres/etc.arc'):
 if name.lower()!='fx.bmp':continue
 data=(lzh_decode if method==0 else lzss_decode)(packed,size);assert len(data)==size
 cache=ROOT/'분석/Saver_Late/decoded_art/gameres/etc.arc/fx.bmp';cache.parent.mkdir(parents=True,exist_ok=True);cache.write_bytes(data)
old=Image.open(ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp').convert('RGBA');new=Image.open(cache).convert('RGBA');fx2=Image.open(ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc2/Fx2.bmp').convert('RGBA')
def transparent(im):
 im=im.copy();im.putdata([(*p[:3],0 if p[:3]==(0,0,0) else 255) for p in im.getdata()]);return im
old,new,fx2=map(transparent,[old,new,fx2]);assert old.size==(999,999) and new.size==(999,1098)
patterns={int(r['values'][0]):r['values'] for r in json.loads((ROOT/'분석/Saver_Late/tables.json').read_text())['magicptn.atr']['rows']}
base=ROOT/'Assets/_Game/Resources/Effects';records=[];blocked=[]
names=['NativeFireball','NativeTornado','NativeThunderV2','NativeTargetEffects','NativeShrinkingTargetEffects','NativeDelayedTargetEffects','NativeCoordinateTargetEffects','NativeCoordinateGroundEffects','NativeThrustBeamEffects','NativeDoubleThrustBeamEffects','NativeCrossTrustEffects']
files=[base/(n+'.png') for n in names]+sorted((base/'ProjectilePatterns').glob('*.png'))
for path in files:
 meta=yaml.safe_load(path.with_name(path.name+'.meta').read_text())['TextureImporter']['spriteSheet']['sprites'];image=Image.open(path).convert('RGBA');changed=False
 for part in meta:
  name=part['name'];r=part['rect'];w,h=int(r['width']),int(r['height']);dx=int(r['x']);dy=image.height-int(r['y'])-h;source=old;target=new
  if path.stem=='NativeFireball':
   i=int(name.rsplit('_',1)[1]);sx=336+i*32 if name.startswith('Projectile') else i*32;sy=0
  elif path.stem=='NativeTornado':
   i=int(name.split('_')[-1]);sx=512+i*64 if i<4 else 250+(i-4)*32;sy=822 if i<4 else 240
  elif path.stem=='NativeThunderV2':
   i=int(name.split('_')[-1]);sx=i*50 if i<5 else 250+(i-5)*32 if i<9 else (i-9)*40;sy=208 if i<9 else 128
  elif 'ThrustBeam' in path.stem:
   rects=[(281,377,10,22),(291,377,2,22),(293,377,18,22),(281,377,22,10),(281,387,22,2),(281,389,22,10)];sx,sy,rw,rh=rects[int(name.split('_')[-1])]
   if path.stem=='NativeDoubleThrustBeamEffects':sx+=70;sy+=30
   assert (rw,rh)==(w,h)
  elif path.stem=='NativeCrossTrustEffects':
   sx,sy,rw,rh=[(371,311,27,39),(251,315,28,44),(280,314,27,60)][int(name.split('_')[-1])];assert (rw,rh)==(w,h)
  else:
   if path.parent.name=='ProjectilePatterns':pattern=int(path.stem.split('_')[-1]);i=int(name.rsplit('_',1)[1])
   else:_,p,i=name.split('_');pattern=int(p);i=int(i)
   if pattern not in patterns:
    blocked.append(dict(path=str(path.relative_to(ROOT)),sprite=name,reason='Pattern absent in later table; existing verified effect retained'));continue
   raw=patterns[pattern];n=[int(v) if j!=1 else 0 for j,v in enumerate(raw)]
   if path.parent.name=='ProjectilePatterns' and name.startswith('Projectile'):
    sx=n[9]+i*w;sy=n[10]
    if n[16] in (11,24):source=target=fx2
   elif path.stem=='NativeCoordinateGroundEffects':sx=n[9]+i*n[13];sy=n[10]+i*n[14]
   else:sx=n[2]+i*n[6];sy=n[3]+i*n[7]
  assert sx>=0 and sy>=0 and sx+w<=source.width and sy+h<=source.height
  before=source.crop((sx,sy,sx+w,sy+h));after=target.crop((sx,sy,sx+w,sy+h));actual=image.crop((dx,dy,dx+w,dy+h))
  if actual.tobytes() not in (before.tobytes(),after.tobytes()):
   blocked.append(dict(path=str(path.relative_to(ROOT)),sprite=name,reason='Existing pixels differ from both verified source crops'));continue
  if 'ThrustBeam' in path.stem and name=='Beam_1':assert all(after.getpixel((0,y))==after.getpixel((1,y)) for y in range(h))
  if 'ThrustBeam' in path.stem and name=='Beam_4':assert all(after.getpixel((x,0))==after.getpixel((x,1)) for x in range(w))
  different=actual.tobytes()!=after.tobytes()
  if different:image.paste(after,(dx,dy));changed=True
  records.append(dict(path=str(path.relative_to(ROOT)),sprite=name,sourceRect=[sx,sy,w,h],targetRect=[dx,dy,w,h],sourceSheet='Fx2 unchanged' if source is fx2 else 'Fx later',pixelSha256=hashlib.sha256(after.tobytes()).hexdigest(),changed=different,sourceDiffers=before.tobytes()!=after.tobytes()))
 if changed:image.save(path)
# Death frames use the unchanged first source row; assert their native provenance too.
for i in range(7):
 path=ROOT/f'Assets/_Game/Art/Converted/Effects/Death/DeathEffect_{i}.png';expected=new.crop((i*32,0,i*32+31,31));assert Image.open(path).convert('RGBA').tobytes()==expected.tobytes()
report=dict(oldSheet=list(old.size),lateSheet=list(new.size),frames=len(records),updatedFrames=sum(r['changed'] for r in records),sourceFrameDifferences=sum(r['sourceDiffers'] for r in records),entries=records,retained=blocked,deathFramesVerified=7,unityPlay=False)
(ROOT/'Tools/LateVersionChecks/effect_art_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ('entries','retained')},ensure_ascii=False));print('Retained:',len(blocked))
