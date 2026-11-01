"""Read-only source audit for missing scenario 01..25 initial battle rosters.

Requires unicorn and capstone on PYTHONPATH for isolated original x86 probes.
Never generates placements or executes a complete original application.
"""
import collections
import hashlib
import importlib.util
import json
import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/OriginalClientChecks'))
from native_pe import PE

OUT = ROOT / '분석/Saver_Late'
EVIDENCE = ROOT / 'Tools/ScenarioPlacementChecks/evidence/initial-roster-paths.asm.txt'


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def installer_map_audit():
    """Recheck all container identities and map ARC headers using retained copies."""
    inventory = json.loads((ROOT / '분석/InstallerVersions/installer_inventory.json').read_text())
    containers = []
    for entry in inventory['containers']:
        path = ROOT / '닥세_리소스/설치파일' / entry['file']
        digest = hashlib.sha256()
        with path.open('rb') as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b''):
                digest.update(block)
        assert path.stat().st_size == entry['bytes'] and digest.hexdigest() == entry['sha256'], path
        containers.append(dict(file=entry['file'], bytes=entry['bytes'], sha256=entry['sha256'], matchesInventory=True))
    sources = {}
    for folder in (ROOT / 'Assets/_Original/SourceData/Native/map',
                   ROOT / '닥세_리소스/설치본_추출/Saver_Files/Saver/MAP'):
        for path in folder.iterdir():
            if path.is_file() and path.name.lower().endswith(('.arc', '.arc.bytes')):
                sources.setdefault(sha(path.read_bytes()), path)
    catalog = module('installer_roster_catalog', ROOT / '닥세_리소스/분석도구/build_late_saver_catalog.py')
    decoder = module('installer_roster_decoder', ROOT / '닥세_리소스/분석도구/analyze_client.py')
    cache, payloads = {}, []
    for payload in inventory['payloads']:
        maps = [f['file'] for f in payload['files'] if Path(f['file']).suffix.lower() == '.mrc']
        archives = [f for f in payload['files'] if Path(f['file']).parent.name.lower() == 'map'
                    and f['file'].lower().endswith('.arc')]
        pairs, archive_rows = [], []
        for entry in archives:
            digest = entry['sha256']
            if digest not in cache:
                path = sources[digest]
                assert sha(path.read_bytes()) == digest
                cache[digest] = list(catalog.arc_entries(path))
            members = cache[digest]
            key = Path(entry['file']).name.lower().removesuffix('set.arc')
            archive_rows.append(dict(file=entry['file'], sha256=digest, members=[n for n, _, _, _ in members]))
            by_name = {n.lower(): (m, size, data) for n, m, size, data in members}
            if key + '.set' not in by_name or key + '.pos' not in by_name:
                continue
            text = {}
            for ext in ('.set', '.pos'):
                method, size, packed = by_name[key + ext]
                raw = (decoder.lzh_decode if method == 0 else decoder.lzss_decode)(packed, size)
                assert len(raw) == size
                text[ext] = raw.decode('cp949')
            positions, units = {}, {}
            for line in text['.pos'].splitlines():
                tokens = line.split(';', 1)[0].split()
                if len(tokens) == 3 and all(t.isdigit() for t in tokens):
                    unit, x, y = map(int, tokens)
                    positions.setdefault(unit, []).append([x, y])
            for line in text['.set'].splitlines():
                tokens = line.split(';', 1)[0].split()
                if len(tokens) in (2, 3) and all(t.isdigit() for t in tokens) and int(tokens[0]) >= 400:
                    unit, job = map(int, tokens[:2])
                    assert unit not in units and unit in positions and len(positions[unit]) == 1
                    units[unit] = [job, *positions[unit][0]]
            pairs.append(dict(scenario=key, count=len(units), archiveSha256=digest,
                              duplicatePositionUnits=[u for u, ps in positions.items() if len(ps) > 1]))
        pairs.sort(key=lambda p: int(p['scenario']))
        payloads.append(dict(containers=payload['containers'], payloadId=payload['payloadId'],
                             isPartialPatch=all(re.match(r'(cspatch|patch|pch)', name, re.I) for name in payload['containers']),
                             mrcFiles=sorted(maps), mrcFileCount=len(maps),
                             scenarioSetArcCount=sum(a['file'].lower().endswith('set.arc') for a in archive_rows),
                             mapArchives=archive_rows, setPosPairs=pairs,
                             nonemptyRosterScenarios=[p['scenario'] for p in pairs if p['count']],
                             emptyRosterScenarios=[p['scenario'] for p in pairs if not p['count']],
                             joinedMonsterCount=sum(p['count'] for p in pairs)))
    full = [p for p in payloads if not p['isPartialPatch']]
    expected = {'26', '51', '52', '53', '54', '55', *map(str, range(314, 324))}
    assert len(containers) == 16 and len(full) == 6
    for payload in full:
        assert set(payload['nonemptyRosterScenarios']) == expected
        assert payload['joinedMonsterCount'] == 610
    return dict(scope='All 16 source-container hashes rechecked against existing extraction inventory; all unique map ARC headers and SET/POS re-read from hash-identical retained copies. No installers or games executed, no fresh full extraction.',
                containers=containers, payloads=payloads, uniqueMapArchivesRechecked=len(cache),
                fullPayloadCount=len(full), patchPayloadCount=len(payloads) - len(full),
                note='EGG volumes form one logical payload; duplicated CAB payloads are grouped. MRC counts include t variants and interiors, not unique scenario counts.')


