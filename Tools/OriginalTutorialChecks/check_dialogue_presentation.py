"""Compare actual native draw operands and source pixels with tutorial UI assets.
No Unity Play, GPU output, physical input or full native process execution.
"""
import hashlib,json,re,sys,tempfile,shutil
from pathlib import Path
from collections import Counter
from PIL import Image
from native_dialogue_emulator import Dialogue
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_lsc,read_ui_library

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def key(image):
 image=image.convert('RGBA');image.putdata([(*p[:3],0 if p[:3]==(0,0,0)else 255)for p in image.get_flattened_data()]);return image

def main():
 checks=0
 def check(ok,label):
  nonlocal checks;checks+=1
  if not ok:raise AssertionError(label)
 n=Dialogue();origin,faces,texts=n.waiting();ui=BASE/'Runtime/Tutorial/TutorialSceneRoot.Waiting.cs';code=ui.read_text()
 def rect(label):
  m=re.search(re.escape(label)+r'.*?new Rect\(([^)]+)\)',code);assert m,label
  return tuple(int(value.strip())for value in m[1].split(','))
 check(rect('Original RT_STRING lesson')==(*origin,562,84),'native centered waiting origin and LSC size')
 face=faces[0];bounds=texts[0][:4];check(rect('Teacher portrait')==(face[0]-origin[0],face[1]-origin[1],64,64),'native full 64px face placement');check(face[3]==0,'native opaque face copy')
 check(rect('Original lesson text')==(bounds[0]-origin[0],bounds[1]-origin[1],bounds[2]-bounds[0],bounds[3]-bounds[1]),'native paragraph rectangle')
 check(texts[0][5]==65535 and 'waitingMessage.color = Color.white;'in code and '((NativeBitmapText)waitingMessage).NativeParagraphWrap = true;'in code,'white native paragraph mode, no global fade')
 sources=[Path(__file__),OUT/'native_dialogue_emulator.py',ROOT/'Tools/OriginalWorldChecks/check_native_commanders.py',ROOT/'Tools/OriginalWorldChecks/native_ai_emulator.py',ui,BASE/'Runtime/Tutorial/TutorialSceneRoot.cs',BASE/'Art/Tutorial/WaitingDialogue.png',BASE/'Art/Tutorial/Dialogue.png',BASE/'Art/Archives/gameres/etc2.arc/board.bmp.png',BASE/'SourceData/Native/wlib/scene38.lsc.bytes']
 scene=read_lsc(sources[-1]);check((scene['width'],scene['height'])==(562,84),'native scene38 dimensions')
 with tempfile.TemporaryDirectory(prefix='ds-dialogue-')as temporary:
  p=Path(temporary)/'waitlib.lce';native=BASE/'SourceData/Native/wlib/waitlib.lce.bytes';sources.append(native);shutil.copyfile(native,p);parts,atlas,_=read_ui_library(p);image=Image.new('RGBA',(562,84))
  for row in scene['decor']:
   part=parts[row['id']];piece=atlas.crop((part['x'],part['y'],part['x']+part['width'],part['y']+part['height']))
   for y in range(row['repeat_y']):
    for x in range(row['repeat_x']):image.alpha_composite(piece,(row['x']+x*part['width'],row['y']+y*part['height']))
  check(key(image).tobytes()==Image.open(BASE/'Art/Tutorial/WaitingDialogue.png').convert('RGBA').tobytes(),'every waiting background RGBA pixel from native LSC/library')
 board=key(Image.open(BASE/'Art/Archives/gameres/etc2.arc/board.bmp.png'));blits=n.battle();image=Image.new('RGBA',(562,104))
 for x,y,source,region,flags in blits:
  check(flags==1,'native board copy uses black color key');image.alpha_composite(board.crop(region),(x-40,y-50))
 # Native10008AD0 also copies the68px recess from board(90,300)-(158,368)
 # at(+18,+18), then places its64px face at(+20,+20).
 image.alpha_composite(board.crop((90,300,158,368)),(18,18));check(image.tobytes()==Image.open(BASE/'Art/Tutorial/Dialogue.png').convert('RGBA').tobytes(),'every battle background RGBA pixel from native tile calls/recess')
 battle=(BASE/'Runtime/Tutorial/TutorialSceneRoot.cs').read_text();check('"MESSAGE", new Rect(40, 50, 562, 104)'in battle and '"Face125", new Rect(20, 20, 64, 64)'in battle,'battle face/frame stays on its separate native layout')
 counts={}
 for name in ['WaitingDialogue','Dialogue']:
  alpha=Counter(Image.open(BASE/f'Art/Tutorial/{name}.png').convert('RGBA').getchannel('A').get_flattened_data());check(set(alpha)=={0,255},'literal color key, no invented uniform half alpha');counts[name]={str(k):v for k,v in alpha.items()}
 report=dict(assertions=checks,nativeCodeExecuted=True,nativeBattleTileCalls=len(blits),waitingOrigin=origin,waitingFaceLocal=[face[0]-origin[0],face[1]-origin[1],64,64],waitingTextLocal=[90,10,455,64],alphaPixels=counts,rgbaPixelComparisons=562*(84+104),unityPlay=False,mismatches=0,nativeSha256=dict(Saver='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',GameCtl=n.gamectlSha),scope='Actual Saver472640/4346D0 centering and472670 face/text operands, GameCtl10008E10 tile operands. DirectDraw/MFC/Win32 rectangle/text calls isolated; actual LSC/library RGBA and all battle frame pixels compared. Not Windows process/GPU/Unity rendering, alpha filtering or input.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in sources})
 (OUT/'dialogue-presentation-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
