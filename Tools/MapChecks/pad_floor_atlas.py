"""Extrude original tile edges without changing any sprite IDs or saved scene cells."""
from pathlib import Path
import math,json,hashlib
import yaml
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
p=ROOT/'Assets/_Game/Art/Converted/NonScenarioMaps/Floors/NativeFloors.png';m=p.with_suffix('.png.meta');meta=yaml.safe_load(m.read_text());ti=meta['TextureImporter'];sprites=ti['spriteSheet']['sprites'];old=Image.open(p).convert('RGBA')
columns=56;step=36;height=2**math.ceil(math.log2(max(36,math.ceil(len(sprites)/columns)*step)));out=Image.new('RGBA',(columns*step,height))
for i,sp in enumerate(sprites):
 r=sp['rect'];x,y,w,h=[int(r[k]) for k in ['x','y','width','height']];assert (w,h)==(32,32)
 tile=old.crop((x,old.height-y-32,x+32,old.height-y));nx=i%columns*step+2;ny=i//columns*step+2;top=height-ny-32
 out.paste(tile,(nx,top));out.paste(tile.crop((0,0,1,32)).resize((2,32)),(nx-2,top));out.paste(tile.crop((31,0,32,32)).resize((2,32)),(nx+32,top))
 out.paste(tile.crop((0,0,32,1)).resize((32,2)),(nx,top-2));out.paste(tile.crop((0,31,32,32)).resize((32,2)),(nx,top+32))
 for tx,ty,sx,sy in [(-2,-2,0,0),(32,-2,31,0),(-2,32,0,31),(32,32,31,31)]:out.paste(tile.crop((sx,sy,sx+1,sy+1)).resize((2,2)),(nx+tx,top+ty))
 r.update(x=nx,y=ny)
 assert out.crop((nx,top,nx+32,top+32)).tobytes()==tile.tobytes()
ti['spriteExtrude']=1;ti['textureSettings']['filterMode']=0;ti['mipmaps']['enableMipMap']=0;ti['userData']='Native floor edges extruded 2px; 32px source pixels and sprite IDs preserved.'
out.save(p);m.write_text(yaml.safe_dump(meta,sort_keys=False,allow_unicode=True))
report=dict(sprites=len(sprites),gutterPixels=2,width=out.width,height=out.height,sourcePixelsPreserved=True,spriteIdsPreserved=True,sceneFilesChanged=False,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),actualUnityPlayback=False)
(ROOT/'Tools/MapChecks/floor_atlas_padding.json').write_text(json.dumps(report,indent=2)+'\n');print('PASS',len(sprites),'original tile crops preserved; 2px edge padding; no scene/ID changes.')
