"""Bounded read-only disassembly using local check dependencies; never runs the input DLL."""
from pathlib import Path
import argparse
import hashlib
import struct
import sys

check_dir = Path(__file__).resolve().parent
sys.path.insert(0, str(check_dir / "python-deps"))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("path")
parser.add_argument("--start", type=lambda value: int(value, 0), required=True)
parser.add_argument("--size", type=lambda value: int(value, 0), default=0x180)
args = parser.parse_args()
if not 0 < args.size <= 0x2000:
    parser.error("--size must be between 1 and 0x2000")
source = Path(args.path)
before = hashlib.sha256(source.read_bytes()).hexdigest()
pe = pefile.PE(str(source))
imports = {entry.address: entry.name.decode(errors="replace")
           for library in getattr(pe, "DIRECTORY_ENTRY_IMPORT", [])
           for entry in library.imports if entry.name}
export_directory = getattr(pe, "DIRECTORY_ENTRY_EXPORT", None)
symbols = {pe.OPTIONAL_HEADER.ImageBase + entry.address: entry.name.decode(errors="replace")
           for entry in (export_directory.symbols if export_directory else []) if entry.name}
data = pe.get_data(args.start - pe.OPTIONAL_HEADER.ImageBase, args.size)
for instruction in Cs(CS_ARCH_X86, CS_MODE_32).disasm(data, args.start):
    annotation = ""
    raw = bytes(instruction.bytes)
    if len(raw) == 6 and raw[:2] in (b"\xff\x15", b"\xff\x25"):
        annotation = imports.get(struct.unpack("<I", raw[2:])[0], "")
    elif len(raw) == 5 and raw[0] in (0xE8, 0xE9):
        target = instruction.address + 5 + struct.unpack("<i", raw[1:])[0]
        annotation = symbols.get(target, "")
    print(f"{instruction.address:#010x}: {instruction.mnemonic:7} {instruction.op_str}" +
          (f" ; {annotation}" if annotation else ""))
assert before == hashlib.sha256(source.read_bytes()).hexdigest(), "Input changed"
print("SOURCE_UNCHANGED", True)
