"""Read-only character-info provenance checks; never execute the original client."""
import hashlib
import json
import struct
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(root / "Tools/StaticAnalysisDeps"))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

source = root / "\uBD84\uC11D/Saver_1.2/extracted/Saver_Files/Saver/Saver.exe"
pe = pefile.PE(str(source))
base = pe.OPTIONAL_HEADER.ImageBase
decoder = Cs(CS_ARCH_X86, CS_MODE_32)
checks = []


def check(address, mnemonic, operands, meaning):
    actual = next(decoder.disasm(pe.get_data(address - base, 15), address))
    assert (actual.mnemonic, actual.op_str) == (mnemonic, operands), (hex(address), actual.mnemonic, actual.op_str)
    checks.append({"address": hex(address), "instruction": f"{mnemonic} {operands}", "meaning": meaning})


for args in [
    (0x43ba57, "mov", "al, byte ptr [ebx]", "Dispatch reads message byte"),
    (0x43ba59, "add", "eax, -0x32", "Dispatch index begins at 0x32"),
    (0x43babf, "call", "0x42c3b0", "Header response updates separate state"),
    (0x43bb2e, "call", "0x42c4f0", "Record response updates information slots"),
    (0x43e53f, "call", "0x43e310", "Header reads string byte length after six words"),
    (0x43e544, "mov", "cl, byte ptr [eax]", "String byte length"),
    (0x43e560, "call", "0x43e330", "Copies length-delimited string"),
    (0x43e56e, "call", "0x43e310", "Header reads following flag byte"),
    (0x43e57a, "and", "ecx, 0x80000001", "Zero-extended flag is reduced to its low bit"),
    (0x43e366, "mov", "byte ptr [ebx + eax], 0", "String helper appends terminator"),
    (0x42c3f6, "mov", "al, byte ptr [edi + 0xc]", "Decoded header string length at +0xc"),
    (0x42c3fb, "je", "0x42c412", "Zero name length selects fallback"),
    (0x42c3fd, "lea", "eax, [edi + 0xd]", "Decoded name bytes start at +0xd"),
    (0x42c400, "lea", "ecx, [esi + 0x10]", "State string destination is +0x10"),
    (0x42c412, "push", "0x450ce4", "Fallback string is supplied by original data"),
    (0x41334c, "mov", "edx, dword ptr [esi + 0xb0]", "Weapon label reads separate state"),
    (0x413356, "mov", "ecx, dword ptr [edx + 0x10]", "Weapon label reads string, no item lookup here"),
    (0x42c4f9, "movsx", "eax, word ptr [esi]", "Record storage slot comes from first signed word"),
    (0x42c50b, "mov", "ecx, 0xb5", "Copies 181 dwords = 724 bytes"),
    (0x42c510, "lea", "edi, [eax + 4]", "Record payload follows four-byte prefix"),
    (0x42c513, "rep movsd", "dword ptr es:[edi], dword ptr [esi]", "Whole decoded payload copied without local hire aggregation"),
    (0x42c516, "mov", "dword ptr [eax + 0x2d8], 1", "Information slot marked occupied"),
    (0x42c754, "cmp", "dword ptr [ecx], 0", "Selection walks occupied slots"),
    (0x42c759, "inc", "esi", "Occupied-record ordinal"),
    (0x42c75a, "cmp", "esi, edi", "Compares requested ordinal"),
    (0x42c75f, "add", "ecx, 0x2dc", "Slot stride is 732 bytes"),
    (0x42c765, "cmp", "eax, 0x14", "Selection scans at most 20 slots"),
    (0x413849, "mov", "ecx, dword ptr [esi + 0xb0]", "Information selection uses separate state manager"),
    (0x41384f, "call", "0x42c740", "Selects information record by ordinal"),
    (0x41385a, "mov", "dword ptr [esi + 0xb4], eax", "Stores selected record pointer"),
    (0x412a85, "mov", "eax, dword ptr [esi + 0xb4]", "Small picture uses selected record"),
    (0x412a8b, "add", "eax, 4", "Skips record prefix"),
    (0x412a8e, "mov", "ax, word ptr [eax + 0x96]", "Small picture class field at payload +0x96"),
    (0x412a95, "cmp", "ax, 0xffff", "Minus one suppresses class picture"),
    (0x412aa1, "call", "0x428010", "Maps class to character picture"),
    (0x412aa9, "call", "dword ptr [0x446668]", "Uses UserUnit character surface"),
    (0x412b4a, "mov", "ebp, dword ptr [esi + 0xb4]", "Quantity uses same selected record"),
    (0x412b64, "add", "ebp, 4", "Skips record prefix for text"),
    (0x412b92, "movsx", "edx, word ptr [ebp + 0x98]", "Quantity is signed word at payload +0x98"),
]:
    check(*args)

mapping = pe.get_data(0x43c004 - base, 0x60)
targets = struct.unpack("<9I", pe.get_data(0x43bfe0 - base, 36))
assert targets[mapping[0]] == 0x43ba74
assert targets[mapping[2]] == 0x43bac9
fallback = pe.get_string_at_rva(0x450ce4 - base).decode("cp949")
assert fallback == "\uc5c6\uc74c"
report = {
    "source": str(source.relative_to(root)).replace("\\", "/"),
    "sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
    "method": "Static PE/x86 inspection only; no original execution or network exchange",
    "instruction_checks_passed": len(checks),
    "dispatch_checks_passed": 2,
    "fallback_string_check_passed": True,
    "header_opcode": "0x32",
    "records_opcode": "0x34",
    "no_name_text": fallback,
    "checks": checks,
    "limits": [
        "Per-record class/count fields do not establish their game-level ownership semantics.",
        "No proof that +0x98 is the total of all hired units or living/deployed units.",
        "Separate weapon-label text does not establish an item ID, equipment slot, or modifier formula.",
        "Global header text and selected record have different provenance; do not assume all fields are per-selected-unit.",
        "Wire dispatch byte values do not establish a complete transport framing contract.",
    ],
}
output = root / "\uBD84\uC11D/Saver_1.2/reports/character_info_sources_reanalysis.json"
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"instruction_checks_passed": len(checks), "dispatch_checks_passed": 2, "fallback_string_check_passed": True, "sha256": report["sha256"]}))
