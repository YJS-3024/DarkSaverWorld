"""Static native troop menu evidence; never executes original binaries."""
from pathlib import Path
import hashlib,json,struct,sys
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'Tools/StaticAnalysisDeps'))
import pefile
root=Path(__file__).resolve().parents[1]
source=root/'extracted/Saver_Files/Saver'
files={name:(source/name).read_bytes() for name in ['GameCtl.dll','UserUnit.dll']}
checks=[]
def check(name,condition):
    if not condition: raise AssertionError(name)
    checks.append(name)
b=files['GameCtl.dll'];pe=pefile.PE(data=b)
def at(va,n):
    off=pe.get_offset_from_rva(va-pe.OPTIONAL_HEADER.ImageBase)
    return b[off:off+n]
labels=[('direct',0x1001a4ac,0x1000c071,1),('attack',0x1001a4a0,0x1000c0a5,2),('defense',0x1001a494,0x1000c0d9,3)]
for name,string,ref,choice in labels:
    check(name+' label push',at(ref,5)==b'\x68'+struct.pack('<I',string))
check('three choices',at(0x1000c167,2)==b'\x6a\x03')
check('callback setsolai',at(0x10009cf5,6)==bytes.fromhex('ff1548620110'))
imp=[i for e in pe.DIRECTORY_ENTRY_IMPORT for i in e.imports if i.address==0x10016248]
check('import signature',len(imp)==1 and imp[0].name==b'?setsolai@@YAXHH@Z')
u=files['UserUnit.dll'];up=pefile.PE(data=u)
def ua(va,n):
    off=up.get_offset_from_rva(va-up.OPTIONAL_HEADER.ImageBase)
    return u[off:off+n]
check('direct mode 1',ua(0x100013d3,11)==bytes.fromhex('c784020801000001000000'))
check('attack mode 2',ua(0x1000140a,6)==bytes.fromhex('c70102000000'))
check('defense mode 8',ua(0x10001499,6)==bytes.fromhex('c70108000000'))
report={'sources':{k:hashlib.sha256(v).hexdigest() for k,v in files.items()},'checks':checks,
 'choices':[{'name':n,'string':hex(v),'push':hex(r),'choice':c} for n,v,r,c in labels],
 'scope':'Original three-choice menu and commander mode storage only. Local targeting, range, tie policy, AP and damage are not recovered server rules.'}
output=root/'reports/troop_orders_reanalysis.json';output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('PASS native troop evidence checks='+str(len(checks)))
