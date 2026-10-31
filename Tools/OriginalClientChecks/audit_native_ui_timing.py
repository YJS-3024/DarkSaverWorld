"""Late native UI/timing follow-up. Static evidence, never a playback benchmark.
Run after audit_native_control_flow.py; requires the same native inputs.
"""
import argparse,json,re
from pathlib import Path
from native_pe import PE
from native_cfg import CFG
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser();p.add_argument('--native-root',type=Path,required=True);p.add_argument('--saver',type=Path,required=True);a=p.parse_args()
old=json.loads((ROOT/'분석/Saver_Late/native_control_flow_audit.json').read_text())
modules={n:PE(a.native_root/n) for n in ('GameCtl.dll','UserUnit.dll','setunit.dll')};modules['Saver.exe']=PE(a.saver)
for n,h in old['sources'].items():assert modules[n].sha256==h,n
cfg={n:CFG(pe) for n,pe in modules.items()};s=modules['Saver.exe'];g=modules['GameCtl.dll'];u=modules['UserUnit.dll']
# These two UI class families have DIFFERENT button callback slots.
windows=[]
slots=(0x10,0x14,0x18,0x1c,0x20,0x24,0x28,0x2c,0x30,0x34,0x44,0x48,0x50)
for row in old['worldConstantIdVtables']:
 v=int(row['vtable'],16);controller=row['windowId']<=4
 windows.append({'windowId':row['windowId'],'vtable':row['vtable'],
  'family':'controller (button slot +0x44)' if controller else 'common-window family (button slot +0x10)',
  'slots':{hex(o):hex(s.thunk(s.u32(v+o))) for o in slots},'buttonEntry':hex(s.thunk(s.u32(v+(0x44 if controller else 0x10))))})
# GetID returns a member, rather than an immediate constant; absent from the old scan.
for v,member in [(0x484838,0xa4),(0x4854bc,0xa8),(0x485d24,0x198),(0x485f58,0xb4)]:
 getter=s.thunk(s.u32(v+4));s.expect(getter,'8b81'+member.to_bytes(4,'little').hex()+'c3')
 windows.append({'windowId':None,'vtable':hex(v),'family':'member-ID window; factory-ID mapping pending','idMember':hex(member),
  'slots':{hex(o):hex(s.thunk(s.u32(v+o))) for o in slots},'buttonEntry':hex(s.thunk(s.u32(v+0x10)))})
entries=sorted({int(w['buttonEntry'],16) for w in windows})
world={hex(at):cfg['Saver.exe'].walk(at) for at in entries}
# Explicit battle button dispatch table; internal messages are local UI routing, not network packets.
buttons=[]
def add(scene,button,action,address,condition='common gateway applies'):
 buttons.append({'sceneId':scene,'buttonOrdinal':button,'action':action,'evidence':hex(address),'condition':condition})
for sc,addr in [(0,0x10004ab1),(3,0x10004b6b),(24,0x10004b33),(25,0x10004b33),(26,0x10004b33),(27,0x10004b33)]:
 add(sc,0,'0x4CF: minimize via window virtual +0x44',addr);add(sc,1,'0x4CB: remove/close window',addr)
add(1,0,'open scene9 (0x4CA), then close scene1 (0x4CB)',0x10004b10)
add(1,1,'close scene1',0x10004afb)
menu={0x10004bb9:'close scene4',0x10004bce:'open scene1; close scene9; request position (5,5)',0x10004c02:'open scene3; request position (450,302)',0x10004c28:'open scene5; request position (228,186)',0x10004c4e:'open scene0; request position (515,5)',0x10004c74:'count four active unit slots; choose scene24+max(count-1,0); toggle chosen group; position (5,231)',0x10004d26:'open scene6; position request targets scene2 (512,136), preserve this original discrepancy',0x10004d4c:'open scene7; request position (10,10)',0x10004d72:'if scene28 absent, open it and request position (228,186)'}
for i in range(9):
 target=g.u32(0x10005050+i*4);assert target in menu;add(4,i,menu[target],target)
for sc,at in [(5,0x10004db0),(6,0x10004dd1),(19,0x10004f5d)]:add(sc,0,f'close scene{sc}',at)
add(7,0,'scroll = max(scroll-1,0)',0x10004e36)
add(7,1,'scroll = min(scroll+1,total-18); no separate lower clamp here',0x10004e1a)
add(7,2,'close scene7',0x10004e05)
add(8,0,'mode1: call script start helper and close scenes0..9, then 0x10008D20; other mode: reset global, send 0x590/0x4CD, call 0x1000E750',0x10004e53,'only non-tutorial and global 0x1001C434 != 0')
add(12,3,'toggle scene4 through presence check (0x4D1)',0x10004f22)
add(12,4,'call string clearing helper on 101 records at 0x1001BDE4, stride8',0x10004f07)
add(28,0,'call 0x1000E780, close28, query 0x1000F040; if result<999 and 0x1006C084==1 open8 at (110,10)',0x10004f9f)
add(28,1,'close scene28',0x10004f8a)
continuous=[{'sceneId':3,'buttons':[6,7],'operation':'6: max(scroll-1,0); 7: increment only when signed WORD total > scroll+4','entry':'0x100052aa'},
 {'sceneId':7,'buttons':[0,1],'operation':'same scroll arithmetic as click callback; total-18 upper bound','entry':'0x1000526a'},
 {'sceneId':19,'buttons':[1,2],'operation':'1: max(scroll-1,0); 2: min(scroll+1,totalFromCallback-12), without separate lower clamp','entry':'0x1000521b'}]