def map_directory_audit():
    """Use retained raw copies when temporary installer extraction is unavailable."""
    folder = ROOT / 'Assets/_Original/SourceData/Native/map'
    files = [p for p in sorted(folder.iterdir()) if p.is_file() and p.name.endswith('.bytes')]
    rows, exceptional_spans = [], []
    for path in files:
        if not path.name.endswith('.arc.bytes'):
            continue
        data = path.read_bytes()
        assert data[0] == 4 and data[1:5] in (b'0901', b'0720'), path
        offset, entries = 5, []
        while offset + 36 <= len(data):
            record = data[offset:offset + 36]
            offset += 36
            assert 0 < record[0] <= 16
            name = record[1:record[0] + 1].decode('cp949')
            if name == 'HEADEND':
                break
            size, packed, start, end = struct.unpack_from('<4I', record, 20)
            assert end == start + packed - 1 and 0 <= start <= end < len(data)
            assert sum(data[start:end + 1]) & 65535 == struct.unpack_from('<H', record, 18)[0]
            entries.append(dict(name=name, decodedBytes=size, packedBytes=packed, start=start, endExclusive=end + 1))
        else:
            raise ValueError(f'No HEADEND: {path}')
        spans = sorted([(0, offset)] + [(e['start'], e['endExclusive']) for e in entries])
        cursor, gaps, overlaps = 0, [], []
        for start, end in spans:
            if start > cursor:
                gaps.append([cursor, start])
            if start < cursor:
                overlaps.append([start, cursor])
            cursor = max(cursor, end)
        if cursor < len(data):
            gaps.append([cursor, len(data)])
        row = dict(file=path.name.removesuffix('.bytes'), sha256=sha(data), bytes=len(data),
                   members=entries, unreferencedByteRanges=gaps, overlappingByteRanges=overlaps)
        rows.append(row)
        if gaps or overlaps:
            exceptional_spans.append(row['file'])
    family = [dict(file=p.name.removesuffix('.bytes'), bytes=p.stat().st_size, sha256=sha(p.read_bytes()))
              for p in files if re.fullmatch(r'26(?:set|t|bg|hmap)?\.(arc|mrc|bmp)\.bytes', p.name)]
    original = ROOT / '닥세_리소스/설치본_추출/Saver_Files/Saver/MAP'
    unusual = [dict(file=p.name, bytes=p.stat().st_size, sha256=sha(p.read_bytes()))
               for p in sorted(original.iterdir()) if p.is_file() and p.suffix.lower()
               not in ('.mrc', '.arc', '.bmp', '.grs')]
    assert next(r for r in rows if r['file'] == '26set.arc')['sha256'] == '1a2604bc2e22a9a0ecf476e6b57776d24803cd4197cda807676cd92641ed7a4b'
    return dict(scope='Current raw .bytes map copies, ignoring Unity .meta; original older extraction used only for unusual adjacent filenames',
                sourceDirectory=str(folder.relative_to(ROOT)),
                fileCount=len(files), archiveCount=len(rows),
                sameScenario26Files=family, map26Equals26t=(folder / '26.mrc.bytes').read_bytes() == (folder / '26t.mrc.bytes').read_bytes(),
                archivesWithUnreferencedOrOverlappingBytes=exceptional_spans,
                archives=rows, unusualOlderAdjacentFiles=unusual,
                note='Zero unreferenced ranges excludes unused bytes outside the ARC index/payload spans; not a proof that every declared member has been fully interpreted.')


