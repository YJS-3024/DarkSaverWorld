from pathlib import Path
import json,collections,hashlib
base=Path('분석/Saver_Late');native=json.loads((base/'scenario_monster_placements.json').read_text());patterns=collections.Counter();count=0
for s in native['scenarios']:
 groups=collections.defaultdict(list)
 for u in s['units']:groups[u['unit']//10].append(u)
 for group in groups.values():
  group.sort(key=lambda u:u['unit']);patterns[str(group[0]['extra'])]+=1;count+=1
records=[]
for n in [301,401,305,701]:
 lines=[l.split() for l in (base/'decoded/gameres/set.arc/ability.atr').read_text().splitlines() if l.split()[:1]==[str(n)]];r=lines[0]
 records.append({'abilityId':n,'pictureId':int(r[1]),'name':r[-1],'spriteObservation':'quadruped' if n in [301,401] else 'upright, unarmed' if n==305 else 'upright, sword','brugosEvidence':'Original 01.deo CHARPOS' if n in [301,401,701] else 'No 305 spawn found in reviewed 01 DEO/EVT/AI; original initial SET/POS unavailable'})
report={'scope':'Native leader/support hypothesis and original Brugos character artwork review; no invented spawn or roster modification','nativeGroups':count,'nativeMembersPerGroup':5,'firstMemberRawExtraHistogram':dict(patterns),'roleMeaning':'First member plus four neighbors is observed. Extra operand meanings are not proven leader flags or follower counts. Supports user recollection of up to four followers, not proof of all 2/3/4 variants.','brugosMonsters':records,'currentContentJobs':sorted({u['job'] for u in json.loads(Path('Assets/_Game/Resources/Scenarios/01/Content.json').read_text())['units']}),'idWarning':'CHARPOS 401 405 means picture/job401 assigned to unit405, not job405.','sources':['decoded/gameres/set.arc/ability.atr','decoded/map/01set.arc/01.deo','decoded/map/01set.arc/01.evt','decoded/map/01set.arc/01.ai']}
(base/'brugos_monster_role_review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('Reviewed',count,'native groups; first-member extra',dict(patterns),'; Brugos jobs',report['currentContentJobs'])
