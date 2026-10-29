"""Read-only audit of saved Unity YAML tiles against converted resources.
This validates serialized data/references/pixels, NOT Unity import, physics or Play.
"""
from pathlib import Path
import hashlib,json,re,sys
from collections import Counter
import yaml
from PIL import Image,ImageChops
from bake_scene_tiles import ROOT,REPORT,ASSETS,sha
from saved_scene_geometry import read_scene_map,geometry_digest
LOADER=getattr(yaml,'CSafeLoader',yaml.SafeLoader)
def documents(path):
 text=path.read_text();parts=re.split(r'^--- !u!(\d+) &(-?\d+)\s*\n',text,flags=re.M);result={}
 for i in range(1,len(parts),3):
  id=int(parts[i+1]);assert id not in result,(path,id)
  d=yaml.load(parts[i+2],Loader=LOADER);kind=next(iter(d));result[id]=(kind,d[kind])
 return result
checks=0
def check(ok,name):
 global checks
 if not ok:raise AssertionError(name)
 checks+=1

def main():
 report=json.loads(REPORT.read_text());atlas=ROOT/report['atlas'];image=Image.open(atlas).convert('RGBA')
 meta=yaml.load(atlas.with_suffix('.png.meta').read_text(),Loader=LOADER);ti=meta['TextureImporter'];sprites={}
 check(ti['spriteMode']==2 and ti['spritePixelsToUnits']==32 and ti['spriteMeshType']==0,'atlas full rectangles/32 PPU')
 for sp in ti['spriteSheet']['sprites']:
  r=sp['rect'];x,y,w,h=[r[k] for k in ['x','y','width','height']]
  check(w==32 and h==32 and sp['internalID'] not in sprites,'unique 32px floor sprite IDs')
  sprites[sp['internalID']]=image.crop((x,image.height-y-h,x+w,image.height-y))
 assets={};textures={};attributes={}
 for path in sorted(ASSETS.rglob('*.asset')):
  g=yaml.load(path.with_suffix('.asset.meta').read_text(),Loader=LOADER)['guid'];kind,d=documents(path)[11400000]
  check(kind=='MonoBehaviour' and d['m_Script']['fileID']==13312,'native Tile asset type '+path.name)
  assets[g]=d
 for path in sorted((ASSETS/'Objects').rglob('*.png')):
  m=yaml.load(path.with_suffix('.png.meta').read_text(),Loader=LOADER);ti2=m['TextureImporter'];textures[m['guid']]=path
  check(ti2['spriteMode']==1 and ti2['spritePixelsToUnits']==32 and ti2['spriteMeshType']==0 and ti2['spritePivot']=={'x':0,'y':1},'object pivot/32 PPU/full rectangle '+path.name)
 rows=[];totalground=totalobjects=0
 towns=dict(re.findall(r'new TownDefinition\("([^\"]+)","[^\"]+","[^\"]+","[^\"]+","([^\"]+)"',(ROOT/'Assets/_Game/Runtime/Core/TownCatalog.cs').read_text()))
 for row in report['maps']:
  path=ROOT/row['scene'];d=documents(path);resource=ROOT/'Assets/_Game/Resources'/row['resourceRoot'];source=read_scene_map(resource);w,h=source['width'],source['height'];canvas=Image.new('RGBA',(w*32,h*32))
  byname={body['m_Name']:(id,body) for id,(kind,body) in d.items() if kind=='GameObject'}
  def component(go,kind):
   return next(body for _,(type,body) in d.items() if type==kind and body.get('m_GameObject',{}).get('fileID')==go)
  def transform(go):return component(go,'Transform')
  layers={}
  for name in ['Ground','Detail','TerrainAttributes']:
   go=byname[name][0];layers[name]=component(go,'Tilemap')
  baked=[b for k,b in d.values() if k=='MonoBehaviour' and b.get('mapTilesBaked')==1]
  check(len(baked)==1 and baked[0][row['loaderField']]==row['key'] and baked[0]['runtimeOnly']==0,'saved loader '+row['key'])
  native=towns.get(row['key'],row['resourceRoot'].split('/')[-1].replace('WorldField',''))
  if row['key'].startswith('Scenario'):native=row['key'][8:]
  defs=[b for k,b in d.values() if k=='MonoBehaviour' and 'mapKey' in b]
  check(len(defs)==1 and str(defs[0]['mapKey'])==native,'original region metadata '+row['key'])
  check(len(layers['Ground']['m_Tiles'])==w*h,'ground full extent '+row['key'])
  check(component(byname['Ground'][0],'TilemapRenderer')['m_Enabled']==1,'visible editable ground '+row['key'])
  check(component(byname['TerrainAttributes'][0],'TilemapRenderer')['m_Enabled']==0,'hidden terrain '+row['key'])
  check(component(byname['TerrainAttributes'][0],'TilemapCollider2D')['m_Enabled']==1,'terrain collider '+row['key'])
  seen=set();refcounts=Counter()
  for entry in layers['Ground']['m_Tiles']:
   pos,t=entry['first'],entry['second'];x,y=pos['x'],pos['y'];check((x,y) not in seen and 0<=x<w and 0<=y<h,'unique ground cell');seen.add((x,y))
   td=assets[layers['Ground']['m_TileAssetArray'][t['m_TileIndex']]['m_Data']['guid']];sp=td['m_Sprite']
   check(td['m_ColliderType']==0 and sp['guid']==meta['guid'] and sp['fileID'] in sprites,'ground asset/sprite reference')
   check(layers['Ground']['m_TileSpriteArray'][t['m_TileSpriteIndex']]['m_Data']==sp,'tilemap sprite cache matches Tile asset')
   canvas.paste(sprites[sp['fileID']],(x*32,(h-1-y)*32));refcounts[t['m_TileIndex']]+=1
  check(all(a['m_RefCount']==refcounts[i] for i,a in enumerate(layers['Ground']['m_TileAssetArray'])),'ground reference counts '+row['key'])
  original=Image.open(resource/'Ground.png').convert('RGBA');check(canvas.tobytes()==original.tobytes(),'all original ground RGBA pixels '+row['key'])
  terrain={};field=row['resourceRoot'].startswith(('Fields/','Scenarios/'))
  for entry in layers['TerrainAttributes']['m_Tiles']:
   pos,t=entry['first'],entry['second'];x,y=pos['x'],pos['y'];td=assets[layers['TerrainAttributes']['m_TileAssetArray'][t['m_TileIndex']]['m_Data']['guid']]
   attr=int(td['m_Name'].split('_')[-1]);terrain[(x,y)]=attr
   check(td['m_ColliderType']==(2 if attr==9 or field and attr in [3,6] else 0),'native terrain blocking '+row['key'])
  expected={(i%w,h-1-i//w):a for i,a in enumerate(source['attributes']) if a}
  check(terrain==expected,'every original terrain attribute '+row['key'])
  trees=0
  for obj in source['objects']:
   go=byname['Object_'+str(obj['index']).zfill(3)][0];tr=transform(go);sr=component(go,'SpriteRenderer');tex=textures[sr['m_Sprite']['guid']];im=Image.open(tex).convert('RGBA');original=resource/(obj['texture']+'.png')
   check(sha(tex)==sha(original),'original object PNG bytes '+row['key'])
   check(tr['m_LocalPosition']=={'x':obj['x'],'y':h-obj['y'],'z':0},'original object coordinates '+row['key'])
   tree=im.size==(64,64) and original.stem in ['1_8_0','1_11_0']
   if tree:
    trees+=1;check(sr['m_Enabled']==0,'split tree source hidden')
    parts={d[p['fileID']][1]['m_GameObject']['fileID']:d[p['fileID']][1] for p in tr['m_Children']}
    check(len(parts)==2,'exactly two saved tree rows')
    joined=Image.new('RGBA',(64,64))
    for child,ct in parts.items():
     name=d[child][1]['m_Name'];cr=component(child,'SpriteRenderer');lower=name=='Tree bottom row';check(name in ['Tree bottom row','Tree canopy row'],'tree row name')
     check(ct['m_LocalPosition']=={'x':0,'y':-1 if lower else 0,'z':0},'tree row placement')
     check(cr['m_SortingOrder']==(10 if lower else 30000)+obj['index'],'tree row depth')
     joined.paste(Image.open(textures[cr['m_Sprite']['guid']]).convert('RGBA'),(0,32 if lower else 0))
    check(joined.tobytes()==im.tobytes(),'saved tree rows preserve original RGBA')
   else:check(sr['m_Enabled']==1,'original object visible')
  # All local references resolve within the saved scene.
  def references(v):
   if isinstance(v,dict):
    if 'fileID' in v and v['fileID'] and 'guid' not in v:check(v['fileID'] in d,'local scene reference')
    for a in v.values():references(a)
   elif isinstance(v,list):
    for a in v:references(a)
  for _,body in d.values():references(body)
  check(len([b for k,b in d.values() if k=='BoxCollider2D'])==4,'four preserved map boundary colliders')
  check(geometry_digest(source,resource)==row['geometrySha256'],'parity with retired original geometry input')
  check(sha(resource/'Ground.png')==row['groundImageSha256'],'original ground image untouched')
  totalground+=w*h;totalobjects+=len(source['objects']);rows.append(dict(scene=row['scene'],groundCells=w*h,objects=len(source['objects']),splitTrees=trees))
 for path,hash in report['preservedMapSceneSha256'].items():check(sha(ROOT/path)==hash,'existing/scenario scene untouched '+path)
 palette=documents(ROOT/report['palette']);pl=[b for k,b in palette.values() if k=='Tilemap'];check(len(pl)==1 and len(pl[0]['m_Tiles'])==report['uniqueFloorTiles'],'paint palette contains all floor tiles')
 result=dict(checks=checks,maps=len(rows),groundCells=totalground,objects=totalobjects,preservedMapScenes=len(report['preservedMapSceneSha256']),results=rows,unityImport=False,unityPlay=False,scope=__doc__.strip())
 (ROOT/'Tools/MapChecks/scene_tiles_check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(f'PASS {checks} serialized/pixel checks, {len(rows)} maps, {totalground} ground cells, {totalobjects} objects. NOT Unity import/Play.')
if __name__=='__main__':main()
