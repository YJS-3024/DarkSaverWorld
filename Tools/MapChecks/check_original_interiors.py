from saved_scene_geometry import read_scene_map
"""Compare converted indoor maps to native MRC/GRS data; no actors/fees/routes inferred."""
from pathlib import Path
import json,hashlib
from PIL import Image
from world_asset_baker import ROOT,load_map,native_map_path,resource,frame
manifest=json.loads((ROOT/'Tools/MapChecks/shop_interiors_resources.json').read_text())
results=[];checks=0;failures=[]
def check(ok,description):
 global checks
 checks+=1
 if not ok:failures.append(description)
for row in manifest['nativeInteriorMaps']:
 key=row['nativeMap'];source=native_map_path(key);native=load_map(key);w,h=native['width'],native['height']
 folder=ROOT/'Assets/_Game/Resources/Interiors'/key;data=read_scene_map(folder);before=len(failures)
 expected_bytes=16+4*len(native['ids'])+7*w*h+16*len(native['sprites'])
 check(source.stat().st_size==expected_bytes,key+' native parsed to exact EOF')
 check((data['width'],data['height'])==(w,h),key+' dimensions')
 check(data['attributes']==[t[3]&127 for t in native['tiles']],key+' terrain')
 check(len(data['objects'])==len(native['sprites']),key+' object count')
 ground=Image.new('RGBA',(w*32,h*32),(0,0,0,255))
 for i,(rid,gid,tile) in enumerate(zip(native['a'],native['b'],native['tiles'])):
  ground.alpha_composite(frame(rid,gid,0),(i%w*32,i//w*32))
  if tile[1]!=255:
   g=resource(tile[0])['groups'][tile[1]];ground.alpha_composite(frame(tile[0],tile[1],g['tiles'][tile[2]]['frames'][0],transparent=True),(i%w*32,i//w*32))
 check(Image.open(folder/'Ground.png').convert('RGBA').tobytes()==ground.tobytes(),key+' floor first-frame pixels')
 for actual,original in zip(data['objects'],native['sprites']):
  check((actual['x'],actual['y'])==(original['x'],original['y']),key+' object coordinates '+str(actual['index']))
  im=frame(original['resource'],original['index'],original['frame'],True,True)
  check(Image.open(folder/(actual['texture']+'.png')).convert('RGBA').tobytes()==im.tobytes(),key+' object pixels '+str(actual['index']))
 results.append(dict(nativeMap=key,width=w,height=h,cells=w*h,objects=len(native['sprites']),source=str(source.relative_to(ROOT)),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),parsedToExactEOF=source.stat().st_size==expected_bytes,legacyNpcFieldPresent='npc' in data,failures=len(failures)-before))
staff=json.loads((ROOT/'Assets/_Game/Resources/Town/OriginalInteriorStaff.json').read_text())['entries']
for row in staff:
 source=next(p for p in (ROOT/'분석/Saver_1.2/decoded/all_arc/gameres').rglob('*') if p.name.lower()==str(row['appearanceId'])+'r.bmp')
 sheet=Image.open(source).convert('RGBA');size=sheet.width//3;x=row['frame']*size;expected=sheet.crop((x,size,x+size,size*2))
 expected.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in expected.getdata()])
 actual=Image.open(ROOT/'Assets/_Game/Resources'/(row['texture']+'.png')).convert('RGBA')
 check(actual.size==expected.size and actual.tobytes()==expected.tobytes(),'staff source frame '+row['nativeMap']+' '+str(row['appearanceId']))
report=dict(rooms=len(results),checks=checks,failures=failures,results=results,staffFrameChecks=len(staff),scope='Native indoor geometry, first-frame pixels and chosen staff sprite crops only. MRC geometry parsing does not establish server NPC count/identities. NPC placement comes only from independent confirmed staff data.',actualUnityPlay=False)
(ROOT/'Tools/MapChecks/original_interiors_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(('PASS' if not failures else 'FAIL'),checks,'native indoor checks; rooms',len(results),'failures',len(failures),'NOT Unity Play')
for failure in failures[:12]:print(failure)
if failures:raise SystemExit(1)
