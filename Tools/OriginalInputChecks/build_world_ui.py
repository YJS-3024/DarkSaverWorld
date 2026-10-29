"""Convert original Wlib artwork/controls into Unity assets; never overwrite edited output."""
import hashlib,json,struct,uuid
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'분석/Saver_1.2';OUT=ROOT/'Assets/_Game/Resources/UI/WorldWindows'
def guid(p):return uuid.uuid5(uuid.NAMESPACE_URL,'darksaver-world-ui/'+str(p.relative_to(ROOT))).hex
def put(p,data):
 data=data.encode() if isinstance(data,str) else data
 if p.exists():assert p.read_bytes()==data,'Edited output: '+str(p)
 else:p.write_bytes(data)
def meta(p,body):put(Path(str(p)+'.meta'),'fileFormatVersion: 2\nguid: '+guid(p)+'\n'+body)
def integer(b,o):return struct.unpack_from('<i',b,o)[0]
OUT.mkdir(parents=True,exist_ok=True);meta(OUT,'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
h=(SRC/'unpacked/legacy_libraries/Wlib_waitlib_lce.header.bin').read_bytes();atlas=Image.open(SRC/'unpacked/legacy_libraries/Wlib_waitlib_lce.bmp').convert('RGBA');bh=(SRC/'unpacked/legacy_libraries/Wlib_waitlib_ltn.header.bin').read_bytes();buttons=Image.open(SRC/'unpacked/legacy_libraries/Wlib_waitlib_ltn.bmp').convert('RGBA')
textures={};proof=[]
def texture(name,im):
 im=im.copy();im.putdata([(r,g,b,0 if r==g==b==0 else 255) for r,g,b,a in im.getdata()]);p=OUT/(name+'.png')
 import io
 buf=io.BytesIO();im.save(buf,format='PNG');put(p,buf.getvalue())
 meta(p,'TextureImporter:\n  serializedVersion: 13\n  mipmaps:\n    enableMipMap: 0\n    sRGBTexture: 1\n  isReadable: 0\n  textureSettings:\n    filterMode: 0\n    aniso: 1\n    wrapU: 1\n    wrapV: 1\n  nPOTScale: 0\n  textureType: 0\n  alphaIsTransparency: 1\n  maxTextureSize: 2048\n  alphaSource: 1\n  platformSettings:\n  - serializedVersion: 4\n    buildTarget: DefaultTexturePlatform\n    maxTextureSize: 2048\n    textureFormat: -1\n    textureCompression: 0\n    compressionQuality: 100\n    crunchedCompression: 0\n    allowsAlphaSplitting: 0\n    overridden: 0\n')
 textures[name]=dict(path=str(p.relative_to(ROOT)),sha256=hashlib.sha256(buf.getvalue()).hexdigest());return '{fileID: 2800000, guid: '+guid(p)+', type: 3}'
def compose(id,suffix=''):
 raw=SRC/f'extracted/Saver_Files/Saver/Wlib/scene{id}{suffix}.lsc';b=raw.read_bytes();w,ht=integer(b,0),integer(b,4);im=Image.new('RGBA',(w,ht));cur=28
 for _ in range(integer(b,24)):
  n,count=integer(b,cur),integer(b,cur+4);cur+=8;r=4+(n-1)*20;assert integer(h,r)==n
  x,y,tw,th=struct.unpack_from('<4i',h,r+4);tile=atlas.crop((x,y,x+tw,y+th))
  for _ in range(count):
   l,t,nx,ny=struct.unpack_from('<4i',b,cur);cur+=16
   for xx in range(nx):
    for yy in range(ny):im.paste(tile,(l+xx*tw,t+yy*th))
 controls=[];count=integer(b,cur);cur+=4
 for i in range(count):
  n,l,t=struct.unpack_from('<3i',b,cur);cur+=12;r=4+(n-1)*24;assert integer(bh,r)==n;x,y,tw,th=struct.unpack_from('<4i',bh,r+4)
  frames=[texture('Button'+str(n)+'_'+str(k),buttons.crop((x+k*tw,y,x+(k+1)*tw,y+th))) for k in range(3)]
  controls.append((n,l,t,tw,th,frames))
 proof.append(dict(source=str(raw.relative_to(ROOT)),sha256=hashlib.sha256(b).hexdigest(),scene=id,suffix=suffix,width=w,height=ht,controls=[list(c[:5]) for c in controls]))
 return w,ht,texture('Window'+str(id)+suffix,im),controls
names={2:'보유 캐릭터',3:'대화',15:'환경설정',19:'도움말',27:'게임 종료',42:'보유 캐릭터 간소화',303:'대화 확장'}
yaml=['%YAML 1.1','%TAG !u! tag:unity3d.com,2011:','--- !u!114 &11400000','MonoBehaviour:','  m_ObjectHideFlags: 0','  m_CorrespondingSourceObject: {fileID: 0}','  m_PrefabInstance: {fileID: 0}','  m_PrefabAsset: {fileID: 0}','  m_GameObject: {fileID: 0}','  m_Enabled: 1','  m_EditorHideFlags: 0','  m_Script: {fileID: 11500000, guid: bffdb6994f0ba0243bf3476b846a2ca4, type: 3}','  m_Name: NativeWorldWindows','  m_EditorClassIdentifier: ','  windows:']
for id,title in names.items():
 w,ht,bg,cs=compose(3,'mm') if id==303 else compose(id);mw=mh=0;mini='{fileID: 0}'
 if id in [2,3,42,303]:mw,mh,mini,_=compose(3 if id==303 else id,'m')
 yaml.extend([f'  - sceneId: {id}',f'    title: {title}',f'    background: {bg}',f'    size: {{x: {w}, y: {ht}}}',f'    minimizedBackground: {mini}',f'    minimizedSize: {{x: {mw}, y: {mh}}}','    controls:'])
 for n,l,t,cw,ch,frames in cs:
  yaml.extend([f'    - nativeId: {n}',f'      rect: {{x: {l}, y: {t}, width: {cw}, height: {ch}}}','      frames:']+['      - '+f for f in frames])
p=OUT/'NativeWorldWindows.asset';put(p,'\n'.join(yaml)+'\n');meta(p,'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
helpbytes=(SRC/'decoded/all_arc/gameres/set/help.txt').read_bytes();put(OUT/'NativeWorldHelp.txt',helpbytes.decode('cp949').replace('\r\n','\n'));meta(OUT/'NativeWorldHelp.txt','TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
# Menu-description board: original20px cells from etc2/board.bmp (0,300).
board=Image.open(SRC/'decoded/all_arc/gameres/etc2/board.bmp').convert('RGBA')
hint=Image.new('RGBA',(562,65))
for dy in range(65):
 for dx in range(562):
  sx=dx if dx<20 else 40+(dx-542) if dx>=542 else 20+(dx-20)%20
  sy=300+dy if dy<20 else 340+(dy-45) if dy>=45 else 320+(dy-20)%20
  hint.putpixel((dx,dy),board.getpixel((sx,sy)))
texture('MenuDescription',hint)
(Path(__file__).with_name('world_ui_conversion.json')).write_text(json.dumps(dict(layouts=proof,textures=textures),ensure_ascii=False,indent=2)+'\n')
print('PASS: 7 native world layouts, 3 minimized layouts, original control frames/help converted; output protected.')