def archive_audit(inventory):
    catalog = module('roster_catalog', ROOT / '닥세_리소스/분석도구/build_late_saver_catalog.py')
    decoder = module('roster_decoder', ROOT / '닥세_리소스/분석도구/analyze_client.py')
    archives = {}
    direct = []
    for payload in inventory['payloads']:
        for entry in payload['files']:
            name = Path(entry['file']).name.lower()
            if re.fullmatch(r'(0?[1-9]|1[0-9]|2[0-5])\.(set|pos)', name):
                direct.append(dict(container=payload['containers'], file=entry['file']))
            if not name.endswith('.arc'):
                continue
            item = archives.setdefault(entry['sha256'], dict(path=entry['extractedFile'],
                                                            origins=[], sha256=entry['sha256']))
            item['origins'].append(dict(containers=payload['containers'], file=entry['file']))
    rows, target_members, placements, texts, supplementary = [], [], [], [], []
    extension_counts = collections.Counter()
    for digest, archive in sorted(archives.items()):
        path = Path(archive['path'])
        assert sha(path.read_bytes()) == digest, path
        members = list(catalog.arc_entries(path))
        rows.append(dict(sha256=digest, origins=archive['origins'],
                         members=[dict(name=n, method=m, bytes=s) for n, m, s, data in members]))
        for name, method, size, packed in members:
            extension_counts[Path(name).suffix.lower()] += 1
            if Path(name).suffix.lower() in ('.bak', '.all', '.b'):
                assert method in (0, 1)
                try:
                    raw = (decoder.lzh_decode if method == 0 else decoder.lzss_decode)(packed, size)
                except (ValueError, IndexError) as error:
                    supplementary.append(dict(archiveSha256=digest, member=name, bytes=size,
                                              kind='decode failed', error=str(error)))
                    continue
                assert len(raw) == size
                item = dict(archiveSha256=digest, member=name, bytes=len(raw), sha256=sha(raw))
                if b'\0' not in raw:
                    text = raw.decode('cp949')
                    item.update(kind='text backup', lines=len(text.splitlines()),
                                numericRows=[dict(line=i, raw=s) for i, s in enumerate(text.splitlines(), 1)
                                             if re.fullmatch(r'\s*\d+(?:\s+\d+){1,5}\s*', s)],
                                preview=text.splitlines()[:2])
                else:
                    item.update(kind='binary; not interpreted as placement',
                                firstNonzeroByte=next((i for i, byte in enumerate(raw) if byte), None))
                    if name.lower() == '29.all':
                        offsets = [m.start() for m in re.finditer(re.escape(struct.pack('<I', 400)), raw)]
                        item.update(value400Offsets=offsets,
                                    tailText=raw[-100:].decode('cp949', errors='replace').strip('\0'))
                supplementary.append(item)
            if Path(name).suffix.lower() in ('.set', '.pos'):
                placements.append(dict(archiveSha256=digest, name=name, origins=archive['origins']))
            target = re.fullmatch(r'(0[1-9]|1[0-9]|2[0-5])set\.arc', path.name.lower())
            if target:
                target_members.append(dict(scenario=target[1], archiveSha256=digest,
                                           member=name, origins=archive['origins']))
                assert method in (0, 1)
                raw = (decoder.lzh_decode if method == 0 else decoder.lzss_decode)(packed, size)
                assert len(raw) == size
                texts.append(dict(scenario=target[1], archiveSha256=digest,
                                  member=name.lower(), sha256=sha(raw), text=raw.decode('cp949')))
    missing = [f'{i:02}' for i in range(1, 26)
               if not any(m['scenario'] == f'{i:02}' and Path(m['member']).suffix.lower()
                          in ('.set', '.pos') for m in target_members)]
    assert len(missing) == 25
    return dict(uniqueArchives=len(rows), memberExtensions=dict(extension_counts),
                archives=rows, targetMembers=target_members,
                allSetPosMembers=placements, standaloneSetPosCandidates=direct,
                supplementaryMembers=supplementary,
                scenariosWithoutSetPos=missing), texts


