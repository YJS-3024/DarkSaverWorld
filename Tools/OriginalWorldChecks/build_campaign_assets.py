"""Native field/scenario resources, with no invented links, rosters or server rules."""
import csv,hashlib,importlib.util,json,re,sys
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';DATA=BASE/'Data/Campaign';ART=BASE/'Art/Campaign'
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_mrc
spec=importlib.util.spec_from_file_location('world_builder',Path(__file__).with_name('build_world_assets.py'));world=importlib.util.module_from_spec(spec);spec.loader.exec_module(world)

def main():
 DATA.mkdir(exist_ok=True);ART.mkdir(exist_ok=True)
 report=dict(sources={},outputs=[],connections=[],policy='Only source-evidenced links; none recovered. No fabricated field NPC, monster roster, placement, battle outcomes or rewards.',unityPlay=False)
 def source(p):report['sources'][p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest();return p
 def output(p,origin):world.metadata.meta(p,ROOT/'Assets');report['outputs'].append(dict(asset=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),source=origin));return p
 def ref(p,kind=4900000):return '{fileID: '+str(kind)+', guid: '+world.guid(p)+', type: 3}'
 unit_table=source(BASE/'Data/Tables/units_all.csv');jobs={int(r['직업']):r for r in csv.DictReader(unit_table.open(encoding='utf-8-sig'))}
 literal=json.loads(source(BASE/'Data/Placements/scenario_monster_placements.json').read_text());placements={s['scenario']:s for s in literal['scenarios']}
 nav=json.loads(source(BASE/'Data/Tutorial/Tutorial301.json').read_text())['map']
 keys=sorted({p.name.removesuffix('set.arc')for p in (BASE/'SourceData/Decoded/map').glob('*set.arc')if int(p.name.removesuffix('set.arc'))not in [*range(101,108),*range(201,208),301]}
  |{p.name.removesuffix('.mrc.bytes')for p in (BASE/'SourceData/Native/map').glob('*.mrc.bytes')if p.name.removesuffix('.mrc.bytes').isdigit()and 1000<int(p.name.removesuffix('.mrc.bytes'))<1500},key=int)
 maps=[];map_paths=[];scripts=[]
 def script(p):
  if not p.exists():return -1
  scripts.append(source(p));return len(scripts)-1
 for key in keys:
  path=source(BASE/'SourceData/Native/map'/f'{key}.mrc.bytes');m=read_mrc(path);map_paths.append(path)
  prefix=BASE/'SourceData/Decoded/map'/f'{key}set.arc'/key
  title_path=prefix.with_suffix('.srf.txt');title=source(title_path).read_text().splitlines()[0].strip()if title_path.exists()else '월드 맵 '+key
  pos=prefix.with_suffix('.pos.txt');entries=[]
  if pos.exists():
   for line in source(pos).read_text().splitlines():
    t=line.split(';')[0].split()
    if len(t)==3 and all(v.isdigit()for v in t)and int(t[0])<400:entries.append(dict(unit=int(t[0]),x=int(t[1]),y=int(t[2])))
  set_path=prefix.with_suffix('.set.txt');known=set_path.exists()and pos.exists()
  if set_path.exists():source(set_path)
  units=[dict(unit=u['unit'],job=u['job'],x=u['x'],y=u['y'],direction=1,name=u['name'])for u in placements.get(key,{}).get('units',[])]
  kind='world'if int(key)>1000 else 'scenario'
  maps.append(dict(id=int(key),key=key,kind=kind,hash=hashlib.sha256(path.read_bytes()).hexdigest(),nativeIndex=len(map_paths)-1,
    introIndex=script(prefix.with_suffix('.deo.txt')),outroIndex=script(prefix.with_suffix('.dee.txt')),eventIndex=script(prefix.with_suffix('.evt.txt')),
    world=dict(id=int(key),ground=-1,title=title,sourceVersion='Saver_Late',nativeKey=key,training=False,
      map=dict(width=m['width'],height=m['height'],attributes=[0 if(t[3]&127)>27 else t[3]&127 for t in m['tiles']],terrain=nav['terrain'],abilities=nav['abilities']),portals=[],npcs=[],decorations=[]),
    rosterSource='original-set-pos'if units else 'original-empty'if known else 'original-missing',units=units,entries=entries))
 graphics=sorted((BASE/'SourceData/Native/map').glob('mdsr*.grs.bytes'),key=lambda p:int(p.name[4:].split('.')[0]));graphic_ids=[]
 for path in graphics:source(path);graphic_ids.append(int(path.name[4:].split('.')[0]))
 pictures=[];picture_ids=[]
 for path in sorted((BASE/'Art/Archives/gameres').glob('**/*r.bmp.png')):
  stem=path.name.split('r')[0]
  if not stem.isdigit():continue
  source(path);target=ART/f'Picture{stem}.png';world.key(Image.open(path)).save(target);output(target,path.relative_to(ROOT).as_posix()+' native sheet; black colorkey');pictures.append(target);picture_ids.append(int(stem))
 faces=[];face_ids=[]
 for path in sorted((BASE/'Art/Archives/gameres/face.arc').glob('*.bmp.png')):
  stem=path.name.split('.')[0]
  if stem.isdigit():faces.append(source(path));face_ids.append(int(stem))
 catalog=DATA/'CampaignCatalog.json';catalog.write_bytes((json.dumps(dict(maps=maps,jobs=[dict(job=j,picture=int(r['직그림']),name=r['이름'].replace('_',' '))for j,r in jobs.items()],connections=[]),ensure_ascii=False,indent=2)+'\n').encode());output(catalog,'Native MRC/SET/POS/scripts; no inferred connections')
 for p in BASE.rglob('*'):
  if(p.is_dir()or p.suffix=='.cs')and not Path(str(p)+'.meta').exists():world.metadata.meta(p,ROOT/'Assets',folder=p.is_dir())
 asset=DATA/'NativeCampaignArt.asset'
 text='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+world.guid(BASE/'Runtime/Campaign/NativeCampaignArt.cs')+', type: 3}\n  m_Name: NativeCampaignArt\n  catalog: '+ref(catalog)+'\n'
 for field,paths,typ in [('maps',map_paths,4900000),('graphics',graphics,4900000),('scripts',scripts,4900000),('pictures',pictures,2800000),('faces',faces,2800000)]:text+='  '+field+':\n'+''.join('  - '+ref(p,typ)+'\n'for p in paths)
 for field,values in [('graphicIds',graphic_ids),('pictureIds',picture_ids),('faceIds',face_ids)]:text+='  '+field+': '+''.join(i.to_bytes(4,'little',signed=True).hex()for i in values)+'\n'
 asset.write_text(text);world.metadata.meta(asset,ROOT/'Assets');Path(str(asset)+'.meta').write_text('fileFormatVersion: 2\nguid: '+world.guid(asset)+'\nNativeFormatImporter:\n  mainObjectFileID: 11400000\n')
 report.update(maps=len(maps),worldMaps=sum(m['kind']=='world'for m in maps),scenarios=sum(m['kind']=='scenario'for m in maps),originalUnits=sum(len(m['units'])for m in maps),sourceMissing=sum(m['rosterSource']=='original-missing'for m in maps),scripts=[p.relative_to(ROOT).as_posix()for p in scripts],asset=asset.relative_to(ROOT).as_posix())
 Path(__file__).with_name('campaign-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:report[k]for k in ['maps','worldMaps','scenarios','originalUnits']}))
if __name__=='__main__':main()
