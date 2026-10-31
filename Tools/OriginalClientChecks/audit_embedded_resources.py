"""Inventory installed PE resources separately from archive text and runtime behavior.

Reads canonical installer extractions; never edits Assets or executes the client.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/CombatApChecks/python-deps'))
import pefile

TYPES = {1: 'CURSOR', 2: 'BITMAP', 3: 'ICON', 4: 'MENU', 5: 'DIALOG', 6: 'STRING',
         9: 'ACCELERATOR', 10: 'RCDATA', 12: 'GROUP_CURSOR', 14: 'GROUP_ICON', 16: 'VERSION'}


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


class Reader:
    def __init__(self, raw): self.raw, self.pos = raw, 0
    def take(self, fmt):
        value = struct.unpack_from(fmt, self.raw, self.pos); self.pos += struct.calcsize(fmt)
        return value[0] if len(value) == 1 else value
    def align(self): self.pos = (self.pos + 3) & ~3
    def text(self):
        first = self.take('<H')
        if first == 0: return ''
        if first == 0xffff: return {'ordinal': self.take('<H')}
        words = [first]
        while (word := self.take('<H')): words.append(word)
        return struct.pack('<' + 'H' * len(words), *words).decode('utf-16le')


def dialog(raw):
    r = Reader(raw); extended = raw[2:4] == b'\xff\xff'
    if extended:
        version, signature, help_id, exstyle, style, count, x, y, w, h = r.take('<HHIIIHhhhh')
    else:
        style, exstyle, count, x, y, w, h = r.take('<IIHhhhh')
    result = dict(extended=extended, style=hex(style), exstyle=hex(exstyle), rect=[x, y, w, h],
                  menu=r.text(), windowClass=r.text(), title=r.text(), controls=[])
    if style & 0x40:
        font = {'pointSize': r.take('<H')}
        if extended:
            font.update(zip(['weight', 'italic', 'charset'], r.take('<HBB')))
        font['face'] = r.text(); result['font'] = font
    for _ in range(count):
        r.align()
        if extended:
            help_id, exstyle, style, x, y, w, h, identifier = r.take('<IIIhhhhI')
        else:
            style, exstyle, x, y, w, h, identifier = r.take('<IIhhhhH')
        window_class, text = r.text(), r.text(); extra = r.take('<H')
        if extra: r.pos += extra - 2  # Nonzero size includes its own WORD.
        result['controls'].append(dict(id=identifier, windowClass=window_class, text=text,
                                       rect=[x, y, w, h], style=hex(style), exstyle=hex(exstyle)))
    result['trailingBytes'] = len(raw) - r.pos
    if not 0 <= result['trailingBytes'] <= 3: raise ValueError('Unparsed dialog bytes: ' + str(result['trailingBytes']))
    return result


def menu(raw):
    r = Reader(raw); version, offset = r.take('<HH')
    if version != 0: return dict(version=version, decoded=False)
    r.pos = 4 + offset
    def items(depth=0):
        if depth > 16: raise ValueError('Menu nesting limit')
        rows = []
        while True:
            flags = r.take('<H'); popup = bool(flags & 16)
            identifier = None if popup else r.take('<H')
            row = dict(id=identifier, flags=hex(flags), text=r.text())
            if popup: row['children'] = items(depth + 1)
            rows.append(row)
            if flags & 128: return rows
    result = dict(version=version, decoded=True, items=items())
    if r.pos != len(raw): raise ValueError('Unparsed menu bytes')
    return result


def inventory_module(path, filename, digest):
    raw = path.read_bytes(); assert sha(raw) == digest, filename
    result = dict(file=filename, sha256=digest, bytes=len(raw), resources=[], strings=[])
    if raw[:2] != b'MZ':
        result.update(format='not-PE', reason='File extension alone is not an executable-format guarantee')
        return result
    pe = pefile.PE(data=raw); result['format'] = 'PE'; leaves = []
    def walk(directory, keys):
        for entry in directory.entries:
            key = str(entry.name) if entry.name is not None else entry.id
            if hasattr(entry, 'directory'): walk(entry.directory, keys + [key])
            else: leaves.append((keys + [key], entry.data.struct))
    if hasattr(pe, 'DIRECTORY_ENTRY_RESOURCE'): walk(pe.DIRECTORY_ENTRY_RESOURCE, [])
    for keys, data in leaves:
        blob = pe.get_data(data.OffsetToData, data.Size)
        assert len(keys) == 3 and len(blob) == data.Size
        kind, identifier, language = keys
        leaf = dict(type=kind, typeName=TYPES.get(kind, str(kind)), id=identifier, language=language,
                    codepage=data.CodePage, bytes=len(blob), sha256=sha(blob), rva=hex(data.OffsetToData),
                    offset=hex(pe.get_offset_from_rva(data.OffsetToData)))
        try:
            if kind == 6:
                r = Reader(blob)
                for index in range(16):
                    length = r.take('<H'); at = r.pos
                    text = blob[at:at + length * 2].decode('utf-16le'); r.pos += length * 2
                    if text: result['strings'].append(dict(id=(identifier - 1) * 16 + index, language=language,
                        text=text, rva=hex(data.OffsetToData + at)))
                # Delphi's library string blocks retain additional zero WORDs.
                # Record them; never discard nonzero suffix data as alignment.
                if r.pos != len(blob):
                    suffix = blob[r.pos:]
                    assert not any(suffix)
                    leaf['zeroTailBytes'] = len(suffix)
            elif kind == 5: leaf['dialog'] = dialog(blob)
            elif kind == 4: leaf['menu'] = menu(blob)
            elif kind == 9:
                assert len(blob) % 8 == 0
                leaf['accelerators'] = [dict(flags=hex(flags), key=key, command=cmd)
                    for flags, key, cmd, pad in struct.iter_unpack('<4H', blob)]
            elif kind == 2:
                header, width, height, planes, bits = struct.unpack_from('<IiiHH', blob)
                leaf['bitmap'] = dict(headerBytes=header, width=width, height=height, planes=planes, bits=bits)
            elif kind == 10: leaf['signatureHex'] = blob[:8].hex()
        except (ValueError, AssertionError, struct.error, UnicodeError) as error:
            leaf['decodeError'] = type(error).__name__ + ': ' + str(error)
        result['resources'].append(leaf)
    return result


def main():
    inventory = json.loads((ROOT / '분석/InstallerVersions/installer_inventory.json').read_text())
    payload = next(p for p in inventory['payloads'] if 'saver.exe' in p.get('containers', []))
    modules = []
    for row in payload['files']:
        name = Path(row['file']).name.lower()
        if Path(name).suffix not in ('.exe', '.dll') or name.startswith(('mfc', 'msvcrt')): continue
        modules.append(inventory_module(Path(row['extractedFile']), row['file'], row['sha256']))
    versions, seen = [], set()
    for payload in inventory['payloads']:
        for row in payload.get('files', []):
            if Path(row['file']).name.lower() != 'saver.exe' or row['sha256'] in seen: continue
            path = Path(row['extractedFile'])
            if not path.exists(): continue
            seen.add(row['sha256']); module = inventory_module(path, row['file'], row['sha256'])
            lesson = [s for s in module['strings'] if 61216 <= s['id'] <= 61251]
            canonical = json.dumps(lesson, ensure_ascii=False, sort_keys=True).encode()
            # Text identity deliberately excludes virtual addresses across builds.
            text = json.dumps([(s['id'], s['language'], s['text']) for s in lesson], ensure_ascii=False).encode()
            versions.append(dict(sha256=row['sha256'], stringCount=len(module['strings']), lessonCount=len(lesson),
                                 lessonTextSha256=sha(text), lessonResourceLocationsSha256=sha(canonical)))
    saver = next(m for m in modules if Path(m['file']).name.lower() == 'saver.exe')
    assert len(saver['strings']) == 60 and len([s for s in saver['strings'] if 61216 <= s['id'] <= 61251]) == 36
    assert len(versions) == 6 and {v['lessonCount'] for v in versions} == {36}
    assert len({v['lessonTextSha256'] for v in versions}) == 1
    recovered = json.loads((ROOT / '분석/Saver_Late/waiting_tutorial_resources.json').read_text())
    assert next(s['text'] for s in saver['strings'] if s['id'] == 61230) == next(s['text'] for s in recovered['resources'] if s['id'] == 61230)
    saver_dialogs = {r['id']: r['dialog'] for r in saver['resources'] if r['type'] == 5}
    assert saver_dialogs[133]['title'] == '연결정보' and saver_dialogs[152]['title'] == '접속할 호스트의 IP'
    assert any(c['id'] == 1009 and c['text'] == '닥세월드 1' for c in saver_dialogs[133]['controls'])
    launcher = next(m for m in modules if m['file'] == 'netgameb.exe')
    signup = next(r['dialog'] for r in launcher['resources'] if r['type'] == 5 and r['id'] == 130)
    assert signup['title'] == '사용자 등록' and len(signup['controls']) == 24
    original = next(r for r in inventory['payloads'] if 'saver.exe' in r.get('containers', []))
    saver_row = next(r for r in original['files'] if r['file'] == 'saver.exe')
    pe = pefile.PE(saver_row['extractedFile'])
    links = []
    for address, value, meaning in [(0x41D10F, '6898000000', 'Constructor uses dialog152'),
                                    (0x41D300, '6885000000', 'Constructor uses dialog133'),
                                    (0x417934, 'e82598feff', 'Caller reaches dialog133 constructor thunk40115E')]:
        assert pe.get_data(address - pe.OPTIONAL_HEADER.ImageBase, len(bytes.fromhex(value))) == bytes.fromhex(value)
        links.append(dict(module='saver.exe', address=hex(address), bytes=value, meaning=meaning,
                          limit='Does not prove the complete startup condition or modal activation'))
    failures = [dict(module=m['file'], type=r['typeName'], id=r['id'], error=r['decodeError'])
                for m in modules for r in m['resources'] if 'decodeError' in r]
    result = dict(scope='PE resource inventory and structured decoding; no runtime execution or automatic Unity changes.',
                  modules=modules, saverVersions=versions, verifiedCodeLinks=links, decodeErrors=failures,
                  limits=['Resource presence does not prove an active gameplay feature.',
                          'Launcher forms and support-library/MFC strings are separate from Saver world and combat windows.',
                          'No-resource DLLs may still contain code/data strings and indirect dispatch.',
                          'Versions without extracted files and non-PE formats are not silently treated as analyzed executable code.'])
    output = ROOT / '분석/Saver_Late/embedded_resource_audit.json'
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(modules=len(modules), peModules=sum(m['format'] == 'PE' for m in modules),
                         resourceLeaves=sum(len(m['resources']) for m in modules),
                         strings=sum(len(m['strings']) for m in modules), dialogs=sum(r['type'] == 5 for m in modules for r in m['resources']),
                         saverVersions=len(versions), decodeErrors=failures), ensure_ascii=False))
    if failures: raise ValueError('Resource decoding incomplete; inspect decodeErrors in the report')


if __name__ == '__main__': main()