def map_audit(inventory):
    visual = module('roster_visuals', ROOT / '닥세_리소스/분석도구/decode_visuals.py')
    unique = {}
    for payload in inventory['payloads']:
        for entry in payload['files']:
            if re.fullmatch(r'(0[1-9]|1[0-9]|2[0-5])\.mrc', Path(entry['file']).name.lower()):
                unique.setdefault(entry['sha256'], entry)
    result = []
    for digest, entry in sorted(unique.items()):
        path = Path(entry['extractedFile'])
        assert sha(path.read_bytes()) == digest
        data = visual.read_mrc(path)  # Parser asserts exact EOF after every tile/sprite.
        result.append(dict(file=path.name, sha256=digest, bytes=path.stat().st_size,
                           width=data['width'], height=data['height'],
                           sprites=len(data['sprites']), exactEof=True))
    return result


def script_audit(texts):
    result = []
    for key in [f'{i:02}' for i in range(1, 26)]:
        records = [t for t in texts if t['scenario'] == key]
        # Each archive digest is counted once, regardless of duplicate installers.
        versions = []
        for digest in sorted({r['archiveSha256'] for r in records}):
            ai, creations, conditions = [], [], []
            commands = collections.Counter()
            for record in [r for r in records if r['archiveSha256'] == digest]:
                suffix = Path(record['member']).suffix
                for line, text in enumerate(record['text'].splitlines(), 1):
                    tokens = text.split(';', 1)[0].split()
                    if not tokens:
                        continue
                    if suffix == '.ai':
                        match = re.fullmatch(r'\s*(\d+)\s+(OD\w+)\s+(AREA|SOILDER)\s*\(([^)]*)\)\s*(.*)', text)
                        if match:
                            unit, mode, selector, args, actions = match.groups()
                            ai.append(dict(line=line, unit=int(unit), mode=mode, selector=selector,
                                           args=list(map(int, args.split(',')))))
                    elif suffix in ('.deo', '.dee', '.evt'):
                        numeric = tokens[0].isdigit() and int(tokens[0]) > 0
                        cmd = tokens[1] if numeric and len(tokens) > 1 else tokens[0]
                        commands[(suffix + ':' + cmd)] += 1
                        if cmd in ('CHARPOS', 'CHARPOS2', 'CHARPOS3'):
                            row = dict(file=record['member'], line=line, raw=text)
                            if suffix == '.evt' and numeric:
                                conditions.append(row)
                            else:
                                creations.append(row)
            assert not any(c['file'].endswith('.evt') for c in creations)
            versions.append(dict(archiveSha256=digest,
                                 members=[dict(member=r['member'], originalSha256=r['sha256'])
                                          for r in records if r['archiveSha256'] == digest],
                                 ai=ai, scriptCreations=creations, eventPositionConditions=conditions,
                                 commands=dict(commands)))
        result.append(dict(scenario=key, versions=versions))
    return result


