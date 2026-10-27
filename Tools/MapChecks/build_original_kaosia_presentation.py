"""Export photograph-observed and user-confirmed native staff frames; do not regenerate maps or enable authored staff.
The temple photograph is vertically offset by32px relative to the original map canvas.
No unseen employees or original server identities are inferred.
"""
from pathlib import Path
import json,re,uuid
from PIL import Image
root=Path(__file__).resolve().parents[2]
res=root/'Assets/_Game/Resources';out=res/'Interiors/Npcs/Original';out.mkdir(exist_ok=True)
template=(res/'Interiors/Npcs/807.png.meta').read_text()
def meta(p,kind='text'):
 q=p.with_name(p.name+'.meta')
 if q.exists():return
 guid=uuid.uuid5(uuid.NAMESPACE_URL,str(p.relative_to(root))).hex
 if kind=='png':q.write_text(re.sub(r'guid: [a-f0-9]{32}','guid: '+guid,template,count=1))
 else:q.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+('folderAsset: yes\nDefaultImporter:' if kind=='folder' else 'TextScriptImporter:')+'\n  externalObjects: {}\n')
meta(out,'folder')
# Pixel positions are source-canvas frame origins, not invented server/feet coordinates.
records=[('2100',807,1,128,128,'KakaoTalk_20150424_030754859.png'),('2100',810,1,480,128,'KakaoTalk_20150424_030754859.png'),('2400',810,1,480,128,'잇힝.png'),('2510',807,0,128,128,'ffeawf2.jpg'),('2600',818,0,288,160,'050805120946168.jpg')]
entries=[]
for native,appearance,frame,px,py,photo in records:
 source=next(p for p in (root/'분석/Saver_1.2/decoded/all_arc/gameres').rglob('*') if p.name.lower()==f'{appearance}r.bmp')
 sheet=Image.open(source).convert('RGBA');size=sheet.width//3
 image=sheet.crop((frame*size,size,(frame+1)*size,2*size));image.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in image.getdata()])
 p=out/f'{appearance}_Down{frame}.png';image.save(p);meta(p,'png')
 entries.append(dict(nativeMap=native,appearanceId=appearance,frame=frame,pixelX=px,pixelY=py,texture=str(p.relative_to(res).with_suffix('')),reference='닥세_리소스/'+photo,evidence='screenshot'))
# User confirmed weapon, accessory and consumables shops use the same two appearances and positions as armor.
for native in ('2000','2200','2300'):
 entries.extend(dict(row,nativeMap=native,evidence='user-confirmed') for row in list(entries) if row['nativeMap']=='2100')
p=res/'Town/OriginalInteriorStaff.json' ;p.write_text(json.dumps(dict(version=1,entries=entries),ensure_ascii=False,indent=2)+'\n');meta(p)
for rel in ['Assets/_Game/Runtime/World/KaosiaOriginalPresentation.cs','Assets/_Game/Runtime/World/OriginalTownStaff.cs']:
 p=root/rel;p.with_name(p.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,rel).hex+'\n')
print('Exported five photo-observed and six user-confirmed staff positions. Temple frame origin includes the photographed 32px vertical camera offset. No map/save/routing asset edits.')
