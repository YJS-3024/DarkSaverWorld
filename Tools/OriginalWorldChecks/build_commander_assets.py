"""Compose the original alternative F2 scene42; preserve existing authored world data."""
import hashlib,json,re,tempfile
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT,metadata,key
from decode_visuals import read_lsc,read_ui_library

def build():
 source=BASE/'SourceData/Native/wlib';out=BASE/'Art/World';inputs=[];outputs=[]
 with tempfile.TemporaryDirectory(prefix='ds-roster-art-') as temp:
  libs={}
  for ext in ['lce','ltn','lnm']:
   p=source/('waitlib.'+ext+'.bytes');inputs.append(p);q=Path(temp)/('waitlib.'+ext);q.write_bytes(p.read_bytes());libs[ext]=read_ui_library(q)[:2]
  layouts={}
  for name,file in [('scene42','CommanderRoster3.png'),('scene42m','CommanderRoster3Minimized.png')]:
   p=source/(name+'.lsc.bytes');inputs.append(p);d=read_lsc(p);im=Image.new('RGBA',(d['width'],d['height']));buttons=[]
   for b in d['decor']:
    a=libs['lce'][0][b['id']];piece=libs['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
    for y in range(b['repeat_y']):
     for x in range(b['repeat_x']):im.alpha_composite(piece,(b['x']+x*a['width'],b['y']+y*a['height']))
   for b in d['buttons']:
    parts,atlas=libs['lnm' if b['kind']=='animation' else 'ltn'];a=parts[b['id']]
    if b['kind']=='animation' or name.endswith('m'):im.alpha_composite(atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height'])),(b['x'],b['y']))
    else:buttons.append(dict(id=b['id'],x=b['x'],y=b['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
   p=out/file;key(im).save(p);outputs.append(p);layouts[name]=dict(scene=42,width=d['width'],height=d['height'],buttons=buttons)
 data=BASE/'Data/World/CommanderRoster3.json';data.write_text(json.dumps(layouts['scene42'],ensure_ascii=False,indent=2)+'\n');outputs.append(data)
 personal=[]
 for identifier,scene in [(18,2),(19,3),(20,4),(21,6),(67,42)]:
  p=source/f'scene{scene}.lsc.bytes';inputs.append(p);d=read_lsc(p);l,t,r,b=d['region'];buttons=[]
  for v in d['buttons']:
   if v['kind']=='animation':continue
   a=libs['ltn'][0][v['id']];buttons.append(dict(id=v['id'],x=v['x'],y=v['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
  mini=source/f'scene{scene}m.lsc.bytes';inputs.append(mini);m=read_lsc(mini);ml,mt,mr,mb=m['region']
  personal.append(dict(id=identifier,scene=scene,width=d['width'],height=d['height'],dragX=l,dragY=t,dragWidth=r-l,dragHeight=b-t,minWidth=m['width'],minHeight=m['height'],minDragX=ml,minDragY=mt,minDragWidth=mr-ml,minDragHeight=mb-mt,buttons=buttons))
 personal_data=BASE/'Data/World/WorldPersonalWindows.json';personal_data.write_text(json.dumps(dict(windows=personal),ensure_ascii=False,indent=2)+'\n');outputs.append(personal_data)
 assert (layouts['scene42']['width'],layouts['scene42']['height'])==(255,282)
 for p in outputs:metadata.meta(p,ROOT/'Assets')
 asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text()
 for field,p,kind in [('commanderWindow',outputs[0],2800000),('commanderMinimized',outputs[1],2800000),('commanderLayout',data,4900000),('personalLayout',personal_data,4900000)]:
  g=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
  text='\n'.join(s for s in text.splitlines() if not s.startswith('  '+field+':'))+'\n';text+='  '+field+': {fileID: '+str(kind)+', guid: '+g+', type: 3}\n'
 asset.write_text(text)
 sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
 for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
  p=Path(__file__).with_name(name);r=json.loads(p.read_text());k=asset.relative_to(ROOT).as_posix()
  if k in r.get('sources',{}):r['sources'][k]=sha(asset)
  for v in r.get('outputs',[]):
   if v['asset']==k:v['sha256']=sha(asset)
  p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
 r=dict(sources={p.relative_to(ROOT).as_posix():sha(p) for p in inputs},outputs=[dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for p in outputs],layout=layouts['scene42'],personal=personal,unityPlay=False,scope='Literal scene42/42m composition and source LSC personal-window drag/button regions. Native F2 chooses scene42 only when raw alternate gate is nonzero and owned record count is3; no inferred ownership or gate. World4347E0 profile positions are separate from tutorial lesson defaults.')
 Path(__file__).with_name('commander-assets.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');return r
if __name__=='__main__':
 r=build();print(json.dumps(dict(size=[r['layout']['width'],r['layout']['height']],buttons=len(r['layout']['buttons']),outputs=len(r['outputs']))))
