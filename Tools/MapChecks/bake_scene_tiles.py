"""Save remaining resource-loader maps as editable Unity Tilemaps without running Unity.
Requires Pillow, PyYAML and UnityPy. Binary/already-authored scenes are never replaced
by default. Scene GUIDs, runtime scenes, account data and original resources are kept.
"""
from pathlib import Path
import argparse,copy,hashlib,json,math,re,uuid
from collections import Counter
import UnityPy,yaml
from PIL import Image
from saved_scene_geometry import read_scene_map,geometry_digest
ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/'Assets/_Game/Art/Converted/NonScenarioMaps'
REPORT=ROOT/'Tools/MapChecks/scene_tiles.json'
MAPS=ROOT/'Assets/_Game/Scenes/Maps'
NAMESPACE=uuid.UUID('6f17fdc1-42b0-4bcf-aee4-7ea12c8c8acd')
ALIASES={'KaosiaWeaponShop':'2000','KaosiaArmorShop':'2100','KaosiaAccessoryShop':'2200','KaosiaMagicTools':'2300','KaosiaBank':'2400','KaosiaWarehouse':'2510','KaosiaTemple':'2600'}
HEADER='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def guid(path):return uuid.uuid5(NAMESPACE,str(path.relative_to(ROOT))).hex
def identity():return {f'e{i}{j}':int(i==j) for i in range(4) for j in range(4)}
def ptr(id=0,asset=None):
 d={'fileID':id}
 if asset is not None:d.update(guid=guid(asset),type=3 if asset.suffix=='.png' else 2)
 return d

def write(path,text):
 path.parent.mkdir(parents=True,exist_ok=True)
 if not path.exists() or path.read_text()!=text:path.write_text(text)
def folder(path):
 if path==ROOT or path==ROOT/'Assets':return
 folder(path.parent);path.mkdir(exist_ok=True)
 meta=path.with_name(path.name+'.meta')
 if not meta.exists():write(meta,f'fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n')

class Dumper(getattr(yaml,"CSafeDumper",yaml.SafeDumper)):
 def ignore_aliases(self,data):return True

def represent_dict(dumper,data):
 # Compact vectors/pointers/tile records; large scene diffs stay bounded.
 flow=bool(data) and len(data)<=18 and all(not isinstance(v,(dict,list,tuple)) for v in data.values())
 return dumper.represent_mapping('tag:yaml.org,2002:map',data,flow_style=flow)
Dumper.add_representer(dict,represent_dict)
def dump(data):return yaml.dump(data,Dumper=Dumper,sort_keys=False,allow_unicode=True,width=220)

def serialize(data,node):
 if isinstance(data,bool):return int(data)
 if isinstance(data,bytes):return data.hex()
 if isinstance(data,dict) and 'fileID' in data:return data
 if isinstance(data,dict) and 'm_FileID' in data:
  if data['m_FileID']!=0:raise ValueError('Unresolved external template pointer')
  return ptr(data['m_PathID'])
 if isinstance(data,list):
  arr=node.m_Children[0] if node.m_Children and node.m_Children[0].m_Type=='Array' else node
  element=next((c for c in arr.m_Children if c.m_Name=='data'),None)
  if not data:return []
  if element is None:raise ValueError('Unknown array schema '+node.m_Name)
  return [serialize(item,element) for item in data]
 if isinstance(data,tuple):data=dict(zip(['first','second'],data))
 if isinstance(data,dict):
  result={}
  if node.m_Version>1:result['serializedVersion']=node.m_Version
  fields={c.m_Name:c for c in node.m_Children}
  for key,value in data.items():result[key]=serialize(value,fields[key]) if key in fields else value
  return result
 return data

class Templates:
 def __init__(self):
  self.data={};self.nodes={};self.ids={}
  for file in ['Map_101_;.unity','Map_02_자모라 요새.unity']:
   for obj in UnityPy.load(str(MAPS/file)).objects:
    kind=obj.type.name
    if kind in ['Transform','Grid','Tilemap','TilemapRenderer','TilemapCollider2D','SpriteRenderer'] and kind not in self.data:
     self.data[kind]=obj.read_typetree();self.nodes[kind]=obj.serialized_type.node;self.ids[kind]=obj.class_id
  self.tile=UnityPy.load(str(ROOT/'Assets/_Game/Art/Converted/RestoredMaps/Tiles/Terrain_09.asset')).objects[0]
 def component(self,kind,gameobject,**values):
  data=copy.deepcopy(self.data[kind]);data['m_GameObject']=ptr(gameobject);data.update(values)
  if kind in ['TilemapRenderer','SpriteRenderer']:
   data['m_Materials']=[{'fileID':10754,'guid':'0000000000000000f000000000000000','type':0}]
  return serialize(data,self.nodes[kind])

