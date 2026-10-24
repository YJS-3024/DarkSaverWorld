"""Native software-font pixels, alias lookup, and narrow/wide argument provenance.
No OS/DirectDraw entrypoint or replacement libc printf is claimed as original playback.
"""
import collections,csv,hashlib,json,shutil,struct,subprocess
from PIL import Image,ImageDraw
from unicorn.x86_const import UC_X86_REG_ECX,UC_X86_REG_EAX,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_ESI,UC_X86_REG_EBX
from analyze_special_skills import ROOT,BASE,OUT,PE,write
from analyze_packet_requests import Harness
from analyze_script_executor import ScriptHarness
from analyze_dialogue_layout import text

class FontHarness(Harness):
 def __init__(self,raw):
  super().__init__('Saver.exe');self.obj=0x30001000;self.glyph=0x30020000;self.canvas=0x30040000;self.strings=0x30002000;self.width=320;self.height=120;self.depth=8
  assert len(raw)==82944
  self.u.mem_write(self.glyph,raw);self.put(self.obj,1,14,14,32,self.glyph,self.glyph,self.glyph+3072,self.glyph+6144)
  self.put(0x446628,0x300f0000);self.put(0x44663c,0x300f0010);self.stubs[0x300f0000]=self.intersect;self.stubs[0x300f0010]=self.equal
 def run(self,entry,args=(),this=0):
  self.put(0x20010000,0x300ff000,*[v&0xffffffff for v in args]);self.u.reg_write(UC_X86_REG_ESP,0x20010000);self.u.reg_write(UC_X86_REG_ECX,this);self.u.emu_start(entry,0x300ff000,count=5000000);assert self.u.reg_read(UC_X86_REG_EIP)==0x300ff000,'instruction limit'
 def args(self,n):
  from unicorn.x86_const import UC_X86_REG_ESP
  sp=self.u.reg_read(UC_X86_REG_ESP);return [self.get(sp+4+i*4) for i in range(n)]
 def intersect(self):
  dst,a,b=self.args(3);x=struct.unpack('<4i',self.u.mem_read(a,16));y=struct.unpack('<4i',self.u.mem_read(b,16));r=(max(x[0],y[0]),max(x[1],y[1]),min(x[2],y[2]),min(x[3],y[3]));ok=r[0]<r[2] and r[1]<r[3];self.put(dst,*r) if ok else self.put(dst,0,0,0,0);self.ret(int(ok),12)
 def equal(self):
  a,b=self.args(2);self.ret(int(self.u.mem_read(a,16)==self.u.mem_read(b,16)),8)
 def descriptor(self):
  d=[0]*31;d[0]=124;d[2]=self.height;d[3]=self.width;d[4]=self.width*(self.depth//8);d[9]=self.canvas;return d
 def draw(self,data,x=2,y=2,align=0,depth=8):
  self.depth=depth;self.u.mem_write(self.canvas,bytes(self.width*self.height*(depth//8)));self.u.mem_write(self.strings,data+b'\0');self.run(0x43f390 if depth==8 else 0x43f5d0,(x,y,self.strings,255 if depth==8 else 65535,*self.descriptor(),align),self.obj)
  return bytes(self.u.mem_read(self.canvas,self.width*self.height*(depth//8)))

def glyph_index(data):
 if len(data)==1 and data[0]>32 and data[0]<=127:return data[0]-32
 if len(data)==2 and data[0]==0xa4 and data[1]>=0xa0:return 96+data[1]-0xa0
 if len(data)==2 and 0xb0<=data[0]<=0xc8 and data[1]>=0xa0:return 192+(data[0]-0xb0)*96+data[1]-0xa0
 return None

class AliasHarness(ScriptHarness):
 def __init__(self):
  super().__init__();self.stubs[0x100146f4]=self.substring;self.stubs[0x10014736]=self.mid;self.stubs[0x100146a0]=self.assign_bytes;self.stubs[0x1001464c]=self.format_int
  self.names=['용사'+str(i) for i in range(5)]
  for i,name in enumerate(self.names):
   self.u.mem_write(0x1001aa8e+412*i,struct.pack('<h',301+i));self.u.mem_write(0x1001ab24+412*i,name.encode('cp949')+b'\0')
  self.put(0x1006a73c,self.string('플레이어'.encode('cp949')))
 def substring(self):self.ret(self.bytes(self.u.reg_read(UC_X86_REG_ECX)).find(text(self,self.args(1)[0]))&0xffffffff,4)
 def mid(self):
  out,start,count=self.args(3);s=self.bytes(self.u.reg_read(UC_X86_REG_ECX));self.put(out,self.string(s[start:start+count]));self.ret(out,12)
 def assign_bytes(self):self.put(self.u.reg_read(UC_X86_REG_ECX),self.string(text(self,self.args(1)[0])));self.ret(clean=4)
 def format_int(self):
  out,fmt,value=self.args(3);assert text(self,fmt)==b'%d';value=value if value<2**31 else value-2**32;self.put(out,self.string(str(value).encode()));self.ret()
 def resolve(self,token,kind):
  out=0x30001000;self.run(0x10010690,(out,self.string(b'1'),self.string(token.encode('cp949')),kind),self.context);return self.bytes(out)

def main():
 checks=[];glyphs=[];widths=[];aliasrows=[];formatrows=[];callbackrows=[]
 def check(case,result):checks.append(dict(case=case,result=result,match=True))
 messages=list(csv.DictReader((OUT/'대사_원문표시연결.csv').open(encoding='utf-8-sig')))
 characters=sorted(set(''.join(r['text'] for r in messages if r['command']=='MESSAGE')))
 # Compare actual x86 raster writes with independently decoded glyph bits for all mapped source characters.
 for name in ('dark14.mxf','Dark14-1.mxf'):
  raw=(ROOT/name).read_bytes();h=FontHarness(raw)
  for ch in characters:
   data=ch.encode('cp949');index=glyph_index(data)
   if index is None:continue
   assert 0<=index<2592
   got=h.draw(data);expected=bytearray(h.width*h.height)
   for y in range(14):
    for x in range(14):
     if not(raw[index*32+y*2+x//8]&(128>>(x%8))):expected[(y+2)*h.width+x+2]=255
   assert got==expected,(name,ch,index)
   glyphs.append(dict(font=name,character=ch,cp949=data.hex(),glyph_index=index,source_offset=index*32,ink_pixels=sum(v!=0 for v in got)))
   check(f'glyph_{name}_{data.hex()}','original 8bit native glyph raster equals independent inverse-bit mask over entire framebuffer')
  for ch in ('A','가','힣','ㄱ'):
   data=ch.encode('cp949');index=glyph_index(data)
   if index is None:continue
   got=h.draw(data,depth=16);mask=h.draw(data,depth=8);expected=b''.join(b'\xff\xff' if v else b'\0\0' for v in mask);assert got==expected
   check(f'16bit_{name}_{data.hex()}','native 16bit glyph writes match 8bit bitmap mask withFFFF foreground')
  for ch in characters:
   if ord(ch)<=127 or glyph_index(ch.encode('cp949')) is not None:continue
   actual=h.draw(ch.encode('cp949'));fallback=h.draw(b'?');assert actual==fallback
   check(f'font_fallback_{name}_{ch.encode("cp949").hex()}','native unsupported CP949 character maps to question-mark glyph; no host-font fallback')
 # Text width is measured by the original font function, not a host font.
 h=FontHarness((ROOT/'dark14.mxf').read_bytes())
 for value,expected in (('A',7),('AB',14),(' ',5),('가',14),('ㄱ',14),('A 가',26),('A\n가',14),('가\nA',7)):
  h.u.mem_write(h.strings,value.encode('cp949')+b'\0');h.run(0x440520,(h.strings,),h.obj);got=h.u.reg_read(UC_X86_REG_EAX);assert got==expected,(value,got)
  widths.append(dict(text=value,width=got));check('width_'+repr(value),'native text width; ASCII7, space5, Korean14, newline returns last-line width')
 # Partial glyphs are rejected by EqualRect after intersection, rather than cropped pixel by pixel.
 for x,y in ((-1,2),(2,-1),(h.width-13,2),(2,h.height-13)):
  assert not any(h.draw('가'.encode('cp949'),x,y));check(f'clip_{x}_{y}','native glyph requires full rectangle inside descriptor; partially visible glyph omitted')
 # Callback last argument: hundreds select alignment; divisible by100 enables shadow pass.
 for depth in (8,16):
  for flags in (0,1,100,101,200,201):
   h=FontHarness((ROOT/'dark14.mxf').read_bytes());surface=0x30003000;h.put(0x451274,h.obj);h.put(h.obj,int(depth==8));h.depth=depth;h.put(surface+0x84,16);draws=[]
   def describe():
    h.put(h.args(1)[0],*h.descriptor());h.ret(clean=4)
   def capture():
    a=h.args(36);draws.append([a[0],a[1],a[3],a[-1]]);h.ret(clean=144)
   h.put(0x446040,0x300f0020);h.stubs[0x300f0020]=lambda:h.ret(255);h.put(0x44604c,0x300f0030);h.stubs[0x300f0030]=describe;h.put(0x446048,0x300f0040);h.stubs[0x300f0040]=lambda:h.ret(h.canvas);h.put(0x446044,0x300f0050);h.stubs[0x300f0050]=lambda:h.ret();h.stubs[0x43f390]=capture;h.stubs[0x43f5d0]=capture
   h.u.mem_write(h.strings,b'A\0');h.run(0x4292c0,(surface,100,50,h.strings,0xffffff,flags))
   align=0 if flags<100 else 6 if flags<200 else 2;expected=([[101,51,0,align]] if depth==16 and flags%100==0 else [])+[[100,50,255,align]]
   # 8bit branch does not submit the black shadow pass in the divisible-by100 arm.
   assert draws==expected,(flags,draws)
   callbackrows.append(dict(pixel_branch=depth,flags=flags,draws=json.dumps(draws)));check(f'font_callback_{depth}_{flags}','native registered text callback alignment flags mapped0/6/2; 16bit divisible-by100 flag adds black(1,1) shadow; 8bit path foreground only')
 # Alias tokens used by source: literal/COM/ME/ID, preserving substring quirks in Left(3).
 h=AliasHarness()
 for token in ('301','A','COM0','COM1','COM2','COM3','COM4','ME','ID','XME','XID','COM10'):
  for kind in (0,1):
   if token.startswith('COM'):i=int(token[3]);expected=h.names[i] if kind else str(301+i)
   elif 'ME' in token[:3] or 'ID' in token[:3]:expected='플레이어'
   else:expected=token
   actual=h.resolve(token,kind).decode('cp949');assert actual==expected,(token,kind,actual,expected)
   aliasrows.append(dict(token=token,kind=kind,actual=actual));check(f'alias_{token}_{kind}','native resolver: COM first suffix digit, kind0 signed ability short/kind1 inline name; ME/ID shared current CString')
 h.u.mem_write(0x1001aa8e,struct.pack('<h',-1));assert h.resolve('COM0',0)==b'-1';check('alias_signed_short','native COM numeric result preserves signed short -1')
 # The shared ME/ID name is updated only for the matching ID in the received name-list path.
 for incoming in (7,8):
  h=AliasHarness();h.put(0x1006a738,7);h.u.mem_write(0x30002000,'목록이름'.encode('cp949')+b'\0');h.u.reg_write(UC_X86_REG_ESI,0x30002000);h.u.reg_write(UC_X86_REG_ECX,incoming);h.stubs[0x1000d4fa]=lambda:h.ret()
  # Harness.run sets ECX, so supply the fixture ID as its this register.
  h.u.reg_write(UC_X86_REG_EAX,7);h.run(0x1000d4eb,this=incoming)
  expected='목록이름' if incoming==7 else '플레이어'
  assert h.resolve('ME',1).decode('cp949')==expected and h.resolve('ID',0).decode('cp949')==expected
  check(f'name_list_cache_{incoming}','native5C60 matching-ID copy fragment updates shared ME/ID CString; full packet handler not rerun')
 # Capture actual argument bytes at Format import for every %s/%S source occurrence.
 for source in messages:
  if source['command']!='MESSAGE' or not ('%s' in source['text'] or '%S' in source['text']):continue
  h=AliasHarness();captured=[]
  def capture_format():
   out,pattern,*args=h.args(5);template=text(h,pattern)
   if template==b'%d':h.format_int();return
   captured.append(dict(template_hex=template.hex(),args_hex=[text(h,p).hex() for p in args]));h.put(out,h.string(b'captured'));h.ret()
  h.stubs[0x1001464c]=capture_format;h.stubs[0x100086d0]=lambda:h.ret(clean=20);h.stubs[0x10008930]=lambda:h.ret(clean=12)
  body=' '.join(json.loads(source['fields']))+' '+source['text'];h.run(0x100108b0,(h.string(body.encode('cp949')),),h.context)
  assert len(captured)==1,(source,captured)
  formatrows.append(dict(archive=source['archive'],file=source['file'],line=source['line'],format=source['text'],arguments_hex=json.dumps(captured[0]['args_hex']),format_import='MFC42 ordinal2818 via1001464C/100160E0; final runtime unavailable',utf16le_units_hypothesis=json.dumps([[hex(int.from_bytes(bytes.fromhex(v)[i:i+2].ljust(2,b'\0'),'little')) for i in range(0,len(bytes.fromhex(v)),2)] for v in captured[0]['args_hex']]),classification='uppercase S kept; final Windows runtime conversion not executed' if '%S' in source['text'] else 'narrow s; actual argument bytes captured under named fixture'))
  check(f'format_capture_{source["archive"]}_{source["file"]}_{source["line"]}','native full MESSAGE parser and alias resolver up to actual Format call; output formatter captured, not substituted with Linux printf')
 # Isolated real display mode setup fragment; do not run OS or create a real window.
 display_modes=[]
 for windowed in (0,1):
  m=Harness('Saver.exe');obj=0x30001000;dd=0x30002000;m.put(obj+0xd8,windowed);m.put(obj+0x20,123);m.put(obj+0x11c,dd);m.put(dd+0xc,0x30003000);got=[]
  def set_mode():
   sp=m.u.reg_read(UC_X86_REG_ESP);got.append([m.get(sp+4+i*4) for i in range(5)]);m.ret(1,20)
  m.put(0x4460b4,0x300f0000);m.stubs[0x300f0000]=set_mode;m.put(0x44606c,0x300f0010);m.stubs[0x300f0010]=lambda:m.ret(clean=4);m.stubs[0x43a6b6]=lambda:m.ret();m.u.reg_write(UC_X86_REG_ESI,obj);m.u.reg_write(UC_X86_REG_EBX,0);m.run(0x43a616)
  assert got==[[123,640,480,0,0] if windowed else [123,640,480,16,1]]
  display_modes.append(dict(windowed=windowed,args=json.dumps(got)));check(f'display_mode_{windowed}','native display-mode fragment requests640x480; fullscreen16bit; windowed depth0; DirectDraw setup captured')
 write('원작_화면모드_원본대조.csv',display_modes)
 # Render original font pixels into a clearly labeled software test surface.
 h=FontHarness((ROOT/'dark14.mxf').read_bytes());h.width=640;h.height=140;h.draw('원본 글꼴 출력 검증 ABC 123'.encode('cp949'),20,20);canvas=bytearray(h.u.mem_read(h.canvas,640*140))
 for y,line in ((50,'안녕 용사여. 열쇠를 얻었습니다.'),(80,'픽셀 재구성 - 원작 실행 화면 아님')):
  pixels=h.draw(line.encode('cp949'),20,y)
  for i,v in enumerate(pixels):
   if v:canvas[i]=v
 image=Image.frombytes('L',(640,140),bytes(canvas)).convert('RGB');image.save(OUT/'글꼴_원본픽셀_재구성.png')
 environment=dict(tools={name:shutil.which(name) for name in ('wine','wine64','qemu-system-i386','Xvfb')},bundled_MFC42=list(str(p.relative_to(BASE)) for p in (BASE/'설치본_추출').rglob('*') if p.is_file() and p.name.lower() in ('mfc42.dll','msvcrt.dll')),scope='Read-only availability check; no original app/OS install/run attempted')
 (OUT/'원작_재생환경점검.json').write_text(json.dumps(environment,ensure_ascii=False,indent=2)+'\n')
 write('글꼴별칭_원본명령대조.csv',checks);write('글꼴_원본문자픽셀.csv',glyphs);write('글꼴_문자폭.csv',widths);write('글꼴_콜백정렬.csv',callbackrows);write('별칭_원본해석.csv',aliasrows);write('대사_포맷원시인수.csv',formatrows)
 result=dict(cases=len(checks),mismatches=0,native_glyph_pixel_cases=len(glyphs),unique_mapped_source_characters=len(glyphs)//2,unmapped_source_characters=[c for c in characters if ord(c)>127 and glyph_index(c.encode('cp949')) is None],native_format_argument_cases=len(formatrows),uppercase_S_cases=sum('%S' in r['format'] for r in formatrows),scope='Native bitmap-font raster bytes, alias resolver and MESSAGE arguments. External CString/surface APIs modeled. Final MFC42 Format, OS full-screen playback, palette and timing not executed.',environment=environment,binary_sha256={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in ('Saver.exe','GameCtl.dll')},font_sha256={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in ('dark14.mxf','Dark14-1.mxf')})
 (OUT/'글꼴별칭검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
 ranges=[('Saver.exe',0x4292c0,0x4294a8),('Saver.exe',0x43ef00,0x43f070),('Saver.exe',0x43f390,0x43f80b),('Saver.exe',0x4402e0,0x44059e),('Saver.exe',0x43a616,0x43a6b6),('GameCtl.dll',0x1000d470,0x1000d510),('GameCtl.dll',0x10010690,0x10010860)]
 (OUT/'글꼴별칭_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/n)],text=True) for n,a,b in ranges))
if __name__=='__main__':main()
