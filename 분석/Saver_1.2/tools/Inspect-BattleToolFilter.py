"""Recheck the original battle tool inventory filter without executing the client."""
from pathlib import Path
import sys,hashlib,json
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'Tools/StaticAnalysisDeps'))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
root=Path(__file__).resolve().parents[1]
b=(root/'extracted/Saver_Files/Saver/GameCtl.dll').read_bytes();pe=pefile.PE(data=b)
checks={0x10009399:'8b480c',0x1000939f:'83f907',0x100093a2:'752c',0x100093bc:'8994ce04020000',0x100093ad:'8984d608020000',0x100093c9:'40',0x1000ae3d:'68a0000000',0x1000ae42:'6a40',0x1000ae4a:'6880000000',0x1000ae4f:'6a20',0x1000afad:'68a0000000',0x1000afb2:'6a60',0x1000afba:'6880000000',0x1000afbf:'6a40'}
for va,expected in checks.items():
    off=pe.get_offset_from_rva(va-pe.OPTIONAL_HEADER.ImageBase)
    assert b[off:off+len(bytes.fromhex(expected))]==bytes.fromhex(expected),hex(va)
# Constructor copies nine (x,y) pairs into 32x32 destination rectangles.
off=pe.get_offset_from_rva(0x9300)
regs={'ecx':-32,'eax':32,'edi':0};slots={}
for ins in Cs(CS_ARCH_X86,CS_MODE_32).disasm(b[off:off+0x5d],0x10009300):
    if ins.mnemonic=='mov' and ins.op_str.startswith('dword ptr [esp + '):
        address,register=ins.op_str.split('], ')
        slots[int(address.split('+ ')[1],16)]=regs[register]
points=[(slots[0x10+i*8],slots[0x14+i*8]) for i in range(9)]
assert points==[(-32,-32),(0,-32),(32,-32),(-32,32),(0,32),(32,32),(-32,0),(32,0),(0,0)]
for va,expected in {0x10009b57:'83c606',0x10009b5a:'83c0fa'}.items():
    off=pe.get_offset_from_rva(va-pe.OPTIONAL_HEADER.ImageBase)
    assert b[off:off+3]==bytes.fromhex(expected)
checks.update({0x10009b57:'83c606',0x10009b5a:'83c0fa'})
report={'cellOffsets':points,'source':'GameCtl.dll','sha256':hashlib.sha256(b).hexdigest(),'checks':{hex(k):v for k,v in checks.items()},'meaning':'Tool constructor reads itemrecord+0x0c, includes only type 7, copies item ID and quantity. No item use effect, AP cost, target or consumption is inferred.'}
(root/'reports/battle_tool_filter_reanalysis.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('PASS native battle tool filter checks='+str(len(checks)+1))
