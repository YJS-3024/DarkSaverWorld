import argparse
import struct
import sys

import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
from capstone.x86_const import X86_OP_MEM, X86_REG_INVALID


def import_map(pe):
    result = {}
    for module in getattr(pe, "DIRECTORY_ENTRY_IMPORT", []):
        dll = module.dll.decode(errors="replace")
        for symbol in module.imports:
            name = symbol.name.decode(errors="replace") if symbol.name else f"ordinal_{symbol.ordinal}"
            result[symbol.address] = f"{dll}!{name}"
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("path")
    parser.add_argument("--match", default="SpriteDraw")
    parser.add_argument("--target", type=lambda value: int(value, 0))
    args = parser.parse_args()

    pe = pefile.PE(args.path)
    imports = import_map(pe)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    text = next(section for section in pe.sections if section.Name.rstrip(b"\0") == b".text")
    code = text.get_data()
    start = image_base + text.VirtualAddress
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = True
    instructions = list(md.disasm(code, start))

    matched_imports = {address: name for address, name in imports.items() if args.match.lower() in name.lower()}
    for address, name in matched_imports.items():
        direct = code.count(b"\xff\x15" + struct.pack("<I", address))
        pointer = code.count(struct.pack("<I", address))
        print(f"IMPORT 0x{address:08X} {name} direct_ff15={direct} raw_pointer={pointer}")

    hits = []
    for index, insn in enumerate(instructions):
        if insn.mnemonic != "call" or not insn.operands:
            continue
        operand = insn.operands[0]
        if operand.type != X86_OP_MEM or operand.mem.base != X86_REG_INVALID or operand.mem.index != X86_REG_INVALID:
            continue
        target = operand.mem.disp & 0xFFFFFFFF
        name = imports.get(target)
        if name and args.match.lower() in name.lower():
            hits.append((index, name))

    if args.target is not None:
        target_rva = args.target - image_base
        try:
            target_offset = pe.get_offset_from_rva(target_rva)
            function_code = pe.__data__[target_offset : target_offset + 0x500]
            print(f"\nTARGET FUNCTION 0x{args.target:08X}")
            return_count = 0
            for insn in md.disasm(function_code, args.target):
                suffix = ""
                if insn.mnemonic == "call" and len(insn.bytes) == 6 and bytes(insn.bytes[:2]) == b"\xff\x15":
                    import_target = struct.unpack("<I", bytes(insn.bytes)[2:6])[0]
                    suffix = " ; " + imports.get(import_target, "")
                print(f"0x{insn.address:08X}: {insn.mnemonic:<7} {insn.op_str}{suffix}")
                if insn.mnemonic.startswith("ret"):
                    return_count += 1
                    if return_count >= 3:
                        break
        except Exception:
            pass
        target_bytes = struct.pack("<I", args.target)
        for section in pe.sections:
            section_data = section.get_data()
            pointer_offset = section_data.find(target_bytes)
            while pointer_offset >= 0:
                pointer_va = image_base + section.VirtualAddress + pointer_offset
                lo = max(0, pointer_offset - 32)
                hi = min(len(section_data), pointer_offset + 36)
                values = []
                for pos in range(lo - (lo % 4), hi - 3, 4):
                    value = struct.unpack("<I", section_data[pos : pos + 4])[0]
                    marker = "*" if pos == pointer_offset else " "
                    values.append(f"{marker}0x{image_base + section.VirtualAddress + pos:08X}=0x{value:08X}")
                print(f"\nTARGET POINTER in {section.Name.rstrip(bytes([0])).decode(errors='replace')} at 0x{pointer_va:08X}")
                print("\n".join(values))
                pointer_offset = section_data.find(target_bytes, pointer_offset + 1)
        for offset in range(0, len(code) - 5):
            if code[offset] != 0xE8:
                continue
            displacement = struct.unpack("<i", code[offset + 1 : offset + 5])[0]
            if start + offset + 5 + displacement != args.target:
                continue
            local_start = code.rfind(b"\x55\x8b\xec", max(0, offset - 0x1800), offset)
            local_start = local_start if local_start >= 0 else max(0, offset - 256)
            local = list(md.disasm(code[local_start : offset + 320], start + local_start))
            call_index = next((i for i, insn in enumerate(local) if insn.address == start + offset), None)
            print(f"\nTARGET CALL 0x{args.target:08X} at 0x{start + offset:08X} prologue=0x{start + local_start:08X}")
            if call_index is not None:
                for insn in local[max(0, call_index - 50) : call_index + 51]:
                    suffix = ""
                    if insn.mnemonic == "call" and len(insn.bytes) == 6 and bytes(insn.bytes[:2]) == b"\xff\x15":
                        target = struct.unpack("<I", bytes(insn.bytes)[2:6])[0]
                        suffix = " ; " + imports.get(target, "")
                    print(f"0x{insn.address:08X}: {insn.mnemonic:<7} {insn.op_str}{suffix}")

    for index, name in hits:
        print(f"\nCALL {name} at 0x{instructions[index].address:08X}")
        lo, hi = max(0, index - 35), min(len(instructions), index + 36)
        for insn in instructions[lo:hi]:
            suffix = ""
            if insn.mnemonic == "call" and insn.operands:
                operand = insn.operands[0]
                if operand.type == X86_OP_MEM and operand.mem.base == X86_REG_INVALID and operand.mem.index == X86_REG_INVALID:
                    suffix = " ; " + imports.get(operand.mem.disp & 0xFFFFFFFF, "")
            print(f"0x{insn.address:08X}: {insn.mnemonic:<7} {insn.op_str}{suffix}")

    if not hits:
        for address, name in matched_imports.items():
            pattern = b"\xff\x15" + struct.pack("<I", address)
            offset = code.find(pattern)
            while offset >= 0:
                prologue = code.rfind(b"\x55\x8b\xec", max(0, offset - 0x1000), offset)
                local_start = prologue if prologue >= 0 else max(0, offset - 128)
                local = list(md.disasm(code[local_start : offset + 192], start + local_start))
                call_index = next((i for i, insn in enumerate(local) if insn.address == start + offset), None)
                print(f"\nCALL {name} at 0x{start + offset:08X} prologue=0x{start + local_start:08X}")
                if call_index is not None:
                    for insn in local[max(0, call_index - 35) : call_index + 36]:
                        suffix = ""
                        if insn.mnemonic == "call" and len(insn.bytes) == 6 and bytes(insn.bytes[:2]) == b"\xff\x15":
                            target = struct.unpack("<I", bytes(insn.bytes)[2:6])[0]
                            suffix = " ; " + imports.get(target, "")
                        print(f"0x{insn.address:08X}: {insn.mnemonic:<7} {insn.op_str}{suffix}")
                offset = code.find(pattern, offset + 1)

    print(f"\nmatched_calls={len(hits)}")


if __name__ == "__main__":
    main()
