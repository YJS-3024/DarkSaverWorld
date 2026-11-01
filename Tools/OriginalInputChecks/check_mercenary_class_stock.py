"""Export source-derived class/level recruit expectations and audit converted catalogs."""
import csv,json,re,hashlib
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
def asset(path):return next(o.read_typetree() for o in UnityPy.load(str(ROOT/path)).objects if o.type.name=='MonoBehaviour')['entries']
hire=asset('Assets/_Game/Runtime/Jobs/MercenaryHireCatalog.asset');jobs=asset('Assets/_Game/Runtime/Jobs/MercenaryJobCatalog.asset')
p=ROOT/'분석/Saver_Late/decoded/gameres/set.arc/solskill.atr';source={int(v[0]):dict(skillId=int(v[0]),skillName=v[1],level=int(v[2]),jobs=list(map(int,v[3].split(','))),maximum=int(v[4])) for line in p.read_text(encoding='utf-8-sig').splitlines() if (v:=line.split()) and v[0].isdigit()}
assert {r['skillId'] for r in hire}==set(source)
for r in hire:assert r==source[r['skillId']],r['skillId']
units={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/mercenaries.csv').open(encoding='utf-8-sig'))}
for j in jobs:
 v=j['numericValues'];native=units[v[0]]
 for i,k in [(2,'체력수'),(4,'마력수'),(6,'공력수'),(8,'방력수'),(33,'가격')]:assert v[i]==int(native[k])
growth=list(csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/experience_progression.csv').open(encoding='utf-8-sig')))
def ids(raw):return [int(v.strip()) for v in raw.split(',') if v.strip() and int(v.strip())>0]
cases=[];summaries={}
for row in growth:
 if not 100<=int(row['jobId'])<=172:continue
 skillids=ids(row['usableSkills']);expected={}
 for id in skillids:
  if id not in source:continue
  for job in source[id]['jobs']:
   maximum=max(expected.get(job,{}).get('maximum',0),source[id]['maximum'])
   expected[job]=dict(jobId=job,maximum=maximum,price=int(units[job]['가격']))
 case=dict(job=int(row['jobId']),name=row['jobName'],level=int(row['level']),magic=ids(row['learnedMagic']),skills=skillids,expected=list(expected.values()));cases.append(case)
 summaries.setdefault(case['job'],case)
descriptions={int(r['직업']):r['설명'] for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/jobs.csv').open(encoding='utf-8-sig'))}
with (ROOT/'Tools/OriginalInputChecks/mercenary_class_stock.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.writer(f);w.writerow(['classId','className','sourceLevel','classSourceDescription','hireSkills','eligibleCount','mercenaryIds','maximumPerJob'])
 for j,row in sorted(summaries.items()):w.writerow([j,row['name'],row['level'],descriptions.get(j,''),','.join(str(s) for s in row['skills'] if s in source),len(row['expected']),','.join(str(e['jobId']) for e in row['expected']),','.join(str(e['maximum']) for e in row['expected'])])
Path('/tmp/ds-mercenary-source-matrix.json').write_text(json.dumps(dict(hire=hire,jobs=jobs,cases=cases),ensure_ascii=False))
report=dict(hireSkills=len(hire),mercenaryRows=len(jobs),classCount=len(summaries),levelRows=len(cases),distinctEligibleSoldiers=len({j for s in source.values() for j in s['jobs']}),sourceSha256=hashlib.sha256(p.read_bytes()).hexdigest(),actualUnityPlayback=False)
(ROOT/'Tools/OriginalInputChecks/mercenary_class_stock_check.json').write_text(json.dumps(report,indent=2)+'\n');print('PASS:',len(hire),'native hire skills,',len(jobs),'soldier base/price rows;',len(cases),'source expectations across',len(summaries),'classes. Static export only.')
