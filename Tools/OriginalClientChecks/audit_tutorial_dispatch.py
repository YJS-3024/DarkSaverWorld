"""Capture late tutorial dispatch evidence. Requires objdump; no original process runs."""
import argparse,hashlib,json,re,struct,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
EXPECTED='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
def main(path):
 b=path.read_bytes();assert hashlib.sha256(b).hexdigest()==EXPECTED
 pe=struct.unpack_from('<I',b,60)[0];opt=pe+24;base=struct.unpack_from('<I',b,opt+28)[0];n=struct.unpack_from('<H',b,pe+6)[0];sh=opt+struct.unpack_from('<H',b,pe+20)[0]
 def string(va):
  for i in range(n):
   size,rva,rs,raw=struct.unpack_from('<4I',b,sh+i*40+8)
   if rva<=va-base<rva+rs:
    p=raw+va-base-rva;return b[p:b.index(b'\0',p)].decode('ascii',errors='replace')
  return ''
 def disasm(start,end):
  text=subprocess.check_output(['objdump','-d','-Mintel',f'--start-address={start}',f'--stop-address={end}',str(path)],text=True)
  return text.replace(str(path),'GameCtl.dll [SHA256 '+EXPECTED+']')
 asm=disasm('0x10010f20','0x10013d60');comparisons=[]
 for line in asm.splitlines():
  m=re.match(r'([0-9a-f]+):.*\bmov\s+(?:esi|edi),0x([0-9a-f]+)',line)
  if m and 0x100112f3<=int(m[1],16)<0x10013cea:
   s=string(int(m[2],16))
   if re.fullmatch('[A-Z][A-Z0-9_]*',s):comparisons.append({'address':'0x'+m[1],'token':s,'stringAddress':'0x'+m[2]})
 sequence=json.loads((ROOT/'분석/Saver_Late/tutorial_command_sequence.json').read_text());found={r['token'] for r in comparisons};missing=sorted(set(sequence['commandTypes'])-found)
 assert missing==['CHARDIR','CHARSETSKILL','SETHP','SETMP','WAITFORMOVEEND']
 output=ROOT/'Tools/OriginalClientChecks/evidence';output.mkdir(exist_ok=True)
 (output/'late-tutorial-dispatch.asm.txt').write_text(asm)
 (output/'late-tutorial-tokenizer.asm.txt').write_text(disasm('0x1000f110','0x1000f200'))
 result={'scope':'Late GameCtl interpreter at 0x10010F20 only. Full original playback not performed; other executors not generalized.', 'sha256':EXPECTED,'comparisons':comparisons,'tutorialCommandsWithComparison':sorted(set(sequence['commandTypes'])&found),'tutorialCommandsWithoutDedicatedComparison':missing, 'sourceLines':{c:[r['sourceLine'] for r in sequence['commands'] if r['command']==c] for c in missing},'controlFlow':{'tokenizer':'0x1000F110 finds space and returns first token; command extraction call at 0x10011278','advance':'0x100112E8..100112F0 increments script record index before dispatch','exactCompare':'0x10011303..1001132C compares full string through NUL; not prefix matching','fallback':'Last COMBINE_CLASS comparison at 0x10013A70; non-match at 0x10013AA6 jumps to 0x10013CEA cleanup, returns 1 at 0x10013D42','limit':'No dedicated dispatch for these five spellings in this interpreter. Do not equate SETHP/SETMP to HPSET/MPSET, WAITFORMOVEEND to WAITMOVEEND, or infer skill/direction assignment from the text alone.'}}
 (ROOT/'분석/Saver_Late/tutorial_dispatch_audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 print(f'PASS: 44 tutorial command spellings; {44-len(missing)} with comparison, {len(missing)} without dedicated comparison in late interpreter.')
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('gamectl',type=Path);main(p.parse_args().gamectl)
