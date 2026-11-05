"""Validate partial typed layouts against compiled offsets and original x86 code."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import struct

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/ScenarioPlacementChecks'))
from audit_initial_roster_sources import packet_probe, PE


def snapshot_probes(ctl, units):
    results = []
    for subtype in (0x50, 0x51):
        for index, unit in enumerate((0, 1, 400, 999)):
            job = (100, 200, 401, 767)[index]
            x, y = (5, 19, 0, 199)[index], (7, 0, 199, 198)[index]
            for value in (0, 1, 0x7fffffff, 0x80000000, 0xffffffff):
                magic = [101, 301, -1, 701, 0, -1, -1, -1]
                skills = [10101, 0, -1, 21101, -1, -1, -1, -1]
                if subtype == 0x50:
                    words = [unit, index, job, -1, 2, 777, 200, 73, 20, 11, 3, 4, 6, 1, x, y]
                    dwords = [value, value ^ 0xffffffff]
                    mapping = {0: 0, 1: 2, 2: 3, 6: 4, 7: 6, 8: 7, 11: 10, 12: 11,
                               13: 12, 18: 14, 19: 15, 22: 8, 23: 9, 24: 1, 43: 13}
                else:
                    words = [unit, index, job, -1, 12, 2, 777, 200, 73, 80, 37, 5, 654, 321,
                             20, 11, 3, 4, 6, 1, x, y] + magic + skills + [32767, -32768, 7, 8, 9, 10, 11]
                    dwords = [value ^ 0xffffffff, value, 0x80000080, 0x80000010]
                    mapping = {0: 0, 1: 2, 2: 3, 3: 4, 6: 5, 7: 7, 8: 8, 9: 9, 10: 10,
                               11: 16, 12: 17, 13: 18, 14: 12, 15: 13, 17: 11, 18: 20,
                               19: 21, 22: 14, 23: 15, 24: 1, 43: 19}
                names = [b'', b'A', '검사'.encode('cp949'), b'a' * 255]
                name = names[index];owner = names[(index + 1) % len(names)]
                body = struct.pack('<' + 'h' * len(words), *words) + struct.pack('<' + 'I' * len(dwords), *dwords)
                body += bytes([len(name)]) + name
                if subtype == 0x51:body += bytes([len(owner)]) + owner
                expected = [0] * 44;expected[16] = 50
                for target, source in mapping.items():expected[target] = words[source] & 0xffffffff
                expected[25:33] = [v & 0xffffffff for v in (magic if subtype == 0x51 else [-1] * 8)]
                expected[33:41] = [v & 0xffffffff for v in (skills if subtype == 0x51 else [-1] * 8)]
                if subtype == 0x50:expected[20:22] = dwords
                else:expected[4:6] = [dwords[1], dwords[0]];expected[20:22] = dwords[2:4]
                result = packet_probe(ctl, units, subtype, unit, job, x, y, body=body, details=True)
                assert result['bodyConsumed'] == len(body), (subtype, 'native read extent')
                assert result['unitInfoDwords'] == {hex(at * 4): v for at, v in enumerate(expected) if at not in (41, 42)}
                copied = dict(experience=4, nextExperience=5, unit=0, level=3, job=1, movementType=6,
                              hp=8, maxHp=7, mp=10, maxMp=9, attackRange=43, movementBudget=13,
                              commandRange=17, maxAp=22, ap=23, team=24, appearance=2, x=18, y=19)
                assert result['runtimeFields'] == dict(active=1, enabled=1, **{key: expected[at] for key, at in copied.items()})
                assert result['magic'] == (magic[:magic.index(-1)] if subtype == 0x51 else [])
                assert result['skills'] == (skills[:skills.index(-1)] if subtype == 0x51 else [])
                results.append(result)
    return results


def main():
    header = ROOT / '분석/Ghidra/NativeUnitLayout.h'
    subprocess.run(['cc', '-std=c11', '-Wall', '-Wextra', '-Werror', '-x', 'c', '-fsyntax-only', str(header)], check=True)
    base = Path('/workspace/.tools/ds-ghidra/installed/saver')
    ctl, units = PE(base / 'GameCtl.dll'), PE(base / 'UserUnit.dll')
    assert ctl.sha256 == 'f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
    assert units.sha256 == '74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b'
    probes = [packet_probe(ctl, units, subtype, *values) for subtype in (0x50, 0x51)
              for values in ((400, 401, 14, 28), (410, 502, 5, 17), (700, 701, 19, 6))]
    snapshots = snapshot_probes(ctl, units)
    result = dict(compiler='C11 sizeof/offsetof static assertions passed',
                  headerSha256=hashlib.sha256(header.read_bytes()).hexdigest(),
                  originalGameCtlSha256=ctl.sha256, originalUserUnitSha256=units.sha256,
                  nativeProbes=probes, snapshotCases=len(snapshots), snapshotProbes=snapshots,
                  sources={p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in [header, Path(__file__), ROOT/'Tools/ScenarioPlacementChecks/audit_initial_roster_sources.py']},
                  scope='Synthetic5C50/51 messages execute original x86 dispatcher/parser and resetcharacter through1000B43F (before MagicFlag). Compare42 integer unitinfo fields,21 runtime fields and first-minus-one spell/skill lists, including uint32 and signed WORD boundaries and exact consumed body length. CString content/allocator/path destruction modeled; status/appearance load after the stop is not executed. No original game process, Unity Play, server formula, or historical deployment.')
    out = ROOT / '분석/Ghidra/unit-layout-check.json'
    out.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(compiledLayout=True, nativeCases=len(probes), snapshotCases=len(snapshots), output=str(out.relative_to(ROOT)))))


if __name__ == '__main__':
    main()
