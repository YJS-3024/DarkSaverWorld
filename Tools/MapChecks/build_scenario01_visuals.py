"""Bake visual-only source bindings without invalidating scenario saves."""
import json
import re
import sys
from PIL import Image
from build_scenario01 import ROOT, SOURCE, OUT, directory, guid
sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
from decode_visuals import read_grs, read_mrc, grs_image


def main():
    data = json.loads((OUT / 'Content.json').read_text())
    directory(OUT / 'Portraits')
    commands = data['intro'] + data['outro'] + [c for e in data['events'] for c in e['actions']]
    ids = sorted({int(c['args'][1]) for c in commands if c['name'] == 'MESSAGE' and c['args'][0] != '0' and c['args'][1].isdigit()})
    template = (ROOT / 'Assets/_Game/Resources/UI/CharacterPortraits/NoFace.png.meta').read_text()
    for name in [str(i) for i in ids] + ['NoFace']:
        candidates = [SOURCE / 'gameres' / folder / (name + '.bmp') for folder in ['face', 'face1', 'face2', 'face3']] if name != 'NoFace' else []
        source = next((p for p in candidates if p.exists()), SOURCE / 'gameres/etc/noface.bmp')
        path = OUT / 'Portraits' / (name + '.png')
        image = Image.open(source).convert('RGBA'); image.putalpha(255)
        assert image.size == (64, 64)
        image.save(path)
        template = template.replace('alphaIsTransparency: 1', 'alphaIsTransparency: 0')
        path.with_name(path.name + '.meta').write_text(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid(path), re.sub(r'spriteID: [0-9a-f]{32}', 'spriteID: ' + guid(path), template), count=1))
    native = ROOT / '닥세_리소스/설치본_추출/Saver_Files/Saver/MAP'
    mrc = read_mrc(native / '01.mrc'); grs = read_grs(native / 'mdsr0.grs')
    sprite = grs['sprites'][1]; path = OUT / 'OpenedChest.png'
    grs_image(sprite['palette'], sprite['images'][0], True).save(path)
    template = (ROOT / 'Assets/_Game/Art/Converted/Map01/Sprites/mdsr0_secondary_0_0.png.meta').read_text()
    path.with_name(path.name + '.meta').write_text(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid(path), re.sub(r'spriteID: [0-9a-f]{32}', 'spriteID: ' + guid(path), template), count=1))
    path = OUT / 'ChestTerrain22.asset'
    source = ROOT / 'Assets/_Game/Art/Converted/Map01/Tiles/Terrain_22.asset'
    path.write_bytes(source.read_bytes())
    path.with_name(path.name + '.meta').write_text(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid(path), source.with_name(source.name + '.meta').read_text(), count=1))
    chests = []
    for event in data['events']:
        for command in event['actions']:
            if command['name'] != 'CHANGESPRITE': continue
            x,y,resource,group,frame,attribute = map(int,command['args'])
            assert (resource,group,frame,attribute) == (0,1,0,22)
            index = next(i for i,s in enumerate(mrc['sprites']) if (s['x'],s['y']) == (x,y))
            chests.append(dict(eventId=event['id'],sourceIndex=index,x=x,y=y,widthTiles=sprite['width']//32,heightTiles=sprite['height']//32))
    path = OUT / 'Visuals.json'
    path.write_text(json.dumps(dict(contentHash=data['sourceHash'],chests=chests,portraitIds=ids,undefinedSourceMagic=[1]),ensure_ascii=False,indent=2)+'\n')
    path.with_name(path.name + '.meta').write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    print('Baked',len(ids),'portraits and',len(chests),'first-match chest bindings; logical content hash unchanged.')

if __name__ == '__main__': main()