class Scene:
 def __init__(self,templates):self.templates=templates;self.docs=[];self.nextid=10;self.transforms={}
 def add(self,kind,id,data,classid):self.docs.append((kind,id,data,classid));return data
 def go(self,name,parent=0,position=(0,0),id=None):
  g=self.nextid if id is None else id;self.nextid=max(self.nextid,g+3);t=g+1
  data={'m_ObjectHideFlags':0,'m_CorrespondingSourceObject':ptr(),'m_PrefabInstance':ptr(),'m_PrefabAsset':ptr(),
   'serializedVersion':6,'m_Component':[{'component':ptr(t)}],'m_Layer':0,'m_Name':name,'m_TagString':'Untagged','m_IsActive':1}
  self.add('GameObject',g,data,1)
  tr=self.templates.component('Transform',g,m_Father=ptr(parent),m_Children=[],m_LocalPosition={'x':position[0],'y':position[1],'z':0})
  self.add('Transform',t,tr,4);self.transforms[t]=tr
  if parent:self.transforms[parent]['m_Children'].append(ptr(t))
  return g,t,data
 def comp(self,go,kind,**values):
  g,t,data=go;i=self.nextid;self.nextid+=1;data['m_Component'].append({'component':ptr(i)})
  return self.add(kind,i,self.templates.component(kind,g,**values),self.templates.ids[kind])
 def mono(self,go,script,fields):
  g,t,data=go;i=self.nextid;self.nextid+=1;data['m_Component'].append({'component':ptr(i)})
  self.add('MonoBehaviour',i,{'m_ObjectHideFlags':0,'m_GameObject':ptr(g),'m_Enabled':1,'m_EditorHideFlags':0,
   'm_Script':{'fileID':11500000,'guid':re.search(r'guid: (\w+)',script.with_suffix('.cs.meta').read_text())[1],'type':3},'m_Name':'','m_EditorClassIdentifier':'',**fields},114)
 def tilemap(self,name,parent,cells,w,h,assetrefs,spriterefs,order,collider=False,visible=True):
  go=self.go(name,parent);n=len(cells)
  tm=self.comp(go,'Tilemap',m_Tiles=cells,m_AnimatedTiles=[],m_TileAssetArray=assetrefs,m_TileSpriteArray=spriterefs,
   m_TileMatrixArray=[{'m_RefCount':n,'m_Data':identity()}] if n else [],
   m_TileColorArray=[{'m_RefCount':n,'m_Data':{'r':1,'g':1,'b':1,'a':1}}] if n else [],m_TileObjectToInstantiateArray=[],
   m_Origin={'x':0,'y':0,'z':0},m_Size={'x':w,'y':h,'z':1},m_TileAnchor={'x':.5,'y':.5,'z':0})
  self.comp(go,'TilemapRenderer',m_Enabled=int(visible),m_SortingOrder=order)
  if collider:self.comp(go,'TilemapCollider2D')
  return go
 def sprite(self,name,parent,position,sprite,size,order,enabled=True):
  go=self.go(name,parent,position);self.comp(go,'SpriteRenderer',m_Sprite=sprite,m_Enabled=int(enabled),m_SortingOrder=order,
   m_Size={'x':size[0]/32,'y':size[1]/32},m_WasSpriteAssigned=1,m_LightProbeUsage=0,m_ReflectionProbeUsage=0)
  return go
 def boundary(self,name,parent,position,size):
  go=self.go(name,parent,position);g,t,data=go;i=self.nextid;self.nextid+=1;data['m_Component'].append({'component':ptr(i)})
  base=self.templates.component('TilemapCollider2D',g)
  for k in ['m_MaximumTileChangeCount','m_ExtrusionFactor','m_UseDelaunayMesh']:base.pop(k,None)
  base.update(serializedVersion=3,m_Size={'x':size[0],'y':size[1]},m_EdgeRadius=0,m_AutoTiling=0)
  self.add('BoxCollider2D',i,base,61)
 def text(self,roots,prefab=False):
  out=HEADER+''.join(f'--- !u!{cid} &{id}\n'+dump({kind:data}) for kind,id,data,cid in self.docs)
  if not prefab:out+='--- !u!1660057539 &9223372036854775807\n'+dump({'SceneRoots':{'m_ObjectHideFlags':0,'m_Roots':[ptr(r) for r in roots]}})
  return out

