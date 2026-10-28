"""Read-only native training ARC/decoded evidence audit. No game execution."""
import argparse
import hashlib
import json
import re
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def archive_entries(path):
    # Same 36-byte directory format/checksum as 분석/도구/Inspect-SaverArc.ps1.
    data = path.read_bytes()
    offset = 1 + data[0]
    entries = []
    while offset + 36 <= len(data):
        length = data[offset]
        assert length <= 16, (path, offset)
        name = data[offset + 1:offset + 1 + length].decode('ascii')
        if name == 'HEADEND':
            return data, entries
        checksum = struct.unpack_from('<H', data, offset + 18)[0]
        unpacked, packed, start, end = struct.unpack_from('<4I', data, offset + 20)
        assert 0 <= start <= end < len(data), (path, name, 'range')
        assert packed == end - start + 1, (path, name, 'packed length')
        assert sum(data[start:end + 1]) & 0xffff == checksum, (path, name, 'checksum')
        entries.append(dict(name=name, unpacked=unpacked, packed=packed,
                            checksum=checksum))
        offset += 36
    raise AssertionError((path, 'missing HEADEND'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--late-dir', type=Path, help='late Saver directory containing MAP')
    parser.add_argument('--output', type=Path,
                        default=ROOT / 'Tools/MapChecks/original_training_check.json')
    args = parser.parse_args()
    original = ROOT / '분석/Saver_1.2'
    rows = []
    for map_id in range(101, 108):
        archive = original / f'extracted/Saver_Files/Saver/MAP/{map_id}set.arc'
        data, entries = archive_entries(archive)
        assert len(entries) == 5
        assert {Path(e['name']).suffix.lower() for e in entries} == {'.evt', '.ai', '.dee', '.deo', '.srf'}
        decoded_dir = original / f'decoded/all_arc/MAP/{map_id}set'
        decoded = {p.suffix.lower(): p for p in decoded_dir.iterdir() if p.is_file()}
        ai = decoded['.ai'].read_text(encoding='cp949').strip()
        lines = [s.strip() for s in ai.splitlines() if s.strip() and not s.startswith(';')]
        assert len(lines) == 2
        rules = []
        for line in lines:
            match = re.fullmatch(r'(400|410) ODNORMAL AREA\((\d+),(\d+),(\d+)\) ATTACK\(30\),MAGIC_(\d+)\(30\),REST\(40\)', line)
            assert match, (map_id, line)
            unit, x, y, radius, magic = map(int, match.groups())
            rules.append(dict(instance=unit, area=[x, y, radius], magic=magic))
        assert {r['instance'] for r in rules} == {400, 410}
        event = decoded['.evt'].read_text(encoding='cp949').split()
        assert event[:8] == ['EVENT', '01', 'INF', '1', 'TICK', '10', 'REGEN', '2']
        assert event[8:] == (['ENDDD'] if map_id in (102, 105) else ['END'])
        for ext in ('.dee', '.deo', '.srf'):
            assert decoded[ext].read_text(encoding='cp949').split() == ([';999'] if ext == '.srf' else ['END', ';999'])
        late_sha = None
        if args.late_dir is not None:
            late = args.late_dir / f'MAP/{map_id}set.arc'
            late_data, late_entries = archive_entries(late)
            assert data == late_data and entries == late_entries, (map_id, 'early/late changed')
            late_sha = hashlib.sha256(late_data).hexdigest()
        rows.append(dict(map=map_id, source=str(archive.relative_to(ROOT)),
                         sha256=hashlib.sha256(data).hexdigest(), lateSha256=late_sha,
                         entries=entries, ai=rules, eventTokens=event,
                         placementRecords=0))
    report = dict(scope='static ARC directory/checksums, early/late binary comparison and existing decoded text; no decompression rerun, Unity or server execution',
                  maps=len(rows), originalArchiveEntries=sum(len(r['entries']) for r in rows),
                  lateArchivesCompared=len(rows) if args.late_dir else 0,
                  assertions='All checked conditions passed; counts are data coverage, not Unity test counts',
                  unresolved=['monster kinds/count/spawn positions', 'class-to-training-map assignment',
                              'TICK time unit and REGEN operand meaning', 'retreat destination/cost/death policy'],
                  rows=rows)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'PASS static training audit: {len(rows)} maps, {report["originalArchiveEntries"]} entries, {report["lateArchivesCompared"]} late archives compared; NOT Unity/server execution')


if __name__ == '__main__':
    main()
