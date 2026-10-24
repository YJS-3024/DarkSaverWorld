"""Offline source/resource binding checks. Does not import/play assets in Unity."""
from pathlib import Path
import hashlib
import json
import struct
import sys

from PIL import Image
import UnityPy

ROOT = Path(__file__).resolve().parents[2]
RESOURCE = ROOT / 'Assets/_Game/Resources/Scenarios/01'


def main():
    data = json.loads((RESOURCE / 'Content.json').read_text())
    canonical = dict(data); canonical.pop('sourceHash')
    assert hashlib.sha256(json.dumps(canonical, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest() == data['sourceHash']
    assert (len(data['intro']), len(data['outro']), len(data['events']), len(data['units'])) == (72, 41, 24, 33)
    assert len({u['id'] for u in data['units']}) == 33
    assert all(sum(u['wave'] == wave for u in data['units']) <= 12 for wave in (0, 1, 2))
    assert all(u['y'] * 40 + u['x'] in data['traversalCells'] for u in data['units'])
    frames = 0
    for character in data['characters']:
        job = character['job']
        source = list((ROOT / '분석/Saver_1.2/decoded/all_arc/gameres').glob('char*/' + str(job) + 'R.bmp'))
        assert len(source) == 1
        image = Image.open(source[0]).convert('RGB'); size = image.width // 3
        for row, direction in enumerate(['Up', 'Down', 'Left', 'Right']):
            for frame in range(3):
                path = RESOURCE / 'Characters' / f'{job}_{direction}_{frame}.png'
                assert path.with_name(path.name + '.meta').exists()
                actual = Image.open(path).convert('RGBA')
                expected = image.crop((frame * size, row * size, (frame + 1) * size, (row + 1) * size))
                assert actual.size == expected.size
                for pixel, source_pixel in zip(actual.getdata(), expected.getdata()):
                    assert pixel[:3] == source_pixel and pixel[3] == (0 if source_pixel == (0, 0, 0) else 255)
                frames += 1
    settings = UnityPy.load(str(ROOT / 'ProjectSettings/EditorBuildSettings.asset'))
    scene_rows = next(o.read_typetree()['m_Scenes'] for o in settings.objects if o.type.name == 'EditorBuildSettings')
    enabled = {row['path'] for row in scene_rows if row['enabled']}
    for scene in ['Entry/Start', 'Entry/Brugos01_Play', 'Runtime/GameCore', 'Runtime/Brugos01_Runtime', 'Maps/Brugos01_Map', 'Battle/Battle']:
        assert 'Assets/_Game/Scenes/' + scene + '.unity' in enabled, scene
    required = ['nco01.ogg', 'ncr04.ogg']
    assert all((RESOURCE / 'Music' / file).is_file() and (RESOURCE / 'Music' / (file + '.meta')).is_file() for file in required)
    sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
    from decode_visuals import read_mrc, read_grs, grs_image
    visuals = json.loads((RESOURCE / 'Visuals.json').read_text())
    assert visuals['contentHash'] == data['sourceHash']
    native = ROOT / '닥세_리소스/설치본_추출/Saver_Files/Saver/MAP'
    mrc = read_mrc(native / '01.mrc'); grs = read_grs(native / 'mdsr0.grs')
    sprite = grs['sprites'][1]
    assert Image.open(RESOURCE / 'OpenedChest.png').convert('RGBA').tobytes() == grs_image(sprite['palette'], sprite['images'][0], True).tobytes()
    for chest in visuals['chests']:
        assert chest['sourceIndex'] == next(i for i, item in enumerate(mrc['sprites']) if (item['x'], item['y']) == (chest['x'], chest['y']))
        assert (chest['widthTiles'], chest['heightTiles']) == (1, 1)
    source = ROOT / '분석/Saver_1.2/decoded/all_arc/gameres'
    for face in visuals['portraitIds'] + ['NoFace']:
        candidates = [source / folder / (str(face) + '.bmp') for folder in ['face','face1','face2','face3']]
        original = next((p for p in candidates if p.exists()), source / 'etc/noface.bmp')
        expected = Image.open(original).convert('RGBA'); expected.putalpha(255)
        assert Image.open(RESOURCE / 'Portraits' / (str(face) + '.png')).convert('RGBA').tobytes() == expected.tobytes()
    attribute = UnityPy.load(str(RESOURCE / 'ChestTerrain22.asset'))
    tile = next(o.read_typetree() for o in attribute.objects if o.type.name == 'MonoBehaviour')
    assert tile['m_Name'] == 'Terrain_22' and tile['m_ColliderType'] == 0
    assert len(visuals['chests']) == 7
    report = dict(source_hash=data['sourceHash'], intro=72, outro=41, events=24, authored_enemies=33,
        source_character_frames=frames, reachable_deployments=33, placement_adjustments=data['placementAdjustments'],
        required_build_scenes=6, music_clips=required,
        scope='Offline source pixels, resource bindings, build-list entries and authored deployment checks; not Unity import or PlayMode.',
        source_portraits=len(visuals['portraitIds']), opened_chest_bindings=len(visuals['chests']),
        undefined_source_magic=[1],
        presentation_gaps=['Actual Unity import, dialogue window chrome/input, and full sequence playback/timing remain unverified.'])
    (Path(__file__).parent / 'scenario01_resource_check.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k not in ['placement_adjustments', 'presentation_gaps']}, ensure_ascii=False))


if __name__ == '__main__':
    main()
