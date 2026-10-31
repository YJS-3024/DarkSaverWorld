"""Editor NPC gateway authoring catalog and native Wlib11 participation artwork."""
import csv,hashlib,importlib.util,json,sys,tempfile
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';DATA=BASE/'Data/Campaign';ART=BASE/'Art/Campaign'
spec=importlib.util.spec_from_file_location('campaign_builder',Path(__file__).with_name('build_campaign_assets.py'));campaign=importlib.util.module_from_spec(spec);spec.loader.exec_module(campaign);world=campaign.world
from decode_visuals import read_mrc,read_lsc,read_ui_library
def main():
 report=dict(sources={},outputs=[],policy='User-authored NPC gateway positions; one NPC per scenario. Disabled and empty by default.',unityPlay=False)
 def source(p):report['sources'][p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest();return p
 def output(p):world.metadata.meta(p,ROOT/'Assets');report['outputs'].append(dict(asset=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()));return p
 native=json.loads(source(DATA/'CampaignCatalog.json').read_text());base=json.loads(source(BASE/'Data/World/KaosiaWorld.json').read_text());ids={m['id']for m in base['maps']}
 keys={p.name.removesuffix('set.arc')for p in (BASE/'SourceData/Decoded/map').glob('*set.arc')}|{m['key']for m in native['maps']}|{str(i)for i in ids}
 table=list(csv.DictReader(source(BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig')));pics={int(p.name.removesuffix('.png')[7:])for p in ART.glob('Picture*.png')}
 jobs=[int(r['직업'])for r in table if r['category']=='npc_or_special'and 800<=int(r['직업'])<900 and int(r['직그림'])in pics]
 infos=[]
 for key in sorted(keys,key=int):
  legacy=key=='1000';p=source(BASE/('SourceData/KaosiaLegacy'if legacy else 'SourceData/Native/map')/(key+'.mrc.bytes'));m=read_mrc(p);id=int(key)
  kind=next((v['kind']for v in native['maps']if v['id']==id),'town'if id==1000 else 'interior')
  title=next((v['title']for v in base['maps']if v['id']==id),next((v['world']['title']for v in native['maps']if v['id']==id),key))
  infos.append(dict(id=id,key=key,width=m['width'],height=m['height'],hash=hashlib.sha256(p.read_bytes()).hexdigest(),title=title,kind=kind,legacy=legacy,nativePath=p.relative_to(ROOT).as_posix()))
 maps=DATA/'GatewayMaps.json';maps.write_text(json.dumps(dict(maps=infos,npcJobs=jobs),ensure_ascii=False,indent=2)+'\n');output(maps)
 raw=source(BASE/'SourceData/Native/wlib/scene11.lsc.bytes');d=read_lsc(raw);libs={}
 with tempfile.TemporaryDirectory()as tmp:
  for ext in ['lce','ltn']:
   p=Path(tmp)/('waitlib.'+ext);p.write_bytes(source(BASE/f'SourceData/Native/wlib/waitlib.{ext}.bytes').read_bytes());libs[ext]=read_ui_library(p)[:2]
  im=Image.new('RGBA',(d['width'],d['height']));controls=[]
  for v in d['decor']:
   a=libs['lce'][0][v['id']];piece=libs['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
   for y in range(v['repeat_y']):
    for x in range(v['repeat_x']):im.alpha_composite(piece,(v['x']+x*a['width'],v['y']+y*a['height']))
  for b in d['buttons']:
   if b['kind']!='button':continue
   a=libs['ltn'][0][b['id']];controls.append(dict(id=b['id'],x=b['x'],y=b['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
  art=ART/'GatewayRooms.png';world.key(im).save(art);output(art)
 ui=DATA/'GatewayWindow.json';ui.write_text(json.dumps(dict(scene=11,width=d['width'],height=d['height'],buttons=controls),indent=2)+'\n');output(ui)
 settings=DATA/'UserGateways.json'
 if not settings.exists():settings.write_text(json.dumps(dict(version=1,applyToGame=False,provenance='user-authored; NPC interaction recalled by user; not recovered original server placements',gates=[]),indent=2)+'\n')
 world.metadata.meta(settings,ROOT/'Assets')
 for p in BASE.rglob('*'):
  if(p.is_dir()or p.suffix=='.cs')and not Path(str(p)+'.meta').exists():world.metadata.meta(p,ROOT/'Assets',folder=p.is_dir())
 asset=DATA/'NativeCampaignArt.asset';text=asset.read_text();text='\n'.join(line for line in text.splitlines()if not any(line.startswith('  '+f+':')for f in ['gatewaySettings','gatewayMaps','gatewayWindowDefinition','gatewayWindow']))+'\n'
 for field,path,kind in [('gatewaySettings',settings,4900000),('gatewayMaps',maps,4900000),('gatewayWindowDefinition',ui,4900000),('gatewayWindow',art,2800000)]:text+='  '+field+': {fileID: '+str(kind)+', guid: '+world.guid(path)+', type: 3}\n'
 asset.write_text(text);report.update(maps=len(infos),npcJobs=len(jobs),settings=settings.relative_to(ROOT).as_posix());Path(__file__).with_name('gateway-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:report[k]for k in ['maps','npcJobs']}))
if __name__=='__main__':main()
