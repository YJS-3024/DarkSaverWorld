"""Expand recruit icons using original GetCharSurface source crop32,32..64,64."""
from pathlib import Path
import json,re,uuid,io,hashlib,copy
import yaml,UnityPy
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];out=ROOT/'Assets/_Game/Resources/UI/MercenaryInfoIcons';folder=out/'Frames';folder.mkdir(exist_ok=True)
def guid(p):return uuid.uuid5(uuid.NAMESPACE_URL,'darksaver-mercenary-offer/'+str(p.relative_to(ROOT))).hex
if not folder.with_suffix('.meta').exists():folder.with_suffix('.meta').write_text('fileFormatVersion: 2\nguid: '+guid(folder)+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
jobs=next(o.read_typetree() for o in UnityPy.load(str(ROOT/'Assets/_Game/Runtime/Jobs/MercenaryJobCatalog.asset')).objects if o.type.name=='MonoBehaviour')['entries'];template=yaml.safe_load((ROOT/'Assets/_Game/Art/Converted/Mercenaries/Sheet200_Down_1.png.meta').read_text());textures={};proof=[]
for sheet in sorted({j['numericValues'][1] for j in jobs if 200<=j['numericValues'][1]<=222}):
 source=ROOT/f'분석/Saver_1.2/decoded/all_arc/gameres/char0/{sheet}r.bmp';im=Image.open(source).convert('RGBA').crop((32,32,64,64));im.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else 255) for r,g,b,_ in im.getdata()])
 if sheet in [200,207]:p=ROOT/f'Assets/_Game/Art/Converted/Mercenaries/Sheet{sheet}_Down_1.png';assert Image.open(p).convert('RGBA').tobytes()==im.tobytes()
 else:
  p=folder/f'{sheet}_Menu.png';buf=io.BytesIO();im.save(buf,format='PNG')
  if p.exists():assert p.read_bytes()==buf.getvalue(),'Edited icon not overwritten'
  else:p.write_bytes(buf.getvalue())
  m=copy.deepcopy(template);m['guid']=guid(p);ti=m['TextureImporter'];ti['internalIDToNameTable']=[];ti['textureSettings']['filterMode']=0;ti['mipmaps']['enableMipMap']=0;ti['spriteMode']=1;ti['spritePixelsToUnits']=32;ti['spriteMeshType']=0;ti['isReadable']=1;ti['spriteSheet']['sprites']=[];ti['spriteSheet']['nameFileIdTable']={};ti['spriteSheet']['spriteID']=guid(p)
  for platform in ti['platformSettings']:platform['textureCompression']=0
  meta=Path(str(p)+'.meta');content=yaml.safe_dump(m,sort_keys=False,allow_unicode=True)
  if meta.exists():assert meta.read_text()==content,'Edited importer not overwritten'
  else:meta.write_text(content)
 g=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1];textures[sheet]=g;proof.append(dict(sheet=sheet,source=str(source.relative_to(ROOT)),crop=[32,32,32,32],texture=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
entries=[]
for j in sorted(jobs,key=lambda j:j['numericValues'][0]):
 id,sheet=j['numericValues'][:2]
 if sheet in textures:entries.append(f'  - jobId: {id}\n    sheetId: {sheet}\n    icon: {{fileID: 21300000, guid: {textures[sheet]}, type: 3}}')
assert len(entries)==92
script=re.search(r'^guid: (\w+)',(ROOT/'Assets/_Game/Runtime/UI/MercenaryInfoIconCatalog.cs.meta').read_text(),re.M)[1]
header='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+script+', type: 3}\n  m_Name: MercenaryInfoIcons\n  m_EditorClassIdentifier: \n  entries:\n'
catalog=out/'MercenaryInfoIcons.asset';expected=header+'\n'.join(entries)+'\n'
if catalog.exists():
 if catalog.read_bytes().startswith(b'%YAML'):
  assert catalog.read_text()==expected,'Edited recruit icon catalog not overwritten'
 else:
  legacy=next(o.read_typetree() for o in UnityPy.load(str(catalog)).objects if o.type.name=='MonoBehaviour')['entries']
  legacy_ids={j['numericValues'][0] for j in jobs if j['numericValues'][1] in [200,207]}
  assert len(legacy)==8 and {r['jobId'] for r in legacy}==legacy_ids and all(r['sheetId'] in [200,207] for r in legacy),'Unrecognized existing catalog'
catalog.write_text(expected)
Path(__file__).with_name('mercenary_offer_icons.json').write_text(json.dumps(dict(icons=92,sheets=len(textures),frames=proof,actualUnityImport=False),ensure_ascii=False,indent=2)+'\n');print('PASS: 92 recruit icons,23 native sheet crops; original200/207 references preserved; no resized artwork.')
