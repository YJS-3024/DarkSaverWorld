"""Verify later Saver world F1..F12 jump table independently of early addresses."""
import argparse,hashlib,json,struct,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def main(path):
 b=path.read_bytes();digest=hashlib.sha256(b).hexdigest();assert digest=='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
 def u16(o):return struct.unpack_from('<H',b,o)[0]
 def u32(o):return struct.unpack_from('<I',b,o)[0]
 pe=u32(60);opt=pe+24;sh=opt+u16(pe+20);base=u32(opt+28)
 def off(va):
  for i in range(u16(pe+6)):
   size,rva,rs,raw=struct.unpack_from('<4I',b,sh+i*40+8)
   if rva<=va-base<rva+rs:return raw+va-base-rva
  raise ValueError(hex(va))
 assert b[off(0x42bd1e):off(0x42bd1e)+6]==bytes.fromhex('8a88b8c34200')
 assert b[off(0x42bd24):off(0x42bd24)+7]==bytes.fromhex('ff248d88c34200')
 meanings=['도움말','보유 캐릭터','자기정보','장비·소지품','대화창','메모 창 (scene48)','기본 분기','환경설정','종료 확인','기본 분기','기본 분기','/공간이동 전달']
 ids=[33,[18,67],21,20,19,81,None,27,43,None,None,None];keys=[]
 for i in range(12):
  index=b[off(0x42c3b8)+0x70+i-13];target=u32(off(0x42c388)+index*4)
  keys.append({'key':f'F{i+1}','target':hex(target),'windowId':ids[i],'meaning':meanings[i]})
 assert all(keys[i-1]['target']=='0x42c373' for i in [7,10,11])
 assert keys[11]['target']=='0x42c33b'
 assert b[off(0x42c35c):off(0x42c35c)+6]==bytes.fromhex('ff90a4000000')
 p=off(0x48ff78);assert b[p:b.index(b'\0',p)].decode('cp949')=='/공간이동'
 asm=subprocess.check_output(['objdump','-d','-Mintel','--start-address=0x42bcf0','--stop-address=0x42c388',str(path)],text=True).replace(str(path),'Saver.exe [SHA256 '+digest+']')
 (ROOT/'Tools/OriginalClientChecks/evidence/late-world-keys.asm.txt').write_text(asm)
 # Verify expected internal window constants are in their own action blocks.
 for i,row in enumerate(keys):
  if row['windowId'] is None:continue
  start=int(row['target'],16);ends=[int(r['target'],16) for r in keys if int(r['target'],16)>start];end=min(ends)
  code=b[off(start):off(end)]
  for value in row['windowId'] if isinstance(row['windowId'],list) else [row['windowId']]:assert bytes([0x6a,value]) in code or b'\xbf'+struct.pack('<I',value) in code,(row,value)
 assert b[off(0x4670e0):off(0x4670e0)+6]==bytes.fromhex('b851000000c3')
 assert u32(off(0x485e80)+4)==0x402d0b
 assert b[off(0x490fc4):b.index(b'\0',off(0x490fc4))]==b'scene48.lsc'
 assert b[off(0x490fb8):b.index(b'\0',off(0x490fb8))].decode('cp949')=='/메모보기'
 for label,start,end in [('factory',0x47513b,0x4751cd),('constructor',0x467090,0x4670e6)]:
  excerpt=subprocess.check_output(['objdump','-d','-Mintel',f'--start-address={start}',f'--stop-address={end}',str(path)],text=True).replace(str(path),'Saver.exe [SHA256 '+digest+']')
  (ROOT/f'Tools/OriginalClientChecks/evidence/late-f6-{label}.asm.txt').write_text(excerpt)
 result={'scope':'World/shared-screen handler only; focused controls/global handlers and combat keys are separate. No server packet analysis.', 'sha256':digest,'handler':'0x42BCF0','keyIndexTable':'0x42C3B8','jumpTable':'0x42C388','keys':keys,'f6':{'windowId':81,'scene':'scene48.lsc','getId':'0x4670E0 returns 81','constructor':'0x467090 sets vtable 0x485E80','factory':'0x47513B uses scene48.lsc; 0x475177 calls constructor thunk 0x40209F','memoLiteral':'0x490FB8 /메모보기; factory at 0x47518D','limit':'Client window and command-string route only. Memo storage, server permissions and incoming contents excluded.'},'f12Difference':'Late virtual +0xA4; early +0xA0. Command text and flag1 retained. No destination or success inferred.'}
 (ROOT/'분석/Saver_Late/world_function_keys_audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 print('PASS: late world F1..F12 targets, window constants, default branches and F12 virtual slot verified.')
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('saver',type=Path);main(p.parse_args().saver)
