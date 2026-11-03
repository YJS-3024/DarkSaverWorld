"""Bind source Glib windows/minimized art, hmaps and SRF text; no gameplay/map edits."""
import hashlib,json,re,sys,tempfile
from pathlib import Path
from PIL import Image
from build_world_assets import ROOT,BASE,metadata,key,guid
from build_combat_assets import script_meta
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'));from decode_visuals import read_lsc,read_ui_library
OUT=Path(__file__).parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 inputs=[Path(__file__)];outputs=[];native=BASE/'SourceData/Native/glib';target=BASE/'Art/World';scenes=[0,1,3,4,5,6,7,9,24,25,26,27,28];old={w['scene']:w for w in json.loads((BASE/'Data/Tutorial/Tutorial301.json').read_text())['windows']};inputs.append(BASE/'Data/Tutorial/Tutorial301.json')
 def ref(p,kind=2800000):return '{fileID: '+str(kind)+', guid: '+guid(p)+', type: 3}'if p else'{fileID: 0}'
 def register(p):outputs.append(dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p)))
 with tempfile.TemporaryDirectory()as tmp:
  libraries={}
  for ext in ['lce','ltn','lnm']:
   source=native/f'gamelib.{ext}.bytes';inputs.append(source);p=Path(tmp)/('gamelib.'+ext);p.write_bytes(source.read_bytes());libraries[ext]=read_ui_library(p)[:2]
  def render(scene,mini=False):
   source=native/f'gscene{scene}{"m"if mini else""}.lsc.bytes';inputs.append(source);spec=read_lsc(source);image=Image.new('RGBA',(spec['width'],spec['height']));buttons=[]
   for d in spec['decor']:
    part=libraries['lce'][0][d['id']];piece=libraries['lce'][1].crop((part['x'],part['y'],part['x']+part['width'],part['y']+part['height']))
    for y in range(d['repeat_y']):
     for x in range(d['repeat_x']):image.alpha_composite(piece,(d['x']+x*part['width'],d['y']+y*part['height']))
   for b in spec['buttons']:
    part=libraries['ltn'if b['kind']=='button'else'lnm'][0][b['id']]
    if b['kind']=='animation':image.alpha_composite(libraries['lnm'][1].crop((part['x'],part['y'],part['x']+part['width'],part['y']+part['height'])),(b['x'],b['y']))
    else:buttons.append(dict(id=b['id'],x=b['x'],y=b['y'],sx=part['x'],sy=part['y'],width=part['width'],height=part['height']))
   existing=old.get(scene)
   if existing:
    path=BASE/f'Art/Tutorial/{"Minimized"if mini else"Window"}F{existing["key"]}.png'
    assert Image.open(path).convert('RGBA').tobytes()==key(image).tobytes(),str(path)
    inputs.extend([path,Path(str(path)+'.meta')])
   else:
    path=target/f'BattleScene{scene}{"Minimized"if mini else""}.png';key(image).save(path);metadata.meta(path,ROOT/'Assets');register(path)
   return spec,path,buttons
  specs=[];full=[];minimized=[]
  for scene in scenes:
   spec,path,buttons=render(scene);full.append(path);left,top,right,bottom=spec['region'];row=dict(id=scene,scene=scene,width=spec['width'],height=spec['height'],dragX=left,dragY=top,dragWidth=right-left,dragHeight=bottom-top,buttons=buttons)
   if (native/f'gscene{scene}m.lsc.bytes').exists():
    mini,path,_=render(scene,True);minimized.append(path);left,top,right,bottom=mini['region'];row.update(minWidth=mini['width'],minHeight=mini['height'],minDragX=left,minDragY=top,minDragWidth=right-left,minDragHeight=bottom-top)
   else:minimized.append(None)
   specs.append(row)
 layout=BASE/'Resources/OriginalBattleWindowLayout.json';layout.write_text(json.dumps(dict(windows=specs),ensure_ascii=False,separators=(',',':'))+'\n');metadata.meta(layout,ROOT/'Assets');register(layout)
 maps=[];hmaps=[];briefings=[]
 for path in sorted((BASE/'Art/Native/map').glob('*hmap.bmp.png'),key=lambda p:int(p.name.split('hmap')[0])):
  token=path.name.split('hmap')[0];id=int(token);assert id not in maps;maps.append(id);hmaps.append(path);inputs.extend([path,Path(str(path)+'.meta')]);source=BASE/f'SourceData/Decoded/map/{token}set.arc/{token}.srf.txt';briefings.append(source if source.exists()else None)
  if source.exists():inputs.append(source)
 script=BASE/'Runtime/UI/NativeBattleWindowArt.cs';script_meta(script);script_meta(BASE/'Runtime/UI/NativeBattleOptions.cs')
 asset=BASE/'Resources/OriginalBattleWindows.asset';asset.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: '+ref(script,11500000)+'\n  m_Name: OriginalBattleWindows\n  m_EditorClassIdentifier: \n  layout: '+ref(layout,4900000)+'\n  full:\n'+''.join('  - '+ref(p)+'\n'for p in full)+'  minimized:\n'+''.join('  - '+ref(p)+'\n'for p in minimized)+'  mapIds: '+''.join(id.to_bytes(4,'little',signed=True).hex()for id in maps)+'\n  minimaps:\n'+''.join('  - '+ref(p)+'\n'for p in hmaps)+'  briefings:\n'+''.join('  - '+ref(p,4900000)+'\n'for p in briefings)+'  markers: '+ref(BASE/'Art/Archives/gameres/etc.arc/mcursor.bmp.png')+'\n');metadata.meta(asset,ROOT/'Assets');meta_path=Path(str(asset)+'.meta');meta_path.write_text('fileFormatVersion: 2\nguid: '+guid(asset)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');register(asset)
 for row in list(outputs):
  meta_path=Path(str(ROOT/row['asset'])+'.meta')
  if meta_path.exists():register(meta_path)
 inputs.extend([script,Path(str(script)+'.meta'),BASE/'Art/Archives/gameres/etc.arc/mcursor.bmp.png',BASE/'Art/Archives/gameres/etc.arc/mcursor.bmp.png.meta'])
 report=dict(windows=len(scenes),sourceMinimaps=len(maps),sourceBriefings=sum(p is not None for p in briefings),sources={p.relative_to(ROOT).as_posix():sha(p)for p in inputs},outputs=outputs,scope='Only original Glib UI resources reassembled; existing tutorial window pixels checked and reused. Original hmap/SRF references, no generated substitutes or authored map/placement/save rewrites.')
 (OUT/'native-battle-window-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','outputs']}))
if __name__=='__main__':main()
