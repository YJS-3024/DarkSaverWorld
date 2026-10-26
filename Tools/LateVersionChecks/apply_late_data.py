"""Apply verified later-installation data to existing Unity assets, retaining GUIDs.
Usage: PYTHONPATH=<UnityPy> python Tools/LateVersionChecks/apply_late_data.py <unshield-root>
Requires UnityPy, Pillow, PyYAML; never runs Windows binaries or edits saves.
"""
import copy,csv,hashlib,importlib.util,io,json,re,struct,sys,uuid
from collections import Counter
from decimal import Decimal
from pathlib import Path
import UnityPy,yaml
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from build_late_saver_catalog import arc_entries
from analyze_client import lzh_decode,lzss_decode
SOURCE=Path(sys.argv[1])/'Saver_Files/saver'
ANALYSIS=ROOT/'분석/Saver_Late'
TABLES=json.loads((ANALYSIS/'tables.json').read_text())
# Preflight all installed source hashes before changing any Unity asset.
installed={str(p.relative_to(SOURCE)).lower():p for p in SOURCE.rglob('*') if p.is_file()}
for record in json.loads((ANALYSIS/'files.json').read_text()):
 assert record['path'] in installed,record['path']
 assert hashlib.sha256(installed[record['path']].read_bytes()).hexdigest()==record['sha256'],record['path']
OUT=ANALYSIS/'unity_catalogs';OUT.mkdir(exist_ok=True)
NAMESPACE=uuid.UUID('23b3f21a-85ac-47cb-a63f-c304722b82c8')
report={'sourceSha256':json.loads((ANALYSIS/'summary.json').read_text())['sourceSha256'],'catalogs':{},'artwork':[],'unavailableArtwork':[]}
def csvwrite(name,header,rows):
 with (OUT/(name+'.csv')).open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.writer(f);w.writerow(header);w.writerows(rows)
def header(name):
 with (ROOT/'분석/Saver_1.2/catalogs'/f'{name}.csv').open(encoding='utf-8-sig') as f:return next(csv.reader(f))
def update(path,entries):
 p=ROOT/'Assets/_Game/Runtime'/path;env=UnityPy.load(str(p));obj=next(x for x in env.objects if x.type.name=='MonoBehaviour');data=obj.read_typetree()
 before={k:v for k,v in data.items() if k!='entries'};data['entries']=entries;obj.save_typetree(data);binary=env.file.save();check=UnityPy.load(binary);actual=next(x for x in check.objects if x.path_id==obj.path_id).read_typetree()
 assert actual['entries']==entries and {k:v for k,v in actual.items() if k!='entries'}==before
 p.write_bytes(binary);report['catalogs'][path]=len(entries)
units=[r['values'][:36]+[' '.join(r['values'][36:])] for r in TABLES['ability.atr']['rows']]
names={int(r[0]):r[35] for r in units}
category=lambda n:'player_job' if n<200 else 'mercenary' if n<300 else 'monster' if n<800 else 'npc_or_special'
csvwrite('units_all',header('units_all'),[r+[category(int(r[0]))] for r in units])
for file,label,asset in [('jobs','player_job','JobCatalog'),('mercenaries','mercenary','MercenaryJobCatalog'),('monsters','monster','MonsterJobCatalog'),('npcs_and_special','npc_or_special','NpcSpecialJobCatalog')]:
 rows=[r for r in units if category(int(r[0]))==label];assert all(len(r)==37 for r in rows)
 csvwrite(file,header(file),[r+[label] for r in rows])
 update(f'Jobs/{asset}.asset',[dict(numericValues=list(map(int,r[:34])),horizontalTransitionText=r[34],jobName=r[35],description=r[36],categoryLabel=label) for r in sorted(rows,key=lambda r:int(r[0]))])