# All direct references to imported time APIs, including loads into registers.
time_names={'clock','timeGetTime','GetTickCount','SetTimer','KillTimer','Sleep','QueryPerformanceCounter','QueryPerformanceFrequency','timeBeginPeriod','timeEndPeriod'}
time_refs=[]
for name,pe in modules.items():
 imports=pe.imports();time_iats={at:im for at,im in imports.items() if im[1] in time_names}
 for at,(length,op) in cfg[name].ins.items():
  refs=[(iat,im) for iat,im in time_iats.items() if re.search(r'(?<![0-9a-f])0x'+format(iat,'x')+r'(?![0-9a-f])',op)]
  for iat,im in refs:
   time_refs.append({'module':name,'at':hex(at),'import':im,'instruction':op,'scope':'direct IAT reference; register-call propagation not inferred'})
update={}
for event in range(1,201):
 index=g.read(0x10003c3c+event-1,1)[0];target=g.u32(0x10003c10+index*4)
 if target!=0x10003be4:update[str(event)]=hex(target)
assert update['4']=='0x10003937' and update['5']=='0x10003972' and update['6']=='0x10003997'
s.expect(0x4832d0,'13010000');assert s.thunk(s.u32(0x4832d0+20))==0x4156b0
s.expect(0x483468,'66040000');assert s.thunk(s.u32(0x483468+20))==0x415200
assert s.string(0x48f16c)=='KEYPRESS'
# Exact clock phase helper caller operands, using decoded predecessor instructions.
phases=[]
items=sorted(cfg['UserUnit.dll'].ins.items())
for i,(at,(length,op)) in enumerate(items):
 if op in ['call   0x1000ac20','call   0x1000ac40','call   0x1000ac60']:
  before=[(pa,po) for pa,(_,po) in items[max(0,i-5):i]]
  pushes=[(pa,po) for pa,po in before if re.match(r'push\s+0x',po)]
  phases.append({'at':hex(at),'helper':op.split()[-1],'nearestImmediatePush':{'at':hex(pushes[-1][0]),'op':pushes[-1][1]} if pushes else None,'note':'nearest push checked in original call context, not a general argument recovery'})
evidence={
 'Saver.exe':[(0x429090,0x42914d),(0x421230,0x421376),(0x433b50,0x433b8c),(0x441e30,0x441e7b),(0x440480,0x440550),(0x442a80,0x442a96),(0x442b00,0x442b20),(0x4156b0,0x4158ba),(0x415200,0x41521c),(0x4180e5,0x418100),(0x4184b0,0x418554),(0x4119b0,0x411c57),(0x467240,0x46742f)],
 'GameCtl.dll':[(0x100029d0,0x10002a53),(0x10004a80,0x10005075),(0x10005200,0x100052eb),(0x10003840,0x10003c10),(0x1000e0c3,0x1000e107)],
 'UserUnit.dll':[(0x1000ac20,0x1000ac82),(0x1000b5d0,0x1000c340)]}
e=ROOT/'Tools/OriginalClientChecks/evidence'
for name,rs in evidence.items():(e/('late-ui-timing-'+name+'.asm.txt')).write_text('\n'.join(modules[name].disasm(lo,hi) for lo,hi in rs))
report={'sources':old['sources'],'kind':'static original-client evidence, not execution',
 'correction':'Earlier 65 constant-ID tables include 5 controller tables (IDs1,2,3,3,4). Their slot+0x10 is not the button callback; button is +0x44. Do not count 65 as decoded UI windows.',
 'windows':windows,'buttonBodies':world,'buttonBodyCoverage':'intraprocedural branches only; calls are NOT expanded or semantically certified',
 'battleButtons':buttons,'battleHeldButtons':continuous,
 'battleDefault':'Unlisted indices return in these dispatchers. Other input paths can still handle them.',
 'battleGateway':{'entry':'0x100029d0','tutorialCloseGuard':'tutorial flag and one-shot guard => first 0x4CB/0x4CF request suppressed, guard cleared; does not suppress every subsequent request in the same callback',
 'positionGuard':'0x4D0 outside tutorial allowed only for scene8,12,28; other scenes return -1',
 'packedPosition':'low16 first coordinate; arithmetic signed high16 second coordinate'},
 'timingImportReferences':time_refs,'updateDispatch':update,'phaseHelperCallers':phases,
 'timingConclusions':{'timer1':'10ms requested; WM_TIMER 1 compares KEYPRESS, posts0x466, then WM_SYSCOMMAND SC_CLOSE. Not the rendering FPS timer.',
 'phase2':'clock()/n %2 (for nonnegative clock)','phase3':'clock()/n %3','phasePingPong':'clock()/n %4 remapped3->1 (0,1,2,1)',
 'phaseClock':'absolute clock; late draws select current phase and skip missed phases',
 'heldMouse':'repeat processing compares elapsed >30 clock ticks; conditional input-loop branches',
 'measuredWallTime':False},
 'remaining':['semantic expansion of world button callees and dynamic window factory mapping','non-button controls and native child-window callbacks','complete scheduling and audiovisual timing dependency graph','original client playback measurement; no Wine/VM or original recording available in this environment']}
path=ROOT/'분석/Saver_Late/native_ui_timing_audit.json';path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'windowCandidates':len(windows),'buttonBodies':len(world),'battleButtonCases':len(buttons),'heldGroups':len(continuous),'timeImportReferences':len(time_refs),'phaseCallers':len(phases),'unresolvedBodyTransfers':sum(len(v['unresolved']) for v in world.values()),'playbackMeasured':False}))
