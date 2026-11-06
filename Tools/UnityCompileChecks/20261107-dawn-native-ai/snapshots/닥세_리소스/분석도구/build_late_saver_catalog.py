"""Build version-scoped late Saver research, without executing original binaries.

Run from repository root; argument is the unshield extraction directory.
Raw table tokens and duplicate rows are retained, including ambiguous fields.
"""
from pathlib import Path
import argparse
import collections
import csv
import hashlib
import importlib.util
import io
import json
import re
import struct


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def sha(data):
    return hashlib.sha256(data).hexdigest()


def arc_entries(path):
    raw = path.read_bytes()
    assert raw[0] == 4 and raw[1:5] in (b'0901', b'0720'), path
    offset = 5
    while offset + 36 <= len(raw):
        record = raw[offset:offset + 36]
        offset += 36
        assert 0 < record[0] <= 16
        name = record[1:record[0] + 1].decode('cp949')
        if name == 'HEADEND':
            return
        assert '/' not in name and '\\' not in name and name not in ('.', '..')
        size, packed, start, end = struct.unpack_from('<4I', record, 20)
        assert end == start + packed - 1 and 0 <= start <= end < len(raw)
        data = raw[start:end + 1]
        assert sum(data) & 65535 == struct.unpack_from('<H', record, 18)[0]
        yield name, record[15], size, data
    raise ValueError(f'No HEADEND: {path}')