def packet_probe(pe, unit_pe, subtype, unit, job, x, y):
    from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
    from unicorn.x86_const import UC_X86_REG_ESP, UC_X86_REG_EAX, UC_X86_REG_EIP, UC_X86_REG_ECX
    machine = Uc(UC_ARCH_X86, UC_MODE_32)
    for address, size in ((0, 0x10000), (pe.base, 0x100000),
                          (0x20000000, 0x100000), (0x30000000, 0x100000)):
        machine.mem_map(address, size)
    for section in pe.sections:
        machine.mem_write(pe.base + section['rva'], pe.data[section['raw']:section['raw'] + section['rawSize']])
    body = bytearray(512)
    coord = 28 if subtype == 0x50 else 40
    for offset, value in ((0, unit), (4, job), (coord, x), (coord + 2, y)):
        struct.pack_into('<h', body, offset, value)
    packet = bytes([0x5c, subtype]) + body
    machine.mem_write(0x30001000, packet)
    machine.mem_write(0x20080000, struct.pack('<3I', 0x300ff000, 0x30001000, len(packet)))
    machine.reg_write(UC_X86_REG_ESP, 0x20080000)
    machine.mem_write(0x1001621c, struct.pack('<I', 0x300f0000))
    captured, raw_structs, read_offsets = [], [], []
    heap = 0x30010000

    def get(address):
        return struct.unpack('<I', machine.mem_read(address, 4))[0]

    def ret(value=0, cleanup=0):
        stack = machine.reg_read(UC_X86_REG_ESP)
        machine.reg_write(UC_X86_REG_EAX, value)
        machine.reg_write(UC_X86_REG_ESP, stack + 4 + cleanup)
        machine.reg_write(UC_X86_REG_EIP, get(stack))

    def hook(uc, address, size, _):
        nonlocal heap
        stack = uc.reg_read(UC_X86_REG_ESP)
        if address in (0x1000c540, 0x1000c560, 0x1000c5c0):
            read_offsets.append(dict(helper=hex(address), bodyOffset=get(get(stack + 8))))
        if address == 0x300f0000:
            ptr = get(stack + 4)
            raw_structs.append(bytes(uc.mem_read(ptr, 0xb0)))
            captured.append({name: get(ptr + offset) for name, offset in
                             (('unit', 0), ('job', 4), ('x', 0x48), ('y', 0x4c))})
            uc.emu_stop()
        elif address in (0x100147fe, 0x1001476e, 0x10014792):
            ret(uc.reg_read(UC_X86_REG_ECX))  # CString construction/destruction modeled.
        elif address == 0x100147e0:
            ret(uc.reg_read(UC_X86_REG_ECX), 4)  # CString assignment modeled.
        elif address == 0x10014774:
            pointer = heap
            heap += 0x1000
            ret(pointer)  # Allocation modeled; original byte-copy helper still runs.
        elif address == 0x300ff000:
            uc.emu_stop()

    machine.hook_add(UC_HOOK_CODE, hook)
    machine.emu_start(0x10002d30, 0, count=30000)
    expected = dict(unit=unit, job=job, x=x, y=y)
    assert captured == [expected], (subtype, captured, expected)
    # Run original UserUnit resetcharacter prefix through coordinate assignment.
    # Its pending-path destruction boundary is modeled; no renderer is started.
    state = Uc(UC_ARCH_X86, UC_MODE_32)
    for address, size in ((0, 0x10000), (unit_pe.base, 0x200000),
                          (0x20000000, 0x100000), (0x30000000, 0x100000)):
        state.mem_map(address, size)
    for section in unit_pe.sections:
        state.mem_write(unit_pe.base + section['rva'], unit_pe.data[section['raw']:section['raw'] + section['rawSize']])
    state.mem_write(0x1010b3a0, struct.pack('<I', 0x30000000))
    state.mem_write(0x300f0000, raw_structs[0])
    state.mem_write(0x20080000, struct.pack('<2I', 0x300ff000, 0x300f0000))
    state.reg_write(UC_X86_REG_ESP, 0x20080000)

    def state_hook(uc, address, size, _):
        if address == 0x1000b28b:
            uc.emu_stop()
        elif address == 0x100118fc:
            stack = uc.reg_read(UC_X86_REG_ESP)
            uc.reg_write(UC_X86_REG_ESP, stack + 4)
            uc.reg_write(UC_X86_REG_EIP, struct.unpack('<I', uc.mem_read(stack, 4))[0])

    state.hook_add(UC_HOOK_CODE, state_hook)
    state.emu_start(0x1000b0a0, 0, count=30000)
    assert state.reg_read(UC_X86_REG_EIP) == 0x1000b28b
    live = {name: struct.unpack('<I', state.mem_read(0x30000000 + unit * 0x2e0 + offset, 4))[0]
            for name, offset in (('unit', 0x10), ('job', 0x18), ('x', 0x70), ('y', 0x74))}
    assert live == expected, live
    return dict(opcode='5C ' + format(subtype, '02X'), input=expected,
                resetcharacterArgument=captured[0], reads=read_offsets,
                liveCharacterFields=live,
                packetOffsets=dict(unit=2, job=6, x=coord + 2, y=coord + 4))


