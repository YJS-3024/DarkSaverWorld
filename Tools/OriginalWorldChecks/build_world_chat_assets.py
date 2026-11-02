"""Compose literal world input window17/scene1, separate from F5 history window19/scene3."""
import hashlib,json,re,tempfile
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT,metadata,key
from decode_visuals import read_lsc,read_ui_library
def build():
 source=BASE/'SourceData/Native/wlib';inputs=[];libs={};outputs=[];layouts={}
 with tempfile.TemporaryDirectory(prefix='ds-world-chat-')as temp:
  for ext in ['lce','ltn','lnm']:
   p=source/('waitlib.'+ext+'.bytes');inputs.append(p);q=Path(temp)/('waitlib.'+ext);q.write_bytes(p.read_bytes());libs[ext]=read_ui_library(q)[:2]
  for scene,identifier,name,x,y in [(1,17,'WorldChatInput',33,435),(47,80,'WorldPrivateChat',442,0)]:
   p=source/f'scene{scene}.lsc.bytes';inputs.append(p);d=read_lsc(p);im=Image.new('RGBA',(d['width'],d['height']));buttons=[]
   for v in d['decor']:
    parts,atlas=libs['lce'];a=parts[v['id']];piece=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
    for yy in range(v['repeat_y']):
     for xx in range(v['repeat_x']):im.alpha_composite(piece,(v['x']+xx*a['width'],v['y']+yy*a['height']))
   for v in d['buttons']:
    parts,atlas=libs['lnm'if v['kind']=='animation'else'ltn'];a=parts[v['id']]
    if v['kind']=='animation':im.alpha_composite(atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height'])),(v['x'],v['y']))
    else:buttons.append(dict(id=v['id'],x=v['x'],y=v['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
   image=BASE/('Art/World/'+name+'.png');key(im).save(image);metadata.meta(image,ROOT/'Assets');outputs.append(image)
   data=BASE/('Data/World/'+name+'.json');l,t,r,b=d['region'];layout=dict(id=identifier,scene=scene,x=x,y=y,width=d['width'],height=d['height'],dragX=l,dragY=t,dragWidth=r-l,dragHeight=b-t,buttons=buttons)
   data.write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n');metadata.meta(data,ROOT/'Assets');outputs.append(data);layouts[identifier]=layout
 asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text()
 for field,p,kind in [('chatWindow',outputs[0],2800000),('chatLayout',outputs[1],4900000),('privateChatWindow',outputs[2],2800000),('privateChatLayout',outputs[3],4900000)]:
  guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1];text='\n'.join(row for row in text.splitlines()if not row.startswith('  '+field+':'))+'\n';text+='  '+field+': {fileID: '+str(kind)+', guid: '+guid+', type: 3}\n'
 asset.write_text(text);sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
 for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
  p=Path(__file__).with_name(name);record=json.loads(p.read_text());k=asset.relative_to(ROOT).as_posix()
  if k in record.get('sources',{}):record['sources'][k]=sha(asset)
  for row in record.get('outputs',[]):
   if row['asset']==k:row['sha256']=sha(asset)
  p.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
 report=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in inputs},outputs=[dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for p in outputs],layout=layouts[17],privateLayout=layouts[80],scope='Original scene1 and47 decoration/button layouts;470A80 X33/Y435 and477520 private-window X442/Y24*index. 470700 edit18,7..461,26 and452D60 private edit4,120..189,135. uGUI input/caret is an adapter; not source Win32 IME rendering.',unityPlay=False);Path(__file__).with_name('world-chat-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');return report
if __name__=='__main__':
 r=build();print(json.dumps(dict(size=[r['layout']['width'],r['layout']['height']],buttons=len(r['layout']['buttons']))))
