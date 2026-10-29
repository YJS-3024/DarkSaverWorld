"""Independent source-mask vs Unity atlas check; no Unity import or OS font claim."""
import json,hashlib
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];raw=(ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/Dark14-1.mxf').read_bytes();path=ROOT/'Assets/_Game/Resources/UI/NativeWorldAlternateFont.png';im=Image.open(path).convert('RGBA');assert im.size==(896,574)
for i in range(2592):
 for y in range(14):
  mask=int.from_bytes(raw[i*32+y*2:i*32+y*2+2],'big')
  for x in range(14):
   ink=(mask&(1<<(15-x)))==0
   pixel=im.getpixel((i%64*14+x,i//64*14+y));assert pixel[3]==(255 if ink else 0),(i,x,y)
meta=Path(str(path)+'.meta').read_text();assert meta.count('name: U_')>=2400
assert 'filterMode: 0' in meta and 'enableMipMap: 0' in meta and 'textureCompression: 0' in meta
print('PASS: 2592 original Dark14-1 glyph masks, Unicode sprite descriptors, Point/uncompressed importer metadata. Not OS font or Unity playback.')
