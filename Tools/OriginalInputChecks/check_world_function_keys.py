"""Audit original Saver1.2 world F-key jump table. Static evidence, not playback."""
import hashlib,json,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/Saver.exe'
b=SOURCE.read_bytes()
def u16(o):return struct.unpack_from('<H',b,o)[0]
def u32(o):return struct.unpack_from('<I',b,o)[0]
assert hashlib.sha256(b).hexdigest()=='9b74f465f4a9c5ab3564fd1b47026aedf7c11e021f09dbf27be81fe47dd5c8f0'
pe=u32(60);opt=pe+24;sections=opt+u16(pe+20);image_base=u32(opt+28)
def offset(va):
 rva=va-image_base
 for i in range(u16(pe+6)):
  o=sections+i*40;start,size,raw=u32(o+12),u32(o+8),u32(o+20)
  if start<=rva<start+size:return raw+rva-start
 raise ValueError(hex(va))
def cstring(va):
 o=offset(va);return b[o:b.index(b'\0',o)].decode('cp949')
def require(va,hexbytes):
 expected=bytes.fromhex(hexbytes);o=offset(va);assert b[o:o+len(expected)]==expected,hex(va)
require(0x43b597,'ff5050') # Active screen virtual key dispatch.
require(0x42eede,'8a88bcf44200')
require(0x42eee4,'ff248d90f44200')
for vtable in [0x448534,0x44878c]:assert u32(offset(vtable)+0x50)==0x42eeb0
records=[
 ('F1',0x42efbc,33,19,'도움말'),
 ('F2',0x42f048,[18,67],[2,42],'보유 캐릭터·장비·금액 요약창'),
 ('F3',0x42f0f5,21,6,'자기 정보'),
 ('F4',0x42f1ef,20,4,'장비 장착·소지품'),
 ('F5',0x42f2e9,19,3,'대화창'),
 ('F6',0x42f47c,None,None,'이 처리기의 기본 분기'),
 ('F7',0x42f47c,None,None,'이 처리기의 기본 분기'),
 ('F8',0x42f392,27,15,'환경설정'),
 ('F9',0x42f41e,43,27,'게임 종료 확인'),
 ('F10',0x42f47c,None,None,'이 처리기의 기본 분기'),
 ('F11',0x42f47c,None,None,'이 처리기의 기본 분기'),
 ('F12',0x42f444,None,None,'/공간이동 명령 전달'),
]
rows=[]
for i,(key,target,window,scene,meaning) in enumerate(records):
 index=b[offset(0x42f4bc)+0x70+i-13]
 actual=u32(offset(0x42f490)+index*4);assert actual==target,key
 rows.append(dict(key=key,target=hex(target),windowId=window,wlibScene=scene,meaning=meaning))
for address,value in {0x450c4c:'scene2.lsc',0x450a90:'scene42.lsc',0x450c28:'scene6.lsc',0x450c34:'scene4.lsc',0x450c40:'scene3.lsc',0x450bec:'scene15.lsc',0x450bc8:'scene19.lsc',0x450b50:'scene27.lsc',0x450d20:'/공간이동'}.items():assert cstring(address)==value
result=dict(scope='Original Saver1.2 world/shared-screen handler; static binary and Wlib artwork analysis, no original executable playback',source=str(SOURCE.relative_to(ROOT)),sha256=hashlib.sha256(b).hexdigest(),handler='0x42EEB0',sharedVtables=['0x448534','0x44878C'],keys=rows,notes=[
 'Window identifiers are not Wlib scene numbers. Factory and virtual GetId were traced separately.',
 'F2 chooses ID67 only when session+4 !=0 and session+0x24 ==3; otherwise ID18. Semantic meaning of that session condition is not assigned.',
 'F6/F7/F10/F11 go to default in this handler; this does not prove absence of every global or focused-control shortcut.',
 'F12 invokes virtual +0xA0 with /공간이동 and flag1; destination/server permission and success not established.',
 'Modal/focused controls may consume keys before this shared world handler.',
 'Late Saver executable hash dd512172... is cataloged but its extracted binary is not present here; these fixed addresses must not be reused for it.'
])
out=Path(__file__).with_name('world_function_keys.json');out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print('PASS: 12 original world F-key jump targets; two shared virtual slots; layout/command strings. Static inspection only.')
