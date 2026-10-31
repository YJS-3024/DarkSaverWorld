"""Exact interpreter branch-site inventory; machine inventory is not semantic proof."""
import argparse,json,re
from pathlib import Path
from native_pe import PE
from native_cfg import CFG
p=argparse.ArgumentParser();p.add_argument('gamectl',type=Path);a=p.parse_args();pe=PE(a.gamectl)
assert pe.sha256=='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
c=CFG(pe);root=Path(__file__).resolve().parents[2];walk=c.walk(0x10010f20)
branches=[]
for at,(n,op) in sorted(c.ins.items()):
 if not 0x10010f20<=at<0x10013d60:continue
 if op.split()[0].startswith('j'):
  m=re.fullmatch(r'j\w+\s+0x([0-9a-f]+)',op);target=int(m[1],16) if m else None
  branches.append({'at':hex(at),'instruction':op,'taken':hex(target) if target else None,'fallthrough':None if op.startswith('jmp') else hex(at+n),'interpretation':'explicit register/flag control transfer; source command attribution requires path conditions'})
imports=pe.imports();calls=[]
for row in walk['calls']:
 row=dict(row)
 if row['target']:
  dest=int(row['target'],16)
  try:
   code=pe.read(dest,6)
   if code[:2]==b'\xff\x25':row['thunkImport']=imports.get(int.from_bytes(code[2:6],'little'))
  except ValueError:pass
 calls.append(row)
result={'sha256':pe.sha256,'range':['0x10010F20','0x10013D60'],'entryInstructionCount':walk['instructionCount'],'branches':branches,'calls':calls,'unresolvedTransfers':walk['unresolved'],'switches':walk['switches'],'semanticCompletion':False,'reason':'Enumerating jump destinations cannot prove command-specific path conditions or indirect callee semantics.'}
path=root/'분석/Saver_Late/tutorial_branch_inventory.json';path.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print({'branches':len(branches),'calls':len(calls),'unresolvedTransfers':len(walk['unresolved'])})