def sprite_meta(path,mode,slices=None):
 data=yaml.safe_load((ROOT/'Assets/_Game/Resources/Scenarios/01/Portraits/NoFace.png.meta').read_text())
 data['guid']=guid(path);ti=data['TextureImporter'];ti['spriteMode']=mode;ti['textureType']=8;ti['spritePixelsToUnits']=32
 ti['isReadable']=0;ti['spriteMeshType']=0;ti['spriteExtrude']=0;ti['spriteGenerateFallbackPhysicsShape']=0;ti['alphaIsTransparency']=1
 ti['textureSettings']['filterMode']=0;ti['nPOTScale']=0;ti['maxTextureSize']=4096
 for platform in ti['platformSettings']:platform['textureCompression']=0;platform['maxTextureSize']=4096
 ti['alignment']=9 if mode==1 else 0;ti['spritePivot']={'x':0,'y':1} if mode==1 else {'x':.5,'y':.5}
 sheet=ti['spriteSheet'];sheet['sprites']=[];sheet['nameFileIdTable']={};sheet['spriteID']=uuid.uuid5(NAMESPACE,str(path.relative_to(ROOT))).hex
 ti['internalIDToNameTable']=[]
 for name,id,x,y in slices or []:
  sample=copy.deepcopy(yaml.safe_load((ROOT/'Assets/_Game/Resources/Scenarios/01/Portraits/NoFace.png.meta').read_text())['TextureImporter']['spriteSheet']['sprites'][0])
  sample.update(name=name,rect={'serializedVersion':2,'x':x,'y':y,'width':32,'height':32},alignment=0,pivot={'x':.5,'y':.5},spriteID=uuid.uuid5(NAMESPACE,name).hex,internalID=id)
  sheet['sprites'].append(sample);sheet['nameFileIdTable'][name]=id;ti['internalIDToNameTable'].append({'first':{213:id},'second':name})
 return dump(data)

def tile_asset(path,sprite,collider):
 folder(path.parent)
 d={'MonoBehaviour':{'m_ObjectHideFlags':0,'m_CorrespondingSourceObject':ptr(),'m_PrefabInstance':ptr(),'m_PrefabAsset':ptr(),'m_GameObject':ptr(),
  'm_Enabled':1,'m_EditorHideFlags':0,'m_Script':{'fileID':13312,'guid':'0000000000000000e000000000000000','type':0},
  'm_Name':path.stem,'m_EditorClassIdentifier':'UnityEngine.dll::UnityEngine.Tilemaps.Tile','m_Sprite':sprite,
  'm_Color':{'r':1,'g':1,'b':1,'a':1},'m_Transform':identity(),'m_InstancedGameObject':ptr(),'m_Flags':1,'m_ColliderType':collider}}
 write(path,HEADER+'--- !u!114 &11400000\n'+dump(d));write(path.with_suffix('.asset.meta'),f'fileFormatVersion: 2\nguid: {guid(path)}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n')

def inventory():
 previous_path=REPORT if REPORT.exists() else ROOT/'Tools/MapChecks/non_scenario_scene_tiles.json'
 previous=json.loads(previous_path.read_text()) if previous_path.exists() else {};rows={}
 for row in previous.get('maps',[]):rows[row['scene']]=row
 for f in sorted(MAPS.glob('*.unity')):
  raw=f.read_bytes()
  if not raw.startswith(b'%YAML'):continue
  s=raw.decode();m=re.search(r'(townKey|shopKey): (\S+)',s)
  if not m:continue
  key=m[2]
  rr='Scenarios/'+key[8:]+'/Map' if key.startswith('Scenario') else 'Interiors/'+(key[8:] if key.startswith('Interior') else ALIASES[key]) if key.startswith('Interior') or key in ALIASES else ('Fields/' if key.startswith('WorldField') else 'Towns/')+key
  row=rows.get(str(f.relative_to(ROOT)),{})
  row.update(scene=str(f.relative_to(ROOT)),key=key,resourceRoot=rr,loaderField=m[1])
  rows[str(f.relative_to(ROOT))]=row
 return sorted(rows.values(),key=lambda r:r['scene'])

