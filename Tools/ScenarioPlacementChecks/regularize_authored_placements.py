"""Regularize authored 01-17 battle groups. This is an authored layout, not recovered native placement.
Dry-run by default. --apply writes units/profileHash and adjustment audit, preserving other content.
"""
import argparse,collections,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def regularize(data):
 w,h=data['width'],data['height'];walk={(n%w,n//w) for n in data['traversalCells']};blocked=set()
 for e in data['events']:
  for c in e.get('conditions',[])+e.get('actions',[]):
   if (c['name']=='CHARPOS' and len(c['args'])==2) or (c['name']=='CHANGESPRITE' and len(c['args'])>=2):
    try:x,y=map(int,c['args'][:2])
    except ValueError:continue
    blocked.add((x,y))
 ex,ey=data['entryX'],data['entryY'];blocked.update((ex+x,ey+y) for x in range(-2,3) for y in range(-2,3))
 # Preserve the existing group's traversable component (including gated regions).
 component={};num=0
 for start in sorted(walk):
  if start in component:continue
  num+=1;stack=[start];component[start]=num
  while stack:
   x,y=stack.pop()
   for nxt in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
    if nxt in walk and nxt not in component:component[nxt]=num;stack.append(nxt)
 groups=collections.defaultdict(list)
 for u in data['units']:groups[(u['wave'],u['id']//10)].append(u)
 report=[]
 for wave in sorted({u['wave'] for u in data['units']}):
  occupied={(u['x'],u['y']) for u in data['units'] if u['wave']==wave}
  for (gw,gid),group in sorted(groups.items()):
   if gw!=wave:continue
   group.sort(key=lambda u:u['id']);old=[(u['x'],u['y']) for u in group]
   if len(group)!=3:report.append(dict(wave=wave,group=gid,status='unsupported group size; preserved'));continue
   occupied.difference_update(old);center=old[0];comp=component.get(old[0]);candidates=[]
   for step in [2,1]:
    templates=[[(0,0),(-step,step),(step,step)],[(0,0),(-step,-step),(step,-step)],[(0,0),(-step,0),(step,0)],[(0,0),(0,-step),(0,step)]]
    for x in range(max(1,round(center[0])-6),min(w-1,round(center[0])+7)):
     for y in range(max(1,round(center[1])-6),min(h-1,round(center[1])+7)):
      for form,offsets in enumerate(templates):
       points=[(x+dx,y+dy) for dx,dy in offsets]
       if any(p not in walk or p in blocked or p in occupied or component.get(p)!=comp for p in points):continue
       if any(max(abs(px-qx),abs(py-qy))<2 for px,py in points for qx,qy in occupied):continue
       if abs(points[0][0]-old[0][0])+abs(points[0][1]-old[0][1])>8:continue
       # Equal two-tile spacing first, then retain area/leader placement.
       score=(2-step)*10000+sum((a-c)**2+(b-d)**2 for (a,b),(c,d) in zip(points,old))+form*.01
       candidates.append((score,points,step,form))
    if candidates:break
   if candidates:
    _,points,step,form=min(candidates);status='regular'
    for u,(x,y) in zip(group,points):u['x'],u['y']=x,y
   else:points=old;step=0;form=None;status='no safe formation; preserved'
   occupied.update(points);report.append(dict(wave=wave,group=gid,status=status,spacing=step,formation=form,units=[u['id'] for u in group],before=old,after=points))
 # No duplicate cells within each active wave; no unit stat/ID/wave changes.
 for wave in {u['wave'] for u in data['units']}:
  units=[u for u in data['units'] if u['wave']==wave];assert len({(u['x'],u['y']) for u in units})==len(units)
 assert all((u['x'],u['y']) in walk for u in data['units'])
 return report

def patch_content(original,before,data):
 def array_bounds(text,key):
  match=re.search(r'"'+key+r'"\s*:\s*',text);assert match,key
  start=match.end();value,end=json.JSONDecoder().raw_decode(text,start);assert isinstance(value,list)
  return start,end
 start,end=array_bounds(original,'units');body=original[start:end];cursor=1;pieces=[];prev=0
 for unit in data['units']:
  while body[cursor].isspace() or body[cursor]==',':cursor+=1
  old,stop=json.JSONDecoder().raw_decode(body,cursor);assert old['id']==unit['id']
  raw=body[cursor:stop]
  for key in ['x','y']:
   raw,n=re.subn(r'("'+key+r'"\s*:\s*)-?\d+',lambda m:m[1]+str(unit[key]),raw);assert n==1
  pieces.extend([body[prev:cursor],raw]);prev=stop;cursor=stop
 pieces.append(body[prev:]);text=original[:start]+''.join(pieces)+original[end:]
 text,n=re.subn(r'("profileHash"\s*:\s*")[^"]*',lambda m:m[1]+data['profileHash'],text);assert n==1
 text,n=re.subn(r'("sourceHash"\s*:\s*")[^"]*',lambda m:m[1]+data['sourceHash'],text);assert n==1
 start,end=array_bounds(text,'placementAdjustments');extra=data['placementAdjustments'][len(before.get('placementAdjustments',[])):]
 body=text[start:end];insert=(',' if before.get('placementAdjustments') else '')+'\n    '+',\n    '.join(json.dumps(row,ensure_ascii=False) for row in extra)+'\n  '
 text=text[:start]+body[:-1]+insert+']'+text[end:]
 assert json.loads(text)==data
 return text

def main():
 p=argparse.ArgumentParser();p.add_argument('--apply',action='store_true');a=p.parse_args();report=[]
 for i in range(1,18):
  path=ROOT/f'Assets/_Game/Resources/Scenarios/{i:02}/Content.json';original=path.read_text();data=json.loads(original);before=json.loads(original);groups=regularize(data)
  changed=[(old,u) for old,u in zip(before['units'],data['units']) if (old['x'],old['y'])!=(u['x'],u['y'])]
  for old,u in zip(before['units'],data['units']):assert {k:v for k,v in old.items() if k not in ['x','y']}=={k:v for k,v in u.items() if k not in ['x','y']}
  if a.apply and changed:
   data['profileHash']=hashlib.sha256(json.dumps(data['units'],sort_keys=True,separators=(',',':')).encode()).hexdigest()
   data.setdefault('placementAdjustments',[]).extend(dict(unit=u['id'],fromX=old['x'],fromY=old['y'],toX=u['x'],toY=u['y'],reason='authored regular formation; not original server placement') for old,u in changed)
   canonical=dict(data);canonical.pop('sourceHash');data['sourceHash']=hashlib.sha256(json.dumps(canonical,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
   for k in before:assert k in ['units','profileHash','sourceHash','placementAdjustments'] or before[k]==data[k]
   path.write_text(patch_content(original,before,data))
   visuals=path.with_name('Visuals.json');visuals.write_text(visuals.read_text().replace(before['sourceHash'],data['sourceHash']))
   if i>1:
    manifest=ROOT/'Tools/MapChecks/scenarios_02_17_resources.json';manifest.write_text(manifest.read_text().replace(before['sourceHash'],data['sourceHash']))
  row=dict(scenario=f'{i:02}',changedUnits=len(changed),groups=groups);report.append(row)
  print(f'{i:02}: {len(changed)} units moved, {sum(g["status"]=="regular" for g in groups)}/{len(groups)} regular groups')
 target=ROOT/'Tools/ScenarioPlacementChecks/regular-formation-report.json' if a.apply else Path('/tmp/regular-formation-preview.json');target.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
