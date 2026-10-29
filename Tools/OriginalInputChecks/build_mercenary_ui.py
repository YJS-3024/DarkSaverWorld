"""Convert the original scene9 hire/dismiss window using the shared Wlib decoder."""
from pathlib import Path
import json
source=Path(__file__).with_name('build_world_ui.py').read_text()
namespace={'__file__':__file__}
exec(source.split('names={')[0].replace("UI/WorldWindows'","UI/Mercenary'"),namespace)
w,h,bg,controls=namespace['compose'](9)
assert (w,h)==(223,277) and len(controls)==5
put,meta,OUT=namespace['put'],namespace['meta'],namespace['OUT']
header=source[source.index("yaml=['%YAML"):source.index('for id,title in names.items():')]
exec(header,namespace);lines=namespace['yaml'];lines=[l.replace('m_Name: NativeWorldWindows','m_Name: NativeMercenary') for l in lines]
lines += ['  - sceneId: 9','    title: 용병 리스트',f'    background: {bg}',f'    size: {{x: {w}, y: {h}}}','    minimizedBackground: {fileID: 0}','    minimizedSize: {x: 0, y: 0}','    controls:']
for n,l,t,cw,ch,frames in controls:lines += [f'    - nativeId: {n}',f'      rect: {{x: {l}, y: {t}, width: {cw}, height: {ch}}}','      frames:']+['      - '+f for f in frames]
p=OUT/'NativeMercenary.asset';put(p,'\n'.join(lines)+'\n');meta(p,'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')
Path(__file__).with_name('mercenary_ui_conversion.json').write_text(json.dumps(dict(layouts=namespace['proof'],textures=namespace['textures']),ensure_ascii=False,indent=2)+'\n')
print('PASS: native scene9 223x277; five original controls; three artwork states; protected output.')
