"""Build entry UI only from verified native atlases/LSC; do not run legacy generators."""
import hashlib
import importlib.util
import io
import json
import re
import sys
import tempfile
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Assets/_Original'
NATIVE = BASE / 'SourceData/Native'
OUT = BASE / 'Art/Entry'
DATA = BASE / 'Data/Entry'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    spec = importlib.util.spec_from_file_location('original_meta', ROOT / 'Tools/OriginalRestart/reset_assets.py')
    metadata = importlib.util.module_from_spec(spec); spec.loader.exec_module(metadata)
    sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
    from decode_visuals import read_lsc, read_ui_library
    canonical = {r['path']: r['sha256'] for r in json.loads((ROOT / '분석/Saver_Late/files.json').read_text())}
    inputs = ['logo6.bmp', 'login.bmp', 'login2.bmp', 'wlib/scene24.lsc', 'wlib/waitlib.lce', 'wlib/waitlib.ltn', 'wlib/waitlib.lnm']
    for name in inputs:
        assert sha((NATIVE / (name + '.bytes')).read_bytes()) == canonical[name], name
    OUT.mkdir(parents=True, exist_ok=True); DATA.mkdir(parents=True, exist_ok=True)
    report = dict(version=1, sourceVersion='Saver_Late', sources={n: canonical[n] for n in inputs}, outputs=[],
                  sourceScope='Native pixel crops, LSC composition and confirmed RGB black color key; no invented UI theme.',
                  localAdaptations=['The original two server-name fields are blanked using native interior pixels; one response-owned local server name is rendered.'],
                  geometry={'logicalSize': [640, 480], 'loginOrigin': [226, 279], 'serverOrigin': [277, 276],
                            'serverSourceRect': [10, 320, 110, 424], 'creationSize': [397, 300],
                            'creationOrigin': [121, 90], 'creationOriginEvidence': 'Centered initial397x300 window in640x480 per direct test. Dlg45 profile0,0 alone does not establish absolute screen coordinates.',
                            'creationCards': [[37, 54, 127, 230], [153, 54, 243, 230], [268, 54, 358, 230]],
                            'nameRect': [254, 241, 342, 256], 'confirmOrigin': [300, 265]},
                  staticEvidence={'executableSha256': 'dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',
                                  'serverPanel': '0x40EF53..0x40EFD7', 'serverHitTests': '0x40EFDF..0x40F05C',
                                  'creationConstructor': '0x45BCD0..0x45C015', 'creationPortraitDraw': '0x45C2EB..0x45C343',
                                  'creationJobs': '0x45BE54..0x45BF2A', 'defaultWindowProfile': '0x4348E3..0x434A26; default string0x4900B4'},
                  limitations=['Original GDI font rasterization not reproduced exactly.', 'Notification screen origin is a Unity presentation placement, not a recovered original global origin.', 'No original server account protocol opcode/packing or server creation stats inferred.'])

    def keyed(image):
        image = image.convert('RGBA')
        image.putdata([(*p[:3], 0 if p[:3] == (0, 0, 0) else 255) for p in image.getdata()])
        return image

    def save(name, image, origin):
        p = OUT / (name + '.png'); image.save(p); metadata.meta(p, ROOT / 'Assets')
        report['outputs'].append(dict(asset=p.relative_to(ROOT).as_posix(), source=origin, sha256=sha(p.read_bytes()),
                                      pixelSha256=sha(image.tobytes()), width=image.width, height=image.height))
        return p

    login = Image.open(io.BytesIO((NATIVE / 'login.bmp.bytes').read_bytes())).convert('RGBA')
    secondary = Image.open(io.BytesIO((NATIVE / 'login2.bmp.bytes').read_bytes())).convert('RGBA')
    pixels = secondary.load()
    ink = Counter(pixels[x, y][:3] for y in range(322, 336) for x in range(130, 188)
                  if pixels[x, y][2] > pixels[x, y][0] and pixels[x, y][2] > pixels[x, y][1] and pixels[x, y][0] < 170)
    server_color = ink.most_common(1)[0][0]
    report['serverNameColor'] = dict(rgb=list(server_color), source='login2.bmp(130,322,188,336), dominant native glyph color')
    save('Login', keyed(login), 'login.bmp; native black color key')
    save('Secondary', keyed(secondary), 'login2.bmp; native black color key')
    panel = secondary.crop((10, 320, 110, 424))
    # Remove only the baked online names, preserving the native framing and arrow.
    # The blank strip is sampled from the same row; this is explicitly a local list adaptation.
    for left, top, right, bottom in [(23, 34, 83, 50), (23, 54, 83, 70)]:
        strip = panel.crop((83, top, 84, bottom)).resize((right-left, bottom-top), Image.Resampling.NEAREST)
        panel.paste(strip, (left, top))
    save('ServerPanel', keyed(panel), 'login2.bmp(10,320,110,424), native row interiors blanked for local server response')
    selected = secondary.crop((118, 320, 193, 339))
    selected.paste(selected.crop((72, 1, 73, 18)).resize((60, 17), Image.Resampling.NEAREST), (12, 1))
    save('ServerSelected', keyed(selected), 'login2.bmp(118,320,193,339), baked online name blanked; native destination290,307')
    with tempfile.TemporaryDirectory(prefix='ds-entry-wlib-') as temporary:
        directory = Path(temporary)
        for suffix in ['.lce', '.ltn', '.lnm']:
            (directory / ('waitlib' + suffix)).write_bytes((NATIVE / ('wlib/waitlib' + suffix + '.bytes')).read_bytes())
        libraries = {suffix: read_ui_library(directory / ('waitlib' + suffix))[:2] for suffix in ['.lce', '.ltn', '.lnm']}
        scene = read_lsc(NATIVE / 'wlib/scene24.lsc.bytes')
        canvas = Image.new('RGBA', (scene['width'], scene['height']))
        for d in scene['decor']:
            parts, atlas = libraries['.lce']; p = parts[d['id']]
            piece = atlas.crop((p['x'], p['y'], p['x']+p['width'], p['y']+p['height']))
            for y in range(d['repeat_y']):
                for x in range(d['repeat_x']): canvas.alpha_composite(piece, (d['x']+x*p['width'], d['y']+y*p['height']))
        for d in scene['buttons']:
            parts, atlas = libraries['.ltn' if d['kind'] == 'button' else '.lnm']; p = parts[d['id']]
            canvas.alpha_composite(atlas.crop((p['x'], p['y'], p['x']+p['width'], p['y']+p['height'])), (d['x'], d['y']))
        save('Creation', keyed(canvas), 'wlib/scene24.lsc + waitlib, literal decor repeats and normal button frames')
        save('CreationButtons', keyed(libraries['.ltn'][1]), 'wlib/waitlib.ltn bitmap, native black color key')
        p = libraries['.ltn'][0][75]
        assert (p['x'], p['y'], p['width'], p['height']) == (276, 399, 46, 19)

    for p in sorted(BASE.rglob('*')):
        if p.is_file() and p.suffix == '.cs' and not Path(str(p)+'.meta').exists(): metadata.meta(p, ROOT / 'Assets')

    def guid(path):
        return re.search(r'^guid: ([0-9a-f]{32})$', Path(str(path)+'.meta').read_text(), re.M)[1]

    art_path = DATA / 'NativeEntryArt.asset'
    art_guid = guid(BASE / 'Runtime/Entry/NativeEntryArt.cs')
    fields = {'background': BASE/'Art/Native/logo6.bmp.png', 'login': OUT/'Login.png', 'secondary': OUT/'Secondary.png',
              'creation': OUT/'Creation.png', 'buttons': OUT/'CreationButtons.png', 'serverPanel': OUT/'ServerPanel.png', 'serverSelected': OUT/'ServerSelected.png'}
    text = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+art_guid+', type: 3}\n  m_Name: NativeEntryArt\n  m_EditorClassIdentifier: \n'
    for key, path in fields.items(): text += '  '+key+': {fileID: 2800000, guid: '+guid(path)+', type: 3}\n'
    text += '  serverNameColor: {r: '+str(server_color[0]/255)+', g: '+str(server_color[1]/255)+', b: '+str(server_color[2]/255)+', a: 1}\n'
    text += '  faces:\n'
    for job in [100,101,103]: text += '  - {fileID: 2800000, guid: '+guid(BASE/f'Art/Archives/gameres/face.arc/{job}.bmp.png')+', type: 3}\n'
    art_path.write_text(text)
    if not Path(str(art_path)+'.meta').exists():
        metadata.meta(art_path, ROOT/'Assets')
        p = Path(str(art_path)+'.meta'); p.write_text('fileFormatVersion: 2\nguid: '+guid(art_path)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    scene_path = BASE / 'Scenes/Start.unity'; scene = scene_path.read_text()
    if 'm_Name: Original Entry\n' not in scene:
        if scene != (ROOT/'Tools/OriginalRestart/foundation/Scenes/Start.unity').read_text():
            raise ValueError('Start scene has other changes; bind the entry component without overwriting them.')
        root_script = guid(BASE/'Runtime/Entry/EntrySceneRoot.cs'); server_script = guid(BASE/'Runtime/Entry/LocalServerManager.cs')
        addition = '''--- !u!1 &2000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2001}
  - component: {fileID: 2002}
  - component: {fileID: 2003}
  m_Layer: 0
  m_Name: Original Entry
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &2001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children: []
  m_Father: {fileID: 0}
'''
        for identifier, script in [(2002,server_script),(2003,root_script)]:
            addition += '--- !u!114 &'+str(identifier)+'\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 2000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+script+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n'
            if identifier == 2003: addition += '  art: {fileID: 11400000, guid: '+guid(art_path)+', type: 2}\n'
        scene = scene.replace('--- !u!1660057539', addition+'--- !u!1660057539').replace('  - {fileID: 1001}\n','  - {fileID: 1001}\n  - {fileID: 2001}\n')
        scene_path.write_text(scene)
    report_path = DATA / 'EntryEvidence.json'; report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n'); metadata.meta(report_path, ROOT/'Assets')
    for p in sorted(BASE.rglob('*')):
        if p.is_dir() and not Path(str(p)+'.meta').exists(): metadata.meta(p, ROOT/'Assets', folder=True)
    report['boundScene'] = scene_path.relative_to(ROOT).as_posix()
    target = ROOT/'Tools/OriginalEntryChecks/entry-assets.json'; target.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(outputs=len(report['outputs']),scene=report['boundScene'],geometry=report['geometry']),ensure_ascii=False))


if __name__ == '__main__':
    main()
