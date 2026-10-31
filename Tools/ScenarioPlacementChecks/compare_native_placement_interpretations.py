"""Compare native POS interpretation hypotheses; never rewrite game placement data."""
import collections,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];base=ROOT/'분석/Saver_Late'
def main():
 source=json.loads((base/'scenario_monster_placements.json').read_text());maps={Path(m['path']).stem:m for m in json.loads((base/'maps.json').read_text())};rows=[];sizes=collections.Counter();shapes=collections.Counter()
 for scenario in source['scenarios']:
  key=scenario['scenario'];w,h=maps[key]['width'],maps[key]['height'];units=scenario['units'];groups=collections.defaultdict(list)
  for u in units:groups[u['unit']//10].append(u)
  aiPath=base/f'decoded/map/{key}set.arc/{key}.ai';areas={}
  if aiPath.exists():
   for m in re.finditer(r'(?m)^\s*(\d+)\s+\w+\s+AREA\((\d+),(\d+),(\d+)\)',aiPath.read_text()):areas[int(m[1])]=tuple(map(int,m.groups()[1:]))
  samples=[]
  for group in groups.values():
   group.sort(key=lambda u:u['unit']);sizes[len(group)]+=1;leader=group[0];offsets=sorted((u['x']-leader['x'],u['y']-leader['y']) for u in group);shapes[str(offsets)]+=1
   samples.append({'leaderId':leader['unit'],'size':len(group),'offsets':offsets,'leaderPosition':[leader['x'],leader['y']],'aiArea':areas.get(leader['unit'])})
  def in_bounds(x,y):return 0<=x<w and 0<=y<h
  swapped=sum(in_bounds(u['y'],u['x']) for u in units)
  relative=sum(in_bounds(u['x']+min(groups[u['unit']//10],key=lambda g:g['unit'])['x'],u['y']+min(groups[u['unit']//10],key=lambda g:g['unit'])['y']) for u in units)
  rows.append({'scenario':key,'count':len(units),'absoluteInBounds':sum(in_bounds(u['x'],u['y']) for u in units),'swappedInBounds':swapped,'leaderRelativeInBounds':relative,'groups':samples})
 report={'scope':'Alternate static hypotheses, not reconstructed server rules; no runtime data modified','unitCount':sum(r['count'] for r in rows),'groupSizeHistogram':dict(sorted(sizes.items())),'commonOffsets':shapes.most_common(8),'scenarios':rows,'conclusions':['POS first field joins SET unit ID; not a character/job ID.','Native groups are not uniformly three members. Decimal ID prefix is a comparison grouping, not proof of a runtime group API.','POS offsets around the leader describe a visible arrangement but do not make POS values relative coordinates.','In-bounds counts can reject some hypotheses but cannot prove axis orientation on square maps.','AI AREA is a behavior-region candidate reference, not proof of initial monster count/coordinates.','01-17 lack the recovered SET/POS roster in this reference; their current regular formations are authored.']}
 (base/'placement_interpretation_comparison.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print('Native positions:',report['unitCount'],'group sizes:',dict(sizes));print('Common shape:',shapes.most_common(1))
if __name__=='__main__':main()
