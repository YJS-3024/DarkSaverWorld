"""Read back imported-format Unity binaries independently of the updater.
No Unity Editor import, rendering or original Windows executable execution.
"""
import hashlib,json,re,struct,sys
from pathlib import Path
import UnityPy,yaml
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
r=ROOT/'Assets/_Game/Runtime';checks={}
def asset(path):
 e=UnityPy.load(str(ROOT/path));o=next(x for x in e.objects if x.type.name=='MonoBehaviour');return o,o.read_typetree()
main,items=asset('Assets/_Game/Runtime/Items/ItemCatalog.asset');rows=items['entries'];assert len(rows)==1212
for row in rows:
 p='Assets/_Game/Data/Items/Item_'+str(row['numericValues'][0]).zfill(4)+'.asset'
 _,individual=asset(p);assert individual['data']==row,p
checks['individualItemDefinitions']=len(rows)
_,jobs=asset('Assets/_Game/Runtime/Jobs/JobCatalog.asset');assert len(jobs['entries'])==73
_,growth=asset('Assets/_Game/Runtime/Combat/ExperienceProgression.asset');g=growth['entries'];assert len(g)==3177
for id in [170,171,172]:assert [x['level'] for x in g if x['jobId']==id]==list(range(1,301))
for old,new in [(167,170),(168,171),(169,172)]:assert next(x for x in g if x['jobId']==old and x['level']==100)['nextJobId']==new
assert sum(x['jobId']==163 and x['level']==35 for x in g)==2
for id in {x['jobId'] for x in g}:
 levels=[x for x in g if x['jobId']==id];assert all(a['requiredXp']<=b['requiredXp'] for a,b in zip(levels,levels[1:])),id
checks.update(jobs=73,growthRows=3177)
_,magic=asset('Assets/_Game/Runtime/Magic/MagicCatalog.asset');assert len(magic['entries'])==440
for id,ea in [(11201,-3),(11202,-5),(11203,-7),(11204,-9)]:assert next(x for x in magic['entries'] if x['numericValues'][0]==id)['numericValues'][8]==ea
assert any(x['numericValues'][0]==30901 for x in magic['entries'])
checks['magicCatalog']=440
# Resolve serialized texture GUIDs through Unity .meta files, not assumed external indices.
textures={}
for p in (ROOT/'Assets/_Game/Art/Converted/Classes').glob('*.png'):
 guid=yaml.safe_load(p.with_name(p.name+'.meta').read_text())['guid'];swapped=bytes((x>>4)|((x&15)<<4) for x in bytes.fromhex(guid));textures[swapped]=p
for suffix,count in [('',73),('_Color2',73),('_Color3',72)]:
 o,data=asset('Assets/_Game/Runtime/Jobs/ClassArtworkCatalog'+suffix+'.asset');assert len(data['entries'])==count
 for row in data['entries']:
  for i,ref in enumerate(row['frames']):
   sprite=o.assets_file.objects[ref['m_PathID']].read_typetree();rd=sprite['m_RD'];external=o.assets_file.externals[rd['texture']['m_FileID']-1];p=textures[external.guid]
   assert p.stem==f"Class{row['jobId']}"+suffix,(row['jobId'],p)
   if row['jobId']>=170:
    assert sprite['m_Rect']==dict(x=float(i%3*32),y=float(192-(i//3+1)*32),width=32.,height=32.)
    assert rd['textureRect']==sprite['m_Rect']
    vertices=rd['m_VertexData']['m_DataSize'];uv=[struct.unpack_from('<2f',vertices,48+j*8) for j in range(4)]
    rect=sprite['m_Rect'];assert abs(min(v[0] for v in uv)*96-rect['x'])<.001
    assert abs(min(v[1] for v in uv)*192-rect['y'])<.001
    assert abs(max(v[1] for v in uv)*192-rect['y']-32)<.001
 assert (170 not in [x['jobId'] for x in data['entries']])==(suffix=='_Color3')
 checks['artwork'+(suffix or '_normal')]=count
_,icons=asset('Assets/_Game/Resources/UI/ClassMenuIcons/ClassMenuIcons.asset');assert len(icons['entries'])==73
_,portraits=asset('Assets/_Game/Resources/UI/CharacterPortraits/CharacterPortraits.asset');assert len(portraits['entries'])==70
checks.update(classMenuIcons=73,dialoguePortraits=70)
report=json.loads((ROOT/'Tools/LateVersionChecks/applied_data_check.json').read_text())
for row in report['artwork']:
 path=ROOT/'Assets/_Game/Art/Converted/Classes'/(f"Class{row['job']}"+(f"_Color{row['color']}" if row['color'] else '')+'.png')
 assert hashlib.sha256(Image.open(path).convert('RGBA').tobytes()).hexdigest()==row['pixelSha256']
checks['artworkPixelChecks']=len(report['artwork'])
assert hashlib.sha256(Image.open(ROOT/'Assets/_Game/Resources/UI/ItemIcons.png').convert('RGBA').tobytes()).hexdigest()==report['itemAtlas']['pixelSha256']
checks.update(itemAtlasPixels=True,unityPlay=False,passed=True)
(ROOT/'Tools/LateVersionChecks/asset_readback_check.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n');print(json.dumps(checks))
