"""Compare production68 geometry with all source-current original target traces.
Relation masks are supplied independently by original ally3/ally4, not by target outputs.
"""
import copy,csv,hashlib,json,struct,subprocess
from pathlib import Path
from check_native_magic_target_catalog import UNITS
from check_native_magic_packets import NativeLevelTargets
from native_ai_emulator import UC_X86_REG_EAX,UC_X86_REG_ESP,UC_X86_REG_EIP
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-redirect-selection')
def relation_table():
 r=next(r for r in csv.DictReader((ROOT/'Assets/_Original/Data/Tables/Magictbl.csv').open(encoding='utf-8-sig'))if r['번호']=='101')
 metadata={k:int(r[c])for k,c in [('id','번호'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
 n=NativeLevelTargets(dict(width=24,height=24,source=0,metadata=metadata,units=copy.deepcopy(UNITS)));result={}
 for unit in UNITS:
  bits=0
  for mask,name in [(1,'ally3'),(2,'ally4')]:
   sp=n.STACK+0xff000;n.uc.mem_write(sp,struct.pack('<III',n.STOP,0,unit['id']));n.uc.reg_write(UC_X86_REG_ESP,sp);n.uc.emu_start(n.exports[name],n.STOP+1,timeout=1000000,count=100000)
   assert n.uc.reg_read(UC_X86_REG_EIP)==n.STOP
   if n.uc.reg_read(UC_X86_REG_EAX):bits|=mask
  result[unit['id']]=bits
 n.close();return result
def main():
 WORK.mkdir(exist_ok=True);target=OUT/'native-magic-target-catalog-check.json';oracle=json.loads(target.read_text())
 for name,digest in oracle['sources'].items():assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest()==digest,name
 relations=relation_table();cases=[]
 for row in oracle['cases']:
  units=copy.deepcopy(UNITS)if row.get('fixture')!='inactive-group'else[copy.deepcopy(UNITS[0]),dict(id=400,x=11,y=10,team=8,nativeActive=False)]
  for u in units:u.setdefault('nativeActive',True);u.setdefault('nativeEnabled',True)
  cases.append(dict(metadata=row['metadata'],source=0,x=row['point'][0],y=row['point'][1],units=units,eligible=[u['id']for u in units if relations[u['id']]&row['metadata']['team']]))
 base_count=len(cases)
 cases+= [dict(c,receivedTeams=[u['team']for u in c['units']],eligible=None)for c in cases]
 inputs=WORK/'cases.json';outputs=WORK/'managed.json';inputs.write_text(json.dumps(cases))
 old=Path('/tmp/ds-npc-gateway-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';exe=WORK/'NativeMagicRedirectDifferential.dll'
 args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeMagicRedirectDifferential.cs'))
 for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
  if str(p)not in args:args+='\n'+str(p)
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
 for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inputs),str(outputs)])]:
  p=subprocess.run(command,cwd=ROOT,capture_output=True,text=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2200:])
 actual=json.loads(outputs.read_text());assert len(actual)==len(cases);bad=[dict(input=c,native=r['native'],managed=a)for c,r,a in zip(cases,oracle['cases']*2,actual)if r['native']!=a]
 (WORK/'mismatches.json').write_text(json.dumps(bad,indent=2))
 if bad:raise AssertionError(str(bad[0])[:2200])
 from check_native_battle_responses import contract_sources
 sources=contract_sources(args,__file__)
 for p in [target,OUT/'check_native_magic_target_catalog.py',OUT/'check_native_magic_packets.py',OUT/'native_ai_emulator.py']:sources[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
 report=dict(cases=len(cases),originalOracleCases=base_count,receivedTeamCases=base_count,relationCases=len(relations)*2,mismatches=0,unityPlay=False,sources=sources,scope='Production OriginalMagicRedirect.Select against all source-current actual UserUnit traces for440 table rows, once with independently executed original ally3/ally4 masks and once with production relation calculation from explicitly received teams. Native output target lists never supply permissions. These contexts overlap; counts are not independent gameplay cases. Controlled grid/flags/teams only; no damage/cost or actual Unity screen equivalence.',logs=str(WORK))
 (OUT/'native-magic-redirect-selection-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
