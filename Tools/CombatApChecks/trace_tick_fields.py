"""Inspect selected import calls and tick field writes; never execute an input DLL."""
import argparse
import hashlib
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent / "python-deps"))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86_const import X86_OP_MEM, X86_REG_ESP, X86_REG_EBP

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("path")
parser.add_argument("--match", default="GetCharAbility")
parser.add_argument("--tick-writes", action="store_true")
args = parser.parse_args()
source = Path(args.path)
before = hashlib.sha256(source.read_bytes()).hexdigest()
pe = pefile.PE(str(source))
imports = {entry.address: entry.name.decode(errors="replace")
           for library in pe.DIRECTORY_ENTRY_IMPORT for entry in library.imports if entry.name}
section = next(s for s in pe.sections if s.Name.rstrip(b"\0") == b".text")
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
md.skipdata = True
instructions = list(md.disasm(section.get_data(), pe.OPTIONAL_HEADER.ImageBase + section.VirtualAddress))
hits = []
for i, instruction in enumerate(instructions):
    if instruction.id == 0 or not instruction.operands:
        continue
    operand = instruction.operands[0]
    if args.tick_writes:
        if operand.type == X86_OP_MEM and operand.mem.base not in (X86_REG_ESP, X86_REG_EBP) and operand.mem.disp in (0x5c, 0x60) and instruction.mnemonic in ("mov", "add", "sub", "inc", "dec"):
            hits.append((i, "candidate tick-field write; struct identity requires follow-up"))
        continue
    if instruction.mnemonic != "call":
        continue
    if operand.type == X86_OP_MEM and operand.mem.base == 0 and operand.mem.index == 0:
        name = imports.get(operand.mem.disp & 0xffffffff, "")
        if args.match.lower() in name.lower():
            hits.append((i, name))
print(f"MATCHES {args.match if not args.tick_writes else 'tick write candidates'}: {len(hits)}")
for index, name in hits[:12]:
    print(f"CALL 0x{instructions[index].address:x} {name}")
    for instruction in instructions[max(0, index-3):index+(5 if args.tick_writes else 24)]:
        print(f"0x{instruction.address:08x}: {instruction.mnemonic:7} {instruction.op_str}")
print("SOURCE_UNCHANGED", before == hashlib.sha256(source.read_bytes()).hexdigest())
