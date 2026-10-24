"""Bake native Unity resources for the first complete scenario connection.
Requires UnityPy and Pillow. Runtime never reads the installed game's files.
"""
import hashlib
import json
from pathlib import Path
import re
import uuid
import struct
from collections import deque

from PIL import Image
import UnityPy

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / '분석/Saver_1.2/decoded/all_arc'
OUT = ROOT / 'Assets/_Game/Resources/Scenarios/01'
NAMESPACE = uuid.UUID('23b3f21a-85ac-47cb-a63f-c304722b82c8')


def guid(path):
    return uuid.uuid5(NAMESPACE, str(path.relative_to(ROOT))).hex


def directory(path):
    path.mkdir(parents=True, exist_ok=True)
    if not path.with_name(path.name + '.meta').exists():
        path.with_name(path.name + '.meta').write_text('fileFormatVersion: 2\nguid: ' + guid(path) + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


def commands(file):
    rows = []
    for line, raw in enumerate(file.read_bytes().decode('cp949').splitlines(), 1):
        text = raw.split(';', 1)[0].strip()
        if not text:
            continue
        tokens = text.split()
        group = 0
        if tokens[0].isdigit() and len(tokens) > 1:
            group = int(tokens.pop(0))
        name = tokens.pop(0).upper()
        body = text.split(None, 5)[5] if name == 'MESSAGE' else ''
        args = tokens[:4] if name == 'MESSAGE' else tokens
        rows.append(dict(name=name, args=args, line=line, text=body, group=group))
    return rows


def main():
    for folder in [OUT.parent.parent, OUT.parent, OUT, OUT / 'Characters']:
        directory(folder)
    intro = commands(SOURCE / 'MAP/01set/01.DEO')
    outro = commands(SOURCE / 'MAP/01set/01.DEE')
    events, active = [], None
    for command in commands(SOURCE / 'MAP/01set/01.evt'):
        name = command['name']
        if name == 'EVENT':
            active = dict(id=int(command['args'][0]), scope='', mode='', conditions=[], actions=[])
            events.append(active)
        elif name in ('COMMANDER', 'CHAR'):
            active['scope'] = name
        elif name in ('ONCE', 'CHARONCE', 'INF'):
            active['mode'] = name
        elif command['group']:
            active['conditions'].append(command)
        elif name != 'END':
            active['actions'].append(command)
    plan = json.loads((ROOT / '닥세_리소스/분석결과/싱글판_배치보완안.json').read_text())
    placements = [u for u in plan['units'] if u['scenario'] == '01']
    stats = {}
    for filename in ['MonsterJobCatalog.asset', 'NpcSpecialJobCatalog.asset']:
        for obj in UnityPy.load(str(ROOT / 'Assets/_Game/Runtime/Jobs' / filename)).objects:
            if obj.type.name != 'MonoBehaviour':
                continue
            for entry in obj.read_typetree()['entries']:
                values = entry['numericValues']
                stats[values[0]] = dict(id=values[0], job=values[0], name=entry['jobName'], hp=values[2], mp=values[4],
                    attack=values[6], defense=values[8], magicAttack=values[10], magicDefense=values[12], experience=values[32], x=0, y=0, wave=0)
    units = []
    for row in placements:
        unit = dict(stats[row['ability_id']], id=row['unit'], x=row['x'], y=row['y'], wave=row['spawn_wave'])
        units.append(unit)
    # Validate the supplement against the actual Unity 01 world collision policy,
    # rather than treating the older TileAttb planning column as live collision.
    raw = (ROOT / '분석/Saver_1.2/extracted/Saver_Files/Saver/MAP/01.mrc').read_bytes()
    resource_count = struct.unpack_from('<i', raw)[0]
    width, height, sprite_count = struct.unpack_from('<iii', raw, 4 + resource_count * 4)
    tile_start = 16 + resource_count * 4 + width * height * 2
    attr = lambda x, y: raw[tile_start + 5 * (y * width + x) + 3] & 127
    component, queue = {(14, 32)}, deque([(14, 32)])
    while queue:
        x, y = queue.popleft()
        for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1)]:
            cell = x + dx, y + dy
            if 0 < cell[0] < width - 1 and 0 < cell[1] < height - 1 and cell not in component and attr(*cell) not in (3, 6, 9):
                component.add(cell); queue.append(cell)
    protected = {(14, 32)}
    for event in events:
        protected.update((int(c['args'][0]), int(c['args'][1])) for c in event['conditions'] if c['name'] == 'CHARPOS')
    sprite_start = tile_start + width * height * 5
    protected.update(struct.unpack_from('<ii', raw, sprite_start + i * 16 + 4) for i in range(sprite_count))
    adjustments = []
    for unit in units:
        old = unit['x'], unit['y']
        if old in component:
            continue
        # Source DEO puts actor700 at19,10. This is an authored fallback anchor,
        # not evidence that the original server initially spawned it there.
        anchor = (19, 10) if unit['id'] in (700, 701, 702) else old
        occupied = {(u['x'], u['y']) for u in units if u['wave'] == unit['wave'] and u['id'] != unit['id']}
        candidates = component - protected - occupied
        x, y = min(candidates, key=lambda c: (abs(c[0] - anchor[0]) + abs(c[1] - anchor[1]), c[1], c[0]))
        adjustments.append(dict(unit=unit['id'], fromX=old[0], fromY=old[1], toX=x, toY=y,
            reason='authored supplemental position is not reachable under Unity01 terrain collision'))
        unit['x'], unit['y'] = x, y
    jobs = sorted({u['job'] for u in units} | {int(c['args'][0]) for c in intro + outro if c['name'] == 'CHARPOS' and c['args'][0].isdigit()})
    for job in jobs:
        if stats[job]['hp'] <= 0 or any(stats[job][key] < 0 for key in ['mp', 'attack', 'defense', 'magicAttack', 'magicDefense', 'experience']):
            raise ValueError('Unusable source stat sentinel: ' + str(job))
    definition = dict(version=1, width=40, height=40, entryX=14, entryY=32,
        profileHash=plan['profile_hash'], intro=intro, outro=outro, events=events, units=units,
        characters=[stats[job] for job in jobs], traversalCells=sorted(y * width + x for x, y in component),
        placementAdjustments=adjustments)
    definition['sourceHash'] = hashlib.sha256(json.dumps(definition, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    output = OUT / 'Content.json'
    output.write_text(json.dumps(definition, ensure_ascii=False, indent=2) + '\n')
    if not output.with_name(output.name + '.meta').exists():
        output.with_name(output.name + '.meta').write_text('fileFormatVersion: 2\nguid: ' + guid(output) + '\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    template = (ROOT / 'Assets/_Game/Art/Converted/Monsters/Kisra701_Down_1.png.meta').read_text()
    count = 0
    for job in jobs:
        files = list((SOURCE / 'gameres').glob('char*/' + str(job) + 'R.bmp'))
        if len(files) != 1:
            raise ValueError('Missing/ambiguous character sheet: ' + str(job))
        image = Image.open(files[0]).convert('RGBA')
        size = image.width // 3
        if image.width != size * 3 or image.height < size * 4:
            raise ValueError('Unknown character grid: ' + str(job))
        for row, direction in enumerate(['Up', 'Down', 'Left', 'Right']):
            for frame in range(3):
                crop = image.crop((frame * size, row * size, (frame + 1) * size, (row + 1) * size))
                crop.putdata([(r, g, b, 0 if r == g == b == 0 else 255) for r, g, b, _ in crop.getdata()])
                path = OUT / 'Characters' / f'{job}_{direction}_{frame}.png'
                crop.save(path)
                meta = path.with_name(path.name + '.meta')
                if not meta.exists():
                    text = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid(path), template, flags=re.M)
                    text = re.sub(r'^  spriteID: .+', '  spriteID: ' + uuid.uuid5(NAMESPACE, path.stem).hex, text, flags=re.M)
                    meta.write_text(text)
                count += 1
    print(json.dumps(dict(intro=len(intro), outro=len(outro), events=len(events), units=len(units), character_frames=count,
        sourceHash=definition['sourceHash'], source='original scripts + authored deployment supplement; not original server initial roster'), ensure_ascii=False))


if __name__ == '__main__':
    main()
