"""Audit the new Assets boundary, source integrity and pre-reset working backup."""
import csv
import hashlib
import io
import json
import re
import sys
import tarfile
from collections import Counter
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def verify_original_sources(index):
    """Compare with independently indexed originals, not just the reset's own hashes."""
    base = ROOT / 'Assets/_Original'
    canonical = {r['path'].lower(): r['sha256'] for r in json.loads((ROOT / '분석/Saver_Late/files.json').read_text())}
    native = {}
    derived = {}
    for row in index['sources']:
        if '/SourceData/Native/' in row['asset']:
            key = row['source'].removeprefix('Saver_Late/')
            path = ROOT / row['asset']
            assert key in canonical and sha(path.read_bytes()) == canonical[key], key
            native[key] = path
        elif row['asset'].endswith('.png') or '/SourceData/Decoded/' in row['asset']:
            derived[row['source'].removeprefix('Saver_Late/')] = row
    # No executable/DLL is imported, and raw libraries/data are not configured gameplay.
    assert not any(Path(key).suffix.lower() in {'.exe', '.dll'} for key in native)
    sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
    from analyze_client import lzh_decode, lzss_decode
    from build_late_saver_catalog import arc_entries
    verified = set()

    def verify_derived(key, raw):
        row = derived[key]
        path = ROOT / row['asset']
        assert sha(raw) == row['originalBytesSha256'], key
        if path.suffix == '.png':
            with Image.open(io.BytesIO(raw)) as original, Image.open(path) as converted:
                assert converted.mode == 'RGB', path
                assert original.size == converted.size, path
                assert original.convert('RGB').tobytes() == converted.tobytes(), path
        else:
            assert path.read_bytes() == raw.decode('cp949').encode('utf-8'), path
        verified.add(key)

    for key, path in native.items():
        if key in derived:
            verify_derived(key, path.read_bytes())
        if key.endswith('.arc'):
            for name, method, size, packed in arc_entries(path):
                source = key + '/' + name.lower()
                if source not in derived:
                    continue
                assert method in (0, 1)
                raw = (lzh_decode if method == 0 else lzss_decode)(packed, size)
                assert len(raw) == size
                verify_derived(source, raw)
    assert verified == set(derived), sorted(set(derived) - verified)

    def tokens(name):
        text = (base / 'SourceData/Decoded/gameres/set.arc' / (name + '.txt')).read_text()
        return [line.split() for line in text.splitlines() if re.match(r'^\s*\d+\s', line)]

    def csv_rows(name):
        with (base / 'Data/Tables' / name).open(encoding='utf-8-sig', newline='') as stream:
            rows = list(csv.reader(stream))
        return rows[0], rows[1:]

    # Every table value comes from native ATR columns. Category fields are analysis
    # annotations; do not equate those names with original server behavior.
    ability = [r[:36] + [' '.join(r[36:])] for r in tokens('ability.atr')]
    names = {int(r[0]): r[35] for r in ability}
    classifiers = {'units_all.csv': lambda n: True, 'jobs.csv': lambda n: n < 200,
                   'mercenaries.csv': lambda n: 200 <= n < 300,
                   'monsters.csv': lambda n: 300 <= n < 800,
                   'npcs_and_special.csv': lambda n: n >= 800}
    tables = {}
    for name, include in classifiers.items():
        header, rows = csv_rows(name)
        assert header[-1] == 'category'
        expected = [r for r in ability if include(int(r[0]))]
        assert [r[:-1] for r in rows] == expected, name
        tables[name] = len(rows)
    header, rows = csv_rows('items.csv')
    assert header[-1] == 'itemCategory'
    assert [r[:-1] for r in rows] == [r[:43] + [' '.join(r[43:])] for r in tokens('itemtbl.atr')]
    tables['items.csv'] = len(rows)
    _, rows = csv_rows('Magictbl.csv')
    assert rows == tokens('magictbl.atr')
    tables['Magictbl.csv'] = len(rows)
    header, rows = csv_rows('experience_progression.csv')
    expected = []
    for r in tokens('levelabl.atr'):
        values = dict(jobId=r[0], jobName=names.get(int(r[0]), ''), level=r[1], requiredXp=r[15],
                      nextJobId=r[12], rewardGold=r[13], rewardXp=r[14], learnedMagic=r[16],
                      usableSkills=r[17] if len(r) > 17 else '')
        expected.append([values[k] for k in header])
    expected.sort(key=lambda r: (int(r[0]), int(r[2])))
    assert rows == expected
    tables['experience_progression.csv'] = len(rows)

    placement = json.loads((base / 'Data/Placements/scenario_monster_placements.json').read_text())
    total = 0
    flat = []
    for scenario in placement['scenarios']:
        def literal(kind):
            source = scenario[kind].split('/decoded/', 1)[1]
            path = base / 'SourceData/Decoded' / (source + '.txt')
            result = {}
            for number, line in enumerate(path.read_text().splitlines(), 1):
                body = line.split(';', 1)[0].split()
                if body and all(re.fullmatch(r'-?\d+', value) for value in body):
                    values = list(map(int, body))
                    assert values[0] not in result, (source, values[0])
                    result[values[0]] = (number, values[1:])
            return result
        sets, positions = literal('set'), literal('pos')
        linked = {unit for unit in sets if unit in positions}
        assert linked == {u['unit'] for u in scenario['units']}, scenario['scenario']
        assert len(linked) == scenario['count']
        for unit in scenario['units']:
            line, values = sets[unit['unit']]
            assert (unit['setLine'], unit['job'], unit['extra']) == (line, values[0], values[1:])
            line, values = positions[unit['unit']]
            assert (unit['posLine'], unit['x'], unit['y']) == (line, values[0], values[1])
            stats = next(r for r in ability if int(r[0]) == unit['job'])
            assert unit['name'] == stats[35] and unit['picture'] == int(stats[1])
            flat.append((str(scenario['scenario']), unit['unit'], unit['job'], unit['x'], unit['y']))
            total += 1
    with (base / 'Data/Placements/scenario_monster_placements.csv').open(encoding='utf-8-sig', newline='') as stream:
        rows = list(csv.DictReader(stream))
    assert Counter((r['scenario'], int(r['unit']), int(r['job']), int(r['x']), int(r['y'])) for r in rows) == Counter(flat)
    assert total == 610 and len(placement['scenarios']) == 16
    return dict(scope='Original copied/derived data only; new entry/local hosting code is checked separately', nativeFilesComparedToCanonical=len(native), imagesAndTextsRecheckedFromNativeBytes=len(verified),
                nativeAtrTableRows=tables, literalSetPosUnits=total, literalSetPosMaps=16,
                authoredGameplayValuesDetected=0,
                derivedAnnotations=['Unit category labels', 'Item category labels', 'CSV field aliases; absent optional token stays empty'],
                newWorkspaceDefaults={'cameraOrthographicSize': 7.5, 'spritePixelsToUnits': 32,
                                      'originalGameplayRule': False})


