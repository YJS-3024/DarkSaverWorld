"""Extract original entry artwork. Source coordinates are top-left pixels.
Screen placement and removing unused billing/options controls are local adaptations.
"""
import sys,json,hashlib,re,uuid
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
native=Path(sys.argv[1])/'Saver_Files/saver';out=ROOT/'Assets/_Game/Resources/UI/Entry';out.mkdir(exist_ok=True)
def meta(path,folder=False):
 p=path.with_name(path.name+'.meta')
 if p.exists():return
 g=uuid.uuid5(uuid.NAMESPACE_URL,str(path.relative_to(ROOT))).hex
 if folder:p.write_text('fileFormatVersion: 2\nguid: '+g+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n');return
 template=(ROOT/'Assets/_Game/Resources/Towns/Kaosia/Ground.png.meta').read_text()
 p.write_text(re.sub(r'guid: [a-f0-9]{32}','guid: '+g,template,count=1))
meta(out,True)
files={p.name.lower():p for p in native.glob('*.bmp')};rows=[]
# Retain the native pixels, including input decoration. Buttons are separate click targets.
for name,file,rect in [('Background','logo6.bmp',None),('LoginPanel','login.bmp',(9,9,206,96)),('Connect','login.bmp',(22,69,73,86)),('Join','login.bmp',(77,69,113,86)),('Exit','login.bmp',(157,69,194,86))]:
 source=Image.open(files[file]).convert('RGBA');image=source if rect is None else source.crop(rect);path=out/(name+'.png');image.save(path);meta(path)
 assert Image.open(path).convert('RGBA').tobytes()==image.tobytes()
 rows.append(dict(asset=str(path.relative_to(ROOT)),source=file,sourceRect=rect,size=list(image.size),sourceSha256=hashlib.sha256(files[file].read_bytes()).hexdigest(),pixelSha256=hashlib.sha256(image.tobytes()).hexdigest()))
# Compose the native three-card creation window from the late Wlib resources.
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_ui_library,read_lsc
wlib=next(p for p in native.iterdir() if p.is_dir() and p.name.lower()=='wlib');ui_files={p.name.lower():p for p in wlib.iterdir()}
scene=read_lsc(ui_files['scene24.lsc']);canvas=Image.new('RGBA',(scene['width'],scene['height']),(0,0,0,0))
libs={ext:read_ui_library(ui_files['waitlib'+ext])[:2] for ext in ['.lce','.ltn','.lnm']}
for v in scene['decor']:
 parts,atlas=libs['.lce'];a=parts[v['id']];image=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
 for y in range(v['repeat_y']):
  for x in range(v['repeat_x']):canvas.alpha_composite(image,(v['x']+x*a['width'],v['y']+y*a['height']))
for v in scene['buttons']:
 parts,atlas=libs['.ltn' if v['kind']=='button' else '.lnm'];a=parts[v['id']];canvas.alpha_composite(atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height'])),(v['x'],v['y']))
canvas.putdata([(*p[:3],0 if p[:3]==(0,0,0) else p[3]) for p in canvas.getdata()]) # Native black color key.
path=out/'CharacterCreation.png';canvas.save(path);meta(path)
rows.append(dict(asset=str(path.relative_to(ROOT)),source='Wlib/scene24.lsc+waitlib',sourceRect=None,size=list(canvas.size),sourceSha256=hashlib.sha256(ui_files['scene24.lsc'].read_bytes()).hexdigest(),pixelSha256=hashlib.sha256(canvas.tobytes()).hexdigest()))
# A flat area of the native panel covers unused server-options art before placing three buttons.
color=Image.open(files['login.bmp']).convert('RGB').getpixel((15,65))
report=dict(assets=rows,panelFillRGB=color,placement='Native login origin226,279 and sourceRect9,9,206,96 verified in entry/layout.json; Unity uses the native-size BltFast geometry. Title start buttons remain local.',removed='Unused original options/billing controls; retained local account registration',unityPlay=False)
(ROOT/'Tools/LateVersionChecks/entry_art_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(assets=len(rows),panelFillRGB=color)))
