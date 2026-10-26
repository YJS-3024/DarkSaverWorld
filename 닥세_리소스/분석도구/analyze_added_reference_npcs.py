"""Compare added NPC screenshots against opaque upper22 rows of native Down frames.
Scores rank visual candidates; they are not confidence probabilities or logical coordinates.
Python3 with Pillow and NumPy. Original screenshots and gameplay assets are never edited.
"""
from pathlib import Path
import numpy as np,json
from PIL import Image
R=Path(__file__).resolve().parents[2];sources=[]
for p in (R/'분석/Saver_1.2/decoded/all_arc/gameres').rglob('*.bmp'):
 if p.stem[-1:].lower()!='r' or not p.stem[:-1].isdigit() or not 804<=int(p.stem[:-1])<=828:continue
 im=Image.open(p).convert('RGB');s=im.width//3
 if im.height<s*4:continue
 for i in range(3):
  rgb=np.array(im.crop((i*s,s,(i+1)*s,2*s)),dtype=float);mask=np.any(rgb>0,axis=2);mask[22:]=False;sources.append((p,i,rgb,mask,s))
cases=[('armor-left','KakaoTalk_20150424_030754859.png',128,128),('armor-right','KakaoTalk_20150424_030754859.png',480,128),('warehouse-left','ffeawf2.jpg',128,128),('bank-right','잇힝.png',480,128),('temple-priest','050805120946168.jpg',288,128)]
results=[]
for name,file,ax,ay in cases:
 shot=Image.open(R/'닥세_리소스'/file).convert('RGB');rows=[]
 for p,i,rgb,mask,s in sources:
  best=(1e30,None)
  for y in range(ay-5,ay+6):
   for x in range(ax-5,ax+6):
    a=np.array(shot.crop((x,y,x+s,y+s)),dtype=float);mse=float(((a[mask]-rgb[mask])**2).mean())
    if mse<best[0]:best=(mse,[x,y])
  rows.append(dict(appearance=int(p.stem[:-1]),direction='Down',frame=i,mse=best[0],spriteTopLeft=best[1],source=str(p.relative_to(R))))
 rows.sort(key=lambda r:r['mse']);results.append(dict(case=name,file='닥세_리소스/'+file,candidates=rows[:5]));print(name,[(r['appearance'],r['frame'],round(r['mse'],2),r['spriteTopLeft']) for r in rows[:3]])
(R/'분석/Saver_Late/screenshots/added_reference_npc_matches.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n')