def native_audit(base):
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    saver, ctl, units = [PE(base / name) for name in ('saver.exe', 'gamectl.dll', 'userunit.dll')]
    assert saver.sha256 == 'dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
    assert ctl.sha256 == 'f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
    assert units.sha256 == '74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b'
    assert ctl.imports()[0x1001621c][1] == '?resetcharacter@@YAHPAUunitinfo@@@Z'
    assert saver.imports()[0x499014][1] == '?OnPacketReceive@@YAXPADH@Z'
    assert ctl.exports()['?OnPacketReceive@@YAXPADH@Z'] == 0x10002d30
    dispatch = []
    for sub, stub in ((0x50, 0x1000eb5a), (0x51, 0x1000eb67)):
        assert ctl.u32(0x1000ed14 + 4 * ctl.read(0x1000ed2c + 2, 1)[0]) == 0x1000eb33
        index = ctl.read(0x1000edd4 + sub - 1, 1)[0]
        assert ctl.u32(0x1000ed50 + 4 * index) == stub
        dispatch.append(dict(opcode='5C ' + format(sub, '02X'), branch=hex(stub)))
    probes = [packet_probe(ctl, units, sub, *values) for sub in (0x50, 0x51)
              for values in ((400, 401, 14, 28), (410, 502, 5, 17), (700, 701, 19, 6))]
    literals = []
    for name in ('saver.exe', 'gamectl.dll', 'userunit.dll', 'setunit.dll', 'dmanage.dll'):
        pe = PE(base / name)
        matches = [dict(va=hex(pe.va(m.start())), text=m.group().decode())
                   for m in re.finditer(rb'[ -~]{4,}', pe.data)
                   if re.search(rb'\.(set|pos|ai|evt|srf|mrc)\b', m.group(), re.I)]
        literals.append(dict(file=name, sha256=pe.sha256, fileLiterals=matches,
                             hasSetPosLiteral=any(re.search(r'\.(set|pos)\b', m['text'], re.I) for m in matches)))
    # Record actual instructions, not hand-written pseudocode.
    ranges = [(saver, 0x43bc50, 0x43be22), (saver, 0x441b01, 0x441b13),
              (ctl, 0x10002d30, 0x10002d44), (ctl, 0x1000eb10, 0x1000eb74),
              (ctl, 0x1000c7d0, 0x1000cb30), (ctl, 0x1000cb90, 0x1000d0b0),
              (ctl, 0x1001076d, 0x10010798), (units, 0x1000ed49, 0x1000ed6f),
              (units, 0x1000ee60, 0x1000eeab), (units, 0x1000b194, 0x1000b28b)]
    output = []
    for pe, start, end in ranges:
        output.append(f'{pe.path.name} SHA256 {pe.sha256} [{start:#x}, {end:#x})')
        output.extend(f'{i.address:#x}: {i.mnemonic} {i.op_str}'
                      for i in decoder.disasm(pe.read(start, end - start), start))
    EVIDENCE.write_text('\n'.join(output) + '\n', encoding='utf-8')
    return dict(binaryLiterals=literals, dispatch=dispatch, probes=probes,
                evidence=str(EVIDENCE.relative_to(ROOT)),
                scope='Original x86 dispatcher/parser and resetcharacter prefix through live-coordinate copy; CString/allocator/path-destruction modeled; synthetic packets, not captured original traffic or full Play')


