"""Isolated original x86 execution; allocator/CString/CArray are explicit models."""
import hashlib
import json
import re
import struct
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
from analyze_script_executor import ScriptHarness
from analyze_packet_requests import Harness
from unicorn.x86_const import UC_X86_REG_ECX


def packet(source):
    h = ScriptHarness(); h.lines([source, 'END']); received = []
    def resize():
        obj = h.u.reg_read(UC_X86_REG_ECX); n, grow = h.args(2)
        old, size = h.get(obj + 4), h.get(obj + 8)
        p = h.heap; h.heap += (n * 4 + 31) & ~15
        h.u.mem_write(p, bytes(n * 4))
        if old and size: h.u.mem_write(p, bytes(h.u.mem_read(old, min(n, size) * 4)))
        h.put(obj + 4, p, n, n); h.ret(clean=8)
    def capture():
        v = h.args(5); raw = struct.pack('<5I', *v); n = raw[12]
        received.append(dict(fields=list(struct.unpack_from('<hhhhI', raw)), targets=list(struct.unpack('<' + 'h' * n, bytes(h.u.mem_read(v[4], n * 2)))) if n else []))
        h.ret()
    h.stubs[0x10014410] = resize; h.stubs[0x10014634] = h.alloc
    h.stubs[0x1001461c] = lambda: h.ret()
    h.put(0x10016268, 0x300f0900); h.stubs[0x300f0900] = capture
    h.stubs[0x1000e3e0] = lambda: h.ret()
    h.step(); return received


def main():
    table = ROOT / '분석/Saver_1.2/decoded/all_arc/gameres/set/Magictbl.atr'
    ids = [int(m[1]) for line in table.read_text(encoding='cp949').splitlines() if (m := re.match(r'^\s*(\d+)\s', line))]
    assert 1 not in ids and len(ids) <= 1000
    first = packet('MAGIC 1 700 -1 -1 801 804')
    second = packet('MAGIC 2901 -2 16 9')
    assert first == [dict(fields=[1,700,-1,-1,0],targets=[801,804])]
    assert second == [dict(fields=[2901,-2,16,9,0],targets=[])]
    h = Harness('setunit.dll'); h.put(0x10269850, 1)
    h.u.mem_write(0x1002dbe0, bytes(1001 * 320))
    for i, magic in enumerate(ids): h.put(0x1002dbe0 + i * 320, magic)
    h.put(0x10005020, 0x7fffffff); h.u.mem_write(0x10005388, bytes([0xa5]) * 128)
    h.run(0x10002600, (1,))
    result = bytes(h.u.mem_read(0x10005388, 128))
    assert result[:4] == b'\xff' * 4 and result[4:] == bytes([0xa5]) * 124
    report = dict(scope='Original x86 main-step and exact-ID lookup with modeled external APIs and a table fixture seeded with real source IDs; not full game playback.',
        source_table_sha256=hashlib.sha256(table.read_bytes()).hexdigest(),magic1=first,magic2901=second,
        lookup_entry='0x10002600',undefined_id=1,lookup_id=-1,unchanged_cached_bytes=124,
        policy='Do not emulate stale cache or substitute magic101; skip undefined visual effect only.')
    (Path(__file__).parent / 'scenario01_native_magic_check.json').write_text(json.dumps(report,indent=2)+'\n')
    print('PASS native MAGIC packets and exact-ID miss/cache preservation')

if __name__ == '__main__': main()
