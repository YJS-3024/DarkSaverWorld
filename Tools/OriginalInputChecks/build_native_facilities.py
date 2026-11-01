"""Convert source Wlib18/20 and original898 ghost sheet. No server rules."""
from pathlib import Path
import hashlib,json,runpy
from PIL import Image
root=Path(__file__).resolve().parents[2]
# Reuse the protected original LSC/atlas parser without executing its catalog pass.
p=root/'Tools/OriginalInputChecks/build_world_ui.py'
ns={'__file__':str(p)}
exec(compile(p.read_text().split('names={')[0],str(p),'exec'),ns)
out=root/'Assets/_Game/Resources/UI/NativeFacilities';out.mkdir(exist_ok=True)
ns['OUT']=out;ns['meta'](out,'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
rows=[]
for scene,title in [(18,'은행'),(20,'창고')]:
 w,h,bg,cs=ns['compose'](scene)
 rows.append((scene,title,w,h,bg,cs))
yaml=['%YAML 1.1','%TAG !u! tag:unity3d.com,2011:','--- !u!114 &11400000','MonoBehaviour:','  m_ObjectHideFlags: 0','  m_Script: {fileID: 11500000, guid: bffdb6994f0ba0243bf3476b846a2ca4, type: 3}','  m_Name: NativeFacilities','  windows:']
for scene,title,w,h,bg,cs in rows:
 yaml += [f'  - sceneId: {scene}',f'    title: {title}',f'    background: {bg}',f'    size: {{x: {w}, y: {h}}}','    controls:']
 for n,l,t,cw,ch,frames in cs:
  yaml += [f'    - nativeId: {n}',f'      rect: {{serializedVersion: 2, x: {l}, y: {t}, width: {cw}, height: {ch}}}','      frames:']+['      - '+f for f in frames]
asset=out/'NativeFacilities.asset';ns['put'](asset,'\n'.join(yaml)+'\n');ns['meta'](asset,'NativeFormatImporter:\n  mainObjectFileID: 11400000\n')
source=root/'분석/Saver_1.2/decoded/all_arc/gameres/char0/898r.bmp'
im=Image.open(source).convert('RGBA');assert im.size==(96,128)
ns['texture']('Ghost898',im)
report=dict(layouts=ns['proof'],ghost=dict(source=str(source.relative_to(root)),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),width=96,height=128,cell=32,rows=['up','down','nativeLeft','nativeRight']),textures=ns['textures'])
Path(__file__).with_name('native_facilities_conversion.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('PASS: original18/20 controls and898 sheet converted; edited outputs protected')
