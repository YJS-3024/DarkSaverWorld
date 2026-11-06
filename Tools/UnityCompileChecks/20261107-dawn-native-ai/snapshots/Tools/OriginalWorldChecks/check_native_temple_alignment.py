"""Read-only native window63 confirmation/cancel and AC request evidence.
No original process, Unity Play, NPC dispatch or server policy execution.
"""
import hashlib,json,sys
from pathlib import Path
from check_native_commanders import Native
from native_ai_emulator import UC_X86_REG_ECX
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'))
from native_pe import PE
SHA='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
class Alignment(Native):
 def __init__(self):
  super().__init__();self.closed=[];self.hooks.update({0x452900:'window-id',0x4758e0:'close-window'})
  self.write(self.window,0x4853e4);self.write(self.window+0xa0,self.manager)
 def hook(self,uc,a,size,data):
  name=self.hooks.get(a)
  if name=='window-id':self.ret(63)
  elif name=='close-window':self.closed.append(self.args(1)[0]);self.ret(0,4)
  else:super().hook(uc,a,size,data)
def main():
 pe=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');assert pe.sha256==SHA
 assert pe.thunk(0x401712)==0x452bc0 and pe.thunk(0x401276)==0x4758e0
 assert pe.string(0x490944)=='당신의 성향을 악으로 바꾸시겠습니까?'
 assert '%ld' in pe.string(0x490978)
 factory=ROOT/'분석/Ghidra/Saver_Late/Bank/factory-473E30.c';text=factory.read_text();part=text.split('case 0x3f:',1)[1].split('break;',1)[0]
 assert 'scene29_lsc'in part and '004528b0'in part
 work=Path('/tmp/ds-native-temple-alignment');work.mkdir(exist_ok=True)
 rows=[]
 for fee in [0,1,255,256,65535,65536,2147483647,2147483648,4294967295]:
  for confirm in [False,True]:
   native=Alignment();native.write(native.window+0xb4,fee);native.run(0x452970,[0 if confirm else 1],native.window)
   packet='ac'+fee.to_bytes(4,'little').hex() if confirm else None
   assert native.sent==([packet]if confirm else []) and native.closed==[63]
   rows.append(dict(feeBits=fee,confirm=confirm,packets=native.sent,closed=native.closed))
 for name,start,end in [('constructor',0x4528b0,0x452900),('click',0x452970,0x4529a3),('text',0x4529e0,0x452b20),('request',0x452bc0,0x452c1f)]:
  (work/(name+'.asm')).write_text(pe.disasm(start,end))
 report=dict(nativeSha256=SHA,window=63,scene=29,cases=len(rows),mismatches=0,opcode='AC',payload='original +B4 DWORD, little-endian; no target-direction byte',text=dict(feeZero=pe.string(0x490944),feeNonzero=pe.string(0x490978),good=pe.string(0x490974),evil=pe.string(0x490970)),vectors=rows,nativeCodeExecuted=True,unityPlay=False,scope='Actual 452970 confirm/cancel and452BC0 serializers execute; window-id/manager removal/final network send are isolated boundaries. Factory63/scene29 and draw strings/fee branch are static evidence. NPC dispatch, incoming quote format, operating fee/current alignment, approval and money persistence remain unverified.',logs=str(work),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [Path(__file__).resolve(),factory,ROOT/'Tools/OriginalWorldChecks/check_native_commanders.py']})
 Path(__file__).with_name('native-temple-alignment-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['vectors','sources']},ensure_ascii=False))
if __name__=='__main__':main()