slots={0:'special',1:'ranged_or_staff',2:'weapon',3:'armor',4:'boots',5:'headgear',6:'accessory_or_manual',7:'consumable',8:'shield',9:'quest_or_misc',10:'travel_or_special_consumable'}
items=[r['values'][:43]+[' '.join(r['values'][43:])] for r in TABLES['itemtbl.atr']['rows']];assert all(len(r)==44 for r in items)
csvwrite('items',header('items'),[r+[slots[int(r[3])]] for r in items])
def integer(value):
 n=Decimal(value);assert n==int(n) and -(2**63)<=int(n)<2**63;return int(n)
update('Items/ItemCatalog.asset',[dict(numericValues=[integer(x) for x in r[:41]],priceNotation=r[16],damageTargets=r[41],itemName=r[42],description=r[43],categoryLabel=slots[int(r[3])]) for r in sorted(items,key=lambda r:int(r[0]))])
magic=[r['values'] for r in TABLES['magictbl.atr']['rows']];assert all(len(r)==17 for r in magic)
magic_header=(ANALYSIS/'decoded/gameres/set.arc/magictbl.atr').read_text().splitlines()[0].lstrip(';').split()
csvwrite('Magictbl',magic_header,magic)
update('Magic/MagicCatalog.asset',[dict(numericValues=list(map(int,[r[0]]+r[2:])),magicName=r[1]) for r in sorted(magic,key=lambda r:int(r[0]))])
levels=[]
for row in TABLES['levelabl.atr']['rows']:
 r=row['values'];assert len(r) in (17,18)
 levels.append(dict(jobId=int(r[0]),jobName=names.get(int(r[0]),''),level=int(r[1]),requiredXp=int(r[15]),nextJobId=int(r[12]),rewardGold=int(r[13]),rewardXp=int(r[14]),learnedMagic=r[16],usableSkills=r[17] if len(r)>17 else ''))
