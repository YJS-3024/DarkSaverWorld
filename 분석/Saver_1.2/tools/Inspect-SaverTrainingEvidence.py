"""Read-only string/xref audit for native training event handlers. Never executes PE files."""
import argparse
import pathlib
import struct

import pefile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=pathlib.Path)
    args = parser.parse_args()
    needles = [b"REGEN", b"regen", b"CHARPOS", b"TICK", b"INF", b".evt", b".EVT", b".dee", b".DEE", b".deo", b".DEO",
               b".ai", b".AI", "훈련".encode("cp949"), "수련".encode("cp949"), b"ODNORMAL"]
    for name in ["Saver.exe", "GameCtl.dll", "DManage.dll", "setunit.dll", "userunit.dll"]:
        path = args.directory / name
        data = path.read_bytes()
        pe = pefile.PE(data=data)
        base = pe.OPTIONAL_HEADER.ImageBase
        print(name)
        for library in getattr(pe, "DIRECTORY_ENTRY_IMPORT", []):
            for entry in library.imports:
                if not entry.name or not entry.name.startswith((b"?dmSetChar@@", b"?dmSetChar2@@", b"?resetcharacter@@")):
                    continue
                print(f"  import {entry.name.decode(errors='replace')} IAT={entry.address:#010x}")
                for section in pe.sections:
                    if not section.Characteristics & 0x20000000:
                        continue
                    code = section.get_data()
                    marker = b"\xff\x15" + struct.pack("<I", entry.address)
                    position = -1
                    while True:
                        position = code.find(marker, position + 1)
                        if position < 0:
                            break
                        print(f"    indirect call {base + section.VirtualAddress + position:#010x}")
        for needle in needles:
            offset = -1
            while True:
                offset = data.find(needle, offset + 1)
                if offset < 0:
                    break
                va = base + pe.get_rva_from_offset(offset)
                end = min(data.find(b"\0", offset), offset + 100)
                if end < offset:
                    end = offset + 100
                print(f"  {va:#010x}: {data[offset:end].decode('cp949',errors='replace')!r}")
                address = struct.pack("<I", va)
                for section in pe.sections:
                    if not section.Characteristics & 0x20000000:
                        continue
                    code = section.get_data()
                    position = -1
                    while True:
                        position = code.find(address, position + 1)
                        if position < 0:
                            break
                        reference = base + section.VirtualAddress + position
                        print(f"    code operand xref {reference:#010x}")


if __name__ == "__main__":
    main()
