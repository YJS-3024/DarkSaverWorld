"""Exact source learned-skill stock and synthetic native modifier branch fixtures."""
import copy,hashlib,itertools,json,os,random,subprocess
from pathlib import Path
from build_native_mercenary_catalog import data
from native_hire_list_emulator import HireListNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-hire-list-checks')
def main():
 WORK.mkdir(exist_ok=True);source=data();assert json.loads((ROOT/'Assets/_Original/Resources/OriginalMercenarySkills.json').read_text())==source,'Generated runtime hire catalog differs from the current original source tables';groups=[]
 for synthetic in [False,True]:
  catalog=copy.deepcopy(source)
  if synthetic:
   catalog['skills'] +=[dict(id=30900+level,maximum=level,jobs=[200,201]if level%2 else[201,291])for level in [1,2,4,10]]
   catalog['equipment'] +=[dict(id=1500+i,skill=skill,boost=boost)for i,(skill,boost)in enumerate([(20101,1),(20103,2),(30901,-1),(30904,-2),(30910,3),(20101,30),(30901,-30),(20101,-1),(21703,-1),(20101,0)])]
  native=HireListNative(catalog);cases=[];ids=[row['id']for row in catalog['skills']];rng=random.Random(2850+synthetic)
  def case(learned,worn,name):cases.append(dict(case=name,skills=learned,equipment=worn,expected=native.resolve(worn,learned)))
  if not synthetic:
   for a,b in itertools.product(ids,repeat=2):case([a,b],[1428],f'pair-{a}-{b}')
   unique={tuple(row['skills'][:8])for row in json.loads((ROOT/'Assets/_Original/Data/Growth/CharacterGrowth.json').read_text())['rows']if row['abilitiesResolved']}
   for i,learned in enumerate(sorted(unique)):case(list(learned),[],f'growth-{i}')
  else:
   for item in catalog['equipment']:
    for skill in [[],[20101],[20103],[30901],[30904],[20101,20102],[30910],[21701]]:case(skill,[item['id']],f'modifier-{item["id"]}-{skill}')
   for a,b in itertools.product([1500,1501,1502,1503,1504,1506],repeat=2):case([20101,30901],[a,b],f'modifier-order-{a}-{b}')
  for i in range(240):case([rng.choice(ids+[-1,0,19999,123456])for _ in range(10)],[rng.choice([1428,-1,0,1500,1501,1502])for _ in range(7)],f'random-{i}')
  groups.append(dict(name='synthetic'if synthetic else'source',catalog=catalog,cases=cases))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(groups,ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeHireListChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeHireListChecks.cs'))
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2000:])
 result=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_hire_list_emulator.py',OUT/'NativeHireListChecks.cs',OUT/'build_native_mercenary_catalog.py',OUT/'native-mercenary-catalog-art.json',ROOT/'Assets/_Original/Resources/OriginalMercenarySkills.json',ROOT/'Assets/_Original/Runtime/Growth/NativeMercenaryCatalog.cs',ROOT/'Assets/_Original/Runtime/World/NativeWorldArt.cs',ROOT/'Assets/_Original/Runtime/World/WorldModel.cs',ROOT/'Assets/_Original/Runtime/World/WorldService.cs',ROOT/'Assets/_Original/Runtime/World/MercenaryTransferService.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs']
 paths.append(ROOT/'Assets/_Original/Data/Growth/CharacterGrowth.json')
 evidence=ROOT/'분석/Ghidra/Saver_Late/Mercenary/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];paths.append(path)
 result.update(nativeCodeExecuted=True,nativeDllSha256=native.sha256,sourceCases=len(groups[0]['cases']),syntheticCases=len(groups[1]['cases']),logs=str(WORK),scope='Full unmodified10002850/10002820 on source-initialized64 skills/1212 item records and explicit synthetic modifier tables. Order, duplicate max,8 skill/7 equipment slots, family/tier adjustment/decrement paths compared. Runtime resource/native list/offer projection; native table parser, financial approval and Unity UI/physical input not executed.',sources={str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest()for path in paths});(OUT/'native-hire-list-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
