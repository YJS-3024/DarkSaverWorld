"""Convert original COMBINE_CLASS RBM masks; keep authored campaign/gateway data intact."""
import hashlib,importlib.util,json,re,sys,struct,uuid
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from build_late_saver_catalog import arc_entries
from analyze_client import lzh_decode,lzss_decode
def build():
 folder=BASE/'Art/Campaign/Combine';folder.mkdir(exist_ok=True)
 spec=importlib.util.spec_from_file_location('scenario_meta',ROOT/'Tools/OriginalRestart/reset_assets.py');meta=importlib.util.module_from_spec(spec);spec.loader.exec_module(meta)
 if not Path(str(folder)+'.meta').exists():meta.meta(folder,BASE,True)
 template=Path(str(BASE/'Art/Tutorial/Board.png')+'.meta').read_text();refs=[];sources={};outputs=[]
 for archive,count in [('effect1',5),('effect2',3)]:
  source=BASE/'SourceData/Native/gameres'/(archive+'.arc.bytes');sources[str(source.relative_to(ROOT))]=hashlib.sha256(source.read_bytes()).hexdigest()
  entries={n:(m,size,b)for n,m,size,b in arc_entries(source)}
  for n in range(1,count+1):
   name='mask'+str(n)+'.rbm';method,size,packed=entries[name];raw=lzh_decode(packed,size)if method==0 else lzss_decode(packed,size)
   mode,w,h,stride=struct.unpack_from('<4I',raw);assert mode==0 and len(raw)==16+stride*h
   # LoadMask retains stored row order; AlphaBlit consumes rows top-to-bottom, mask/256 opacity.
   alpha=b''.join(raw[16+y*stride:16+y*stride+w]for y in range(h));image=Image.new('RGBA',(w,h),'white');image.putalpha(Image.frombytes('L',(w,h),alpha))
   target=folder/(archive+'-'+str(n)+'.png');image.save(target)
   guid=uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+target.relative_to(BASE).as_posix()).hex
   Path(str(target)+'.meta').write_text(re.sub(r'^guid: .*$', 'guid: '+guid,template,flags=re.M));refs.append('{fileID: 2800000, guid: '+guid+', type: 3}')
   outputs.append(dict(asset=str(target.relative_to(ROOT)),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),archive=archive,member=name,width=w,height=h,rawAlphaSha256=hashlib.sha256(alpha).hexdigest()))
 asset=BASE/'Data/Campaign/NativeCampaignArt.asset';text=asset.read_text();text=re.sub(r'  combineMasks:\n(?:  - .*\n)*','',text);asset.write_text(text+'  combineMasks:\n'+''.join('  - '+ref+'\n'for ref in refs))
 for name in ['campaign-assets.json','gateway-assets.json']:
  p=OUT/name;report=json.loads(p.read_text());key=str(asset.relative_to(ROOT));digest=hashlib.sha256(asset.read_bytes()).hexdigest()
  if key in report.get('sources',{}):report['sources'][key]=digest
  for row in report.get('outputs',[]):
   if row['asset']==key:row['sha256']=digest
  p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 report=dict(sources=sources,outputs=outputs,unityPlay=False,policy='Native row order and original mask extents. Unity RawImage alpha is multiplied by255/256, preserving mask opacity factor; Unity RGB rendering is not a certified RGB565 pixel reproduction.')
 (OUT/'scenario-effect-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');return len(outputs)
if __name__=='__main__':print(build())
