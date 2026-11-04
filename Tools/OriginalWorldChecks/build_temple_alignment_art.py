"""Extract literal scene29 frame/button layout without choosing temple operating values."""
import hashlib,json,sys,tempfile,uuid
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original'
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'));from decode_visuals import read_lsc,read_ui_library
source=BASE/'SourceData/Native/wlib';inputs=[source/'scene29.lsc.bytes',source/'waitlib.lce.bytes',source/'waitlib.ltn.bytes']
def meta(p):
 m=Path(str(p)+'.meta')
 if m.exists():return
 if p.suffix=='.png':
  template=(BASE/'Resources/OriginalClassDropDenied.png.meta').read_text();import re
  m.write_text(re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex,template,flags=re.M))
 else:m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
def main():
 d=read_lsc(inputs[0]);buttons=[]
 with tempfile.TemporaryDirectory()as tmp:
  libs={}
  for ext in ['lce','ltn']:
   p=Path(tmp)/('waitlib.'+ext);p.write_bytes((source/('waitlib.'+ext+'.bytes')).read_bytes());libs[ext]=read_ui_library(p)[:2]
  image=Image.new('RGBA',(d['width'],d['height']))
  for b in d['decor']:
   a=libs['lce'][0][b['id']];tile=libs['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
   for y in range(b['repeat_y']):
    for x in range(b['repeat_x']):image.alpha_composite(tile,(b['x']+x*a['width'],b['y']+y*a['height']))
  for b in d['buttons']:
   assert b['kind']=='button';a=libs['ltn'][0][b['id']];buttons.append(dict(id=b['id'],x=b['x'],y=b['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
  # Same black transparency key as the existing original wlib converters.
  data=image.load()
  for y in range(image.height):
   for x in range(image.width):
    if data[x,y][:3]==(0,0,0):data[x,y]=(0,0,0,0)
  art=BASE/'Resources/OriginalTempleAlignment.png';image.save(art);meta(art)
 layout=BASE/'Resources/OriginalTempleAlignmentLayout.json';layout.write_text(json.dumps(dict(scene=29,width=d['width'],height=d['height'],buttons=buttons),indent=2)+'\n');meta(layout)
 report=dict(window=63,scene=29,size=[image.width,image.height],buttons=buttons,unityPlay=False,sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [Path(__file__),*inputs]},outputs=[dict(asset=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest())for p in [art,layout]])
 Path(__file__).with_name('temple-alignment-art.json').write_text(json.dumps(report,indent=2)+'\n');print('Extracted original scene29: '+str(report['size'])+', '+str(len(buttons))+' buttons')
if __name__=='__main__':main()
