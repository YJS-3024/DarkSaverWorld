"""Check serialized map scenes against original MRC positions and render passes.
Requires UnityPy (offline asset inspection; does not run Unity or rewrite scenes).
Run from any folder: python Tools/MapChecks/check_render_depth.py
"""
from pathlib import Path
import json
import struct
import sys

import UnityPy

ROOT = Path(__file__).resolve().parents[2]
SCENES = ROOT / 'Assets/_Game/Scenes/Maps'
MAPS = ROOT / '분석/Saver_1.2/extracted/Saver_Files/Saver/MAP'


def check_scene(path):
    key = {'Brugos01_Map': '01', 'Kaosia_Map': '1000', 'World1010_Map': '1010'}.get(path.stem)
    if key is None:
        key = path.stem.split('_', 2)[1]
    raw = (MAPS / (key + '.mrc')).read_bytes()
    resources = struct.unpack_from('<i', raw)[0]
    width, height, count = struct.unpack_from('<iii', raw, 4 + resources * 4)
    start = 16 + resources * 4 + 7 * width * height
    if len(raw) != start + 16 * count:
        raise ValueError('MRC length differs: ' + key)
    expected = {'Object_' + format(i, '03'): struct.unpack_from('<ii', raw, start + i * 16 + 4)
                for i in range(count)}
    env = UnityPy.load(str(path))
    objects = {o.path_id: o.read_typetree() for o in env.objects
               if o.type.name in ('GameObject', 'Transform')}
    tiles, sprites, errors = {}, {}, []
    for obj in env.objects:
        if obj.type.name not in ('SpriteRenderer', 'TilemapRenderer'):
            continue
        renderer = obj.read_typetree()
        game_object = objects[renderer['m_GameObject']['m_PathID']]
        name = game_object['m_Name']
        if obj.type.name == 'TilemapRenderer':
            if name in tiles:
                errors.append('duplicate tile layer: ' + name)
            tiles[name] = renderer
            continue
        if name not in expected or name in sprites:
            errors.append('unknown/duplicate map object: ' + name)
            continue
        sprites[name] = renderer
        index = int(name[7:])
        if renderer['m_SortingOrder'] != 30000 + index:
            errors.append('foreground record order: ' + name)
        transforms = [objects[c['component']['m_PathID']] for c in game_object['m_Component']
                      if c['component']['m_PathID'] in objects
                      and 'm_LocalPosition' in objects[c['component']['m_PathID']]]
        x, y = expected[name]
        if len(transforms) != 1 or transforms[0]['m_LocalPosition'] != {'x': float(x), 'y': float(height - y), 'z': 0.0}:
            errors.append('MRC position: ' + name)
        elif transforms[0]['m_Father']['m_PathID']:
            parent_id = transforms[0]['m_Father']['m_PathID']
            while parent_id:
                parent = objects[parent_id]
                if (parent['m_LocalPosition'] != dict(x=0.0, y=0.0, z=0.0)
                        or parent['m_LocalScale'] != dict(x=1.0, y=1.0, z=1.0)
                        or parent['m_LocalRotation'] != dict(x=0.0, y=0.0, z=0.0, w=1.0)):
                    errors.append('nonidentity map parent: ' + name)
                parent_id = parent['m_Father']['m_PathID']
    if set(sprites) != set(expected):
        errors.append('MRC object set differs')
    if set(tiles) != {'Ground', 'Detail', 'TerrainAttributes'}:
        errors.append('tile layer set differs')
    else:
        for name, order in [('Ground', 0), ('Detail', 10)]:
            if tiles[name]['m_SortingOrder'] != order or not tiles[name]['m_Enabled']:
                errors.append('tile pass: ' + name)
        if tiles['TerrainAttributes']['m_Enabled']:
            errors.append('terrain debug renderer visible')
        if len({r['m_SortingLayerID'] for r in [tiles['Ground'], tiles['Detail'], *sprites.values()]}) != 1:
            errors.append('mixed sorting layers')
    return dict(scene=str(path.relative_to(ROOT)), map=key, objects=count, errors=errors)


def main():
    generated=[];serialized=[];interiors=[]
    for path in sorted(SCENES.glob('*.unity')):
        if path.read_bytes()[:5]==b'%YAML' and 'ShopInteriorScene' in path.read_text():interiors.append(str(path.relative_to(ROOT)))
        elif path.read_bytes()[:5]==b'%YAML' and 'NativeTownScene' in path.read_text():generated.append(str(path.relative_to(ROOT)))
        else:serialized.append(path)
    rows = [check_scene(path) for path in serialized]
    report = dict(scenes=len(rows), objects=sum(r['objects'] for r in rows),
                  errors=sum(len(r['errors']) for r in rows), unity_play_verified=False,
                  scope='Serialized scene/MRC render order and placement; not Unity import or visual playback.',
                  native_shop_alias_scenes=interiors,runtime_generated_scenes=generated,runtime_resource_reports=['four_towns_check.json','world_fields_check.json','shop_interiors_check.json'],maps=rows)
    (Path(__file__).parent / 'render_depth_check.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'maps'}, ensure_ascii=False))
    if not rows or report['errors']:
        sys.exit(1)


if __name__ == '__main__':
    main()
