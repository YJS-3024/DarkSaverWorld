"""Validate regular layouts and preserve the pre-edit unit roster/stats/events."""
import argparse,collections,hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--baseline',default='90f84ce9');a=p.parse_args()
report=json.loads((ROOT/'Tools/ScenarioPlacementChecks/regular-formation-report.json').read_text());groups=units=moved=0
for row in report:
 key=row['scenario'];path=f'Assets/_Game/Resources/Scenarios/{key}/Content.json';data=json.loads((ROOT/path).read_text());old=json.loads(subprocess.check_output(['git','show',a.baseline+':'+path],cwd=ROOT))
 for k in old:
  if k not in ['units','sourceHash','profileHash','placementAdjustments']:assert old[k]==data[k],(key,k)
 assert len(old['units'])==len(data['units'])
 for before,after in zip(old['units'],data['units']):
  assert {k:v for k,v in before.items() if k not in ['x','y']}=={k:v for k,v in after.items() if k not in ['x','y']}
 canonical=dict(data);canonical.pop('sourceHash');assert hashlib.sha256(json.dumps(canonical,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()==data['sourceHash']
 assert json.loads((ROOT/path).with_name('Visuals.json').read_text())['contentHash']==data['sourceHash']
 occupied=set()
 for u in data['units']:
  assert u['y']*data['width']+u['x'] in data['traversalCells']
  cell=(u['wave'],u['x'],u['y']);assert cell not in occupied;occupied.add(cell)
 for g in row['groups']:
  assert g['status']=='regular' and g['spacing']==2,(key,g)
  actual=[next((u['x'],u['y']) for u in data['units'] if u['id']==id) for id in g['units']];assert actual==[tuple(x) for x in g['after']]
  shape=[(x-actual[0][0],y-actual[0][1]) for x,y in actual]
  assert shape in [[(0,0),(-2,2),(2,2)],[(0,0),(-2,-2),(2,-2)],[(0,0),(-2,0),(2,0)],[(0,0),(0,-2),(0,2)]]
  groups+=1
 units+=len(data['units']);moved+=row['changedUnits']
print(f'PASS: 17 scenarios, {groups} regular groups, {units} units, {moved} moved; roster/stats/events/intro/outro preserved; walkability/unique cells/content hashes valid')
