"""Read a bounded x86 code range; never load or execute the target DLL."""
import argparse
import struct

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path")
    parser.add_argument("--start", type=lambda v: int(v, 0), required=True)
    parser.add_argument("--size", type=lambda v: int(v, 0), default=0x180)
    args = parser.parse_args()
    pe = pefile.PE(args.path)
    imports = {}
    for library in getattr(pe, "DIRECTORY_ENTRY_IMPORT", []):
        for entry in library.imports:
            name = entry.name.decode(errors="replace") if entry.name else str(entry.ordinal)
            imports[entry.address] = library.dll.decode(errors="replace") + "!" + name
    symbols = {
        pe.OPTIONAL_HEADER.ImageBase + entry.address: entry.name.decode(errors="replace")
        for entry in getattr(getattr(pe, "DIRECTORY_ENTRY_EXPORT", None), "symbols", []) if entry.name
    }
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


if __name__ == "__main__":
    main()