def other_file_audit(base, archives):
    """Inspect alternative DAT/ALL and creation callers without inferring a roster."""
    from unicorn import Uc, UC_ARCH_X86, UC_MODE_32
    from unicorn.x86_const import UC_X86_REG_ESP, UC_X86_REG_ECX, UC_X86_REG_EAX
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    from capstone.x86 import X86_OP_IMM, X86_OP_MEM
    saver = PE(base / 'saver.exe')
    raw = (base / 'candid.dat').read_bytes()
    packed_size, unpacked_size, checksum = struct.unpack_from('<HHI', raw)
    assert len(raw) == 8 + packed_size
    machine = Uc(UC_ARCH_X86, UC_MODE_32)
    for address, size in ((0, 0x10000), (saver.base, 0x100000),
                          (0x20000000, 0x100000), (0x30000000, 0x100000)):
        machine.mem_map(address, size)
    for section in saver.sections:
        machine.mem_write(saver.base + section['rva'], saver.data[section['raw']:section['raw'] + section['rawSize']])
    context = 0x4950f8
    fields = {0x10: 0x30030000, 0x14: 0xffffffff, 0x18: unpacked_size,
              0x1c: 0x30010000, 0x20: 0x30020000, 0x24: packed_size + 1,
              0x28: unpacked_size + 1, 0x2c: 0, 0x30: 0, 0x34: 2, 0x38: 0}
    for offset, value in fields.items():
        machine.mem_write(context + offset, struct.pack('<I', value))
    machine.mem_write(0x30010000, raw[8:])
    machine.mem_write(0x20080000, struct.pack('<I', 0x300ff000))
    machine.reg_write(UC_X86_REG_ESP, 0x20080000)
    machine.reg_write(UC_X86_REG_ECX, context)
    machine.emu_start(0x43b7b0, 0x300ff000, count=2000000)
    get = lambda address: struct.unpack('<I', machine.mem_read(address, 4))[0]
    decoded = bytes(machine.mem_read(0x30020000, get(context + 0x30)))
    assert machine.reg_read(UC_X86_REG_EAX) == 1
    assert len(decoded) == unpacked_size and get(context + 0x14) == checksum
    plain = OUT / 'decoded/gameres/set.arc/candid.txt'
    assert decoded.decode('cp949').splitlines() == plain.read_text().splitlines()
    candid = dict(file='candid.dat', sha256=sha(raw), bytes=len(raw),
                  decodedBytes=len(decoded), decodedSha256=sha(decoded),
                  nativeDecoder='Saver 0x43b7b0 -> 0x478930', checksum=hex(checksum),
                  checksumMatches=True, matchesText=str(plain.relative_to(ROOT)),
                  classification='Same text as original chat prohibited-word table; not a monster roster',
                  scope='Original x86 decompressor and callbacks; context and buffers prepared in memory, no original app/file dialogs')

    # Identify statically resolvable creation calls through direct imports/thunks.
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    decoder.skipdata = True  # Embedded switch tables must not stop the scan.
    calls = []
    symbols = ('?dmSetChar@@', '?dmSetChar2@@', '?resetcharacter@@')
    for name in ('saver.exe', 'gamectl.dll', 'userunit.dll', 'setunit.dll', 'dmanage.dll'):
        pe = PE(base / name)
        imports = {a: s for a, (dll, s) in pe.imports().items() if s.startswith(symbols)}
        local = {pe.thunk(a): s for s, a in pe.exports().items() if s.startswith(symbols)}
        for section in pe.sections:
            if not section['flags'] & 0x20000000:
                continue
            for instruction in decoder.disasm(pe.data[section['raw']:section['raw'] + section['rawSize']], pe.base + section['rva']):
                if instruction.mnemonic != 'call' or len(instruction.operands) != 1:
                    continue
                operand = instruction.operands[0]
                symbol = None
                if operand.type == X86_OP_MEM and not operand.mem.base and not operand.mem.index:
                    symbol = imports.get(operand.mem.disp)
                elif operand.type == X86_OP_IMM and pe.executable(operand.imm):
                    target = pe.thunk(operand.imm)
                    symbol = local.get(target)
                    if not symbol and pe.read(target, 2) == b'\xff\x25':
                        symbol = imports.get(pe.u32(target + 2))
                if symbol:
                    calls.append(dict(file=name, sha256=pe.sha256,
                                      call=hex(instruction.address), function=symbol))
    assert any(c['call'] == '0x1000ed67' and c['file'] == 'userunit.dll' for c in calls)
    assert {c['call'] for c in calls if c['file'] == 'gamectl.dll'} >= {'0x1000cac9', '0x1000d045', '0x1001078f'}

    # Inspect original binary ALL observations independently of a text decoder.
    all_file = next(a for a in archives['supplementaryMembers']
                    if a['member'] == '29.all')
    # Filename/word observations are retained, not converted into new units.
    dat = (base / 'rollingcommon.dat').read_bytes()
    older_root = ROOT / '닥세_리소스/설치본_추출/Saver_Files/Saver'
    older_arcs = [p for p in older_root.rglob('*') if p.suffix.lower() == '.arc']
    known = {a['sha256'] for a in archives['archives']}
    outside = [str(p.relative_to(older_root)) for p in older_arcs if sha(p.read_bytes()) not in known]
    result = dict(candid=candid,
                  rollingCommon=dict(file='rollingcommon.dat', sha256=sha(dat), bytes=len(dat),
                                     classification='Text announcements and game tips',
                                     containsMonsterPositionTable=False),
                  allBinary=all_file,
                  olderExtractedSource=dict(arcFiles=len(older_arcs),
                                            arcsOutsideKnownInventory=outside),
                  creationCallsites=calls,
                  callsiteScope='Direct calls and direct IAT/jump thunks in five named late binaries; unresolved register/virtual calls not exhaustively classified')
    with EVIDENCE.open('a', encoding='utf-8') as stream:
        for start, end in ((0x43b7b0, 0x43b7da), (0x43b2f0, 0x43b36e), (0x478930, 0x478978)):
            stream.write(f'\nSaver.exe SHA256 {saver.sha256} [{start:#x}, {end:#x})\n')
            for i in decoder.disasm(saver.read(start, end - start), start):
                stream.write(f'{i.address:#x}: {i.mnemonic} {i.op_str}\n')
    return result


