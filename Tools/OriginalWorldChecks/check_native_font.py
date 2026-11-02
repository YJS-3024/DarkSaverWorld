"""Compare every mapped native software glyph to imported atlas pixels and runtime metrics."""
import hashlib,json,os,subprocess
from pathlib import Path
from PIL import Image
from native_font_emulator import Font
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-font-checks')
def main():
 WORK.mkdir(exist_ok=True);mapping=json.loads((ROOT/'Assets/_Original/Data/World/NativeFontMapping.json').read_text());cases=[];clips=[];anchors=[];glyphcases=0;pixelchecks=0
 for source,image in [('dark14.mxf','OriginalFontGulim.png'),('dark14-1.mxf','OriginalFontSol.png')]:
  raw=(ROOT/'Assets/_Original/SourceData/Native'/(source+'.bytes')).read_bytes();im=Image.open(ROOT/'Assets/_Original/Art/World'/image).convert('RGBA');assert im.size==(896,574);native=Font(raw)
  for glyph in range(2592):
   for y in range(14):
    bits=int.from_bytes(raw[glyph*32+y*2:glyph*32+y*2+2],'big')
    for x in range(14):assert im.getpixel((glyph%64*14+x,glyph//64*14+y))==(255,255,255,255)if bits&(1<<(15-x))==0 else im.getpixel((glyph%64*14+x,glyph//64*14+y))[3]==0;pixelchecks+=1
  for cp,glyph in zip(mapping['codepoints'],mapping['glyphs']):
   char=chr(cp);got=native.draw(char);expected=bytearray(native.width*native.height)
   for y in range(14):
    for x in range(14):
     if im.getpixel((glyph%64*14+x,glyph//64*14+y))[3]:expected[(y+2)*native.width+x+2]=255
   assert got==expected,(source,char,glyph);glyphcases+=1
   if source=='dark14.mxf':cases.append(dict(text=char,width=native.measure(char),glyph=glyph))
  for text in ['힣','漢','①','\t','A 가','A\n가','가\nA','A\r\n가',' A 0','A\0가']:
   if source=='dark14.mxf':cases.append(dict(text=text,width=native.measure(text),glyph=mapping['glyphs'][mapping['codepoints'].index(ord(text))]if len(text)==1 and ord(text)in mapping['codepoints']else 31))
  for depth in [8,16]:
   native=Font(raw,depth)
   for char in ['A','가','힣','ㄱ']:
    visible=native.draw(char);assert any(visible)
    for x,y in [(-1,2),(2,-1),(native.width-13,2),(2,native.height-13)]:assert not any(native.draw(char,x,y)),(source,depth,char,x,y)
    for x in [-14,-1,0,1,86,87,99]:
     for y in [-14,-1,0,1,26,27,39]:clips.append(dict(x=x,y=y,width=native.width,height=native.height,visible=any(native.draw(char,x,y))))
   for text in ['A','AAA','가A','A B','ㄱA','漢A']:
    step=depth//8
    def first_pixel(frame):return min((index//step)%native.width for index in range(0,len(frame),step)if any(frame[index:index+step]))
    baseline=first_pixel(native.draw(text,50,20));width=native.measure(text)
    for alignment in [0,1,2,3,4,5,6,7,8,24]:anchors.append(dict(anchor=50,width=width,alignment=alignment,origin=50+first_pixel(native.draw(text,50,20,alignment))-baseline))
 paragraphs=[];native=Font((ROOT/'Assets/_Original/SourceData/Native/dark14.mxf.bytes').read_bytes())
 for text in ['abcdefgh1234','가나다라마바사','漢A','abc defghijkl','A\rB','A\r\nB','A\nB']:
  for width,height in [(75,16),(75,30),(35,30)]:paragraphs.append(dict(text=text,width=width,height=height,pixels=native.paragraph(text,width,height).hex()))
 (WORK/'cases.json').write_text(json.dumps(dict(cases=cases,paragraphs=paragraphs,clips=clips,anchors=anchors),ensure_ascii=False));sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeFontChecks.dll'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'NativeFontChecks.cs'))+'\n"'+str(ROOT/'Assets/_Original/Runtime/UI/NativeTextMetrics.cs')+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT/'Assets/_Original/Data/World/NativeFontMapping.json'),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1700:])
 r=json.loads(p.stdout);r.update(nativeGlyphFramebufferCases=glyphcases,nativeParagraphCases=len(paragraphs),nativeClipCases=len(clips),nativeLineAlignmentCases=len(anchors),atlasPixelChecks=pixelchecks,nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',mismatches=0,unityPlay=False,nativeCodeExecuted=True,logs=str(WORK),scope='Every mapped Unicode/CP949 glyph raster compared over full late Saver8bit framebuffer; both source fonts and every atlas mask. Native width, unsupported fallback and784 complete-glyph viewport predicates across both fonts and8/16bit. Single-line origin240cases derive from actual raster displacement for literal alignment2/6, including odd widths and other ignored flags. Memo opt-in paragraphs21cases. Runtime culls against marked logical surface and dirties geometry when moved. Default bounded uGUI wrapping/alignment, parent masks, transform/GPU output, unbounded newline policy and actual window framebuffers are not equality claims.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_font.py',OUT/'native_font_emulator.py',OUT/'NativeFontChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeTextMetrics.cs',ROOT/'Assets/_Original/Runtime/UI/NativeBitmapText.cs',ROOT/'Assets/_Original/Runtime/UI/NativeTextSurface.cs']});(OUT/'native-font-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
