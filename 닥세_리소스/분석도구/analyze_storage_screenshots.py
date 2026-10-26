"""Compare repository reference screenshots to native interiors/NPC frames.
Static visual evidence only; no game execution or storage policy inference.
Python3 + Pillow + NumPy. Run from repository root.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json,hashlib,sys
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(Path(__file__).parent))
from decode_visuals import read_ui_library,read_lsc
OUT=ROOT/'분석/Saver_Late/screenshots';OUT.mkdir(exist_ok=True)

def render_map(id):
 folder=ROOT/'Assets/_Game/Resources/Interiors'/id;data=json.loads((folder/'Map.json').read_text());im=Image.open(folder/'Ground.png').convert('RGBA')
 for s in data['objects']:im.alpha_composite(Image.open(folder/(s['texture']+'.png')).convert('RGBA'),(s['x']*32,s['y']*32))
 return im

def render_ui(key):
 root=ROOT/'닥세_리소스/설치본_추출/Saver_Files/Saver/Wlib';files={p.name.lower():p for p in root.iterdir()};libs={ext:read_ui_library(files['waitlib'+ext])[:2] for ext in ['.lce','.ltn','.lnm']};d=read_lsc(files[f'scene{key}.lsc']);canvas=Image.new('RGBA',(d['width'],d['height']),(0,0,0,255))
 for v in d['decor']:
  parts,atlas=libs['.lce'];a=parts[v['id']];im=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
  for y in range(v['repeat_y']):
   for x in range(v['repeat_x']):canvas.alpha_composite(im,(v['x']+x*a['width'],v['y']+y*a['height']))
 for v in d['buttons']:
  parts,atlas=libs['.lnm' if v['kind']=='animation' else '.ltn'];a=parts[v['id']];canvas.alpha_composite(atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height'])),(v['x'],v['y']))
 return canvas

def main():
 shot_path=ROOT/'닥세_리소스/ĸó1.jpg';shot=Image.open(shot_path).convert('RGB');rect=(470,57,543,99);actual=np.array(shot.crop(rect),dtype=float);map_results=[]
 for id in ['2400','2510']:
  source=render_map(id).convert('RGB');best=(1e12,None)
  for width in range(570,581):
   scale=width/640;scaled=source.resize((width,round(480*scale)),Image.Resampling.BILINEAR)
   for tx in range(-3,8):
    for ty in range(-3,8):
     expected=np.array(scaled.crop((rect[0]-tx,rect[1]-ty,rect[2]-tx,rect[3]-ty)),dtype=float);mse=float(((actual-expected)**2).mean())
     if mse<best[0]:best=mse,dict(scale=scale,offset=[tx,ty])
  map_results.append(dict(nativeMap=id,region=list(rect),mse=best[0],registration=best[1]))
 layout=next(r['registration'] for r in map_results if r['nativeMap']=='2510');scale=layout['scale'];results=[]
 for path in (ROOT/'분석/Saver_1.2/decoded/all_arc/gameres').rglob('*.bmp'):
  if path.stem[-1:].lower()!='r' or not path.stem[:-1].isdigit() or not 804<=int(path.stem[:-1])<=828:continue
  sprite=Image.open(path).convert('RGBA');size=sprite.width//3
  if sprite.height<size*4:continue
  for col in range(3):
   frame=sprite.crop((col*size,size,(col+1)*size,size*2));rgba=np.array(frame);rgba[:,:,3]=np.where(np.any(rgba[:,:,:3]>0,axis=2),255,0);frame=Image.fromarray(rgba).resize((round(size*scale),round(size*scale)),Image.Resampling.BILINEAR)
   rgba=np.array(frame);mask=rgba[:,:,3]>240;mask[round(22*scale):,:]=False;rgb=rgba[:,:,:3].astype(float);best=(1e12,None)
   for y in range(95,127):
    for x in range(423,452):
     pixels=np.array(shot.crop((x,y,x+frame.width,y+frame.height)),dtype=float);score=float(((pixels[mask]-rgb[mask])**2).mean())
     if score<best[0]:best=score,[x,y]
   results.append(dict(appearance=int(path.stem[:-1]),direction='Down',frame=col,mse=best[0],frameTopLeft=best[1],source=str(path.relative_to(ROOT))))
 results.sort(key=lambda r:r['mse']);assert results[0]['appearance']==810 and results[0]['frame']==1
 assert next(r['mse'] for r in map_results if r['nativeMap']=='2510')<next(r['mse'] for r in map_results if r['nativeMap']=='2400')/5
 # Export an inspection plate. It is a source comparison, never a Unity screenshot.
 plate=Image.new('RGB',(960,620),(45,45,45));draw=ImageDraw.Draw(plate)
 plate.paste(shot.crop((416,95,480,155)).resize((192,180)),(8,27));draw.text((8,6),'Screenshot1: right-counter NPC',fill='white')
 best=results[0];im=Image.open(ROOT/best['source']).convert('RGB');s=im.width//3;plate.paste(im.crop((s,s,2*s,2*s)).resize((128,128)),(222,27));draw.text((222,6),'810R Down frame1',fill='white')
 for i,id in enumerate(['2400','2510']):
  region=render_map(id).crop((416,32,624,192)).convert('RGB').resize((312,240));plate.paste(region,(i*330+8,242));draw.text((i*330+8,222),id+(' bank' if id=='2400' else ' warehouse'),fill='white')
 ui=Image.open(ROOT/'닥세_리소스/ĸó3.jpg').convert('RGB');plate.paste(ui.crop((0,150,366,415)).resize((274,198)),(678,27));draw.text((678,6),'Screenshot3: storage UI',fill='white');plate.save(OUT/'storage_comparison.png')
 for key in [10,20]:render_ui(key).save(OUT/f'Wlib_scene{key}.png')
 report=dict(referenceFiles=[dict(file=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),size=list(Image.open(p).size)) for p in [shot_path,ROOT/'닥세_리소스/ĸó3.jpg',ROOT/'닥세_리소스/ĸó4.jpg']],mapCandidates=map_results,npcCandidates=results[:10],method='Bilinear scale/translation search for exposed shelves. RGB MSE of opaque upper22 sprite rows; lower body excluded due to counter occlusion. Scores rank candidates, not statistical confidence.',findings={'storageUIVisible':True,'displayedStorageFee':0,'alwaysFreeConfirmed':False,'warehouse2510VisualMatch':'strong','visibleStaffAppearance810':'strong','exactNpcName':None,'npcCount':'at least one visible; obscured area uncounted','spriteTopLeftApproxNativePixels':[480,128],'spriteTopLeftApproxSourceTile':[15,4],'logicalNpcCoordinateConfirmed':False,'bank2400StaffConfirmed':False},currentImplementation={'warehouseMap':'2510','warehouseAppearance':827,'warehouseNativeCell':[9,6],'warehouseAssignment':'authored placeholder; screenshot corroborates810 instead','bankAppearance':807,'bankAssignment':'authored, not resolved by these screenshots'},unityPlay=False)
 ui_source=read_lsc(ROOT/'닥세_리소스/설치본_추출/Saver_Files/Saver/Wlib/scene20.lsc')
 report['uiTemplate']=dict(scene='Wlib/scene20.lsc',width=ui_source['width'],height=ui_source['height'],headings=['가진 물건','맡길 물건'],buttons=ui_source['buttons'],scope='Native storage-style base panel; screenshot fee/items/currency are dynamic overlays. Exact screenshot/client version and control flow are not proven identical.')
 (OUT/'storage_analysis.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(bestMap='2510',bestAppearance=best['appearance'],bestFrame=best['frame'],npcMse=best['mse'],mapMse={r['nativeMap']:r['mse'] for r in map_results})))

if __name__=='__main__':main()
