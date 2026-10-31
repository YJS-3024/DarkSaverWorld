"""Build only the recovered waiting-room lesson's original assets."""
import csv
import hashlib
import io
import json
import struct
import sys
import tempfile
from pathlib import Path
from PIL import Image
from decode_visuals import read_mrc, read_grs, grs_image, read_lsc, read_ui_library


def build(root, native, save, register, key, battle_map):
    base = root / 'Assets/_Original'
    source = json.loads((root / '분석/Saver_Late/waiting_tutorial_resources.json').read_text())
    m = read_mrc(native('map/1500.mrc'))
    grs = {rid: read_grs(native(f'map/mdsr{rid}.grs')) for rid in m['ids']}
    ground = Image.new('RGBA', (m['width'] * 32, m['height'] * 32))
    for i, (rid, group, detail) in enumerate(zip(m['a'], m['b'], m['tiles'])):
        x, y = i % m['width'] * 32, i // m['width'] * 32
        g = grs[rid]['groups'][group]
        ground.alpha_composite(grs_image(g['palette'], g['images'][0]), (x, y))
        if detail[1] != 255:
            g = grs[detail[0]]['groups'][detail[1]]
            ground.alpha_composite(grs_image(g['palette'], g['images'][g['tiles'][detail[2]]['frames'][0]], True), (x, y))
    for s in m['sprites']:
        g = grs[s['resource']]['sprites'][s['index']]
        ground.alpha_composite(grs_image(g['palette'], g['images'][s['frame']], True), (s['x'] * 32, s['y'] * 32))
    fields = {'waitingGround': save('WaitingGround1500', ground, 'map/1500.mrc literal base/detail/decor frames')}
    panels, minimized, layouts = [], [], []
    with tempfile.TemporaryDirectory(prefix='ds-waiting-wlib-') as temp:
        libraries = {}
        for ext in ['lce', 'ltn', 'lnm']:
            p = Path(temp) / f'waitlib.{ext}'
            p.write_bytes(native(f'wlib/waitlib.{ext}').read_bytes())
            libraries[ext] = read_ui_library(p)[:2]

        def compose(name):
            d = read_lsc(native('wlib/' + name + '.lsc'))
            im = Image.new('RGBA', (d['width'], d['height']))
            for v in d['decor']:
                parts, atlas = libraries['lce']; a = parts[v['id']]
                piece = atlas.crop((a['x'], a['y'], a['x'] + a['width'], a['y'] + a['height']))
                for y in range(v['repeat_y']):
                    for x in range(v['repeat_x']):
                        im.alpha_composite(piece, (v['x'] + x * a['width'], v['y'] + y * a['height']))
            buttons = []
            for v in d['buttons']:
                parts, atlas = libraries['lnm' if v['kind'] == 'animation' else 'ltn']; a = parts[v['id']]
                if v['kind'] == 'animation':
                    im.alpha_composite(atlas.crop((a['x'], a['y'], a['x'] + a['width'], a['y'] + a['height'])), (v['x'], v['y']))
                else:
                    buttons.append(dict(id=v['id'], x=v['x'], y=v['y'], sx=a['x'], sy=a['y'], width=a['width'], height=a['height']))
            return d, im, buttons

        fields['waitingButtons'] = save('WaitingButtons', key(libraries['ltn'][1]), 'wlib/waitlib.ltn original three-state button atlas')
        for identifier, scene, x, y in [(18, 2, 180, 75), (19, 3, 20, 195), (20, 4, 115, 220), (21, 6, 300, 200)]:
            d, im, buttons = compose('scene' + str(scene))
            panels.append(save('WaitingWindow' + str(identifier), key(im), 'wlib/scene' + str(scene) + ' literal layout; buttons separate'))
            layouts.append(dict(id=identifier, scene=scene, x=x, y=y, width=d['width'], height=d['height'], buttons=buttons))
            _, mini, buttons = compose('scene' + str(scene) + 'm')
            for b in buttons:
                mini.alpha_composite(libraries['ltn'][1].crop((b['sx'], b['sy'], b['sx'] + b['width'], b['sy'] + b['height'])), (b['x'], b['y']))
            minimized.append(save('WaitingMinimized' + str(identifier), key(mini), 'wlib/scene' + str(scene) + 'm native icon'))
        for field, name in [('waitingDialogue', 'scene38'), ('waitingChatExpanded', 'scene3mm')]:
            _, im, buttons = compose(name)
            fields[field] = save(field[0].upper() + field[1:], key(im), 'wlib/' + name + '.lsc literal layout')
    # Native cursor controller41AA70 loads131 and162..165; state1 selects the latter.
    inventory = json.loads((root / '분석/InstallerVersions/installer_inventory.json').read_text())
    candidates = [Path(f['extractedFile']) for payload in inventory['payloads'] for f in payload.get('files', [])
                  if f.get('file') == 'saver.exe' and f.get('sha256') == source['executableSha256']]
    exe = next(p for p in candidates if p.exists())
    raw = exe.read_bytes(); assert hashlib.sha256(raw).hexdigest() == source['executableSha256']
    sys.path.insert(0, str(root / 'Tools/CombatApChecks/python-deps'))
    import pefile
    pe = pefile.PE(data=raw); cursor = Image.new('RGBA', (160, 32)); cursor_ids = [131, 162, 163, 164, 165]
    bitmaps = next(t.directory.entries for t in pe.DIRECTORY_ENTRY_RESOURCE.entries if t.id == 2)
    for i, number in enumerate(cursor_ids):
        entry = next(r for r in bitmaps if r.id == number).directory.entries[0].data.struct
        dib = pe.get_data(entry.OffsetToData, entry.Size)
        header = struct.unpack_from('<I', dib)[0]; bits = struct.unpack_from('<H', dib, 14)[0]
        colors = struct.unpack_from('<I', dib, 32)[0] or (1 << bits if bits <= 8 else 0)
        bmp = b'BM' + struct.pack('<IHHI', len(dib) + 14, 0, 0, 14 + header + colors * 4) + dib
        im = Image.open(io.BytesIO(bmp)); assert im.size == (32, 32)
        cursor.alpha_composite(key(im), (i * 32, 0))
    fields['waitingCursors'] = save('WaitingCursors', cursor, 'Saver.exe SHA256' + source['executableSha256'] + ' RT_BITMAP131/162/163/164/165')
    for field, job in [('waitingFighterFace', 100), ('waitingKnightFace', 101), ('waitingMagicianFace', 103)]:
        path = base / 'Art/Archives/gameres/face.arc' / (str(job) + '.bmp.png')
        im = Image.open(path); assert im.size == (64, 64)
        fields[field] = save(field[0].upper() + field[1:], im, 'face.arc/' + str(job) + '.bmp native64x64')
    item_rows = {int(r['번호']): r for r in csv.DictReader((base / 'Data/Tables/items.csv').open(encoding='utf-8-sig'))}
    atlas = Image.open(base / 'Art/Archives/gameres/item.arc/item.bmp.png')
    icons = Image.new('RGBA', (96, 32))
    for i, number in enumerate([1, 762, 777]):
        picture = int(item_rows[number]['그림1']); x = picture % 100 * 32; y = picture // 100 * 32
        icons.alpha_composite(key(atlas.crop((x, y, x + 32, y + 32))), (i * 32, 0))
    fields['waitingItems'] = save('WaitingItems', icons, 'items.csv 그림1; item.bmp original100-column32px cells:1/762/777')
    # UserUnit10004F51..10004F60 maps raw FF to attribute0 before path lookup.
    # Masking FF to127 here would turn every indoor floor tile into a wall.
    definition = dict(map=dict(width=m['width'], height=m['height'], attributes=[0 if t[3] == 255 else t[3] & 127 for t in m['tiles']],
                              terrain=battle_map['terrain'], abilities=battle_map['abilities']),
                      lines=[dict(id=r['id'], text=r['text'], skipped=r['skipped']) for r in source['resources']], windows=layouts)
    p = base / 'Data/Tutorial/Waiting1500.json'
    p.write_bytes((json.dumps(definition, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
    fields['waitingDefinition'] = register(p, 'Saver_Late RT_STRING61216..61251 +1500.mrc +Wlib; panel origins presentation defaults')
    return fields, panels, minimized
