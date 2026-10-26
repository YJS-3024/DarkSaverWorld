"""Build/check native status crops for static field actors (GameCtl 0x10005410)."""
import argparse
import csv
import hashlib
import json
import re
import uuid
from pathlib import Path
from PIL import Image
# Same stable asset namespace as build_scenario01, without its UnityPy dependency.
ROOT = Path(__file__).resolve().parents[2]
def guid(path):
    return uuid.uuid5(uuid.UUID('23b3f21a-85ac-47cb-a63f-c304722b82c8'), str(path.relative_to(ROOT))).hex


def crop(source, sprite_id):
    image = Image.open(source).convert('RGBA')
    size = 64 if sprite_id // 100 == 7 else 32
    assert image.width >= size * 2 and image.height >= size * 2
    result = image.crop((size, size, size * 2, size * 2))
    result.putdata([(*p[:3], 0 if p[:3] == (0, 0, 0) else 255) for p in result.getdata()])
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    output = ROOT / 'Assets/_Game/Resources/UI/UnitStatusPictures'
    template = (ROOT / 'Assets/_Game/Art/Converted/Monsters/Kisra701_Down_1.png.meta').read_text()
    source_root = ROOT / '분석/Saver_Late/decoded_art/gameres'
    sources = {int(p.stem[:-1]): p for p in source_root.glob('char*/*.bmp')
               if p.stem[:-1].isdigit() and p.stem[-1:].lower() == 'r'}
    jobs = {int(r['직업']): int(r['직그림']) for r in csv.DictReader(
        (ROOT / '분석/Saver_Late/unity_catalogs/monsters.csv').open(encoding='utf-8-sig'))}
    if not args.check:
        output.mkdir(parents=True, exist_ok=True)
        folder_meta = Path(str(output) + '.meta')
        if not folder_meta.exists():
            folder_meta.write_text('fileFormatVersion: 2\nguid: ' + guid(output) + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
    records = []
    for job in (301, 302, 303, 304):
        sprite_id = jobs[job]
        source = sources[sprite_id]
        expected = crop(source, sprite_id)
        target = output / (str(job) + '.png')
        if not args.check:
            expected.save(target)
            meta = re.sub(r'^guid: \w+', 'guid: ' + guid(target), template, count=1, flags=re.M)
            # Single-sprite mode imports the whole crop; clear unused multi-sprite entries.
            import yaml
            fields = yaml.safe_load(meta)
            importer = fields['TextureImporter']
            importer['internalIDToNameTable'] = []
            importer['spriteMode'] = 1
            importer['spritePixelsToUnits'] = 32
            importer['spriteMeshType'] = 0
            importer['spriteSheet']['sprites'] = []
            importer['spriteSheet']['spriteID'] = guid(target)
            importer['spriteSheet']['nameFileIdTable'] = {}
            for platform in importer.get('platformSettings', []):
                platform['textureCompression'] = 0
            Path(str(target) + '.meta').write_text(yaml.safe_dump(fields, sort_keys=False))
        actual = Image.open(target).convert('RGBA')
        assert actual.size == expected.size and actual.tobytes() == expected.tobytes(), job
        assert Path(str(target) + '.meta').exists()
        records.append(dict(job=job, spriteId=sprite_id, source=str(source.relative_to(ROOT)),
                            sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                            sourceRect=[32, 32, 64, 64], output=str(target.relative_to(ROOT))))
    # Existing animator frames used by normal and large status windows must match the fixed crops.
    for sprite_id, target in ((100, ROOT / 'Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png'),
                              (701, ROOT / 'Assets/_Game/Art/Converted/Monsters/Kisra701_Down_1.png')):
        expected = crop(sources[sprite_id], sprite_id)
        actual = Image.open(target).convert('RGBA')
        assert actual.size == expected.size and actual.tobytes() == expected.tobytes(), sprite_id
    report = dict(staticActors=records, animatorSamples=[100, 701], fixedSourceFrame='Down[1]',
                  displaySize=[32, 32], unityPlay=False,
                  scope='Exact source crops/color key and existing frame pixels; not Unity import/rendering.')
    (ROOT / 'Tools/MapChecks/unit_status_pictures_check.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print('PASS native fixed status crops: four static actors; 32px fighter and 64px large-unit frames')


if __name__ == '__main__':
    main()
