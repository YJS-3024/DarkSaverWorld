"""Native map EVENT line loader and event-notification execution boundary."""
import hashlib,json,re,struct,subprocess
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EBP,UC_X86_REG_EIP
from analyze_script_executor import ScriptHarness
from analyze_special_skills import ROOT,BASE,OUT,write

def load_lines(lines):
 h=ScriptHarness();sp=0x20010000;it=iter(lines);read=0
 # Static C++ event array constructor creates400 empty CString slots in each record.
 for event in range(100):h.u.mem_write(0x1006c120+event*0x64c+0xc,struct.pack('<400I',*([h.empty]*400)))
 def readline():
  nonlocal read
  dst=h.args(1)[0]
  try:line=next(it)
  except StopIteration:h.ret(0,4);return
  read+=1;h.put(dst,h.string(line.encode('cp949')));h.ret(1,4)
 h.stubs[0x10014664]=readline
 for offset in (0x10,0x14,0x18,0x1c,0x20,0x24,0x28,0x2c):h.put(sp+offset,h.empty)
 h.u.reg_write(UC_X86_REG_ESP,sp);h.u.reg_write(UC_X86_REG_EBP,0)
 try:h.u.emu_start(0x10013db9,0x10014056,count=20000000)
 except Exception as e:raise RuntimeError((read,lines[read-1] if read else '',hex(h.u.reg_read(UC_X86_REG_EIP)),hex(h.u.reg_read(UC_X86_REG_EBP)))) from e
 assert h.u.reg_read(UC_X86_REG_EIP)==0x10014056
 return h,read

def main():
 rows=[];bodies=[];conditions=[];root=BASE/'설치본_추출/ARC_해제/MAP'
 # Entire original line-loading loop, stopping before file-wrapper cleanup.
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  for p in sorted(folder.iterdir()):
   if p.suffix.lower()!='.evt':continue
   lines=p.read_bytes().decode('cp949').splitlines();h,n=load_lines(lines);assert n==len(lines)
   expected={};event=0;valid={};skipped=0
   for line in lines:
    stripped=line.strip();token=stripped.split(' ',1)[0];tail=stripped[len(token):].strip()
    if token=='EVENT' and event<1:
     assert tail.isdigit();event=int(tail);expected[event]=[];valid[event]=1
    elif token=='END':
     if event:expected[event].append('END');event=0
    elif not token or token=='999':
     if event:valid[event]=0;event=0
    elif event>0:
     if token.isdigit() and int(token)>0:
      skipped+=1;parts=tail.split(' ',1);conditions.append(dict(archive=folder.name,file=p.name,event=event,group=int(token),condition=parts[0],arguments=parts[1] if len(parts)>1 else '',raw=stripped,client_action_stored=False))
     else:expected[event].append(stripped)
   for eid,body in expected.items():
    rec=0x1006c120+0x64c*eid;assert h.get(rec)==valid[eid],(p.name,eid)
    for i,line in enumerate(body):assert h.bytes(rec+0x10+4*i).decode('cp949')==line,(p.name,eid,i)
    bodies.append(dict(archive=folder.name,file=p.name,event=eid,valid=valid[eid],stored_lines=len(body),body=json.dumps(body,ensure_ascii=False)))
   rows.append(dict(case='load_'+folder.name+'/'+p.name,result=f'native load loop; {len(expected)} events; {skipped} positive numeric-prefix lines skipped',match=True))
 # Verify declarative/legacy tokens have no action effect in the examined step dispatcher.
 for command in ('COMMANDER','ONCE','CHARDEAD 400','CHANGETILE CLEAR','CHANGETILE START1'):
  h=ScriptHarness();h.lines([command,'END']);h.step()
  assert h.get(h.context+0x34)==2 and h.get(h.context+0xc)==1 and not h.effects
  rows.append(dict(case='action_boundary_'+command,result='one line consumed, no side-effect handler reached in main action dispatcher',match=True))
 # Notification handler consumes event IDs then activates/runs each sequentially.
 for events in ([],[1],[3,7,9]):
  h=ScriptHarness();h.put(0x10093864,h.context);captured=[]
  def activate():
   value=h.args(1)[0];captured.append(('activate',value&65535,(value>>16)&65535));h.put(h.context+0xc,1);h.ret(clean=4)
  def step():captured.append(('step',));h.put(h.context+0xc,0);h.ret()
  h.stubs.update({0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x1000f620:activate,0x10010de0:step})
  h.u.mem_write(0x30001000,struct.pack('<h',len(events))+b''.join(struct.pack('<h',e) for e in events));h.run(0x1000dc10,(0x30001000,))
  assert captured==[v for e in events for v in [('activate',e,65535),('step',)]]
  rows.append(dict(case='event_notice_'+str(events),result='native 5C15 handler activates IDs with actorFFFF and steps until inactive',match=True))
 # Scene action numeric parsing connects to the exact existing map mutation APIs.
 for command,helper,expected in [('CHANGESPRITE 5 15 0 1 0 22',0x10014610,[5,15,0,1,0,22]),('CHANGETILE2 5 15 0 2 3 22 1',0x10014616,[5,15,0,2,3,1,22])]:
  h=ScriptHarness();h.lines([command,'END']);calls=[]
  def mutate():calls.append(h.args(len(expected)));h.ret(clean=4*len(expected))
  h.stubs[helper]=mutate;h.step();assert calls==[expected],(command,calls)
  rows.append(dict(case='mutation_command_'+command.split()[0],result='native token/numeric parser invokes map API with ordered arguments',match=True))
 # Native camera centering and both bounds; small maps retain original negative upper bound.
 for size in ((40,40),(20,15),(10,8)):
  for center in ((0,0),(10,7),(20,20),(50,50),(-1,-1)):
   h=ScriptHarness();h.put(0x1001c490,0x30007000);h.put(0x30007088,*size);h.put(0x1001a0b8,20,15)
   h.run(0x10003c70,tuple(v&0xffffffff for v in center))
   expected=[min(max(center[i]-[10,7][i],0),size[i]-[20,15][i]) for i in range(2)]
   got=[h.get(a) for a in (0x1001c460,0x1001c464)];assert got==[v&0xffffffff for v in expected]
   rows.append(dict(case=f'camera_{size}_{center}',result='native origin '+str(expected)+'; min(max(center-half,0),map-viewport)',match=True))
 write('맵_이벤트적재_원본명령대조.csv',rows);write('맵_이벤트_클라이언트본문.csv',bodies);write('맵_발생조건_원문.csv',conditions)
 ranges=[(0x10013db9,0x10014056),(0x1000ef50,0x1000ef7d),(0x1000dc10,0x1000dcd5),(0x100127c9,0x1001286a),(0x10010320,0x1001043e),(0x10003c70,0x10003cf6)]
 (OUT/'맵_이벤트적재_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'GameCtl.dll')],text=True) for a,b in ranges))
 summary=dict(cases=len(rows),mismatches=0,event_files=sum(r['case'].startswith('load_') for r in rows),events=len(bodies),positive_numeric_lines_skipped=len(conditions),scope='native text-line loading loop with modeled static empty CString construction/ReadString; original notification parse/control with activation/step effects captured; native map command parsing and camera bounds; no eligibility engine or full app',finding='positive numeric-prefix condition lines are excluded from client action storage; server/internal trigger decisions not reconstructed',binary_sha256=hashlib.sha256((ROOT/'GameCtl.dll').read_bytes()).hexdigest())
 (OUT/'맵_이벤트적재검증.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n');print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
