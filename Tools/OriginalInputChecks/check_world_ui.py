"""Verify Unity catalog references against original Wlib descriptors; no Unity import."""
import hashlib,json,re,struct
from pathlib import Path
import yaml
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];FOLDER=ROOT/'Assets/_Game/Resources/UI/WorldWindows';SRC=ROOT/'분석/Saver_1.2';checks=0
def check(ok,context):
 global checks
 assert ok,context
 checks+=1
def read_yaml(p):return yaml.safe_load('\n'.join(l for l in p.read_text().splitlines() if not l.startswith(('%','---'))))
def num(b,o):return struct.unpack_from('<i',b,o)[0]
assets={re.search(r'^guid: (\w+)',m.read_text(),re.M)[1]:Path(str(m)[:-5]) for m in FOLDER.glob('*.meta')}
def image(ref):
 path=assets[ref['guid']];im=Image.open(path);check(im.mode=='RGBA',str(path));meta=read_yaml(Path(str(path)+'.meta'))['TextureImporter'];check(meta['textureSettings']['filterMode']==0,'point');check(meta['mipmaps']['enableMipMap']==0,'no mip');check(meta['platformSettings'][0]['textureCompression']==0,'no compression');return im
windows=read_yaml(FOLDER/'NativeWorldWindows.asset')['MonoBehaviour']['windows'];check(len(windows)==7,'layouts')
header=(SRC/'unpacked/legacy_libraries/Wlib_waitlib_lce.header.bin').read_bytes();atlas=Image.open(SRC/'unpacked/legacy_libraries/Wlib_waitlib_lce.bmp').convert('RGBA');bh=(SRC/'unpacked/legacy_libraries/Wlib_waitlib_ltn.header.bin').read_bytes();buttons=Image.open(SRC/'unpacked/legacy_libraries/Wlib_waitlib_ltn.bmp').convert('RGBA')
def transparent(im):
 im=im.copy();im.putdata([(r,g,b,0 if r==g==b==0 else 255) for r,g,b,a in im.getdata()]);return im
for w in windows:
 id=w['sceneId'];name='scene3mm' if id==303 else 'scene'+str(id);raw=(SRC/f'extracted/Saver_Files/Saver/Wlib/{name}.lsc').read_bytes();width,height=num(raw,0),num(raw,4)
 check(w['size']=={'x':width,'y':height},name+' size');expected=Image.new('RGBA',(width,height));cur=28
 for _ in range(num(raw,24)):
  item,count=num(raw,cur),num(raw,cur+4);cur+=8;x,y,tw,th=struct.unpack_from('<4i',header,4+(item-1)*20+4);tile=atlas.crop((x,y,x+tw,y+th))
  for _ in range(count):
   l,t,nx,ny=struct.unpack_from('<4i',raw,cur);cur+=16
   for j in range(nx):
    for k in range(ny):expected.paste(tile,(l+j*tw,t+k*th))
 check(image(w['background']).tobytes()==transparent(expected).tobytes(),name+' original composed pixels')
 count=num(raw,cur);cur+=4;check(len(w['controls'])==count,name+' buttons')
 for c in w['controls']:
  item,left,top=struct.unpack_from('<3i',raw,cur);cur+=12;x,y,bw,bhgt=struct.unpack_from('<4i',bh,4+(item-1)*24+4)
  check(c['nativeId']==item and c['rect']==dict(x=left,y=top,width=bw,height=bhgt),name+' native button coordinates')
  for i,ref in enumerate(c['frames']):check(image(ref).tobytes()==transparent(buttons.crop((x+i*bw,y,x+(i+1)*bw,y+bhgt))).tobytes(),name+' native button frame '+str(i))
 if id in [2,3,42,303]:
  mini=image(w['minimizedBackground']);check(mini.size==(w['minimizedSize']['x'],w['minimizedSize']['y']),name+' compact')
manifest=json.loads(Path(__file__).with_name('world_ui_conversion.json').read_text())
for t in manifest['textures'].values():check(hashlib.sha256((ROOT/t['path']).read_bytes()).hexdigest()==t['sha256'],'output hash')
print(f'PASS {checks} static assertions: original pixels/control positions, Unity GUID references, import metadata, generated hashes. Not Unity execution.')
