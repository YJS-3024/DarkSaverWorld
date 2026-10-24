"""Original dialogue line splitting, layout, page advance and notice timeout.
Text renderer, portrait lookup and CString primitives are modeled/captured; no Windows UI.
"""
import collections,hashlib,json,re,struct,subprocess
from unicorn.x86_const import UC_X86_REG_EAX,UC_X86_REG_ECX
from analyze_special_skills import BASE,ROOT,OUT,write
from analyze_script_executor import ScriptHarness

def text(h,p):return bytes(h.u.mem_read(p,1024)).split(b'\0')[0]
def split_native(value):
 h=ScriptHarness();obj=0x30002000;h.run(0x10008780,(h.string(value.encode('cp949')),),obj);n=h.u.reg_read(UC_X86_REG_EAX)
 assert n<=100,(value,n)
 return [h.bytes(obj+0x1c+4*i).decode('cp949') for i in range(n)]
def main():
 checks=[];inventory=[];splitrows=[];layouts=[];pages=[];formats=[]
 def check(case,result):checks.append(dict(case=case,result=result,match=True))
 fixtures={'':[],'a':['a'],'a|b':['a','b'],'a||b':['a','','b'],'|a':['','a'],'a|':['a'],'a||':['a',''],'|':[''],' 안녕 | 용사여 ':['안녕','용사여'],'a\tb':['a\tb']}
 for raw,expected in fixtures.items():
  got=split_native(raw);assert got==expected,(raw,got,expected);check('split_'+repr(raw),'native pipe splitting preserves interior blank rows and trims ASCII boundary spaces; trailing delimiter is not extra final row')
 root=BASE/'설치본_추출/ARC_해제/MAP'
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  for p in sorted(folder.iterdir()):
   if p.suffix.lower() not in ('.evt','.deo','.dee'):continue
   event=None
   for number,raw in enumerate(p.read_bytes().decode('cp949').splitlines(),1):
    code=raw.split(';',1)[0].strip();t=code.split(None,5)
    if t and t[0]=='EVENT':event=t[1]
    if not t or t[0] not in ('MESSAGE','MASSAGE','MESSGE'):continue
    fields=t[1:5];body=t[5] if len(t)>5 else '';parts=split_native(body)
    row=dict(archive=folder.name,file=p.name,line=number,event=event,command=t[0],fields=json.dumps(fields,ensure_ascii=False),text=body,native_split_lines=json.dumps(parts,ensure_ascii=False),line_count=len(parts),three_line_pages=(len(parts)+2)//3 if fields and fields[0] in ('1','2') else None,layout_path='portrait lines' if fields and fields[0] in ('1','2') else 'notice stores whole formatted text; standalone split here is diagnostic',format_tokens=json.dumps(re.findall(r'%(?:[-+0-9.*]*[A-Za-z%])',body)))
    inventory.append(row)
    if t[0]=='MESSAGE':splitrows.append(row);check(f'source_split_{folder.name}_{p.name}_{number}','all source text passed through original pipe splitter; no printf alias/name replacement or global event order assumed')
 # Dialogue setup and page advance run original methods, portrait API only captured.
 for mode in (1,2):
  for count in (0,1,2,3,4,6,7):
   for special in (0,1):
    h=ScriptHarness();o=0x30002000;body='|'.join('행'+str(i+1) for i in range(count));portraits=[]
    def portrait():portraits.append(h.args(4));h.ret()
    h.put(0x1001626c,0x300f0900);h.stubs[0x300f0900]=portrait;h.put(0x1006c118,special);h.run(0x10008930,(mode,399,h.string(body.encode('cp949'))),o)
    initial=3 if special else 0
    assert [h.get(o+k) for k in (8,12,16,20)]==[40,50 if mode==1 else 300,561,104]
    assert h.get(o+0x1b4)==count and h.get(o+0x1b0)==(0 if special and initial>=count else initial)
    assert portraits and portraits[0][1:]==[399,0,0]
    assert h.get(o+4)==(0 if special and initial>=count else 1)
    check(f'setup_{mode}_{count}_{special}','original portrait dialogue setup: mode1 top y50, mode2 lower y300; three-row paging; global special behavior')
    layouts.append(dict(mode=mode,line_count=count,special=special,rect=json.dumps([h.get(o+k) for k in (8,12,16,20)]),first_line=h.get(o+0x1b0),active=h.get(o),waiting=h.get(o+4),portrait_args=json.dumps(portraits)))
    if not special:
     n=0
     while h.get(o) and n<5:
      n+=1;h.run(0x10008be0,this=o);pages.append(dict(mode=mode,line_count=count,advance=n,first_line=h.get(o+0x1b0),active=h.get(o),waiting=h.get(o+4)))
     assert not h.get(o) and n==max(1,(count+2)//3)
     check(f'pages_{mode}_{count}','native advances by three source lines; closes at >= total; empty source closes on first advance')
 # Draw calls and 10-second notice expiry. No font rasterization is inferred.
 for kind in (0,1):
  for elapsed in (0,9999,10000,10001):
   h=ScriptHarness();o=0x30002000;h.put(o,1,1,40,130,561,65);h.put(o+0x1ac,kind);h.put(o+0x1bc,1000);h.now=1000+elapsed
   for i,s in enumerate(('첫째','둘째','셋째')):h.put(o+0x1c+4*i,h.string(s.encode('cp949')))
   h.put(o+0x1b0,0);h.put(o+0x1b4,3);drawn=[]
   def font():
    a=h.args(6);drawn.append([a[1],a[2],text(h,a[3]).decode('cp949'),a[4],a[5]]);h.ret()
   def rect():
    dst,l,t,r,b=h.args(5);h.put(dst,l,t,r,b);h.ret(clean=20)
   h.stubs[0x10008d50]=lambda:h.ret();h.stubs[0x100145d4]=lambda:h.ret(clean=20);h.put(0x100161cc,0x300f0910);h.stubs[0x300f0910]=rect;h.put(0x1001c450,0x300f0920);h.stubs[0x300f0920]=font
   h.run(0x10008a10,this=o)
   if kind==0:assert drawn==[[320,153,'첫째',0xffffff,100]] and h.get(o)==int(elapsed<=10000)
   else:assert drawn==[[150,153,'첫째',0xffffff,0],[150,173,'둘째',0xffffff,0],[150,193,'셋째',0xffffff,0]] and h.get(o)==1
   check(f'draw_timeout_{kind}_{elapsed}','notice center/text API mode100 expires strictly after10000ms; portrait draws three white rows20px apart with API mode0')
   layouts.append(dict(mode='notice' if kind==0 else 'portrait_draw',line_count=3,special=0,rect=json.dumps([40,130,561,65]),first_line=h.get(o+0x1b0),active=h.get(o),waiting=h.get(o+4),portrait_args=json.dumps(drawn,ensure_ascii=False)))
 # Full MESSAGE parser and literal actor/name resolver to expose field ordering/format args.
 for mode in (0,1,2,3):
  h=ScriptHarness();o=0x30002000;h.put(0x1006a708,o);captured=[]
  def fmt():
   out,pattern,*args=h.args(5);template=text(h,pattern);values=[text(h,p) for p in args];n=template.count(b'%s');assert n<=3
   result=template%tuple(values[:n]) if n else template;h.put(out,h.string(result));captured.append(dict(template=template.decode('cp949'),args=[v.decode('cp949') for v in values],result=result.decode('cp949')));h.ret()
  def portrait():h.ret()
  def find_substring():
   obj=h.u.reg_read(UC_X86_REG_ECX);needle=text(h,h.args(1)[0]);h.ret(h.bytes(obj).find(needle)&0xffffffff,4)
  h.stubs[0x100146f4]=find_substring;h.stubs[0x1001464c]=fmt;h.put(0x1001626c,0x300f0900);h.stubs[0x300f0900]=portrait
  raw=f'{mode} 399 700 A 안녕|%s'
  h.run(0x100108b0,(h.string(raw.encode('cp949')),),h.context)
  assert bool(captured)==(mode<3)
  if mode<3:assert captured[0]['args']==['700','A',str(mode)] and captured[0]['result']=='안녕|700',(mode,captured)
  formats.append(dict(mode=mode,raw=raw,format_capture=json.dumps(captured,ensure_ascii=False),dialogue_kind=h.get(o+0x1ac),geometry=json.dumps([h.get(o+k) for k in (8,12,16,20)])))
  check(f'message_fields_{mode}','native four-header parser: mode0 notice,1/2 portrait, unknown3 no presentation branch; native alias resolver exercised for literal tokens; CString substring primitive modeled')
 # Actual upper caller repeats scene steps while they return0, within one update.
 for active,sequence in ((0,[1]),(1,[1]),(1,[0,1]),(1,[0,0,1])):
  h=ScriptHarness();h.put(0x10093864,h.context);h.put(h.context+0xc,active);h.put(0x1001c4e4,10);got=[]
  def step():
   value=sequence[len(got)];got.append(value);h.ret(value)
  h.stubs[0x10010de0]=step;h.stubs[0x10003b54]=lambda:h.ret();h.run(0x10003b29)
  assert got==(sequence if active else []) and h.get(0x1001c4e4)==11
  check(f'caller_repeat_{active}_{sequence}','native upper update calls scene step repeatedly on return0, stops on1; one update is not necessarily one command')
 write('대사_원본명령대조.csv',checks);write('대사_원문표시연결.csv',inventory);write('대사_배치타이밍.csv',layouts);write('대사_페이지진행.csv',pages);write('대사_인수치환.csv',formats)
 ranges=[(0x10008780,0x10008c5c),(0x100108b0,0x10010de0),(0x10003b14,0x10003b54),(0x10002da1,0x10002dfc)]
 (OUT/'대사_실행근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'GameCtl.dll')],text=True) for a,b in ranges))
 result=dict(cases=len(checks),mismatches=0,source_message_counts=dict(collections.Counter(r['command'] for r in inventory)),native_source_split_cases=len(splitrows),max_source_lines=max(r['line_count'] for r in splitrows),source_mode_counts=dict(collections.Counter(json.loads(r['fields'])[0] if json.loads(r['fields']) else '' for r in splitrows)),scope='Original dialogue split/setup/page/draw/timeout and MESSAGE header/format path; CString/font/portrait/window APIs modeled or captured; literal resolver path executed. No actual font raster, full COM/ME/ID resolver semantics, input routing or original Windows playback.',binary_sha256=hashlib.sha256((ROOT/'GameCtl.dll').read_bytes()).hexdigest())
 (OUT/'대사_추가검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
