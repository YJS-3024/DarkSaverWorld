"""Static proof of the item table's linked magic field; no original execution."""
from pathlib import Path
import sys,hashlib,json,csv
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'Tools/StaticAnalysisDeps'))
import pefile
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_REG_ESP
root=Path(__file__).resolve().parents[1]
folder=root/'extracted/Saver_Files/Saver'
raw={name:(folder/name).read_bytes() for name in ['setunit.dll','GameCtl.dll']}
b=raw['setunit.dll'];p=pefile.PE(data=b);m=Cs(CS_ARCH_X86,CS_MODE_32);m.detail=True
start=0x10001bf1;off=p.get_offset_from_rva(start-p.OPTIONAL_HEADER.ImageBase)
delta=0;regs={};pushes=[]
for ins in m.disasm(b[off:off+0x172],start):
    if ins.mnemonic=='lea' and ins.operands[1].type==X86_OP_MEM and ins.operands[1].mem.base==X86_REG_ESP:
        regs[ins.operands[0].reg]=delta+ins.operands[1].mem.disp
    elif ins.mnemonic=='mov' and ins.operands[0].type==X86_OP_REG:regs.pop(ins.operands[0].reg,None)
    elif ins.mnemonic=='push':
        op=ins.operands[0];pushes.append(regs.get(op.reg) if op.type==X86_OP_REG else None);delta-=4
args=pushes[::-1][2:];base=delta+0xf4
assert len(args)==44 and args[0]==base and args[3]-base==0xc and args[17]-base==0x30
check={0x100099b5:'8b4730',0x100099b8:'85c0',0x100099ba:'7e7a',0x10009a02:'e8791e0000',0x10009a0d:'c782f8010000ff050000'}
b=raw['GameCtl.dll'];p=pefile.PE(data=b)
for address,value in check.items():
    off=p.get_offset_from_rva(address-p.OPTIONAL_HEADER.ImageBase)
    assert b[off:off+len(bytes.fromhex(value))]==bytes.fromhex(value),hex(address)
items=list(csv.reader((root/'catalogs/items.csv').open(encoding='utf-8-sig')))[1:]
magic={int(r[0]):r[1] for r in list(csv.reader((root/'converted/Magictbl.csv').open(encoding='utf-8-sig')))[1:]}
links=[{'itemId':int(r[0]),'magicId':int(r[17]),'magicName':magic.get(int(r[17]))} for r in items if r[3]=='7']
assert len(links)==26 and all(r['magicName'] is not None for r in links)
result={'sources':{n:hashlib.sha256(v).hexdigest() for n,v in raw.items()},'columnOffsets':{str(n):hex(a-base) for n,a in enumerate(args[:41]) if a is not None},'codeChecks':{hex(k):v for k,v in check.items()},'consumableLinks':links,'scope':'Column17 is linked magic, not an amount or item consumption rule. 0x5ff is client UI state, not a recovered network opcode. Server effects, amount/AP and consumption approvals remain separate.'}
(root/'reports/item_magic_link_reanalysis.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('PASS original item magic link; scanf columns=44, consumable links=26, code checks='+str(len(check)))
