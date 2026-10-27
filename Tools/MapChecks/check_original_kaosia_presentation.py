"""Audit original Kaosia artwork and screenshot staff resources, without modifying scenes or saves."""
from pathlib import Path
import json,hashlib,math
from PIL import Image
from world_asset_baker import ROOT,load_map,frame,resource,native_map_path
checks=0

def check(ok,name):
 global checks
 if not ok:raise AssertionError(name)
 checks+=1

folder=ROOT/'Assets/_Game/Resources/Towns/Kaosia';data=json.loads((folder/'Map.json').read_text());source=load_map('1000')
check((data['width'],data['height'])==(source['width'],source['height'])==(43,43),'user-confirmed early Kaosia dimensions')
check(data['attributes']==[t[3]&127 for t in source['tiles']],'original collision attributes')
check(len(data['objects'])==len(source['sprites'])==225,'original decoration count')
canvas=Image.new('RGBA',(43*32,43*32),(0,0,0,255))
for i,(rid,group,tile) in enumerate(zip(source['a'],source['b'],source['tiles'])):
 canvas.alpha_composite(frame(rid,group,0),(i%43*32,i//43*32))
 if tile[1]!=255:
  g=resource(tile[0])['groups'][tile[1]];canvas.alpha_composite(frame(tile[0],tile[1],g['tiles'][tile[2]]['frames'][0],transparent=True),(i%43*32,i//43*32))
check(Image.open(folder/'Ground.png').convert('RGBA').tobytes()==canvas.tobytes(),'all 1849 original floor cells')
for actual,original in zip(data['objects'],source['sprites']):
 check((actual['x'],actual['y'])==(original['x'],original['y']),'decoration coordinates')
 check(Image.open(folder/(actual['texture']+'.png')).convert('RGBA').tobytes()==frame(original['resource'],original['index'],original['frame'],True,True).tobytes(),'decoration source pixels')
staff=json.loads((ROOT/'Assets/_Game/Resources/Town/OriginalInteriorStaff.json').read_text());check(staff['version']==1 and len(staff['entries'])==11,'five photographed and six user-confirmed staff rectangles')
check([(r['nativeMap'],r['appearanceId'],r['pixelX'],r['pixelY']) for r in staff['entries']]==[('2100',807,128,128),('2100',810,480,128),('2400',810,480,128),('2510',807,128,128),('2600',818,288,160),('2000',807,128,128),('2000',810,480,128),('2200',807,128,128),('2200',810,480,128),('2300',807,128,128),('2300',810,480,128)],'only source-observed and user-confirmed rectangles')
weapon=[(r['appearanceId'],r['frame'],r['pixelX'],r['pixelY'],r['texture']) for r in staff['entries'] if r['nativeMap']=='2000']
armor=[(r['appearanceId'],r['frame'],r['pixelX'],r['pixelY'],r['texture']) for r in staff['entries'] if r['nativeMap']=='2100']
check(weapon==armor and all(r['evidence']=='user-confirmed' for r in staff['entries'] if r['nativeMap']=='2000'),'user-confirmed weapon staff match armor exactly')
accessory=[(r['appearanceId'],r['frame'],r['pixelX'],r['pixelY'],r['texture']) for r in staff['entries'] if r['nativeMap']=='2200']
check(accessory==armor and all(r['evidence']=='user-confirmed' for r in staff['entries'] if r['nativeMap']=='2200'),'user-confirmed accessory staff match armor exactly')
consumables=[(r['appearanceId'],r['frame'],r['pixelX'],r['pixelY'],r['texture']) for r in staff['entries'] if r['nativeMap']=='2300']
check(consumables==armor and all(r['evidence']=='user-confirmed' for r in staff['entries'] if r['nativeMap']=='2300'),'user-confirmed consumables staff match armor exactly')
mage=[r for r in staff['entries'] if r['nativeMap']=='2800']
check(not mage,'unverified mage-house placements removed')
for row in staff['entries']:
 check((ROOT/row['reference']).exists(),'evidence reference exists')
 source=next(p for p in (ROOT/'분석/Saver_1.2/decoded/all_arc/gameres').rglob('*') if p.name.lower()==str(row['appearanceId'])+'r.bmp')
 sheet=Image.open(source).convert('RGBA');size=sheet.width//3;index=row['frame'];im=sheet.crop((index*size,size,(index+1)*size,2*size));im.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in im.getdata()])
 p=ROOT/'Assets/_Game/Resources'/(row['texture']+'.png');check(Image.open(p).convert('RGBA').tobytes()==im.tobytes(),'native Down frame pixels')
 check(p.with_name(p.name+'.meta').exists(),'texture importer exists')
 m=json.loads((ROOT/'Assets/_Game/Resources/Interiors'/row['nativeMap']/'Map.json').read_text());x=row['pixelX']/32+.5;y=m['height']-row['pixelY']/32-.5
 nearest=min(math.hypot(xx+.5-x,m['height']-yy-.5-y) for yy in range(m['height']) for xx in range(m['width']) if m['attributes'][yy*m['width']+xx]!=9)
 if row['nativeMap']=='2800':
  check(nearest<=3 and row['pixelX']+im.width<=m['width']*32 and row['pixelY']+im.height<=m['height']*32,'mage-house staff fit room and wide counter interaction distance')
 else:
  check(nearest<=2,'walkable interaction cell within existing interaction distance')
report=dict(checks=checks,originalMap='Saver_1.2/MAP/1000.mrc',sourceSha256=hashlib.sha256(native_map_path('1000').read_bytes()).hexdigest(),floorCells=1849,decorationObjects=225,photoObservedStaff=[r for r in staff['entries'] if r['evidence']=='screenshot'],userConfirmedStaff=[r for r in staff['entries'] if r['evidence']=='user-confirmed'],userAuthoredStaff=[r for r in staff['entries'] if r['evidence']=='user-authored'],disabledAuthoredKaosiaNpcs=8,portalCoordinatesChanged=False,saveFilesModified=False,apiCompileErrors=dict(runtime=0,editor=0,editModeSources=0),actualUnityTestsExecuted=False,actualUnityPlayExecuted=False,
 unknown=['Unseen bank left and warehouse right not reconstructed from another photograph', 'Temple exact animation phase and native server feet coordinates not asserted', 'Bank/warehouse transactions and exact original full HUD/interaction behavior not implemented', 'Building-to-interior association and door thresholds remain existing inferred routing, not proven original server placement'],
 preservedUserPolicies=['Kaosia early43x43 map and spawn21,21','Single-player initial funds and free recovery removed'])
out=ROOT/'Tools/LateVersionChecks/kaosia_original_presentation_check.json';out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('PASS',checks,'offline original Kaosia/resource checks; Unity Play not run.')
