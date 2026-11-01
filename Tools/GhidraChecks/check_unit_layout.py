"""Validate partial typed layouts against compiled offsets and original x86 code."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/ScenarioPlacementChecks'))
from audit_initial_roster_sources import packet_probe, PE


def main():
    header = ROOT / '분석/Ghidra/NativeUnitLayout.h'
    subprocess.run(['cc', '-std=c11', '-Wall', '-Wextra', '-Werror', '-x', 'c', '-fsyntax-only', str(header)], check=True)
    base = Path('/workspace/.tools/ds-ghidra/installed/saver')
    ctl, units = PE(base / 'GameCtl.dll'), PE(base / 'UserUnit.dll')
    assert ctl.sha256 == 'f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
    assert units.sha256 == '74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b'
    probes = [packet_probe(ctl, units, subtype, *values) for subtype in (0x50, 0x51)
              for values in ((400, 401, 14, 28), (410, 502, 5, 17), (700, 701, 19, 6))]
    result = dict(compiler='C11 sizeof/offsetof static assertions passed',
                  headerSha256=hashlib.sha256(header.read_bytes()).hexdigest(),
                  originalGameCtlSha256=ctl.sha256, originalUserUnitSha256=units.sha256,
                  nativeProbes=probes,
                  scope='Synthetic messages through original x86 parser and resetcharacter prefix; CString/allocation/path destruction modeled. Not original game execution, Unity Play, or recovered historical deployment.')
    out = ROOT / '분석/Ghidra/unit-layout-check.json'
    out.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(compiledLayout=True, nativeCases=len(probes), output=str(out.relative_to(ROOT)))))


if __name__ == '__main__':
    main()
