"""Original tutorial source/resource/build registration audit, not a Play test."""
from pathlib import Path
import hashlib,json,re
from collections import Counter
from PIL import Image
import UnityPy
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/_Game/Resources/Tutorial'
checks=0
def check(ok,name):
 global checks
 if not ok:raise AssertionError(name)
 checks+=1
source=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/tutorial/TUTORIAL.DEO'
text=source.read_text('cp949');runtime=(OUT/'TUTORIAL.txt').read_text()
check(text==runtime,'runtime text equals recovered original')
late=ROOT/'분석/Saver_Late/decoded/map/tutorial.arc/tutorial.deo'
check(late.read_text('utf-8-sig')==runtime,'early/late tutorial text equality ignoring line endings')
commands=[x.split()[0] for x in text.splitlines() if x.strip() and not x.startswith(';')]
check(len(commands)==420,'420 source commands');check(commands.count('MESSAGE')==100,'100 dialogue commands');check(len(set(commands))==44,'44 source command names')
check(commands.count('SETHP')==1 and commands.count('SETMP')==1,'two unregistered literal spellings retained')
check(Image.open(OUT/'Dialogue.png').size==(562,104),'native even-rounded dialogue frame')
frames=list((OUT/'Characters').glob('*.png'));check(len(frames)==108,'native character/direction/color frames')
for p in frames:
 check(p.with_suffix('.png.meta').exists(),'frame importer '+p.name)
 check(Image.open(p).size==(32,32),'32px original actor '+p.name)
for p in [ROOT/'Assets/_Game/Resources/Scenarios/Shared/Music/nco01.ogg',ROOT/'Assets/_Game/Resources/UI/CharacterPortraits/125.png',ROOT/'Assets/_Game/Resources/Effects/NativeFireballEffects.asset',ROOT/'Assets/_Game/Resources/UI/BattleWindows/NativeBattleWindows.asset']:
 check(p.exists(),'required native reference '+p.name)
build=UnityPy.load(str(ROOT/'ProjectSettings/EditorBuildSettings.asset'))
scenes=next(o.read_typetree()['m_Scenes'] for o in build.objects if o.type.name=='EditorBuildSettings')
path='Assets/_Game/Scenes/Maps/Map_301_;.unity';found=[s for s in scenes if s['path']==path]
check(len(found)==1 and found[0]['enabled'],'map301 enabled exactly once')
guid=re.search(r'guid: (\w+)',(ROOT/(path+'.meta')).read_text())[1]
check(found[0]['guid']=={f'data[{i}]':int(guid[i*8:i*8+8][::-1],16) for i in range(4)},'native scene GUID roundtrip')
actors=UnityPy.load(str(ROOT/'Assets/_Game/Resources/Effects/NativeFireballEffects.asset'))
check(any(101 in o.read_typetree().get('magicIds',[]) for o in actors.objects if o.type.name=='MonoBehaviour'),'fireball renderer supports tutorial101')
atlas=Image.open(ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc2/board.bmp').convert('RGBA')
for name,kind in [('Move',4),('Attack',12),('Magic',8),('Target',3)]:
    x,y=(0,0) if kind==3 else (200+(kind-4)%3*100,(kind-4)//3*100)
    crop=atlas.crop((x,y,x+31,y+31));crop.putdata([(r,g,b,0 if r==g==b==0 else a) for r,g,b,a in crop.getdata()])
    expected=Image.new('RGBA',(32,32));expected.alpha_composite(crop,(0,0))
    path=OUT/'Ranges'/(name+'.png')
    check(path.exists() and Image.open(path).tobytes()==expected.tobytes(),'native checker pixels '+name)
    check(Path(str(path)+'.meta').exists(),'range texture importer '+name)
death_path=ROOT/'Assets/_Game/Resources/Effects/CombatDeathEffectCatalog.asset'
check(death_path.exists(),'native corpse catalog available to runtime Resources')
death=UnityPy.load(str(death_path))
refs=next(o.read_typetree()['frames'] for o in death.objects if o.type.name=='MonoBehaviour')
check(len(refs)==7 and all(r['m_FileID']>0 and r['m_PathID']!=0 for r in refs),'seven serialized original death frame references retained')
fx=Image.open(ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp').convert('RGBA')
for col in range(7):
    expected=fx.crop((col*32,0,col*32+31,31))
    expected.putdata([(r,g,b,0 if r==g==b==0 else 255) for r,g,b,a in expected.getdata()])
    actual=Image.open(ROOT/f'Assets/_Game/Art/Converted/Effects/Death/DeathEffect_{col}.png').convert('RGBA')
    check(actual.size==(31,31) and actual.tobytes()==expected.tobytes(),'native corpse pixels '+str(col))
terrain_source=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/set/TileAttr.atr'
def terrain_rows(text):return [line.split() for line in text.splitlines() if line.strip() and not line.lstrip().startswith(';')]
original=terrain_rows(terrain_source.read_text('cp949'))
late=terrain_rows((ROOT/'분석/Saver_Late/decoded/gameres/set.arc/tileattr.atr').read_text('utf-8-sig'))
check(original==late,'early/late original terrain triples agree')
code=(ROOT/'Assets/_Game/Runtime/Events/OriginalTutorialTerrain.cs').read_text().split('values={',1)[1].split('};',1)[0]
generated=[[int(value) for value in row.split(',')] for row in re.findall(r'\{([^{}]+)\}',code)]
check(len(generated)==27 and all(len(row)==30 for row in generated),'generated original terrain table shape27x10x3')
for attr,row in enumerate(original):
    for troop in range(10):
        check(generated[attr][troop*3:troop*3+3]==list(map(int,row[1+troop*3:4+troop*3])),f'original attack/defense/movement triple {attr}/{troop}')
fixture=json.loads((ROOT/'Assets/_Game/Tests/EditMode/OriginalTutorial301Terrain.json').read_text())
import struct
mrc_bytes=(ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/MAP/301.mrc').read_bytes()
resource_count=struct.unpack_from('<i',mrc_bytes)[0];w,h,_=struct.unpack_from('<iii',mrc_bytes,4+resource_count*4)
check(fixture['width']==w and fixture['height']==h and len(fixture['attributes'])==w*h,'editor fixture dimensions match original301')
start=16+resource_count*4+2*w*h
attrs=[mrc_bytes[start+i*5+3]&0x7f for i in range(w*h)];attrs=[a if a<=0x1b else 0 for a in attrs]
check(fixture['attributes']==attrs,'editor fixture300 normalized terrain cells match original301')
check(fixture['sourceSha256']==hashlib.sha256(mrc_bytes).hexdigest(),'editor fixture original301 source hash')
report=dict(checks=checks,commands=Counter(commands),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),nativeMap=301,scope='Static source/resources/build settings only; not Unity Play, input, audio or timing verification')
(ROOT/'Tools/MapChecks/original_tutorial_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
import yaml
frame_ids=set()
for meta in sorted((OUT/'Characters').glob('*.png.meta')):
 importer=yaml.safe_load(meta.read_text())['TextureImporter']
 sheet=importer['spriteSheet']
 check(importer['spriteMode']==1 and importer['spriteMeshType']==0,'full native single sprite '+meta.stem)
 check(not sheet.get('sprites') and not sheet.get('nameFileIdTable'),'no stale Kisra sub-sprites '+meta.stem)
 check(sheet['spriteID'] not in frame_ids,'unique native frame importer '+meta.stem)
 frame_ids.add(sheet['spriteID'])
print('PASS',checks,'original tutorial source/resource checks; NOT Unity Play')
