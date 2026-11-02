"""Native MXF zero-bit masks and audited CP949 mapping; no OS-font rasterization."""
import hashlib,json,re
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT,metadata

def build():
 outputs=[];sources=[];target=BASE/'Art/World';target.mkdir(parents=True,exist_ok=True);(BASE/'Resources').mkdir(parents=True,exist_ok=True)
 for name,file in [('dark14.mxf','OriginalFontGulim.png'),('dark14-1.mxf','OriginalFontSol.png')]:
  p=BASE/'SourceData/Native'/(name+'.bytes');raw=p.read_bytes();assert len(raw)==2592*32;sources.append(p);im=Image.new('RGBA',(896,574));pixels=im.load()
  for glyph in range(2592):
   for y in range(14):
    bits=int.from_bytes(raw[glyph*32+y*2:glyph*32+y*2+2],'big')
    for x in range(14):
     if bits&(1<<(15-x))==0:pixels[glyph%64*14+x,glyph//64*14+y]=(255,255,255,255)
  p=target/file;im.save(p);metadata.meta(p,ROOT/'Assets');outputs.append(p)
 codepoints=[];glyphs=[]
 encoding_points=[];encoding_values=[]
 for cp in range(65536):
  try:b=chr(cp).encode("cp949")
  except UnicodeEncodeError:continue
  encoding_points.append(cp);encoding_values.append(int.from_bytes(b,"big"))
 encoding=BASE/"Resources/OriginalCodePage949.json";encoding.write_text(json.dumps(dict(unicode=encoding_points,encoded=encoding_values),separators=(",",":"))+"\n");metadata.meta(encoding,ROOT/"Assets");outputs.append(encoding)
 for cp in range(33,65536):
  try:b=chr(cp).encode('cp949')
  except UnicodeEncodeError:continue
  glyph=None
  if len(b)==1 and b[0]<=127:glyph=b[0]-32
  elif len(b)==2 and b[0]==0xa4 and b[1]>=0xa0:glyph=96+b[1]-0xa0
  elif len(b)==2 and 0xb0<=b[0]<=0xc8 and b[1]>=0xa0:glyph=192+(b[0]-0xb0)*96+b[1]-0xa0
  if glyph is not None:assert 0<=glyph<2592;codepoints.append(cp);glyphs.append(glyph)
 mapping=BASE/'Data/World/NativeFontMapping.json';mapping.write_text(json.dumps(dict(codepoints=codepoints,glyphs=glyphs),separators=(',',':'))+'\n');metadata.meta(mapping,ROOT/'Assets');outputs.append(mapping)
 script=BASE/'Runtime/UI/NativeBitmapFont.cs';metadata.meta(script,ROOT/'Assets');guid=lambda p:re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
 asset=BASE/'Resources/OriginalBitmapFont.asset';asset.parent.mkdir(exist_ok=True)
 text='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid(script)+', type: 3}\n  m_Name: OriginalBitmapFont\n'
 for field,p,kind in [('normal',outputs[0],2800000),('alternate',outputs[1],2800000),('mapping',mapping,4900000)]:text+='  '+field+': {fileID: '+str(kind)+', guid: '+guid(p)+', type: 3}\n'
 asset.write_text(text);metadata.meta(asset,ROOT/'Assets');Path(str(asset)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(asset)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');outputs.append(asset)
 for p in [BASE/'Runtime/UI',BASE/'Resources']:metadata.meta(p,ROOT/'Assets',folder=True)
 for p in (BASE/'Runtime/UI').glob('*.cs'):metadata.meta(p,ROOT/'Assets')
 sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
 r=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in sources},outputs=[dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for p in outputs],glyphsPerFont=2592,codepoints=len(codepoints),codePage949Characters=len(encoding_points),glyphSize=14,asciiAdvance=7,spaceAdvance=5,koreanAdvance=14,unityPlay=False,scope='Original inverse-bit MXF14x14 masks; mapping is native ASCII/Jamo/KS syllable range, unsupported codepoints use question-mark glyph. Window wrapping/alignment is a bounded uGUI adapter, not full native paragraph reproduction.')
 Path(__file__).with_name('native-font-assets.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');return r
if __name__=='__main__':
 r=build();print(json.dumps({k:r[k]for k in ['glyphsPerFont','codepoints','outputs']}))