def main():
    assets = ROOT / 'Assets'
    base = assets / '_Original'
    report = json.loads((ROOT / 'Tools/OriginalRestart/reset-report.json').read_text())
    index = json.loads((base / 'SourceIndex.json').read_text())
    entry_manifest = ROOT / 'Tools/OriginalEntryChecks/entry-check.json'
    entry = json.loads(entry_manifest.read_text()) if entry_manifest.exists() else None
    assert sorted(p.name for p in assets.iterdir()) == ['_Original', '_Original.meta']
    assert not (assets / '_Game').exists()
    paths = [p for p in assets.rglob('*') if p.is_file() and p.suffix != '.meta']
    assert not any(p.suffix in {'.prefab', '.dll', '.exe', '.shader', '.uxml', '.uss'} for p in paths)
    tutorial_path = ROOT/'Tools/OriginalTutorialChecks/tutorial-check.json'
    tutorial = json.loads(tutorial_path.read_text()) if tutorial_path.exists() else None
    expected_assets = {base/'Data/Entry/NativeEntryArt.asset'} if entry else set()
    if tutorial: expected_assets.add(base/'Data/Tutorial/NativeTutorialArt.asset')
    assert {p for p in paths if p.suffix == '.asset'} == expected_assets
    assert not any('Resources' in p.relative_to(assets).parts for p in paths)
    scripts = [p for p in paths if p.suffix == '.cs']
    if entry:
        assert {p.relative_to(ROOT).as_posix() for p in scripts} == set(entry['codeSha256'])
        for p in scripts: assert sha(p.read_bytes()) == entry['codeSha256'][p.relative_to(ROOT).as_posix()], p
        assert entry['runtimeApiErrors'] == entry['editorApiErrors'] == entry['playerApiErrors'] == 0
        assert entry['assertions'] >= 76
    else:
        assert [p.relative_to(assets).as_posix() for p in scripts] == ['_Original/Editor/OriginalFoundationSetup.cs']
    for p in scripts:
        script = p.read_text()
        assert 'RuntimeInitializeOnLoadMethod' not in script and 'InitializeOnLoad]' not in script
    scenes = [p for p in paths if p.suffix == '.unity']
    assert len(scenes) == 1 and scenes[0] == base / 'Scenes/Start.unity'
    scene = scenes[0].read_text()
    assert scene.count('MonoBehaviour:') == (3 if tutorial else 2 if entry else 0) and 'Main Camera' in scene
    assert 'orthographic: 1' in scene
    guids = set()
    for p in assets.rglob('*.meta'):
        assert Path(str(p)[:-5]).exists(), p
        match = re.search(r'^guid: ([0-9a-f]{32})$', p.read_text(), re.M)
        assert match and match[1] not in guids, p
        guids.add(match[1])
    for p in assets.rglob('*'):
        if p.suffix != '.meta':
            assert Path(str(p) + '.meta').is_file(), p
    expected_guids = re.findall(r'guid: ([0-9a-f]{32})', (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text())
    assert len(expected_guids) == 1 and expected_guids[0] in guids
    source_paths = set()
    images = native = text = tables = 0
    for row in index['sources']:
        p = ROOT / row['asset']
        assert p.is_relative_to(base) and p.is_file() and row['asset'] not in source_paths
        source_paths.add(row['asset'])
        raw = p.read_bytes()
        assert sha(raw) == row['sha256'], p
        if '/Native/' in row['asset'] and p.suffix == '.bytes':
            assert sha(raw) == row['originalSha256'], p
            native += 1
        if p.suffix == '.png':
            with Image.open(p) as image:
                assert image.size == (row['width'], row['height'])
                assert sha(image.convert('RGB').tobytes()) == row['pixelSha256'], p
            images += 1
        if '/Decoded/' in row['asset']:
            assert row['encoding'] == 'CP949 to UTF-8'
            raw.decode('utf-8')
            text += 1
        if '/Tables/' in row['asset']:
            assert raw == (ROOT / row['source']).read_bytes()
            tables += 1
    other_paths = {p.relative_to(ROOT).as_posix() for p in paths} - source_paths
    expected_other = {'Assets/_Original/SourceIndex.json'} | {
        'Assets/_Original/' + p.relative_to(ROOT / 'Tools/OriginalRestart/foundation').as_posix()
        for p in (ROOT / 'Tools/OriginalRestart/foundation').rglob('*') if p.is_file()
    }
    if entry:
        evidence = json.loads((base/'Data/Entry/EntryEvidence.json').read_text())
        expected_other |= set(entry['codeSha256']) | {r['asset'] for r in evidence['outputs']} | {
            'Assets/_Original/Data/Entry/NativeEntryArt.asset', 'Assets/_Original/Data/Entry/EntryEvidence.json'}
    if tutorial:
        tutorial_evidence = json.loads((base/'Data/Tutorial/TutorialEvidence.json').read_text())
        assert tutorial['runtimeApiErrors'] == tutorial['editorApiErrors'] == tutorial['playerApiErrors'] == 0
        assert tutorial['fullTutorialRuns'] >= 5 and tutorial['sourceCommands'] == 420
        for row in tutorial_evidence['outputs']:
            assert sha((ROOT/row['asset']).read_bytes()) == row['sha256'], row['asset']
        expected_other |= {r['asset'] for r in tutorial_evidence['outputs']} | {
            'Assets/_Original/Data/Tutorial/NativeTutorialArt.asset', 'Assets/_Original/Data/Tutorial/TutorialEvidence.json'}
    assert other_paths == expected_other, other_paths ^ expected_other
    assert native == report['nativeFiles'] and images == report['imageOutputs'] and tables == 8
    # The first reset captured all actual old files, including modified and untracked ones.
    before = json.loads((ROOT / report['backupIndex']).read_text())
    backup = ROOT / report['backup']
    with tarfile.open(backup, 'r') as saved:
        members = [m for m in saved.getmembers() if m.isfile()]
        assert len(members) == len(before)
        for member in members:
            assert sha(saved.extractfile(member).read()) == before[member.name], member.name
    source_audit = verify_original_sources(index)
    result = dict(nativeFiles=native, decodedTextFiles=text, pixelVerifiedPng=images, originalTableFiles=tables,
                  sourceOutputs=len(source_paths), metadataGuids=len(guids), remainingOldGameScripts=0,
                  scenes=1, sceneMonoBehaviours=3 if tutorial else 2 if entry else 0, backupFilesVerified=len(before),
                  newGameImplemented=False, entryImplemented=bool(entry), tutorialImplemented=bool(tutorial), newGameScope='Entry through native tutorial301' if tutorial else 'Entry through first commander creation' if entry else None,
                  unityExecuted=False, independentSourceAudit=source_audit)
    target = ROOT / 'Tools/OriginalRestart/foundation-check.json'
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
