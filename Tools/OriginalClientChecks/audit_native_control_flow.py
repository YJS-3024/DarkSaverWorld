"""Extract reproducible late-client callback bindings and bounded code evidence.
Usage: python audit_native_control_flow.py --native-root PATH --saver PATH
This enumerates constant-ID vtables, not every possible UI subclass or callback branch.
"""
import argparse,json,re,subprocess
from pathlib import Path
from native_pe import PE
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--native-root',type=Path,required=True);p.add_argument('--saver',type=Path,required=True);a=p.parse_args()
modules={n:PE(a.native_root/n) for n in ('GameCtl.dll','UserUnit.dll','setunit.dll')};modules['Saver.exe']=PE(a.saver)
expected={'Saver.exe':'dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689','GameCtl.dll':'f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0','UserUnit.dll':'74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b'}
for n,h in expected.items():assert modules[n].sha256==h,(n,'unexpected binary')
s=modules['Saver.exe'];g=modules['GameCtl.dll'];u=modules['UserUnit.dll'];t=modules['setunit.dll']
assert u.imports()[0x10013114][1]=='clock'
assert g.imports()[0x1001618c][1]=='clock'
u.expect(0x10010efd,'83 f8 50');u.expect(0x10010f16,'3d f4 01 00 00')
u.expect(0x1001081f,'83 f8 62');u.expect(0x10005589,'89 8c 06 74 01 00 00')
assert t.string(0x10005240).lower()=='magicptn.atr'
zero=[]
for i in (64,65):
 va=0x10021bb0+i*328
 section=next(x for x in t.sections if x['rva']<=va-t.base<x['rva']+x['virtualSize'])
 assert va-t.base>=section['rva']+section['rawSize']
 zero.append({'patternId':i,'record':hex(va),'initialStorage':'PE zero-filled virtual data','fxptnOffset':'0x13c'})
asm=subprocess.check_output(['objdump','-d','-Mintel',str(a.saver)],text=True)
vt={}
for line in asm.splitlines():
 m=re.match(r'\s*([0-9a-f]+):.*mov\s+DWORD PTR \[(?:esi|ecx|edi|eax)\],0x([0-9a-f]+)',line)
 if not m:continue
 at,v=int(m[1],16),int(m[2],16)
 try:
  getter=s.thunk(s.u32(v+4));b=s.read(getter,6)
  if b[0]!=0xb8 or b[5]!=0xc3:continue
  wid=int.from_bytes(b[1:5],'little');callback=s.thunk(s.u32(v+16))
  if wid>150 or not s.executable(callback):continue
  if v not in vt:
   slots={hex(o):hex(s.thunk(s.u32(v+o))) for o in (0x10,0x24,0x28,0x2c,0x30,0x34,0x48,0x50)}
   vt[v]={'windowId':wid,'vtable':hex(v),'idGetter':hex(getter),'slots':slots,'constructorWrites':[],'buttonBranchSemantics':'not exhaustively classified', 'buttonSlot':('0x44' if wid<=4 else '0x10'), 'family':('controller; slot 0x10 is not button handler' if wid<=4 else 'common-window candidate')}
  vt[v]['constructorWrites'].append(hex(at))
 except (ValueError,IndexError):continue
battle=[]
for scene in range(29):
 index=g.read(0x10005030+scene,1)[0];target=g.u32(0x10004ffc+index*4)
 assert g.executable(target)
 battle.append({'sceneId':scene,'dispatchIndex':index,'buttonHandler':hex(target)})
ranges={
 'Saver.exe':[(0x433d00,0x433dd5)],
 'GameCtl.dll':[(0x100110b5,0x10011251),(0x10011338,0x10011b90),(0x100131f2,0x100132e0),(0x10013c1f,0x10013d60),(0x10004a80,0x10005200),(0x10009ec8,0x1000a010)],
 'UserUnit.dll':[(0x100054c0,0x100055dd),(0x10010775,0x10010980),(0x1000a950,0x1000abf0),(0x10010ee0,0x10011080)],
 'setunit.dll':[(0x10002063,0x10002210),(0x1000272e,0x10002820),(0x10002ec0,0x10002ee0)]}
evidence=ROOT/'Tools/OriginalClientChecks/evidence';evidence.mkdir(exist_ok=True)
for name,rs in ranges.items():
 (evidence/('late-control-flow-'+name+'.asm.txt')).write_text('\n'.join(modules[name].disasm(lo,hi) for lo,hi in rs))
report={'scope':'Late original client static analysis; no runtime validation or server decisions',
 'sources':{n:v.sha256 for n,v in modules.items()},
 'worldConstantIdVtables':sorted(vt.values(),key=lambda v:(v['windowId'],v['vtable'])),
 'worldCoverageLimit':'Constant-return GetID only; not a proof that all UI classes were enumerated. Window IDs are not LSC scene IDs.',
 'battleSceneDispatch':battle,'battleCoverageLimit':'Dispatcher targets only; button-level semantics and Continue path remain separate.',
 'missingPatterns':zero,'missingPatternFlow':'GetMagicTbl copies pattern+0x13c into legacy+0x60; zero type defaults to UserUnit 0x10005550 and clears +0x174/+0x1c4. No replacement pattern.',
 'timing':{'clockImport':'MSVCRT.clock','unitGateTicks':80,'discardBacklogAboveTicks':500,'unitGate':'one increment per call, not a catch-up loop','wallClockMeasured':False},
 'input':{'commonButtonDispatch':'Saver 0x433dcd vtable+0x10, ordinal index after release inside original pressed control rectangle','doubleClickClock':s.imports()[0x4997f0],'doubleClickThreshold':250},
 'completion':'partial; per-control semantics, complete call-chain timing and original playback remain unverified'}
out=ROOT/'분석/Saver_Late/native_control_flow_audit.json';out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'vtables':len(vt),'battleSceneEntries':len(battle),'zeroFilledMissingPatterns':len(zero),'output':str(out)},ensure_ascii=False))