def tile_records(indices,w,h,assets,sprites,terrain=False):
 order=list(dict.fromkeys(indices));order=[k for k in order if k is not None];lookup={k:i for i,k in enumerate(order)};counts=Counter(indices)
 a=[{'m_RefCount':counts[k],'m_Data':ptr(11400000,assets[k])} for k in order]
 s=[] if terrain else [{'m_RefCount':counts[k],'m_Data':sprites[k]} for k in order]
 cells=[]
 for offset,key in enumerate(indices):
  if key is None:continue
  i=lookup[key]
  cells.append(({'x':offset%w,'y':h-1-offset//w,'z':0},{'m_TileIndex':i,'m_TileSpriteIndex':65535 if terrain else i,'m_TileMatrixIndex':0,
   'm_TileColorIndex':0,'m_TileObjectToInstantiateIndex':65535,'dummyAlignment':0,'m_AllTileFlags':1}))
 return cells,a,s

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--rebuild',action='store_true',help='Only replace generated scenes whose hash still matches the last manifest.');args=parser.parse_args()
 rows=inventory();assert rows
 before={str(f.relative_to(ROOT)):sha(f) for f in MAPS.glob('*.unity')}
 floors={};objects={};cache={}
 for row in rows:
  rr=ROOT/'Assets/_Game/Resources'/row['resourceRoot'];d=read_scene_map(rr);im=Image.open(rr/'Ground.png').convert('RGBA');w,h=d['width'],d['height']
  assert im.size==(w*32,h*32) and len(d['attributes'])==w*h
  if row['resourceRoot'] not in cache:
   keys=[]
   for y in range(h):
    for x in range(w):
     tile=im.crop((x*32,y*32,x*32+32,y*32+32));key=hashlib.sha256(tile.tobytes()).hexdigest();floors.setdefault(key,tile);keys.append(key)
   cache[row['resourceRoot']]=keys
  row['floorHashes']=cache[row['resourceRoot']];row['data']=d
  for o in d['objects']:
   path=rr/(o['texture']+'.png');objects.setdefault((sha(path),path.name),path)
 templates=Templates();folder(ASSETS);folder(ASSETS/'Floors');folder(ASSETS/'Objects');atlas=ASSETS/'Floors/NativeFloors.png'
 columns=64;height=2**math.ceil(math.log2(max(32,math.ceil(len(floors)/columns)*32)))
 canvas=Image.new('RGBA',(columns*32,height),(0,0,0,0));slices=[];floorassets={};floorrefs={}
 for i,key in enumerate(sorted(floors)):
  x=i%columns*32;y=i//columns*32;canvas.alpha_composite(floors[key],(x,height-y-32));id=int(key[:15],16)+21300001
  name='Floor_'+key[:20];slices.append((name,id,x,y));path=ASSETS/'Floors'/(name+'.asset')
  floorassets[key]=path;floorrefs[key]=ptr(id,atlas);tile_asset(path,floorrefs[key],0)
 canvas.save(atlas);write(atlas.with_suffix('.png.meta'),sprite_meta(atlas,2,slices))
 objectassets={};treeparts={}
 for (hash,name),source in sorted(objects.items()):
  path=ASSETS/'Objects'/hash[:20]/name;folder(path.parent)
  if not path.exists():path.write_bytes(source.read_bytes())
  write(path.with_suffix('.png.meta'),sprite_meta(path,1));objectassets[(hash,name)]=path
  im=Image.open(source).convert('RGBA')
  if im.size==(64,64) and source.stem in ['1_8_0','1_11_0']:
   parts=[]
   for suffix,box in [('bottom',(0,32,64,64)),('canopy',(0,0,64,32))]:
    part=path.with_name(path.stem+'_'+suffix+'.png');im.crop(box).save(part);write(part.with_suffix('.png.meta'),sprite_meta(part,1));parts.append(part)
   treeparts[(hash,name)]=parts
 manifest=[];updated=0
 for row in rows:
  scene=ROOT/row['scene'];raw=scene.read_bytes();s=raw.decode() if raw.startswith(b'%YAML') else ''
  placeholder='mapTilesBaked: 1' not in s and s.count('\nGameObject:')==1 and 'Grid:' not in s
  generated='mapTilesBaked: 1' in s
  if not placeholder:
   if not args.rebuild:manifest.append({k:v for k,v in row.items() if k not in ['data','floorHashes']});continue
   if not row.get('sceneSha256') or sha(scene)!=row['sceneSha256']:raise ValueError('Preserve edited scene; refusing rebuild: '+row['scene'])
  d=row['data'];w,h=d['width'],d['height'];rr=ROOT/'Assets/_Game/Resources'/row['resourceRoot'];sc=Scene(templates)
  root=sc.go(row['key']+' native editable map',id=1)
  script=ROOT/'Assets/_Game/Runtime/World'/('ShopInteriorScene.cs' if row['loaderField']=='shopKey' else 'NativeTownScene.cs')
  sc.mono(root,script,{row['loaderField']:row['key'],'runtimeOnly':0,'mapTilesBaked':1})
  grid=sc.go('Map Grid',root[1]);sc.comp(grid,'Grid',m_CellSize={'x':1,'y':1,'z':0},m_CellGap={'x':0,'y':0,'z':0})
  native=row['resourceRoot'].split('/')[-1].replace('WorldField','')
  towns=dict(re.findall(r'new TownDefinition\("([^\"]+)","[^\"]+","[^\"]+","[^\"]+","([^\"]+)"', (ROOT/'Assets/_Game/Runtime/Core/TownCatalog.cs').read_text()))
  native=towns.get(row['key'],native)
  if row['key'].startswith('Scenario'):native=row['key'][8:]
  titles={}
  for catalog in ['TownCatalog.cs','NativeInteriorCatalog.cs']:
   titles.update(dict(re.findall(r'new TownDefinition\("([^\"]+)","([^\"]+)"',(ROOT/'Assets/_Game/Runtime/Core'/catalog).read_text())))
  titles.update({'WorldField'+i:t for i,t in re.findall(r'Field\("(\d+)","([^\"]+)"',(ROOT/'Assets/_Game/Runtime/Core/FieldCatalog.cs').read_text())})
  titles.update(dict(re.findall(r'new KaosiaFacilityDefinition\("([^\"]+)","([^\"]+)"',(ROOT/'Assets/_Game/Runtime/Core/KaosiaFacilityCatalog.cs').read_text())))
  titles.update(dict(re.findall(r'new ShopInteriorDefinition\("([^\"]+)","([^\"]+)"',(ROOT/'Assets/_Game/Runtime/Core/ShopInteriorCatalog.cs').read_text())))
  titles.update({'Scenario'+k:t for k,t in re.findall(r'new ScenarioDefinition\(\"([^\"]+)\",\"([^\"]+)\"',(ROOT/'Assets/_Game/Runtime/Core/ScenarioCatalog.cs').read_text())})
  sc.mono(grid,ROOT/'Assets/_Game/Runtime/Maps/MapSceneDefinition.cs',{'mapKey':native,'displayName':titles.get(row['key'],row['key']),'summary':'원본 변환 지형·장식 / 편집 가능한 고정 Tilemap'})
  cells,a,spr=tile_records(row['floorHashes'],w,h,floorassets,floorrefs);sc.tilemap('Ground',grid[1],cells,w,h,a,spr,0)
  sc.tilemap('Detail',grid[1],[],w,h,[],[],10)
  attrassets={};isfield=row['resourceRoot'].startswith(('Fields/','Scenarios/'))
  for attr in set(d['attributes'])-{0}:
   path=ASSETS/'Terrain'/('Field' if isfield else 'Interior')/f'Terrain_{attr:02d}.asset'
   tile_asset(path,ptr(),2 if attr==9 or isfield and attr in [3,6] else 0);attrassets[attr]=path
  attrs=[a if a else None for a in d['attributes']];cells,a,spr=tile_records(attrs,w,h,attrassets,{},True)
  sc.tilemap('TerrainAttributes',grid[1],cells,w,h,a,spr,0,collider=True,visible=False)
  obroot=sc.go('Objects',grid[1]);trees=0
  for o in d['objects']:
   source=rr/(o['texture']+'.png');key=(sha(source),source.name);asset=objectassets[key];size=Image.open(source).size
   x,y=o['x'],h-o['y'];index=o['index'];order=10+index if size[1]<=32 else max(-32768,min(32767,20000-round((y-size[1]/32)*10)))
   tree=key in treeparts
   go=sc.sprite('Object_'+str(index).zfill(3),obroot[1],(x,y),ptr(21300000,asset),size,order,not tree)
   if tree:
    trees+=1;sc.mono(go,ROOT/'Assets/_Game/Runtime/World/MapTreeSlices.cs',{})
    sc.sprite('Tree bottom row',go[1],(0,-1),ptr(21300000,treeparts[key][0]),(64,32),10+index)
    sc.sprite('Tree canopy row',go[1],(0,0),ptr(21300000,treeparts[key][1]),(64,32),30000+index)
  for name,pos,size in [('West map limit',(-.5,h*.5),(1,h+2)),('East map limit',(w+.5,h*.5),(1,h+2)),('South map limit',(w*.5,-.5),(w+2,1)),('North map limit',(w*.5,h+.5),(w+2,1))]:sc.boundary(name,grid[1],pos,size)
  original=sha(scene);write(scene,sc.text([root[1]]));updated+=1
  manifest.append(dict(scene=row['scene'],key=row['key'],resourceRoot=row['resourceRoot'],loaderField=row['loaderField'],width=w,height=h,groundCells=w*h,terrainCells=sum(a!=0 for a in d['attributes']),objects=len(d['objects']),splitTrees=trees,
   originalPlaceholderSha256=row.get('originalPlaceholderSha256',original),sceneSha256=sha(scene),geometrySha256=geometry_digest(d,rr),groundImageSha256=sha(rr/'Ground.png')))
 # A paintable shared palette; native resources/metadata remain untouched.
 palette=Scene(templates);root=palette.go('Native non-scenario floor palette',id=1);palette.comp(root,'Grid',m_CellSize={'x':1,'y':1,'z':0},m_CellGap={'x':0,'y':0,'z':0})
 keys=sorted(floors);pw=columns;ph=math.ceil(len(keys)/pw);values=keys+[None]*(pw*ph-len(keys));cells,a,spr=tile_records(values,pw,ph,floorassets,floorrefs)
 palette.tilemap('Ground',root[1],cells,pw,ph,a,spr,0)
 palette.add('MonoBehaviour',11400000,{'m_ObjectHideFlags':0,'m_GameObject':ptr(),'m_Enabled':1,'m_EditorHideFlags':0,'m_Script':{'fileID':12395,'guid':'0000000000000000e000000000000000','type':0},'m_Name':'Palette Settings','m_EditorClassIdentifier':'','cellSizing':0,'m_TransparencySortMode':0,'m_TransparencySortAxis':{'x':0,'y':0,'z':1}},114)
 pp=ASSETS/'NativeFloors.prefab';write(pp,palette.text([root[1]],True));write(pp.with_suffix('.prefab.meta'),f'fileFormatVersion: 2\nguid: {guid(pp)}\nPrefabImporter:\n  externalObjects: {{}}\n')
 selected={row['scene'] for row in rows};preserved={k:v for k,v in before.items() if k not in selected};assert all(sha(ROOT/k)==v for k,v in preserved.items())
 report=dict(version=1,maps=manifest,mapCount=len(manifest),groundCells=sum(r['groundCells'] for r in manifest),objects=sum(r['objects'] for r in manifest),uniqueFloorTiles=len(floors),uniqueObjectImages=len(objects),palette=str(pp.relative_to(ROOT)),atlas=str(atlas.relative_to(ROOT)),preservedMapSceneSha256=preserved,scope='All resource-loader maps are saved scene Tilemaps. Geometry comes from scenes; prior input parity is recorded as digests, not duplicate runtime geometry files.')
 write(REPORT,json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(f'Baked {updated} scenes; {len(floors)} floor tiles; {len(objects)} object images. NOT Unity import/Play.')
if __name__=='__main__':main()
