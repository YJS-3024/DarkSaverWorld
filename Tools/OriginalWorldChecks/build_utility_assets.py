"""Original world settings/help/quit/memo frames; no substitute labels or operating rules."""
import hashlib,json,re,tempfile
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT,metadata,key
from decode_visuals import read_lsc,read_ui_library

def build():
 source=BASE/'SourceData/Native/wlib';target=BASE/'Art/World';inputs=[];outputs=[];layouts=[]
 with tempfile.TemporaryDirectory(prefix='ds-world-ui-')as temp:
  libs={}
  for ext in ['lce','ltn','lnm']:
   p=source/('waitlib.'+ext+'.bytes');inputs.append(p);q=Path(temp)/('waitlib.'+ext);q.write_bytes(p.read_bytes());libs[ext]=read_ui_library(q)[:2]
  for identifier,scene in [(27,15),(33,19),(81,48)]:
   p=source/f'scene{scene}.lsc.bytes';inputs.append(p);d=read_lsc(p);im=Image.new('RGBA',(d['width'],d['height']));buttons=[]
   for b in d['decor']:
    a=libs['lce'][0][b['id']];piece=libs['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
    for y in range(b['repeat_y']):
     for x in range(b['repeat_x']):im.alpha_composite(piece,(b['x']+x*a['width'],b['y']+y*a['height']))
   for b in d['buttons']:
    parts,atlas=libs['lnm'if b['kind']=='animation'else'ltn'];a=parts[b['id']]
    if b['kind']=='animation':im.alpha_composite(atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height'])),(b['x'],b['y']))
    else:buttons.append(dict(id=b['id'],x=b['x'],y=b['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
   p=target/f'WorldUtility{identifier}.png';key(im).save(p);metadata.meta(p,ROOT/'Assets');outputs.append(p)
   l,t,r,b=d['region'];layouts.append(dict(id=identifier,scene=scene,x=(640-d['width'])//2 if identifier==33 else 0,y=(480-d['height'])//2 if identifier==33 else 0,width=d['width'],height=d['height'],dragX=l,dragY=t,dragWidth=r-l,dragHeight=b-t,buttons=buttons))
 data=BASE/'Data/World/WorldUtilities.json';data.write_text(json.dumps(dict(windows=layouts),ensure_ascii=False,indent=2)+'\n');metadata.meta(data,ROOT/'Assets');outputs.append(data)
 guid=lambda p:re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
 asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text();text=re.sub(r'^  utilityLayout:.*\n','',text,flags=re.M);text=re.sub(r'^  utilityWindows:\n(?:  - .*\n)*','',text,flags=re.M)
 text+='  utilityLayout: {fileID: 4900000, guid: '+guid(data)+', type: 3}\n  utilityWindows:\n'
 for p in outputs[:3]:text+='  - {fileID: 2800000, guid: '+guid(p)+', type: 3}\n'
 asset.write_text(text);sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
 for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
  p=Path(__file__).with_name(name);r=json.loads(p.read_text());k=asset.relative_to(ROOT).as_posix()
  if k in r.get('sources',{}):r['sources'][k]=sha(asset)
  for v in r.get('outputs',[]):
   if v['asset']==k:v['sha256']=sha(asset)
  p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
 r=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in inputs},outputs=[dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for p in outputs],windows=layouts,unityPlay=False,scope='Literal original wlib scene15/19/48; existing world scene27 is reused for quit frames and controls. Original4347E0 default0 0 0/profile restoration and4346D0 forced Help centering; literal LSC drag regions. Runtime drag clamping and actual rendering remain uGUI adapter boundaries.')
 Path(__file__).with_name('utility-assets.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');return r
if __name__=='__main__':
 r=build();print(json.dumps(dict(windows=[v['id']for v in r['windows']],outputs=len(r['outputs']))))
