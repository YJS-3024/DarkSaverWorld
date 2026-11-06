"""Actual20 receiver, shared08CA0/08AD0 and10F20 gate against production state.
MFC storage, allocation, dialogue construction and drawing remain host boundaries.
"""
import hashlib,json,subprocess
from pathlib import Path
from check_native_auxiliary_packets import Auxiliary
from check_native_script_frame import Frame
from check_native_message_pages import NativeMessage
from native_ai_emulator import UC_X86_REG_ECX
from check_native_battle_responses import contract_sources
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-victory-notice')
class Shared(Frame,Auxiliary,NativeMessage):
    def hook(self,uc,address,size,data):
        if address==0x100109e0:
            parts=self.text(self.args(1)[0]).decode('cp949').split(' ',4)
            self.show(parts[4],int(parts[0]));self.ret(0,4)
        elif address==0x10008790:return # Run the actual plain constructor, including its retained fields.
        elif address==0x10009890:self.ret(0,12)
        else:super().hook(uc,address,size,data)
    def show(self,text,mode):
        # Constructor boundary.08840/08790/089F0 are verified independently by
        # check_native_message_pages; run the actual subsequent page/draw/gate.
        rows=text.split('|');rows=rows[:-1]if rows[-1]==''else rows
        for i,row in enumerate(rows):self.write(self.OBJECT+0x1c+i*4,self.string(row.encode('cp949')))
        for offset,value in [(0,1),(4,1),(8,40),(12,130 if mode==0 else 50),(16,561),(20,65 if mode==0 else 104),(0x1ac,mode),(0x1b0,0),(0x1b4,len(rows)),(0x1bc,self.clock)]:self.write(self.OBJECT+offset,value)
    def state(self):
        return dict(line=self.read(self.SCRIPT+0x34),active=self.read(self.SCRIPT+12),visible=bool(self.read(self.OBJECT)),first=self.read(self.OBJECT+0x1b0),
                    waiting=bool(self.read(self.SCRIPT+0x30)and self.read(self.OBJECT)),mode=self.read(self.OBJECT+0x1ac),packets=list(self.sent))
def main():
    WORK.mkdir(exist_ok=True);data=[]
    def tick(op,**kwargs):return dict(op=op,**kwargs)
    portrait='MESSAGE 1 100 0 0 one|two|three|four'
    sequences=[('before-first-message',['GOLD 8','END'],[tick('notice',name='Other'),tick('frame'),tick('frame'),tick('click')]),
      ('replace-second-page',[portrait,'GOLD 8','END'],[tick('frame'),tick('click'),tick('notice',name='Other'),tick('frame'),tick('click'),tick('frame'),tick('frame')]),
      ('track-after-close',[portrait,'GOLD 8','END'],[tick('frame'),tick('click'),tick('click'),tick('notice',name='Other'),tick('frame',elapsed=10000),tick('frame',elapsed=1),tick('frame')]),
      ('same-name-preserves-page',[portrait,'GOLD 8','END'],[tick('frame'),tick('click'),tick('notice',name='Local'),tick('frame'),tick('click'),tick('frame')]),
      ('last-wins',[portrait,'GOLD 8','END'],[tick('frame'),tick('notice',name='Other'),tick('frame',elapsed=5000),tick('notice',name='New'),tick('frame',elapsed=10000),tick('frame',elapsed=1),tick('frame')])]
    for key,name in [('empty-name',''),('embedded-NUL-same','Local\0ignored'),('embedded-NUL-other','Other\0ignored'),('maximum-name','A'*255)]:
        sequences.append((key,[portrait,'GOLD 8','END'],[tick('frame'),tick('click'),tick('notice',name=name),tick('frame'),tick('click'),tick('frame')]))
    for key,body,ticks in sequences:
        for ignored in [0,1,255]:
            data.append(dict(key=key+':'+str(ignored),body=body,ticks=[dict(dict(elapsed=0,ignored=ignored,name=''),**t)for t in ticks]))
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'Victory.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeVictoryDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    paths=[Path(__file__),*[OUT/n for n in ['check_native_auxiliary_packets.py','check_native_script_frame.py','check_native_message_pages.py','native_event_step_emulator.py','native_scenario_end_emulator.py','native_result_kills_emulator.py']]]
    baseline=contract_sources(args,__file__)|{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths}
    rsp=WORK/'checks.rsp';rsp.write_text(args);sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet'
    inp=WORK/'input.json';out=WORK/'managed.json';inp.write_text(json.dumps(data));exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,cmd in [('compile',[str(dotnet),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(inp),str(out)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    compared=0;cases=[]
    managed=json.loads(out.read_text());assert len(managed)==len(data)
    for c,actual in zip(data,managed):
        assert c['key']==actual['key']and len(c['ticks'])==len(actual['frames'])
        n=Shared();n.open();n.setup(c['body']);n.write(0x1006a73c,n.string(b'Local'));n.confirmActions=[];n.confirming=False
        rows=[];clock=1000
        for t,m in zip(c['ticks'],actual['frames']):
            if t['op']=='notice':
                raw=t['name'].encode('cp949');n.uc.mem_write(0x0d060000,bytes(256));n.receive(bytes([0x5c,0x20,t['ignored'],len(raw)])+raw)
            elif t['op']=='click':
                n.run(0x10002e10,[123,234])
            elif t['op']=='frame':
                clock+=t['elapsed'];n.clock=clock;n.labels=[];n.draws=[];n.formats=[];n.bodies=[];n.blits=[]
                try:
                    if n.read(n.OBJECT):n.uc.reg_write(UC_X86_REG_ECX,n.OBJECT);n.run(0x10008ad0,[])
                    n.frame(clock)
                except Exception as error:raise RuntimeError((c['key'],t,n.state()))from error
            expected=n.state()
            # Closed windows keep stale page/mode internally. They are not
            # rendered; compare these fields only while visible.
            if not expected['visible']:expected['mode']=m['mode']
            if not expected['visible']or expected['mode']==0:expected['first']=m['first']
            # Native line starts0 before first frame, matching the source p.
            assert m==expected,(c['key'],t,expected,m)
            rows.append(expected);compared+=1
        cases.append(dict(key=c['key'],frames=rows))
    for p,d in baseline.items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==d,('source-changed-during-check',p)
    result=dict(nativeCases=len(cases),stateComparisons=compared,mismatches=0,unityPlay=False,originalGameCtlSha256=n.sha256,sources=baseline,logs=str(WORK),cases=cases,
        scope='Actual EB10/D8F0 incoming20, OnLButtonDown08CA0 page acknowledgement, Update08AD0 strict plain timeout and10F20 shared-message tracking compared to production CampaignPresentation/NativeCampaignDialogue. Plain08790 is executed; portrait MESSAGE construction, ANSI CString allocation and graphics callbacks are bounded hosts; no actual Unity UI/OS input/audio or server winner policy claim.')
    (OUT/'native-victory-notice-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
