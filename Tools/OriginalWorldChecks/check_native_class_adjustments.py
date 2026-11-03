"""Source81/82/84/86/88/89/8A packet builders plus guarded current manager callbacks."""
import hashlib,itertools,json,os,subprocess,struct
from pathlib import Path
from native_class_adjustments_emulator import ClassAdjustmentsNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-class-adjustments-checks')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True);native=ClassAdjustmentsNative();simple=[];stats=[];magic=[];change=[];receivers=[];drags=[]
 for record in [0,1,255,256,32767,65534]:
  for kind,points in itertools.product(['costs','options','confirm','cancel'],[-1,0,1,2]):simple.append(dict(record=record,kind=kind,points=points,expected=native.simple(kind,record,points)))
  for stat,direction in itertools.product(range(6),[1,2]):stats.append(dict(record=record,stat=stat,direction=direction,expected=native.stat(record,stat,direction,11,10,10,1)))
  for id,mode,list_kind,index,ready in itertools.product([101,301,21701,65534],[1,2],[0,1],[0,7],[0,1]):magic.append(dict(record=record,magic=id,mode=mode,list=list_kind,index=index,ready=ready,expected=native.magic(record,id,mode,list_kind,index,ready)))
  for job,slot,points,loaded in itertools.product([120,241,65534],range(3),[0,1,2],[False,True]):change.append(dict(record=record,job=job,slot=slot,points=points,loaded=loaded,expected=native.change(record,job,slot,points,loaded)))
 for record,list_kind,count in itertools.product([0,1,255,65534],[0,1],[1,2,8]):
  for index in range(count):drags.append(dict(record=record,list=list_kind,index=index,count=count,expected=native.drag(record,list_kind,index,count)))
 for costs in [[1,2,3,4,5,6],[0]*6,[-32768,-1,0,1,1000,32767]]:
  packet=b'\x8b'+struct.pack('<6h',*costs);receivers.append(dict(packet=packet.hex(),expected=native.receive(packet)))
 for opaque,right,left,count in itertools.product([0,1,65535],[-1,120,241],[-1,100,103],[0,1,2,10]):
  choices=[110+i for i in range(count)];packet=b'\x87'+struct.pack('<Hhhh',opaque,right,left,count)+struct.pack('<'+'h'*count,*choices);receivers.append(dict(packet=packet.hex(),expected=native.receive(packet)))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(simple=simple,stats=stats,magic=magic,change=change,receivers=receivers,drags=drags)));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeClassAdjustmentChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 names=['Entry/LocalServerManager.NativeClassAdjustments.cs','Entry/LocalServerManager.NativeClassMagic.cs','Entry/LocalServerManager.StatAdjustment.cs','Entry/LocalServerManager.MagicLoadout.cs','Entry/LocalServerManager.ClassChange.cs']
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeClassAdjustmentChecks.cs'))+'\n-main:NativeClassAdjustmentChecks\n'
 for path in [*[ROOT/'Assets/_Original/Runtime'/n for n in names],OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs',OUT/'ClassPacketManagerHost.cs',OUT/'CommanderAdjustmentChecks.cs']:args+='\n"'+str(path)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2300:])
 result=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_class_adjustments_emulator.py',OUT/'NativeClassAdjustmentChecks.cs',OUT/'ClassPacketManagerHost.cs',OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs',OUT/'CommanderAdjustmentChecks.cs',*[ROOT/'Assets/_Original/Runtime'/n for n in names],*sorted((ROOT/'Assets/_Original/Runtime/Growth').glob('*.cs')),ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.ClassChange.cs']
 evidence=ROOT/'분석/Ghidra/Saver_Late/ClassAdjustments/evidence.json';native_evidence=json.loads(evidence.read_text());paths.append(evidence)
 for row in native_evidence['files']:assert sha(ROOT/row['asset'])==row['sha256'];paths.append(ROOT/row['asset'])
 result.update(nativeSimpleCases=len(simple),nativeStatCases=len(stats),nativeMagicCases=len(magic),nativeClassCases=len(change),nativeReceiverCases=len(receivers),nativeDragCases=len(drags),nativeCodeExecuted=True,unityPlay=False,logs=str(WORK),scope='Original462190/462250/462310/462B10/4623A0/462900/462980/461DD0/462000/462A00 execute actual byte/WORD builders; alloc/send are host boundaries. Valid encoded fields, source magic-point/class-point/private predicates and extra86 after81 observed. Current original-wire projections/real managed service/owned context/authority/save refusal and synchronous callback invalidation tested. Typed approvals retain conservative signed-domain checks; no source signed-overflow/private-prediction equivalence, full native server response parsing, class population, economy or Unity Play claim.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in paths});(OUT/'native-class-adjustments-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
