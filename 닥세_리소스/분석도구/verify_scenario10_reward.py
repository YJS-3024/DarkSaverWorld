import sys,json
from pathlib import Path
sys.path.insert(0,str(Path('닥세_리소스/분석도구').resolve()))
from analyze_script_executor import ScriptHarness
rows=[]
for command in ('GETGENERAL 109','GET_GENERAL 109','GETGENERAL 102'):
 for native_event in (False,True):
  h=ScriptHarness();calls=[]
  def capture():
   calls.append(h.args(1)[0]);h.ret()
  h.stubs[0x1000e940]=capture
  if not native_event:h.lines([command,'DO 100','END'])
  else:
   event=99;record=0x1006c120+event*0x64c;h.put(record,1)
   for i in range(399):h.put(record+0x10+4*i,h.empty)
   for i,line in enumerate((command,'DO 100','END')):h.put(record+0x10+4*i,h.string(line.encode('ascii')))
   h.put(0x100161e4,0x300f0200);h.stubs[0x300f0200]=lambda:h.ret(17)
   h.run(0x1000f620,((event&65535)|((7&65535)<<16),),h.context)
  mode=h.get(h.context+4)
  cursor=h.step()
  rows.append(dict(command=command,nativeEventActivation=native_event,contextMode=mode,cursorAfterStep=cursor,getGeneralCalls=calls))

for row in rows:
 expected=[] if row['nativeEventActivation'] or row['command'].startswith('GET_GENERAL') else [int(row['command'].split()[1])]
 assert row['getGeneralCalls']==expected,row
import hashlib
from analyze_special_skills import ROOT,OUT
report=dict(cases=rows,passed=len(rows),binarySha256=hashlib.sha256((ROOT/'GameCtl.dll').read_bytes()).hexdigest(),
 scope='Original GameCtl x86 step/tokenization and native event activation; CString/time/unit APIs modeled, GetGeneral sender0x1000e940 intercepted. No original server or full game replay.',
 conclusion='GET_GENERAL109 is not dispatched as GETGENERAL109. Scenario10 DEE has no independent grant. A historical server-only award cannot be established.')
(OUT/'시나리오10_보상명령_원본실행대조.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('PASS',len(rows),'native script spelling/context comparisons; underscore form invokes no client GetGeneral sender')
