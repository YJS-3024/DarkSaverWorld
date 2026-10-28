"""Read-only source/art checks for the direct-play feedback. No Unity execution."""
from pathlib import Path
import argparse,sys,json,hashlib,struct,io
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/StaticAnalysisDependencies'))
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
import pefile
from decode_visuals import read_lsc,read_ui_library
args=argparse.ArgumentParser();args.add_argument('--native-root',type=Path,required=True);options=args.parse_args()
source=options.native_root
checks=[]
def check(ok,label):
    if not ok:raise AssertionError(label)
    checks.append(label)
files={p.name.lower():p for p in (source/'Glib').iterdir()}
parts,atlas,*_=read_ui_library(files['gamelib.lce'])
for n in [0,3,5]:
    d=read_lsc(files[f'gscene{n}.lsc']);im=Image.new('RGBA',(d['width'],d['height']))
    for v in d['decor']:
        a=parts[v['id']];part=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
        for y in range(v['repeat_y']):
            for x in range(v['repeat_x']):im.alpha_composite(part,(v['x']+x*a['width'],v['y']+y*a['height']))
    target=Image.open(ROOT/f'Assets/_Game/Resources/UI/BattleWindows/Window{n}.png').convert('RGB')
    check(im.size==target.size and ImageChops.difference(im.convert('RGB'),target).getbbox() is None,'native window background'+str(n))
mini=read_lsc(files['gscene0.lsc']);fill=next(v for v in mini['decor'] if v['id']==8)
check((fill['x'],fill['y'],fill['repeat_x']*parts[8]['width'],fill['repeat_y']*parts[8]['height'])==(11,25,100,100),'native minimap inner viewport')
base=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/char4'
for job in [807,810,818]:
    path=next(p for p in base.iterdir() if p.name.lower()==str(job)+'r.bmp');im=Image.open(path).convert('RGBA');size=im.width//3
    for frame in range(3):
        expected=im.crop((frame*size,size,(frame+1)*size,size*2));expected.putdata([(r,g,b,0 if r==g==b==0 else a) for r,g,b,a in expected.getdata()])
        target=Image.open(ROOT/f'Assets/_Game/Resources/Interiors/Npcs/Original/{job}_Down{frame}.png').convert('RGBA')
        check(expected.tobytes()==target.tobytes(),'staff idle frame'+str((job,frame)))
exe=source/'Saver.exe';before=hashlib.sha256(exe.read_bytes()).hexdigest();pe=pefile.PE(str(exe));resources={r.id:r for r in pe.DIRECTORY_ENTRY_RESOURCE.entries}
r=next(x for x in resources[1].directory.entries if x.id==7).directory.entries[0].data.struct;raw=pe.get_data(r.OffsetToData,r.Size);hx,hy=struct.unpack_from('<HH',raw)
cur=struct.pack('<HHHBBBBHHII',0,2,1,32,32,2,0,hx,hy,len(raw)-4,22)+raw[4:]
image=Image.open(io.BytesIO(cur)).convert('RGBA');target=Image.open(ROOT/'Assets/_Game/Resources/UI/NativeCursor.png').convert('RGBA')
check(image.tobytes()==target.tobytes() and (hx,hy)==(0,0),'native cursor148/image7 hotspot and pixels')
for name in ['1_8_0','1_11_0']:
    check(Image.open(ROOT/f'Assets/_Game/Resources/Towns/Kaosia/Sprites/{name}.png').size==(64,64),'identified two-row tree'+name)
native_map=next(p for p in (source/'MAP').iterdir() if p.name.lower()=='301hmap.bmp')
check(Image.open(native_map).convert('RGBA').tobytes()==Image.open(ROOT/'Assets/_Game/Resources/Tutorial/NativeMinimap.png').convert('RGBA').tobytes(),'native301hmap pixels')
marker_sheet=Image.open(ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc/MCURSOR.BMP').convert('RGBA')
for i in range(10):
    expected=marker_sheet.crop((192+i*3,0,195+i*3,3));expected.putdata([(r,g,b,0 if r==g==b==0 else a) for r,g,b,a in expected.getdata()])
    target=Image.open(ROOT/f'Assets/_Game/Resources/Tutorial/MinimapMarker{i}.png').convert('RGBA')
    check(expected.tobytes()==target.tobytes(),'native minimap marker'+str(i))
check(Image.open(ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc2/board.bmp').convert('RGB').getpixel((100,400))==(222,0,16),'native minimap viewport red')
check(hashlib.sha256(exe.read_bytes()).hexdigest()==before,'original executable unchanged')
report=dict(checks=len(checks),passed=checks,unityPlay=False,sourceSha256=before,minimapViewport=[11,25,100,100],limits=['Tutorial hmap/markers/viewport only; general server visibility not established','World80ms cadence is a local presentation setting, not native timing proof','Warehouse transactions remain unimplemented'])
(ROOT/'Tools/MapChecks/direct_play_feedback_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('PASS',len(checks),'direct feedback source/art checks; NOT Unity Play')
