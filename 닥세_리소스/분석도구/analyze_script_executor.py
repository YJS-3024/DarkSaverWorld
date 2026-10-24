"""Native scene-script tokenization and step/wait/control flow with explicit CString models."""
import collections,hashlib,json,re,struct,subprocess
from unicorn.x86_const import UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EAX
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,BASE,PE,write
class ScriptHarness(Harness):
 def __init__(self):
  super().__init__('GameCtl.dll');self.now=1000;self.moving=0;self.test_value=0;self.random=0;self.effects=[]
  self.empty=self.string(b'');self.stubs={0x10014652:self.construct,0x10014622:self.copy,0x10014628:self.copy,0x1001462e:lambda:self.ret(),0x100145b6:lambda:self.ret(),0x100145e0:lambda:self.ret(),0x100146e2:self.find,0x100146dc:lambda:self.part(False),0x10014700:lambda:self.part(True),0x100146e8:lambda:self.trim(True),0x10014706:lambda:self.trim(False),0x100146b2:self.construct,0x1000bd30:lambda:self.effect('scene_done'),0x1000ef80:lambda:self.effect('event_done'),0x1000e670:lambda:self.effect('send5B')}
  for iat,stub,fn in [(0x1001618c,0x300f0000,self.atoi),(0x1001609c,0x300f0010,lambda:self.ret(self.now)),(0x100162ac,0x300f0020,self.unit),(0x10016198,0x300f0030,lambda:self.ret(self.random)),(0x1001619c,0x300f0040,lambda:self.ret()),(0x100161a0,0x300f0050,lambda:self.ret(1234)),(0x100162f0,0x300f0060,lambda:self.effect('end_mode')),(0x100161d0,0x300f0070,lambda:self.effect('post_message',16))]:
   self.put(iat,stub);self.stubs[stub]=fn
  self.stubs[0x10010440]=lambda:self.ret(self.test_value,4)
  self.context=0x30000000;self.put(0x1006a708,0x30006000);self.put(self.context+4,2,0,1,0,0)
 def string(self,data):
  p=self.heap+12;self.heap+=((len(data)+13+15)//16)*16;self.put(p-12,1,len(data),len(data));self.u.mem_write(p,data+bytes(1));return p
 def bytes(self,obj):
  p=self.get(obj);return bytes(self.u.mem_read(p,self.get(p-8)))
 def args(self,n):return [self.get(self.u.reg_read(UC_X86_REG_ESP)+4+4*i) for i in range(n)]
 def construct(self):self.put(self.u.reg_read(UC_X86_REG_ECX),self.empty);self.ret()
 def copy(self):
  obj=self.u.reg_read(UC_X86_REG_ECX);src=self.args(1)[0];self.put(obj,self.get(src));self.ret(obj,4)
 def find(self):
  obj=self.u.reg_read(UC_X86_REG_ECX);v=self.bytes(obj).find(bytes([self.args(1)[0]&255]));self.ret(v&0xffffffff,4)
 def part(self,right):
  obj=self.u.reg_read(UC_X86_REG_ECX);out,n=self.args(2);n=n if n<2**31 else 0;data=self.bytes(obj);data=(data[-n:] if n else b'') if right else data[:n]
  self.put(out,self.string(data));self.ret(out,8)
 def trim(self,left):
  obj=self.u.reg_read(UC_X86_REG_ECX);s=self.bytes(obj);self.put(obj,self.string(s.lstrip() if left else s.rstrip()));self.ret()
 def atoi(self):
  p=self.args(1)[0];data=bytes(self.u.mem_read(p,128)).split(bytes(1))[0];m=re.match(rb'\s*([+-]?\d+)',data);self.ret((int(m[1]) if m else 0)&0xffffffff)
 def unit(self):self.put(0x30007100,self.moving);self.ret(0x30007000)
 def effect(self,name,clean=0):self.effects.append(name);self.ret(clean=clean)
 def lines(self,lines):
  for i,line in enumerate(lines,1):self.put(self.context+0x50+4*i,self.string(line.encode('cp949')))
  self.put(self.context+0x34,0)
 def step(self):
  self.run(0x10010de0,this=self.context)
  return self.get(self.context+0x34)

def main():
 rows=[]
 for text,head,tail in [(b'CHARPOS 1 2',b'CHARPOS',b'1 2'),(b'DO    100 ',b'DO',b'100'),(b'END',b'END',b''),(b'',b'',b''),(b' MESSAGE hi',b'',b'MESSAGE hi'),(b'CHARPOS\t1',b'CHARPOS\t1',b''),('MESSAGE 용사여  안녕'.encode('cp949'),b'MESSAGE','용사여  안녕'.encode('cp949'))]:
  for entry,expected in [(0x1000f020,head),(0x1000f220,tail)]:
   h=ScriptHarness();p=h.string(text);h.run(entry,(0x30001000,p));assert h.bytes(0x30001000)==expected
   rows.append(dict(case=f'token_{entry:x}_{text.hex()}',result='native ASCII-space split and trim match; CP949 preserved',match=True))
 h=ScriptHarness();h.lines(['DO 100','END']);assert h.step()==2 and h.get(h.context+0x20)==1 and h.get(h.context+0x24)==100
 for elapsed,advance in [(0,False),(99,False),(100,True)]:
  h=ScriptHarness();h.lines(['DO 100','END']);h.step();h.now+=elapsed;line=h.step();assert (line==3)==advance
  rows.append(dict(case='timed_wait_'+str(elapsed),result='native elapsed >= delay releases next command; line already advanced on DO',match=True))
 for moving in (0,1):
  h=ScriptHarness();h.lines(['WAITMOVEEND 7','END']);h.step();assert h.get(h.context+0x24)==0xfffffffe and h.get(h.context+0x2c)==7
  h.moving=moving;line=h.step();assert line==2 and h.get(h.context+0x20)==moving
  if not moving:assert h.step()==3
  rows.append(dict(case='move_wait_'+str(moving),result='native unit+100 bit1 gate; END follows only after movement clears',match=True))
 for threshold in (0,1,50,100,101):
  for random in (0,1,49,50,99,100,101):
   h=ScriptHarness();h.lines([f'RANDOM {threshold}','END']);h.random=random;h.step();assert h.get(0x1001a508)==int(random%101<threshold)
   rows.append(dict(case=f'random_{threshold}_{random}',result='native rand modulo101 strictly less than threshold',match=True))
 for command in ('TEST','TESTNOT'):
  for value in (0,1,2):
   h=ScriptHarness();h.lines([command,'END']);h.put(h.context+0x34,1);h.put(0x1001a508,value);h.step();assert h.get(0x1001a50c)==(int(value==0) if command=='TESTNOT' else value)
   rows.append(dict(case=f'test_{command}_{value}',result='native TEST copies prior result; TESTNOT normalizes inverse; no expression evaluation',match=True))
 # Full native branch blocks with a visible camera-position effect captured at final helper.
 for random in (0,50,100):
  h=ScriptHarness();h.random=random;h.lines(['RANDOM 50','TEST','(','SETPOSITION 3 4',')','TESTNOT','(','SETPOSITION 8 9',')','END']);positions=[]
  def position():positions.append(h.args(2));h.ret()
  h.stubs[0x10003c70]=position
  for _ in range(10):h.step()
  assert positions==([[3,4]] if random<50 else [[8,9]]),positions
  rows.append(dict(case='block_camera_'+str(random),result='native RANDOM/TEST/(commands)/TESTNOT choose exactly one camera branch',match=True))
 for name,iat in (('HPSET',0x1001620c),('MPSET',0x10016208)):
  h=ScriptHarness();h.lines([name+' 7 123','END']);received=[]
  def setter():received.append(h.args(1)[0]);h.ret()
  h.put(iat,0x300f0100);h.stubs[0x300f0100]=setter;h.step();assert received==[(123<<16)|7]
  rows.append(dict(case='native_action_'+name,result='native numeric parser packs unit/value shorts into existing state API',match=True))
 # END and ENDGAME call different completion paths.
 for name,completion in (('END','scene_done'),('ENDGAME','send5B')):
  h=ScriptHarness();h.lines([name]);h.step();assert completion in h.effects
  rows.append(dict(case='completion_'+name,result='native completion side effects captured: '+','.join(h.effects),match=True))
 for event in (1,3,99):
  for actor in (-1,7):
   h=ScriptHarness();record=0x1006c120+event*0x64c;h.put(record,1)
   for i in range(399):h.put(record+0x10+4*i,h.empty)
   h.put(record+0x10,h.string(b'DO 100'));h.put(record+0x14,h.string(b'END'))
   h.put(0x100161e4,0x300f0200);h.stubs[0x300f0200]=lambda:h.ret(17)
   h.run(0x1000f620,((event&65535)|((actor&65535)<<16),),h.context)
   assert h.bytes(h.context+0x54)==b'DO 100' and h.bytes(h.context+0x58)==b'END'
   assert h.get(h.context+4)==1 and h.get(h.context+0xc)==1 and h.get(h.context+0x10)==event and h.get(h.context+0x14)==actor&0xffffffff
   assert h.get(h.context+0x18)==(0xffffffff if actor==-1 else 17) and h.get(h.context+0x34)==0
   rows.append(dict(case=f'event_activate_{event}_{actor}',result='native 399-line context copy; event/actor identity and wait/cursor reset',match=True))
 # Event queue is bounded at100 and waits for active scene completion.
 h=ScriptHarness();del h.stubs[0x1000ef80];h.put(0x10093864,h.context);h.put(h.context+0xc,1);h.put(0x100936d0,0)
 for i in range(100):h.put(0x100936d4+4*i,0xffffffff)
 for i in range(101):h.run(0x1000ef80,((i%99+1)|(7<<16),))
 assert h.get(0x100936d0)==100,h.get(0x100936d0)
 rows.append(dict(case='event_queue_capacity',result='native 100 queued event/actor pairs; 101st ignored while busy',match=True))
 # Real command references in the main dispatcher, not lexical upper-case count alone.
 p=PE(ROOT/'GameCtl.dll');asm=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',str(ROOT/'GameCtl.dll')],text=True);refs=[]
 for line in asm.splitlines():
  m=re.match(r'\s*([0-9a-f]+):\s+mov\s+(?:esi|edi),0x(1001[0-9a-f]+)',line)
  if not m or not 0x100111be<=int(m[1],16)<0x10013b35:continue
  try:command=p.string(int(m[2],16))
  except (ValueError,UnicodeDecodeError):continue
  if re.fullmatch('[A-Z_][A-Z_0-9]{1,29}',command):refs.append(dict(command=command,comparison_va='0x'+m[1],literal_va='0x'+m[2],scope='native inline strcmp dispatch; not full handler execution'))
 write('이벤트_실행기_명령분기.csv',refs);write('이벤트_실행기_원본명령대조.csv',rows)
 counts=collections.Counter();files=[];events=[]
 root=BASE/'설치본_추출/ARC_해제/MAP'
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  for path in sorted(folder.iterdir()):
   if path.name.endswith('.utf8.txt') or path.suffix.lower() not in ('.ai','.evt','.deo','.dee'):continue
   text=path.read_bytes().decode('cp949');n=0
   if path.suffix.lower()=='.evt':
    event=None;block=[];start=0
    for lineno,line in enumerate(text.splitlines(),1):
     match=re.match(r'^EVENT\s+(\S+)',line.strip())
     if match:
      if event is not None:events.append(dict(archive=folder.name,file=path.name,event=event,start_line=start,line_count=len(block),declarations=json.dumps([v for v in block if v.strip() in ('COMMANDER','ONCE')],ensure_ascii=False),numbered_conditions=json.dumps([v for v in block if re.match(r'^\s*\d+\s+',v)],ensure_ascii=False)))
      event=match[1];start=lineno;block=[]
     elif event is not None:block.append(line)
    if event is not None:events.append(dict(archive=folder.name,file=path.name,event=event,start_line=start,line_count=len(block),declarations=json.dumps([v for v in block if v.strip() in ('COMMANDER','ONCE')],ensure_ascii=False),numbered_conditions=json.dumps([v for v in block if re.match(r'^\s*\d+\s+',v)],ensure_ascii=False)))
   for line in text.splitlines():
    line=line.strip()
    if not line or line.startswith(';'):continue
    match=re.match(r'(?:(\d+)\s+)?([A-Z_][A-Z_0-9]*)(?:\s|$)',line)
    if match:counts[match[2]]+=1;n+=1
   files.append(dict(archive=folder.name,file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),candidate_command_lines=n))
 write('이벤트_실행기_스크립트목록.csv',files);write('이벤트_실행기_이벤트블록.csv',events)
 write('이벤트_실행기_어휘대조.csv',[dict(token=k,occurrences=v,dispatcher_reference=any(r['command']==k for r in refs),interpretation='dispatcher reference' if any(r['command']==k for r in refs) else 'could be declaration, condition or separate engine; do not call unsupported') for k,v in counts.most_common()])
 ranges=[(0x1000f020,0x1000f300),(0x10010de0,0x10011254),(0x1001286a,0x10012ca7),(0x100130ad,0x10013199),(0x10013ada,0x10013c05),(0x10013c20,0x100140f0),(0x1000ef50,0x1000f020),(0x1000f620,0x1000f6b5)]
 (OUT/'이벤트_실행기_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'GameCtl.dll')],text=True) for a,b in ranges))
 result=dict(cases=len(rows),mismatches=0,dispatcher_reference_sites=len(refs),dispatcher_unique_keywords=len(set(r['command'] for r in refs)),script_files=len(files),script_file_types=dict(collections.Counter(r['file'].split('.')[-1].lower() for r in files)),event_blocks=len(events),lexical_keywords=len(counts),scope='native token helpers and full scene step for selected commands/waits/END; CString primitives, condition evaluator, RNG/time/unit API/notifications modeled; not every scene/condition handler or full app',server_processing_excluded=True,binary_sha256=hashlib.sha256((ROOT/'GameCtl.dll').read_bytes()).hexdigest())
 (OUT/'이벤트_실행기검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
