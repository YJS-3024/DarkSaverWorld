"""Recover the earlier waiting-room lesson from the late client's RT_STRING.

This extracts original resources and verifies specific native instructions. It
does not claim to reconstruct every input condition or to have played the game.
"""
import argparse
import hashlib
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/CombatApChecks/python-deps'))
import pefile

EXPECTED = 'dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('executable', type=Path)
    args = parser.parse_args()
    raw = args.executable.read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    if digest != EXPECTED:
        raise ValueError('Expected the catalogued Saver_Late executable: ' + EXPECTED)
    pe = pefile.PE(data=raw)
    rows = []
    for resource in pe.DIRECTORY_ENTRY_RESOURCE.entries:
        if resource.id != 6:  # RT_STRING: sixteen length-prefixed UTF-16 strings.
            continue
        for block in resource.directory.entries:
            for language in block.directory.entries:
                entry = language.data.struct
                data = pe.get_data(entry.OffsetToData, entry.Size)
                pos = 0
                for index in range(16):
                    length = struct.unpack_from('<H', data, pos)[0]
                    pos += 2
                    text_rva = entry.OffsetToData + pos
                    text = data[pos:pos + length * 2].decode('utf-16le')
                    pos += length * 2
                    identifier = (block.id - 1) * 16 + index
                    if 61216 <= identifier <= 61251:
                        rows.append(dict(id=identifier, index=identifier - 61216,
                                         language=language.id, rva=hex(text_rva),
                                         fileOffset=hex(pe.get_offset_from_rva(text_rva)),
                                         text=text, skipped=text.startswith('|')))
                if pos != len(data):
                    raise ValueError('RT_STRING block size mismatch')
    rows.sort(key=lambda row: row['id'])
    assert [row['id'] for row in rows] == list(range(61216, 61252))
    assert {row['language'] for row in rows} == {1042}
    assert [row['id'] for row in rows if row['skipped']] == [61236, 61237, 61238]
    fixtures = [
        (0x43507A, 'bf20ef0000', 'Constructor: first string ID61216'),
        (0x435096, '81ff43ef000076e1', 'Constructor: inclusive last ID61251'),
        (0x4353FC, '68dc050000', 'Initialization requests map1500'),
        (0x435412, '66c781d2000000dc05', 'Initialization writes map1500'),
        (0x435663, '80387c752d', 'Leading vertical bar enters skip path'),
        (0x435688, '8996c401000083f8247cb3', 'Skip increments index and loops below36'),
        (0x4357F3, '83bee80100000f', 'Move lesson target x15'),
        (0x435BF9, '83beec01000007', 'Move and exit target y7'),
        (0x435805, '83e802', 'Return area is relative to x2'),
        (0x43581C, '83e807', 'Return area is relative to y7'),
        (0x435A5C, '83bee801000010', 'First pickup x16'),
        (0x435A6F, '83f808', 'First pickup y8'),
        (0x435A95, '83e00f05fa020000', 'Armor762 plus15 for job103'),
        (0x435AC2, '83f809', 'Second pickup y9'),
        (0x435BEC, '83bee801000013', 'Final door x19'),
        (0x41AAE3, '6883000000', 'Default cursor bitmap131'),
        (0x41AB15, 'c7442434a2000000', 'Animated cursor resources begin162'),
        (0x41ACC3, '8bc8894368894b6c', 'Native cursor hotspot12,12 from eax12'),
    ]
    evidence = []
    for address, expected, meaning in fixtures:
        value = bytes.fromhex(expected)
        assert pe.get_data(address - pe.OPTIONAL_HEADER.ImageBase, len(value)) == value, hex(address)
        evidence.append(dict(address=hex(address), bytes=expected, meaning=meaning))
    targets = [struct.unpack('<I', pe.get_data(0x435C14 - pe.OPTIONAL_HEADER.ImageBase + i * 4, 4))[0] for i in range(37)]
    assert targets[3] == 0x4357F3 and targets[29] == 0x435A5C and targets[36] == 0x435BEC
    wait_targets = [dict(resourceId=61215 + i, address=hex(a)) for i, a in enumerate(targets) if i > 0]
    click_dispatch = pe.get_data(0x435F9C - pe.OPTIONAL_HEADER.ImageBase, 35)
    assert [61216 + i for i, v in enumerate(click_dispatch) if v < 2] == [61216,61217,61227,61235,61242,61243,61248,61249,61250]
    unit_path = args.executable.parent / 'userunit.dll'
    unit_raw = unit_path.read_bytes(); unit_pe = pefile.PE(data=unit_raw)
    canonical = json.loads((ROOT / '분석/Saver_Late/files.json').read_text())
    assert hashlib.sha256(unit_raw).hexdigest() == next(r['sha256'] for r in canonical if r['path'] == 'userunit.dll')
    floor_bytes = bytes.fromhex('8a40033cff740625ff000000c333c0c3')
    assert unit_pe.get_data(0x10004F51 - unit_pe.OPTIONAL_HEADER.ImageBase, len(floor_bytes)) == floor_bytes
    assets = {}
    for name in ['map/1500.mrc', 'wlib/scene2.lsc', 'wlib/scene3.lsc', 'wlib/scene38.lsc']:
        path = ROOT / 'Assets/_Original/SourceData/Native' / (name + '.bytes')
        assets[name] = dict(path=path.relative_to(ROOT).as_posix(),
                            sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    report = dict(sourceVersion='Saver_Late', executableSha256=digest,
                  encoding='RT_STRING, length-prefixed UTF-16LE',
                  nativeConstructor='0x435010', nativeVtable='0x484598',
                  resourceCount=len(rows), nonSkippedCount=sum(not r['skipped'] for r in rows),
                  screenshotMatchedResourceId=61230, nativeInstructionChecks=evidence,
                  nativeStepWaitTargets=wait_targets,
                  clickDispatch=[dict(resourceId=61216+i, dispatch=v) for i,v in enumerate(click_dispatch)],
                  floorLookup=dict(module='UserUnit.dll', sha256=hashlib.sha256(unit_raw).hexdigest(),
                                   address='0x10004F51', bytes=floor_bytes.hex(), rawFFBecomesAttribute=0),
                  cursor=dict(defaultBitmap=131, doorBitmaps=[162,163,164,165], hotspot=[12,12],
                              constructor='0x41AA70', stateSetter='0x41B170', doorState=1),
                  assets=assets, resources=rows,
                  limits=['Resource ordering, wait jump targets, click dispatch, item positions and floor fallback verified statically; see analysis document for implementation scope.',
                          'One user-supplied recreation screenshot inspected; video13-55s not viewed.',
                          'Native wall-clock playback speed and rendered input are not measured by this extractor.'])
    output = ROOT / '분석/Saver_Late/waiting_tutorial_resources.json'
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(resources=len(rows), nonSkipped=report['nonSkippedCount'],
                          instructionChecks=len(evidence), output=output.relative_to(ROOT).as_posix()),
                     ensure_ascii=False))


if __name__ == '__main__':
    main()
