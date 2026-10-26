"""Extract native Wlib room window and notice artwork; no Unity Editor required."""
from pathlib import Path
import sys,json,hashlib,uuid,re
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from analyze_storage_screenshots import render_ui
from decode_visuals import read_lsc,read_ui_library
out=ROOT/'Assets/_Game/Resources/UI/ScenarioRooms';out.mkdir(exist_ok=True)
template=(ROOT/'Assets/_Game/Resources/UI/Entry/LoginPanel.png.meta').read_text()
def meta(p,folder=False):
 q=p.with_name(p.name+'.meta')
 if q.exists():return
 g=uuid.uuid5(uuid.NAMESPACE_URL,str(p.relative_to(ROOT))).hex
 q.write_text('fileFormatVersion: 2\nguid: '+g+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n' if folder else re.sub(r'guid: [0-9a-f]{32}','guid: '+g,template,count=1))
def png(p,im):im.save(p);meta(p)
meta(out,True)
root=ROOT/'닥세_리소스/설치본_추출/Saver_Files/Saver/Wlib'
scene=read_lsc(root/'scene12.lsc');png(out/'Background.png',render_ui(12))
parts,atlas,*_=read_ui_library(root/'waitlib.ltn');controls=[]
for v in scene['buttons']:
 a=parts[v['id']]
 for state in range(3):
  x=a['x']+state*a['width'];im=atlas.crop((x,a['y'],x+a['width'],a['y']+a['height'])).convert('RGBA')
  im.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else z) for r,g,b,z in im.getdata()]);png(out/f'Button{v["id"]}_{state}.png',im)
 controls.append(dict(id=v['id'],x=v['x'],y=v['y'],width=a['width'],height=a['height']))
# Guide artwork is a native Down frame; placement/role is the local single-player adaptation.
p=next(p for p in (ROOT/'분석/Saver_1.2/decoded/all_arc/gameres').rglob('*') if p.name.lower()=='807r.bmp');sheet=Image.open(p).convert('RGBA');size=sheet.width//3;im=sheet.crop((size,size,2*size,2*size));im.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in im.getdata()]);png(out/'Guide.png',im)
png(out.parent/'ScenarioNotice.png',render_ui(28))
p=out/'Layout.json';p.write_text(json.dumps(dict(width=scene['width'],height=scene['height'],controls=controls),separators=(',',':'))+'\n')
p.with_name(p.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,str(p.relative_to(ROOT))).hex+'\nTextScriptImporter:\n  externalObjects: {}\n')
print('PASS Wlib scene12',scene['width'],scene['height'],'controls',len(controls),'and native notice28; no map/save edits')