def numeric_rows(raw, composite=False):
    rows = []
    for line, text in enumerate(raw.decode('cp949').splitlines(), 1):
        tokens = text.split()
        if tokens and re.fullmatch(r'-?\d+', tokens[0]):
            key = ':'.join(tokens[:2] if composite else tokens[:1])
            rows.append(dict(key=key, line=line, values=tokens))
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('extracted', type=Path)
    parser.add_argument('--output', type=Path, default=Path('분석/Saver_Late'))
    args = parser.parse_args()
    root = args.extracted / 'Saver_Files/saver'
    output = args.output
    output.mkdir(parents=True, exist_ok=True)
    decoder = load('late_arc_decoder', '닥세_리소스/분석도구/analyze_client.py')
    visuals = load('late_visual_decoder', '닥세_리소스/분석도구/decode_visuals.py')
    oldroot = Path('닥세_리소스/설치본_추출/Saver_Files/Saver')
    old = {str(p.relative_to(oldroot)).lower(): p for p in oldroot.rglob('*') if p.is_file()}
    current_paths = {str(p.relative_to(root)).lower(): p for p in root.rglob('*') if p.is_file()}

    def write(name, data):
        p = output / name
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')

    def csvwrite(name, rows, fields):
        with (output / name).open('w', encoding='utf-8-sig', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=fields)
            writer.writeheader()
            writer.writerows(rows)

    manifest, executables, keywords = [], [], []
    for p in sorted(root.rglob('*')):
        if not p.is_file():
            continue
        key = str(p.relative_to(root)).lower()
        raw = p.read_bytes()
        q = old.get(key)
        status = 'added' if q is None else 'same' if sha(q.read_bytes()) == sha(raw) else 'changed'
        manifest.append(dict(path=key, bytes=len(raw), sha256=sha(raw), previous=status))
        if p.name.lower() in ('gamectl.dll', 'userunit.dll', 'saver.exe'):
            keywords.append(dict(file=p.name, GETGENERAL=raw.count(b'GETGENERAL\0'), GET_GENERAL=raw.count(b'GET_GENERAL\0'),
                                 scope='Exact null-terminated ASCII strings only; dispatch behavior not executed'))
        if p.suffix.lower() in ('.exe', '.dll') and raw[:2] == b'MZ':
            pe = struct.unpack_from('<I', raw, 60)[0]
            if raw[pe:pe + 4] == b'PE\0\0':
                executables.append(dict(path=key, sha256=sha(raw), peTimestamp=struct.unpack_from('<I', raw, pe + 8)[0],
                                        previous=status, oldAddressesReusable=status == 'same'))
    write('files.json', manifest)
    write('executables.json', executables)
    write('reward_keyword_strings.json', keywords)
    artwork = []
    for name, method, size, packed in arc_entries(root / 'gameres/char6.arc'):
        if name.lower() not in ('169r.bmp', '170r.bmp', '171r.bmp', '172r.bmp'):
            continue
        data = (decoder.lzh_decode if method == 0 else decoder.lzss_decode)(packed, size)
        assert len(data) == size
        image = visuals.Image.open(io.BytesIO(data))
        artwork.append(dict(archive='gameres/char6.arc', entry=name,
                            width=image.width, height=image.height, sha256=sha(data)))
    write('new_job_artwork.json', artwork)

    decoded, errors, arcmanifest = {}, [], []
    for p in sorted(root.rglob('*.arc')):
        if p.parent.name.lower() != 'map' and str(p.relative_to(root)).lower() != 'gameres/set.arc':
            continue
        archive = str(p.relative_to(root)).lower()
        for name, method, size, packed in arc_entries(p):
            if Path(name).suffix.lower() not in ('.atr', '.at', '.a', '.txt', '.evt', '.dee', '.deo', '.srf', '.set', '.pos', '.ai'):
                continue
            try:
                if method not in (0, 1):
                    raise ValueError(f'Unsupported compression {method}')
                data = (decoder.lzh_decode if method == 0 else decoder.lzss_decode)(packed, size)
                assert len(data) == size
                text = data.decode('cp949')
            except Exception as ex:
                errors.append(dict(archive=archive, entry=name, error=str(ex)))
                continue
            key = archive + '/' + name.lower()
            decoded[key] = data
            dest = output / 'decoded' / key
            dest.parent.mkdir(parents=True, exist_ok=True)
            with dest.open('w', encoding='utf-8', newline='') as stream:
                stream.write(text)
            arcmanifest.append(dict(archive=archive, entry=name, originalBytes=len(data), originalSha256=sha(data), storedEncoding='utf-8'))
    write('decoded_manifest.json', arcmanifest)
    write('decode_errors.json', errors)

    oldtables = {}
    for name, method, size, data in arc_entries(old['gameres/set.arc']):
        if name.lower().endswith('.atr'):
            oldtables[name.lower()] = (decoder.lzh_decode if method == 0 else decoder.lzss_decode)(data, size)
    tables, differences = {}, []
    for key, raw in decoded.items():
        if not key.startswith('gameres/set.arc/') or not key.endswith('.atr'):
            continue
        name = key.rsplit('/', 1)[1]
        composite = name == 'levelabl.atr'
        rows = numeric_rows(raw, composite)
        before = numeric_rows(oldtables.get(name, b''), composite)
        def groups(values):
            result = collections.defaultdict(list)
            for row in values:
                result[row['key']].append(row['values'])
            return result
        a, b = groups(rows), groups(before)
        tables[name] = dict(rows=rows, duplicateKeys={k: len(v) for k, v in a.items() if len(v) > 1})
        differences.append(dict(table=name, rows=len(rows), previousRows=len(before), uniqueKeys=len(a), previousUniqueKeys=len(b),
            addedKeys=sorted(a.keys() - b.keys()), removedKeys=sorted(b.keys() - a.keys()),
            changed=[dict(key=k, before=b[k], after=a[k]) for k in sorted(a.keys() & b.keys()) if a[k] != b[k]],
            duplicateKeys=tables[name]['duplicateKeys']))
    write('tables.json', tables)
    write('table_differences.json', differences)

    magic_ids = {r['key'] for r in tables['magictbl.atr']['rows']}
    growth = tables['levelabl.atr']['rows']
    missing_abilities = collections.defaultdict(list)
    empty_ability_tokens = []
    for row in growth:
        if len(row['values']) != 18:
            continue  # Retain source; do not invent a missing column or shift its meaning.
        for column in (16, 17):
            for ability in row['values'][column].split(','):
                if not ability:
                    empty_ability_tokens.append(dict(key=row['key'], line=row['line'], column=column))
                elif ability != '0' and ability not in magic_ids:
                    missing_abilities[ability].append(row['key'])
    write('source_quality.json', dict(
        growthWidthCounts=dict(collections.Counter(len(r['values']) for r in growth)),
        growthNon18ColumnRows=[r for r in growth if len(r['values']) != 18],
        duplicateGrowthKeys=tables['levelabl.atr']['duplicateKeys'],
        missingGrowthAbilityIds=dict(missing_abilities), emptyAbilityTokens=empty_ability_tokens,
        note='References checked against this installation only. No guessed IDs or missing values inserted.'))

    units = tables['ability.atr']['rows']
    # First field is job/appearance ID; retain original token columns rather than guessing new categories.
    csvwrite('units.csv', [dict(id=r['values'][0], name=r['values'][35], description=' '.join(r['values'][36:]),
        fields=json.dumps(r['values'], ensure_ascii=False)) for r in units], ['id', 'name', 'description', 'fields'])
    csvwrite('items.csv', [dict(id=r['values'][0], name=r['values'][42], description=' '.join(r['values'][43:]),
        fields=json.dumps(r['values'], ensure_ascii=False)) for r in tables['itemtbl.atr']['rows']], ['id', 'name', 'description', 'fields'])
    csvwrite('magic.csv', [dict(id=r['values'][0], name=r['values'][1], fields=json.dumps(r['values'], ensure_ascii=False))
        for r in tables['magictbl.atr']['rows']], ['id', 'name', 'fields'])
    job_ids = {r['key'] for r in tables['levelptl.atr']['rows']}
    csvwrite('jobs.csv', [dict(id=r['values'][0], name=r['values'][35], description=' '.join(r['values'][36:]),
        fields=json.dumps(r['values'], ensure_ascii=False)) for r in units if r['key'] in job_ids], ['id', 'name', 'description', 'fields'])

    maps, failures = [], []
    for p in sorted(root.rglob('*')):
        if p.suffix.lower() != '.mrc':
            continue
        try:
            data = visuals.read_mrc(p)
            maps.append(dict(path=str(p.relative_to(root)).lower(), width=data['width'], height=data['height'],
                resourceIds=data['ids'], decorations=len(data['sprites']), sha256=sha(p.read_bytes())))
        except Exception as ex:
            failures.append(dict(path=str(p.relative_to(root)), error=str(ex)))
    write('maps.json', maps)
    write('map_errors.json', failures)

    rewards, placements, commands = [], [], collections.Counter()
    for key, raw in decoded.items():
        if not key.startswith('map/'):
            continue
        event = ''
        for line, text in enumerate(raw.decode('cp949').splitlines(), 1):
            tokens = text.split(';', 1)[0].split()
            if not tokens:
                continue
            if tokens[0].isdigit() and len(tokens) > 1:
                tokens = tokens[1:]
            cmd = tokens[0].upper()
            commands[cmd] += 1
            if cmd == 'EVENT' and len(tokens) > 1:
                event = tokens[1]
            record = dict(source=key, line=line, event=event, command=cmd, args=' '.join(tokens[1:]))
            if cmd in ('GETGENERAL', 'GET_GENERAL', 'GETITEM', 'GOLD', 'GETMONEY', 'GETEXP', 'FIRSTCLEAR', 'SAVECLEAR', 'SCENECLEAR'):
                rewards.append(record)
            if cmd in ('CHARPOS', 'LOADSCENE', 'SETPOSITION', 'CHARPOSITION'):
                placements.append(record)
    write('reward_commands.json', rewards)
    write('placement_commands.json', placements)
    write('script_commands.json', dict(commands.most_common()))
    scenarios = []
    for archive in sorted({k.split('/')[1] for k in decoded if k.startswith('map/') and 'set.arc/' in k}):
        source_keys = [k for k in decoded if k.startswith('map/' + archive + '/')]
        scenario_rewards = [r for r in rewards if r['source'] in source_keys]
        scenarios.append(dict(archive=archive, sources=source_keys,
            rewardCommands=scenario_rewards,
            note='Source occurrences include ordinary events; not a guaranteed first-clear payout or simultaneous deployment.'))
    write('scenarios.json', scenarios)
    previous_clear = json.loads(Path('닥세_리소스/분석결과/시나리오_클리어보상_원본대조.json').read_text())
    previous_clear = {r['scenario']: r for r in previous_clear['scenarios']}
    clear_rewards = []
    for scene in scenarios:
        scenario = scene['archive'].removesuffix('set.arc')
        archive_key = 'map/' + scene['archive']
        previous_path = old.get(archive_key)
        same = previous_path is not None and previous_path.read_bytes() == current_paths[archive_key].read_bytes()
        if same and scenario in previous_clear:
            row = dict(previous_clear[scenario])
            row['analysisInheritance'] = 'Identical archive bytes; prior explicit source-command interpretation retained, not late binary/server execution proof'
            # Old x86 probes remain old-version evidence, even when script bytes match.
            if scenario == '10':
                row.update(status='late_dispatch_unconfirmed', rewards=[], confirmedRewards=[],
                    notes=['GET_GENERAL 109 remains in EVT. Late DLL has GETGENERAL string but no GET_GENERAL string. Old x86 probe does not prove late dispatch behavior. No confirmed late clear reward.'])
        else:
            grant_commands = [r for r in scene['rewardCommands'] if r['command'] in ('GETGENERAL', 'GET_GENERAL', 'GETITEM', 'GOLD', 'GETMONEY', 'GETEXP')]
            row = dict(scenario=scenario, status='requires_clear_link_analysis' if grant_commands else 'no_explicit_grant_command_found',
                       rewards=[], notes=['New or changed archive; no historical server-only reward inferred.'], evidence=grant_commands)
        row['lateArchiveSha256'] = sha(current_paths[archive_key].read_bytes())
        row['lateSources'] = scene['sources']
        clear_rewards.append(row)
    write('clear_rewards.json', dict(scenarios=clear_rewards,
        scope='Explicit script-linked clear rewards, inherited only for byte-identical archives. No blanket first-clear or original server guarantee.'))
    tutorial = decoded.get('map/tutorial.arc/tutorial.deo')
    oldtutorial = Path('분석/Saver_1.2/decoded/all_arc/gameres/tutorial/TUTORIAL.DEO').read_bytes()
    summary = dict(source='saver/data1.cab', sourceSha256=sha(Path('saver/data1.cab').read_bytes()),
        gameFiles=len(manifest), fileComparison=dict(collections.Counter(x['previous'] for x in manifest)),
        tableSummary=[{k: v for k, v in d.items() if k not in ('changed', 'addedKeys', 'removedKeys')} |
                      dict(addedKeys=len(d['addedKeys']), removedKeys=len(d['removedKeys']), changedKeys=len(d['changed'])) for d in differences],
        decodedTextFiles=len(decoded), decodeErrors=len(errors), maps=len(maps), mapErrors=len(failures),
        scenarioArchives=len({k.split('/')[1] for k in decoded if k.startswith('map/') and 'set.arc/' in k}),
        rewardCommandOccurrences=len(rewards), tutorialSameText=tutorial.decode('cp949').splitlines() == oldtutorial.decode('cp949').splitlines(),
        scope='Static later-installation data; no original server, executable execution or Unity Play verification')
    write('summary.json', summary)
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
