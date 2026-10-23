"""Verify this installed client's magic field mapping without executing its code."""
import argparse
import hashlib
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent / "python-deps"))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86_const import X86_OP_MEM, X86_OP_REG, X86_REG_ESI, X86_REG_EAX

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("path")
parser.add_argument("--gamectl", help="Optionally cross-check request arithmetic in GameCtl.dll")
args = parser.parse_args()
source = Path(args.path)
before = hashlib.sha256(source.read_bytes()).hexdigest()
pe = pefile.PE(str(source))
base = pe.OPTIONAL_HEADER.ImageBase
assert base == 0x10000000, "Unexpected binary layout"
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True

def instructions(start, end):
    return md.disasm(pe.get_data(start-base, end-start), start)

# This loader holds ESI = new-record base + 0x138 and pushes sscanf outputs
# right-to-left. Track only LEA-to-register and PUSH-register in that range.
registers = {}
pushes = []
for ins in instructions(0x10001FD6, 0x1000201D):
    op = ins.operands
    if ins.mnemonic == "lea" and op[0].type == X86_OP_REG and op[1].type == X86_OP_MEM:
        assert op[1].mem.base == X86_REG_ESI and op[1].mem.index == 0
        registers[op[0].reg] = 0x138 + op[1].mem.disp
    elif ins.mnemonic == "push" and op[0].type == X86_OP_REG:
        pushes.append(0x138 if op[0].reg == X86_REG_ESI else registers[op[0].reg])
offsets = list(reversed(pushes))
expected = [0, 4] + list(range(0x104, 0x140, 4))
assert offsets == expected, f"Unexpected sscanf field sequence: {offsets}"
fmt = pe.get_data(0x10005250-base, 128).split(b"\0", 1)[0].decode("ascii")
assert fmt.count("%") == 17, f"Unexpected format: {fmt}"

# Names follow the previously audited Magictbl header, not inferred from size.
names = ["ID", "Name", "Attribute", "Range", "Area", "MP", "Time", "Team",
         "Unit", "EA", "EB", "EC", "TICK", "LVPT", "Pattern", "IconX", "IconY"]
print("NEW_LAYOUT_VERIFIED", " ".join(f"{name}=+0x{offset:x}" for name, offset in zip(names, offsets)))

# GetMagicTbl creates a legacy buffer from the new row. Track only direct
# new-row reads; pattern-table reads and strings are deliberately excluded.
aliases = {}
mapping = {}
for ins in instructions(0x1000266E, 0x10002804):
    op = ins.operands
    if ins.mnemonic != "mov" or len(op) != 2:
        continue
    if op[0].type == X86_OP_REG:
        aliases.pop(op[0].reg, None)
        if op[1].type == X86_OP_MEM and 0x1002DBE0 <= op[1].mem.disp < 0x1002DD20:
            aliases[op[0].reg] = op[1].mem.disp - 0x1002DBE0
    elif op[0].type == X86_OP_MEM and op[0].mem.base == 0 and op[0].mem.index == 0 and op[1].type == X86_OP_REG:
        dest = op[0].mem.disp
        if 0x10005388 <= dest < 0x10005500 and op[1].reg in aliases:
            mapping[dest-0x10005388] = aliases[op[1].reg]
assert mapping[0] == 0, "Legacy first field is not the expected ID field"
assert mapping[4] == 0x110, "Legacy +4 is not the expected MP field"
assert mapping[0x170] == 0x104, "Legacy fifth cost input is not attribute"
assert [mapping[x] for x in (8, 12, 16, 20)] == [0x108, 0x10C, 0x118, 0x11C]
assert 0x12C not in mapping.values(), "TICK appears in direct legacy field copies"
print("LEGACY_FIELDS_VERIFIED +0=ID; +4=MP; sum-inputs=Range+Area+Team+Unit+Attribute")
print("TICK_NOT_DIRECTLY_COPIED_TO_LEGACY", True)
if args.gamectl:
    control_source = Path(args.gamectl)
    control_before = hashlib.sha256(control_source.read_bytes()).hexdigest()
    control_pe = pefile.PE(str(control_source))
    assert control_pe.OPTIONAL_HEADER.ImageBase == base
    for start, end in ((0x1000E421, 0x1000E440), (0x1000E572, 0x1000E591)):
        reads = []
        multipliers = []
        for ins in md.disasm(control_pe.get_data(start-base, end-start), start):
            op = ins.operands
            if len(op) == 2 and op[1].type == X86_OP_MEM and op[1].mem.base == X86_REG_EAX:
                if ins.mnemonic == "mov":
                    reads.append(op[1].mem.disp)
                elif ins.mnemonic == "imul":
                    multipliers.append(op[1].mem.disp)
        assert reads == [0x170, 0x14, 0x10, 0xC, 8], f"Unexpected reads at {start:#x}: {reads}"
        assert multipliers == [0], f"Unexpected multiplier at {start:#x}: {multipliers}"
    assert control_before == hashlib.sha256(control_source.read_bytes()).hexdigest()
    print("REQUEST_ARITHMETIC_VERIFIED ID*(Attribute+Unit+Team+Area+Range); not TICK or MP")
unchanged = before == hashlib.sha256(source.read_bytes()).hexdigest()
assert unchanged, "Input changed during inspection"
print("SOURCE_UNCHANGED", unchanged)