def main():
    if sys.argv[1:] == ['--installer-maps-only']:
        destination = OUT / 'initial_roster_source_audit.json'
        report = json.loads(destination.read_text())
        report['installerMapReview'] = installer_map_audit()
        destination.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        review = report['installerMapReview']
        print(json.dumps(dict(containers=len(review['containers']), uniqueMapArcs=review['uniqueMapArchivesRechecked'],
                              fullPayloads=review['fullPayloadCount'], patchPayloads=review['patchPayloadCount'],
                              nonemptyRosters=16, unitsPerFullPayload=610), ensure_ascii=False))
        return
    if sys.argv[1:] == ['--directory-only']:
        destination = OUT / 'initial_roster_source_audit.json'
        report = json.loads(destination.read_text())
        report['mapDirectoryReview'] = map_directory_audit()
        destination.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        review = report['mapDirectoryReview']
        print(json.dumps(dict(scope='directory-only; prior native results not rerun',
                              files=review['fileCount'], archives=review['archiveCount'],
                              exceptionalByteSpans=review['archivesWithUnreferencedOrOverlappingBytes'],
                              scenario26Files=[r['file'] for r in review['sameScenario26Files']]), ensure_ascii=False))
        return
    inventory_path = ROOT / '분석/InstallerVersions/installer_inventory.json'
    inventory = json.loads(inventory_path.read_text())
    late = next(p for p in inventory['payloads'] if 'saver.exe' in p['containers'])
    executable = next(f for f in late['files'] if Path(f['file']).name.lower() == 'saver.exe')
    archives, texts = archive_audit(inventory)
    maps = map_audit(inventory)
    scripts = script_audit(texts)
    native = native_audit(Path(executable['extractedFile']).parent)
    report = dict(scope='Installation inventory and alternative original files/commands for scenario 01..25; no generated battle roster',
                  inventorySha256=sha(inventory_path.read_bytes()), payloads=len(inventory['payloads']),
                  archives=archives, maps=maps, scripts=scripts, native=native,
                  conclusions=[
                      'Initial 01..25 SET/POS missing in examined raw ARC member directories, not merely missing extracted filenames.',
                      'AI AREA/SOILDER expresses behavior/reference, not a job/initial-position roster.',
                      '01..25 EVT position commands are numeric-prefix conditions, not CHARPOS actor-creation actions.',
                      'DEO/DEE CHARPOS creates scripted actors; reuse/removal prevents treating all occurrences as an initial battle roster.',
                      'MRC parser consumes all bytes as map resource IDs/tile arrays/decorative sprites; no trailing unit roster.',
                      'Late client 5C50/51 receivers pass message-supplied unit/job/x/y into resetcharacter.',
                      'Presence of a receive path does not recover actual scenario01..25 historical packet values or prove the server source file.',
                      'Absence of filename literals is supporting evidence only; dynamic names/embedded data cannot be excluded by this search alone.'
                  ])
    destination = OUT / 'initial_roster_source_audit.json'
    report['otherFiles'] = other_file_audit(Path(executable['extractedFile']).parent, archives)
    report['mapDirectoryReview'] = map_directory_audit()
    destination.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(payloads=report['payloads'], uniqueArcs=archives['uniqueArchives'],
                          targetArcVariants=len({r['archiveSha256'] for r in archives['targetMembers']}),
                          targetScenarios=len(scripts), mrcVariants=len(maps),
                          missingSetPos=len(archives['scenariosWithoutSetPos']),
                          nativePacketProbes=len(native['probes']), output=str(destination.relative_to(ROOT))),
                     ensure_ascii=False))


if __name__ == '__main__':
    main()
