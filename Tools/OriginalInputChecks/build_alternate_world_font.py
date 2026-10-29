"""Original Dark14-1 zero-bit glyph masks; reuse already audited atlas cell descriptors."""
from pathlib import Path
import uuid,re,hashlib,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];raw=(ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/Dark14-1.mxf').read_bytes();assert len(raw)==2592*32
out=ROOT/'Assets/_Game/Resources/UI/NativeWorldAlternateFont.png';im=Image.new('RGBA',(896,574))
for glyph in range(2592):
 for y in range(14):
  for x in range(14):
   if raw[glyph*32+y*2+x//8]&(128>>(x%8))==0:im.putpixel((glyph%64*14+x,glyph//64*14+y),(255,255,255,255))
import io
buf=io.BytesIO();im.save(buf,format='PNG')
if out.exists():assert out.read_bytes()==buf.getvalue(),'Edited font atlas'
else:out.write_bytes(buf.getvalue())
meta=(ROOT/'Assets/_Game/Resources/UI/NativeWorldNameFont.png.meta').read_text();meta=re.sub(r'^guid: \w+', 'guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'darksaver-world-alternate-font').hex,meta,count=1,flags=re.M)
mp=Path(str(out)+'.meta')
if mp.exists():assert mp.read_text()==meta,'Edited font metadata'
else:mp.write_text(meta)
(Path(__file__).with_name('alternate_world_font.json')).write_text(json.dumps(dict(sourceSha256=hashlib.sha256(raw).hexdigest(),atlasSha256=hashlib.sha256(out.read_bytes()).hexdigest(),glyphs=2592,source='Dark14-1.mxf',description='Original zero-bit ink; full 14x14 cells,64 columns,41 rows. Metadata reuses original audited CP949-to-Unicode sprite names; no OS font substitution.'),indent=2)+'\n')
print('PASS original alternate font: 2592 glyph masks')