levels.sort(key=lambda r:(r['jobId'],r['level']))
csvwrite('experience_progression',header('experience_progression'),[[r[k] for k in header('experience_progression')] for r in levels]);update('Combat/ExperienceProgression.asset',levels)
# Independent editor item definitions must match the live catalog too.
item_rows=[dict(numericValues=[integer(x) for x in r[:41]],priceNotation=r[16],damageTargets=r[41],itemName=r[42],description=r[43],categoryLabel=slots[int(r[3])]) for r in items]
changed_definitions=0
for row in item_rows:
 path=ROOT/'Assets/_Game/Data/Items'/('Item_'+str(row['numericValues'][0]).zfill(4)+'.asset')
 template=path if path.exists() else ROOT/'Assets/_Game/Data/Items/Item_0000.asset'
 env=UnityPy.load(str(template));obj=next(o for o in env.objects if o.type.name=='MonoBehaviour');tree=obj.read_typetree()
 if path.exists() and tree['data']==row:continue
 tree['data']=row;tree['m_Name']=path.stem;obj.save_typetree(tree);binary=env.file.save()
 assert next(o for o in UnityPy.load(binary).objects if o.type.name=='MonoBehaviour').read_typetree()['data']==row
 path.write_bytes(binary);changed_definitions+=1
 meta=path.with_name(path.name+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(NAMESPACE,str(path.relative_to(ROOT))).hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
report['individualItemDefinitions']=len(item_rows)
report['lastRunChangedDefinitions']=changed_definitions

# Refresh the shared item atlas; its imported GUID and cell IDs remain stable.
for name,method,size,packed in arc_entries(SOURCE/'gameres/item.arc'):
 if name.lower()!='item.bmp':continue
 raw=(lzh_decode if method==0 else lzss_decode)(packed,size);assert len(raw)==size
 sourcepath=ANALYSIS/'decoded_art/gameres/item.arc/item.bmp';sourcepath.parent.mkdir(parents=True,exist_ok=True);sourcepath.write_bytes(raw)
 offset=struct.unpack_from('<I',raw,10)[0];palette_count=(offset-54)//4
 assert max(raw[offset:offset+3200*320])<palette_count
 image=Image.open(io.BytesIO(raw)).convert('RGBA');assert image.size==(3200,320)
 image.putdata([(*p[:3],0 if p[:3]==(0,0,0) else 255) for p in image.getdata()]);image.save(ROOT/'Assets/_Game/Resources/UI/ItemIcons.png')
 report['itemAtlas']=dict(width=3200,height=320,paletteEntries=palette_count,pixelSha256=hashlib.sha256(image.tobytes()).hexdigest())

# Native class sheets and relation colors. Metadata GUIDs of existing images stay intact.
sheets={}
for p in sorted((SOURCE/'gameres').glob('char*.arc')):
 for name,method,size,packed in arc_entries(p):
  if re.fullmatch(r'\d+r\.bmp',name,re.I):
   sheet=int(name[:-5]);sheets[sheet]=(p,name,method,size,packed)
folder=ROOT/'Assets/_Game/Art/Converted/Classes'
for r in units:
 if category(int(r[0]))!='player_job':continue
 job,sheet=int(r[0]),int(r[1]);arc,name,method,size,packed=sheets[sheet]
 raw=(lzh_decode if method==0 else lzss_decode)(packed,size);assert len(raw)==size
 native=Image.open(io.BytesIO(raw));assert native.mode=='P' and native.size in [(96,128),(96,192)]
 sourcepath=ANALYSIS/'decoded_art'/arc.relative_to(SOURCE)/name.lower();sourcepath.parent.mkdir(parents=True,exist_ok=True);sourcepath.write_bytes(raw)
 palette=native.getpalette();width,height=native.size
 for color in [0,2,3]:
  indices=[v+16*color if 0xA0<=v<=0xAF else v for v in native.getdata()];rgba=[]
  if any(3*v+2>=len(palette) for v in indices):
   report['unavailableArtwork'].append(dict(job=job,color=color,reason='Native palette lacks remapped color entries'));continue
  for v in indices:
   rgb=tuple(palette[3*v:3*v+3]);assert len(rgb)==3,(job,color,v,len(palette),name);rgba.append((*rgb,0 if rgb==(0,0,0) else 255))
  image=Image.new('RGBA',native.size);image.putdata(rgba)
  dest=folder/(f'Class{sheet}'+(f'_Color{color}' if color else '')+'.png');changed=not dest.exists() or Image.open(dest).convert('RGBA').tobytes()!=image.tobytes()
  if changed:image.save(dest)
  meta=dest.with_name(dest.name+'.meta')
  if not meta.exists():
   template=yaml.safe_load((folder/'Class169.png.meta').read_text());template['guid']=uuid.uuid5(NAMESPACE,str(dest.relative_to(ROOT))).hex
   imp=template['TextureImporter'];imp['textureType']=0;imp['spriteMode']=0;imp['internalIDToNameTable']=[];imp['spriteSheet']['sprites']=[]
   meta.write_text(yaml.safe_dump(template,sort_keys=False))
  assert Image.open(dest).convert('RGBA').tobytes()==image.tobytes()
  report['artwork'].append(dict(job=job,color=color,changed=changed,width=width,height=height,pixelSha256=hashlib.sha256(image.tobytes()).hexdigest()))
def fix_native_uv(file, entries, icon=False):
 # Unity-created source sprites serialize a zero UV stream. Build the new UVs
 # from actual local vertex positions and source rectangles, never those zeros.
 for row in entries:
  if row['jobId']<170:continue
  refs=[row['icon']] if icon else row['frames']
  for ref in refs:
   obj=file.objects[ref['m_PathID']];tree=obj.read_typetree();rd=tree['m_RD'];buf=bytearray(rd['m_VertexData']['m_DataSize']);rect=tree['m_Rect'];pivot=tree['m_Pivot'];ppu=tree['m_PixelsToUnits']
   for i in range(4):
    x,y,_=struct.unpack_from('<3f',buf,i*12)
    struct.pack_into('<2f',buf,48+i*8,(x*ppu+rect['width']*pivot['x']+rect['x'])/96,(y*ppu+rect['height']*pivot['y']+rect['y'])/192)
   rd['m_VertexData']['m_DataSize']=bytes(buf);obj.save_typetree(tree)

# Add three native Sprite sets to existing catalogs without replacing authored references.
for color in [0,2,3]:
 suffix=f'_Color{color}' if color else '';path=ROOT/f'Assets/_Game/Runtime/Jobs/ClassArtworkCatalog{suffix}.asset';env=UnityPy.load(str(path));obj=next(o for o in env.objects if o.type.name=='MonoBehaviour');f=obj.assets_file;data=obj.read_typetree()
 template=next(r for r in data['entries'] if r['jobId']==169)
 for job in [170,171,172]:
  if any(r['job']==job and r['color']==color for r in report['unavailableArtwork']):continue
  if any(r['jobId']==job for r in data['entries']):continue
  texture=folder/f'Class{job}{suffix}.png';guid=yaml.safe_load(texture.with_name(texture.name+'.meta').read_text())['guid'];external=copy.copy(f.externals[-1]);external.guid=bytes((b>>4)|((b&15)<<4) for b in bytes.fromhex(guid));external.path='';external.type=3;f.externals.append(external);frames=[]
  for index,ref in enumerate(template['frames']):
   old=f.objects[ref['m_PathID']];new=copy.copy(old);pid=int.from_bytes(hashlib.sha256(f'late-class:{color}:{job}:{index}'.encode()).digest()[:8],'little')&0x7fffffffffffffff;assert pid not in f.objects
   tree=old.read_typetree();tree['m_Name']=f'Class{job}_'+['Up','Down','Left','Right'][index//3]+f'_{index%3}';tree['m_Rect']['y']+=64
   rd=tree['m_RD'];rd['texture']={'m_FileID':len(f.externals),'m_PathID':2800000};rd['textureRect']['y']+=64;rd['uvTransform']['w']+=64
   vertices=rd['m_VertexData'];assert vertices['m_VertexCount']==4 and len(vertices['m_DataSize'])==80
   buf=bytearray(vertices['m_DataSize'])
   for v in range(4):
    at=48+v*8+4;uv=struct.unpack_from('<f',buf,at)[0];struct.pack_into('<f',buf,at,(uv*128+64)/192)
   vertices['m_DataSize']=bytes(buf)
   key=tree['m_RenderDataKey'];tree['m_RenderDataKey']=(key[0],pid);new.path_id=pid;new.save_typetree(tree);f.objects[pid]=new;frames.append({'m_FileID':0,'m_PathID':pid})
  data['entries'].append({'jobId':job,'frames':frames})
 fix_native_uv(f,data['entries']);data['entries'].sort(key=lambda r:r['jobId']);expected=73-sum(r['color']==color for r in report['unavailableArtwork']);assert len(data['entries'])==expected;obj.save_typetree(data);binary=env.file.save();verify=UnityPy.load(binary);main=next(o for o in verify.objects if o.path_id==11400000).read_typetree();assert len(main['entries'])==expected
 path.write_bytes(binary);report['catalogs'][str(path.relative_to(ROOT))]=expected
# Upgrade the class-menu status crops and source-backed dialogue portraits.
path=ROOT/'Assets/_Game/Resources/UI/ClassMenuIcons/ClassMenuIcons.asset';env=UnityPy.load(str(path));obj=next(o for o in env.objects if o.type.name=='MonoBehaviour');f=obj.assets_file;tree=obj.read_typetree();template=next(r for r in tree['entries'] if r['jobId']==169)
for job in [170,171,172]:
 if any(r['jobId']==job for r in tree['entries']):continue
 texture=folder/f'Class{job}.png';guid=yaml.safe_load(texture.with_name(texture.name+'.meta').read_text())['guid'];external=copy.copy(f.externals[-1]);external.guid=bytes((b>>4)|((b&15)<<4) for b in bytes.fromhex(guid));external.path='';external.type=3;f.externals.append(external)
 old=f.objects[template['icon']['m_PathID']];new=copy.copy(old);pid=int.from_bytes(hashlib.sha256(f'late-menu:{job}'.encode()).digest()[:8],'little')&0x7fffffffffffffff;assert pid not in f.objects
 sp=old.read_typetree();sp['m_Name']=f'ClassMenu{job}';sp['m_Rect']['y']+=64;rd=sp['m_RD'];rd['texture']={'m_FileID':len(f.externals),'m_PathID':2800000};rd['textureRect']['y']+=64;rd['uvTransform']['w']+=64
 data=bytearray(rd['m_VertexData']['m_DataSize']);assert len(data)==80
 for v in range(4):
  at=48+v*8+4;uv=struct.unpack_from('<f',data,at)[0];struct.pack_into('<f',data,at,(uv*128+64)/192)
 rd['m_VertexData']['m_DataSize']=bytes(data);sp['m_RenderDataKey']=(sp['m_RenderDataKey'][0],pid);new.path_id=pid;new.save_typetree(sp);f.objects[pid]=new;tree['entries'].append({'jobId':job,'icon':{'m_FileID':0,'m_PathID':pid}})
fix_native_uv(f,tree['entries'],icon=True);obj.save_typetree(tree);path.write_bytes(env.file.save());report['classMenuIcons']=len(tree['entries'])
path=ROOT/'Assets/_Game/Resources/UI/CharacterPortraits/CharacterPortraits.asset';env=UnityPy.load(str(path));obj=next(o for o in env.objects if o.type.name=='MonoBehaviour');f=obj.assets_file;tree=obj.read_typetree()
for name,method,size,packed in arc_entries(SOURCE/'gameres/face.arc'):
 if not Path(name).stem.isdigit() or not 100<=int(Path(name).stem)<=172:continue
 job=int(Path(name).stem);raw=(lzh_decode if method==0 else lzss_decode)(packed,size);assert len(raw)==size
 sourcepath=ANALYSIS/'decoded_art/gameres/face.arc'/name.lower();sourcepath.parent.mkdir(parents=True,exist_ok=True);sourcepath.write_bytes(raw)
 image=Image.open(io.BytesIO(raw)).convert('RGBA');assert image.size==(64,64);image.putalpha(255);png=path.parent/f'{job}.png';image.save(png);meta=png.with_name(png.name+'.meta')
 if not meta.exists():
  config=yaml.safe_load((path.parent/'100.png.meta').read_text());config['guid']=uuid.uuid5(NAMESPACE,str(png.relative_to(ROOT))).hex;meta.write_text(yaml.safe_dump(config,sort_keys=False))
 if any(r['jobId']==job for r in tree['entries']):continue
 guid=yaml.safe_load(meta.read_text())['guid'];external=copy.copy(f.externals[-1]);external.guid=bytes((b>>4)|((b&15)<<4) for b in bytes.fromhex(guid));external.path='';external.type=3;f.externals.append(external);tree['entries'].append({'jobId':job,'image':{'m_FileID':len(f.externals),'m_PathID':2800000}})
tree['entries'].sort(key=lambda r:r['jobId']);assert len(tree['entries'])==70;obj.save_typetree(tree);path.write_bytes(env.file.save());report['dialoguePortraits']=70;report['unavailableDialoguePortraits']=[170,171,172]

report['unityPlay']=False
(ROOT/'Tools/LateVersionChecks/applied_data_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'catalogs':report['catalogs'],'artwork':len(report['artwork']),'changedArtwork':sum(r['changed'] for r in report['artwork'])},ensure_ascii=False))
