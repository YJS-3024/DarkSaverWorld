"""Decode original buttonless scene28 into a point-RGBA runtime resource; preserve existing GUID."""
import hashlib,json,struct,sys,tempfile,uuid,re
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_lsc,read_ui_library
def compose():
 base=ROOT/'Assets/_Original/SourceData/Native/wlib';spec=read_lsc(base/'scene28.lsc.bytes');assert(spec['width'],spec['height'])==(562,84)and not spec['buttons']
 with tempfile.TemporaryDirectory(prefix='ds-original-notice-')as tmp:
  path=Path(tmp)/'waitlib.lce';path.write_bytes((base/'waitlib.lce.bytes').read_bytes());entries,sheet=read_ui_library(path)[:2];image=Image.new('RGBA',(562,84))
  for row in spec['decor']:
   entry=entries[row['id']];piece=sheet.crop((entry['x'],entry['y'],entry['x']+entry['width'],entry['y']+entry['height']))
   for y in range(row['repeat_y']):
    for x in range(row['repeat_x']):image.alpha_composite(piece,(row['x']+x*entry['width'],row['y']+y*entry['height']))
 raw=bytearray(image.tobytes())
 for at in range(0,len(raw),4):raw[at+3]=0 if raw[at:at+3]==b'\x00\x00\x00' else 255
 return bytes(raw),spec
def main():
 rgba,spec=compose();path=ROOT/'Assets/_Original/Resources/OriginalWorldNotice.bytes';path.write_bytes(b'DSN1'+struct.pack('<HH',562,84)+rgba);meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 inputs=[Path(__file__),ROOT/'Assets/_Original/SourceData/Native/wlib/scene28.lsc.bytes',ROOT/'Assets/_Original/SourceData/Native/wlib/waitlib.lce.bytes'];report=dict(width=562,height=84,sourceDecorations=len(spec['decor']),sourceButtons=0,resource=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),resourceGuid=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1],rgbaSha256=hashlib.sha256(rgba).hexdigest(),scope='Generated asset container DSN1+u16 dimensions+RGBA from original LSC/LCE. Not a network frame; no source/generated PNG/player save edits.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in inputs});(OUT/'native-world-message-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
