"""Conservative intraprocedural x86 branch inventory; not a decompiler.
Calls are recorded, not followed. Indirect switches are over-approximations until
an analyst establishes their index bounds. Unresolved transfers remain explicit.
"""
import re,subprocess
class CFG:
 def __init__(self,pe):
  self.pe=pe;self.ins={}
  output=subprocess.check_output(['objdump','-d','-Mintel','--insn-width=16',str(pe.path)],text=True)
  for line in output.splitlines():
   m=re.match(r'\s*([0-9a-f]+):\s*((?:[0-9a-f]{2}\s+)+)\s*([a-z][^\n]*)$',line)
   if m:
    at=int(m[1],16);self.ins[at]=(len(m[2].split()),m[3].strip())
 def walk(self,start,limit=20000):
  pending=[start];seen={};calls=[];switches=[];unresolved=[]
  while pending and len(seen)<limit:
   at=pending.pop()
   if at in seen:continue
   if at not in self.ins:
    unresolved.append({'at':hex(at),'reason':'no decoded instruction'});continue
   n,op=self.ins[at];seen[at]=op;mn=op.split()[0];nextat=at+n
   direct=re.fullmatch(r'(?:j\w+|call)\s+0x([0-9a-f]+)',op)
   if mn=='call':
    target=int(direct[1],16) if direct else None
    resolved=self.pe.thunk(target) if target else None
    iat=re.search(r'ds:0x([0-9a-f]+)',op)
    imported=self.pe.imports().get(int(iat[1],16)) if iat else None
    calls.append({'at':hex(at),'instruction':op,'target':hex(resolved) if resolved else None,'import':imported})
   if mn.startswith('ret'):continue
   if mn in ('int3','ud2'):
    unresolved.append({'at':hex(at),'reason':'trap/padding reached'});continue
   if mn.startswith('j'):
    if direct:pending.append(int(direct[1],16))
    else:
     table=re.search(r'\[[a-z]+\*4\+0x([0-9a-f]+)\]',op)
     targets=[]
     if table:
      base=int(table[1],16)
      for i in range(256):
       try:v=self.pe.u32(base+4*i)
       except ValueError:break
       if v not in self.ins or not self.pe.executable(v):break
       targets.append(v)
      if targets:
       switches.append({'at':hex(at),'table':hex(base),'candidateTargets':[hex(t) for t in targets],'bounds':'conservative contiguous executable pointers; input bounds require manual proof'})
       pending.extend(targets)
     if not targets:unresolved.append({'at':hex(at),'reason':'indirect transfer','instruction':op})
    if mn=='jmp':continue
   pending.append(nextat)
  if pending:unresolved.append({'reason':'instruction limit reached'})
  return {'entry':hex(start),'instructionCount':len(seen),'calls':calls,'switches':switches,'unresolved':unresolved,'instructions':[{'at':hex(k),'op':v} for k,v in sorted(seen.items())]}
