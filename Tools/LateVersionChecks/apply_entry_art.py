"""Extract original entry artwork. Source coordinates are top-left pixels.
Native layout evidence is in entry/layout.json; local account services remain adapters.
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
for name,file,rect in [('Background','logo6.bmp',None),('LoginAtlas','login.bmp',None),('LoginPanel','login.bmp',(9,9,206,96)),('Connect','login.bmp',(22,69,73,86)),('Join','login.bmp',(77,69,113,86)),('Exit','login.bmp',(157,69,194,86))]:
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
# Local account form: original signup opened a browser, so this is explicitly an adaptation.
# Reuse only unprinted native paper and original confirm/cancel button states.
registration=Image.new('RGBA',canvas.size,(0,0,0,0))
parts,atlas=libs['.lce']
for v in scene['decor']:
 if 158<=v['id']<=164:
  a=parts[v['id']];patch=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
  for y in range(v['repeat_y']):
   for x in range(v['repeat_x']):registration.alpha_composite(patch,(v['x']+x*a['width'],v['y']+y*a['height']))
registration.putdata([(*p[:3],0 if p[:3]==(0,0,0) else p[3]) for p in registration.getdata()])
path=out/'RegistrationPanel.png';registration.save(path);meta(path)
rows.append(dict(asset=str(path.relative_to(ROOT)),source='Wlib/scene24.lsc background decor158..164 only',size=list(registration.size),adaptation=True,pixelSha256=hashlib.sha256(registration.tobytes()).hexdigest()))
a=parts[185];field=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
field_out=Image.new('RGBA',(210,22))
xs=[(0,2,0,2),(2,field.width-4,2,206),(field.width-2,2,208,2)]
ys=[(0,2,0,2),(2,field.height-4,2,18),(field.height-2,2,20,2)]
for sx,sw,dx,dw in xs:
 for sy,sh,dy,dh in ys:field_out.alpha_composite(field.crop((sx,sy,sx+sw,sy+sh)).resize((dw,dh),Image.Resampling.NEAREST),(dx,dy))
path=out/'RegistrationField.png';field_out.save(path);meta(path)
rows.append(dict(asset=str(path.relative_to(ROOT)),source='Wlib lce185 border-preserving210x22 adaptation',size=[210,22],adaptation=True,pixelSha256=hashlib.sha256(field_out.tobytes()).hexdigest()))
parts,atlas=libs['.ltn']
for label,button in [('Confirm',75),('Cancel',53)]:
 a=parts[button]
 for state in range(3):
  patch=atlas.crop((a['x']+state*a['width'],a['y'],a['x']+(state+1)*a['width'],a['y']+a['height']))
  patch.putdata([(*p[:3],0 if p[:3]==(0,0,0) else p[3]) for p in patch.getdata()])
  path=out/f'Registration{label}_{state}.png';patch.save(path);meta(path)
  rows.append(dict(asset=str(path.relative_to(ROOT)),source=f'Wlib ltn{button} state{state}',size=list(patch.size),pixelSha256=hashlib.sha256(patch.tobytes()).hexdigest()))
# Historical panel sample retained for the extraction audit; runtime no longer covers the action strip.
color=Image.open(files['login.bmp']).convert('RGB').getpixel((15,65))
report=dict(assets=rows,panelFillRGB=color,placement='Native stage-zero login, origin226,279, four original button positions and native-size hover/press atlas patches; no extra title buttons or panel fill. See entry/layout.json.',removed='Original web signup adapted to the local account form; local server selection retained. Retired billing and unconfirmed original mode backend are inactive.',unityPlay=False)
(ROOT/'Tools/LateVersionChecks/entry_art_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(assets=len(rows),panelFillRGB=color)))
