"""Presentation command boundaries, signed clock waits, native frame functions and asset preview.
Controlled clocks and captured display/audio APIs; preview is reconstruction, not gameplay.
"""
import collections,csv,hashlib,json,struct,subprocess
from PIL import Image,ImageDraw
from unicorn.x86_const import UC_X86_REG_EAX,UC_X86_REG_ESP,UC_X86_REG_ECX
from analyze_special_skills import BASE,ROOT,OUT,write
from analyze_script_executor import ScriptHarness
from analyze_packet_requests import Harness

def main():
 checks=[];clock_rows=[];calls=[];sources=[]
 def check(case,detail):checks.append(dict(case=case,result=detail,match=True))
 # Real scene-step instructions including signed comparison after DWORD subtraction.
 for start in (1000,0xfffffff0):
  for duration in (0,1,100,1000):
   for elapsed in sorted({0,max(0,duration-1),duration,duration+1,0x7fffffff,0x80000000}):
    h=ScriptHarness();h.now=start;h.lines([f'DO {duration}','END']);h.step();h.now=(start+elapsed)&0xffffffff;cursor=h.step()
    signed=elapsed if elapsed<0x80000000 else elapsed-0x100000000
    assert (cursor==3)==(signed>=duration),(start,duration,elapsed,cursor)
    clock_rows.append(dict(start=start,duration=duration,elapsed_u32=elapsed,elapsed_i32=signed,cursor=cursor,released=cursor==3))
    check(f'wait_{start}_{duration}_{elapsed}','native DO uses signed int32(now-start) >= duration; wrapping near DWORD end tested')
 for actor in (-1,0):
  h=ScriptHarness();h.lines(['DO 1000','END']);h.put(h.context+0x14,actor&0xffffffff);h.step();h.step();assert (h.get(h.context+0x34)==3)==(actor<0)
  check(f'negative_actor_wait_{actor}','negative related actor bypasses wait gate; not a universal enforced scene delay')
 for moving in (0,1):
  h=ScriptHarness();h.lines(['DO2 7','END']);h.step();h.moving=moving;h.step();assert h.get(h.context+0x34)==2 and h.get(h.context+0x20)==moving
  if not moving:assert h.step()==3
  check(f'move_release_{moving}','DO2 movement clear returns once before next command; elapsed time alone does not release')
 for pause in (0,1):
  h=ScriptHarness();h.lines(['DO 100','END']);h.put(0x1001c4d4,pause);h.step();assert h.get(h.context+0x34)==(1 if pause else 2)
  check(f'global_pause_{pause}','global1001C4D4 holds cursor after initialization; no command consumed')
 # Dialog entry: render helper captured while actual dispatch sets the waiting fields.
 for busy in (0,1):
  h=ScriptHarness();h.lines(['MESSAGE 1 399 0 A 안녕|용사여','END']);render=[]
  h.stubs[0x100029e0]=lambda:h.ret(busy)
  def message():render.append(bytes(h.u.mem_read(h.args(1)[0],80)).split(b'\0')[0].decode('cp949'));h.ret(clean=4)
  h.stubs[0x100108b0]=message;h.step()
  assert render==([] if busy else ['1 399 0 A 안녕|용사여'])
  assert h.get(h.context+0x1c)==(0 if busy else 1) and h.get(h.context+0x30)==(0 if busy else 1)
  check(f'message_entry_{busy}','native MESSAGE dispatch: busy query and render helper capture; context+1C/+30 latch; text layout not emulated')
 # All installed source commands of interest, not uppercase-normalized copies.
 root=BASE/'설치본_추출/ARC_해제/MAP'
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  for path in sorted(folder.iterdir()):
   if path.suffix.lower() not in ('.evt','.deo','.dee'):continue
   for line,raw in enumerate(path.read_bytes().decode('cp949').splitlines(),1):
    tokens=raw.split(';',1)[0].strip().split()
    if tokens and tokens[0] in ('CHARDIR','CAHRDIR','PLAYMIDI','STOPMIDI','MOVEPOSITION','SETPOSITION'):
     sources.append(dict(archive=folder.name,file=path.name,line=line,command=tokens[0],raw=raw))
 for raw in sorted(set(r['raw'].strip() for r in sources if r['command']=='PLAYMIDI')|{'PLAYMIDI bgm\\test.mid','PLAYMIDI bgm\\test.mid 0','PLAYMIDI bgm\\test.mid 2'}):
  h=ScriptHarness();h.lines([raw,'END']);got=[]
  def play():
   p,mode=h.args(2);got.append((bytes(h.u.mem_read(p,128)).split(b'\0')[0].decode('cp949'),mode));h.ret()
  h.put(0x100162f4,0x300f0900);h.stubs[0x300f0900]=play;h.step();t=raw.split();expected=(t[1],int(t[2]) if len(t)>2 else 1);assert got==[expected],(raw,got)
  calls.append(dict(command=raw,api='PlayMidi',args=json.dumps(got,ensure_ascii=False)))
  check('midi_'+raw,'native token parser forwards filename and mode; absent mode defaults1; playback API captured')
 for raw in sorted(set(r['raw'].strip() for r in sources if r['command'] in ('CHARDIR','CAHRDIR'))):
  h=ScriptHarness();h.lines([raw,'END']);assert h.step()==2
  calls.append(dict(command=raw,api='none in main step',args='[]'))
  check('source_direction_'+raw,'native main step consumes source CHARDIR/CAHRDIR; no direction command dispatch identified; no actor renderer entered')
 for name in ('SETPOSITION','MOVEPOSITION'):
  h=ScriptHarness();h.lines([name+' 19 7','END']);got=[]
  def position():got.append(h.args(2));h.ret()
  h.stubs[0x10003c70]=position;h.step();assert got==[[19,7]]
  calls.append(dict(command=name+' 19 7',api='GameCtl10003C70',args=json.dumps(got)))
  check('camera_'+name,'same native target-camera helper; no interpolated camera operation in this dispatch')
 # Sound DLL PlayMidi wrapper: CString/file check/DirectMusic final call modeled.
 audio=[]
 for enabled in (0,1):
  for exists in (0,1):
   for mode in (0,1,2,3):
    a=Harness('soundll.dll');a.put(0x10006028,enabled);a.u.mem_write(0x100060a4,b'\x01');a.put(0x1000602c,77)
    for addr,data in ((0x30001000,b'bgm\\new.mid'),(0x30001100,b'bgm\\current.mid'),(0x30001200,b'bgm\\previous.mid')):a.u.mem_write(addr,data+b'\0')
    a.put(0x100060ac,0x30001100);a.put(0x100060a0,0x30001200);played=[]
    def args(n):return [a.get(a.u.reg_read(UC_X86_REG_ESP)+4+i*4) for i in range(n)]
    def string(pointer):return bytes(a.u.mem_read(pointer,128)).split(b'\0')[0].decode()
    def ctor():a.put(a.u.reg_read(UC_X86_REG_ECX),args(1)[0]);a.ret(clean=4)
    def assign():a.put(a.u.reg_read(UC_X86_REG_ECX),a.get(args(1)[0]));a.ret(clean=4)
    def play_music():played.append((string(args(2)[0]),args(2)[1]));a.ret(1,8)
    a.put(0x1000309c,0x300f0900)
    a.stubs={0x10002678:ctor,0x1000265a:assign,0x10002672:ctor,0x10002666:lambda:a.ret(),0x1000264e:a.alloc,0x10002648:lambda:a.ret(),0x300f0900:lambda:a.ret(0 if exists else 0xffffffff),0x10001b00:play_music}
    a.run(0x100021d0,(0x30001000,mode));target='bgm\\previous.mid' if mode==3 else 'bgm\\new.mid'
    assert played==([(target,3)] if enabled and exists else []),(enabled,exists,mode,played)
    assert a.u.reg_read(UC_X86_REG_EAX)==1
    assert a.get(0x1000602c)==((1 if mode==3 else mode) if enabled and exists else 77)
    assert string(a.get(0x100060a0))==('bgm\\current.mid' if enabled and mode==2 else 'bgm\\previous.mid')
    audio.append(dict(bgm_enabled=enabled,file_exists=exists,mode=mode,played=json.dumps(played),saved_mode=a.get(0x1000602c),return_value=1))
    check(f'audio_mode_{enabled}_{exists}_{mode}','native soundll wrapper mode0/1/2/3, file/BGM gate and return1 verified; DirectMusic call captured')
 midi_inventory=[];midi_paths={str(p.relative_to(ROOT)).replace('\\','/').lower():p for p in (ROOT/'BGM').iterdir() if p.is_file()}
 for row in sources:
  if row['command']!='PLAYMIDI':continue
  token=row['raw'].split()[1];path=midi_paths.get(token.replace('\\','/').lower());midi_inventory.append(dict(**row,filename=token,installed_file_found=bool(path),asset_sha256=hashlib.sha256(path.read_bytes()).hexdigest() if path else ''))
 write('연출_MIDI자산연결.csv',midi_inventory);write('연출_MIDI모드검증.csv',audio)
 # Three time-based frame selectors are independent of the number of render calls.
 h=Harness('userunit.dll');h.put(0x10012068,0x300f0900);now=0;h.stubs[0x300f0900]=lambda:h.ret(now)
 frame_rows=[]
 for period in (100,150,333,1000):
  times=sorted({0,period-1,period,period+1,2*period-1,2*period,3*period,4*period,100000,0xffffffff})
  for now in times:
   for entry,expected in ((0x1000ac40,(now//period)&1),(0x1000ac50,(now//period)%3),(0x1000ac70,[0,1,2,1][(now//period)&3])):
    h.run(entry,(period,));actual=h.u.reg_read(UC_X86_REG_EAX);assert actual==expected
    frame_rows.append(dict(entry=hex(entry),period_ms=period,time_ms=now,frame=actual));check(f'frame_{entry:x}_{period}_{now}','native unsigned GetTickCount division and frame cycle matched')
 # Original body renderer -> exact source rectangles -> actual extracted BMPs, under controlled time.
 draws=[];preview_frames=[];specimens=(301,700)
 images={}
 for display in specimens:
  matches=[p for d in (BASE/'설치본_추출/ARC_해제/gameres').glob('char*.arc') for p in d.iterdir() if p.name.lower()==f'{display}r.bmp']
  assert len(matches)==1,(display,matches);images[display]=(matches[0],Image.open(matches[0]).convert('RGB'))
 for time in (0,332,333,665,666,998,999,1331,1332):
  for display in specimens:
   u=Harness('userunit.dll');b=0x30010000;u.put(0x1010a3a0,b);u.put(b+0x68,display);u.put(b+0x1c,2);u.put(b+0x148,3);u.put(b+0x100,0)
   u.put(0x100e328c,0,0);u.put(0x10015108,20,15);u.put(0x1010a3d8,1);u.put(0x1010a3dc,0);u.put(0x100df40c+16*display,0x1111);u.put(0x1010a3c0,0x2222)
   captured=[]
   def rect():
    sp=u.u.reg_read(UC_X86_REG_ESP);p,l,t,r,bt=[u.get(sp+4+i*4) for i in range(5)];u.put(p,l,t,r,bt);u.ret(1,20)
   def draw():
    sp=u.u.reg_read(UC_X86_REG_ESP)
    if u.get(sp+12)==0x1111:captured.append(struct.unpack('<4i',u.u.mem_read(u.get(sp+16),16)))
    u.ret(1,20)
   u.put(0x10012068,0x300f0900);u.put(0x10012144,0x300f0910)
   u.stubs={0x10004f30:lambda:u.ret(0),0x1000ad00:lambda:u.ret(0),0x300f0900:lambda:u.ret(time),0x300f0910:rect,0x1001176e:draw};u.run(0x1000b5d0,(0,8,6))
   size=64 if display==700 else 32;col=[0,1,2,1][(time//333)&3];expected=(col*size,0,(col+1)*size,size);assert captured==[expected]
   path,img=images[display];assert expected[2]<=img.width and expected[3]<=img.height
   draws.append(dict(display=display,time_ms=time,rect=json.dumps(captured[0]),asset=str(path.relative_to(BASE)),asset_sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
   check(f'body_asset_{display}_{time}','native body source rectangle matches selected BMP crop at real clock boundary; terrain0/facing3')
 sheet=Image.new('RGB',(640,320),(235,235,235));pen=ImageDraw.Draw(sheet);pen.text((10,8),'RECONSTRUCTION - native frame selection / original BMP crops',fill='black')
 for row,display in enumerate(specimens):
  for col,time in enumerate((0,333,666,999)):
   r=next(r for r in draws if r['display']==display and r['time_ms']==time);box=json.loads(r['rect']);img=images[display][1].crop(box).resize((96,96),Image.Resampling.NEAREST);x,y=10+col*155,35+row*140;sheet.paste(img,(x,y));pen.text((x,y+100),f'ID{display} t={time}ms',fill='black')
 sheet.save(OUT/'연출_본체프레임_재구성.png')
 for time in (0,333,666,999):
  frame=Image.new('RGB',(320,180),(235,235,235));p=ImageDraw.Draw(frame);p.text((6,5),'RECONSTRUCTION / NOT GAMEPLAY',fill='black')
  for index,display in enumerate(specimens):
   r=next(r for r in draws if r['display']==display and r['time_ms']==time);img=images[display][1].crop(json.loads(r['rect'])).resize((96,96),Image.Resampling.NEAREST);frame.paste(img,(10+index*155,35));p.text((10+index*155,140),f'ID{display} t={time}',fill='black')
  preview_frames.append(frame)
 preview_frames[0].save(OUT/'연출_본체프레임_재구성.gif',save_all=True,append_images=preview_frames[1:],duration=333,loop=0)
 gif_delay=Image.open(OUT/'연출_본체프레임_재구성.gif').info['duration']
 write('연출_시간검증.csv',checks);write('연출_DO시계경계.csv',clock_rows);write('연출_명령전달.csv',calls);write('연출_원문명령.csv',sources);write('연출_프레임선택.csv',frame_rows);write('연출_본체자산연결.csv',draws)
 ranges=[('GameCtl.dll',0x10010edc,0x10011111),('GameCtl.dll',0x10012ca7,0x10013199),('userunit.dll',0x1000ac40,0x1000ac90),('soundll.dll',0x100021d0,0x1000235f),('soundll.dll',0x10002430,0x100024d0),('Saver.exe',0x402005,0x402024),('Saver.exe',0x43dcb6,0x43dcc7)]
 (OUT/'연출_시간그래픽_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/name)],text=True) for name,a,b in ranges))
 result=dict(cases=len(checks),mismatches=0,source_command_counts=dict(collections.Counter(r['command'] for r in sources)),clock_cases=len(clock_rows),frame_selector_cases=len(frame_rows),native_body_asset_cases=len(draws),audio_mode_cases=len(audio),missing_midi_assets=sorted({r['filename'] for r in midi_inventory if not r['installed_file_found']}),gif_encoded_delay_ms=gif_delay,preview='Original BMP crops selected by native body renderer at controlled times. GIF delay is centisecond-quantized (see gif_encoded_delay_ms); not original gameplay/video, DirectDraw palette/transparency or measured scheduler cadence.',scope='Original isolated x86 execution; CString/clock/audio/display/system APIs modeled or captured. No server/Windows app or actual MIDI playback.',binary_sha256={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('GameCtl.dll','userunit.dll','Saver.exe','soundll.dll')})
 (OUT/'연출_시간그래픽검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
