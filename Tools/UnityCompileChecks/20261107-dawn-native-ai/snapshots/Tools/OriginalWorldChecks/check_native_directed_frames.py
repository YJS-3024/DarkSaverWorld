"""Compare actual GameCtl Update/10F20 to the production directed presentation.

MFC message rendering is a boundary: ticks explicitly acknowledge the whole
message, not one physical click. This is command cadence, never measured FPS.
"""
import hashlib,json,subprocess
from pathlib import Path
from check_native_event_corpus import Corpus,load_bodies
from check_native_script_frame import Frame
from check_native_battle_responses import contract_sources
from native_event_evidence import event_corpus_sources
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-directed-frames')
class Oracle(Corpus,Frame):
    def __init__(self,random_value):
        super().__init__(random_value);self.localCalls=[]
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address,'')
        if 'CheckMagicTarget@@'in name:
            _,*args=self.args(5);signed=lambda v:v-2**32 if v>=2**31 else v
            self.localCalls.append(dict(kind='CheckMagicTarget-no-target',args=list(map(signed,args))));self.ret(0)
        elif 'dmKillChar@@'in name or'dmKillChar2@@'in name:
            self.localCalls.append(dict(kind='dmKillChar2'if'dmKillChar2'in name else'dmKillChar',args=self.args(1)));self.ret()
        else:super().hook(uc,address,size,data)
def main():
    WORK.mkdir(exist_ok=True)
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'DirectedFrames.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeDirectedEventDifferential.cs'))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    baseline=contract_sources(args,__file__)|event_corpus_sources()
    sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet'
    r=subprocess.run([str(dotnet),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True)
    (WORK/'compile.log').write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-3000:]
    exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    bodies,_=load_bodies(sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*/*.evt.txt')))
    data=[]
    for b in bodies:
        for subject,team,rand in [(0,0,0),(0,0,100),(0,7,0),(-1,-1,0)]:
            ticks=[dict(elapsed=0 if i==0 else 200,dismiss=i>0,moving=False,resultVisible=False)for i in range(len(b['body'])*3+20)]
            data.append(dict(key=b['file']+':'+str(b['number'])+':'+str(subject)+':'+str(team)+':'+str(rand),body=b['body'],subject=subject,team=team,localTeam=0,randomValue=rand,ticks=ticks))
    controls=[['DO 500','GOLD 123','END'],['FIRSTCLEAR','TESTNOT','(','GOLD 1',')','GOLD 2','END'],['RANDOM 50','TEST','(','GOLD 7',')','SCENECLEAR','END'],['MESSAGE 1 709 700 A test','GOLD 9','END'],['ALL']]
    for i,body in enumerate(controls):
        ticks=[dict(elapsed=ms,dismiss=i>=5,moving=i<2,resultVisible=False)for i,ms in enumerate([0,200,200,299,1,200,200,200,200,200,200,200])]
        data.append(dict(key='control:'+str(i),body=body,subject=0,team=0,localTeam=0,randomValue=100,ticks=ticks))
    data.append(dict(key='result-visible-suppresses-message',body=['MESSAGE 1 709 700 A hidden','GOLD 9','END'],subject=0,team=0,localTeam=0,randomValue=0,
        ticks=[dict(elapsed=0,dismiss=False,moving=False,resultVisible=True)for i in range(3)]))
    input_path=WORK/'input.json';input_path.write_text(json.dumps(data,ensure_ascii=False));output=WORK/'managed.json'
    r=subprocess.run([str(dotnet),str(exe),str(input_path),str(output)],cwd=ROOT,text=True,capture_output=True);(WORK/'run.log').write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-3000:]
    managed=json.loads(output.read_text());assert len(managed)==len(data)
    comparisons=0;cases=[];unsupported=[]
    for c,m in zip(data,managed):
        assert c['key']==m['key']
        if not m['supported']:
            unsupported.append(c['key']);continue
        n=Oracle(c['randomValue']);n.setup(c['body'],subject=c['subject'],team=c['team'],local_team=c['localTeam'])
        clock=1000;trace=[]
        for tick,actual in zip(c['ticks'],m['frames']):
            clock+=tick['elapsed']
            if tick['dismiss']:
                n.write(n.SCRIPT+0x1c,0);n.write(n.SCRIPT+0x30,0);n.write(0x0d020000,0)
            elif n.read(n.SCRIPT+0x1c):n.write(0x0d020000,1)
            if c['subject']>=0:n.write(n.unit_address(c['subject'])+0x100,int(tick['moving']))
            n.existing=[8]if tick['resultVisible']else[]
            native=n.frame(clock)
            expected={k:native[k]for k in ['line','active','execution','testResult']}
            expected['waiting']=bool(n.read(n.SCRIPT+0x1c))
            expected.update(packets=native['sent'],mutations=[r[0]+' '+r[1]for r in native['mutations']],profiles=[r[0]for r in native['profile']],localCalls=list(n.localCalls))
            observed={k:actual[k]for k in expected}
            assert observed==expected,(c['key'],len(trace),c['body'],expected,observed)
            comparisons+=1;trace.append(dict(clock=clock,**expected))
            if not native['active']:break
        else:raise AssertionError(('supported-event-not-complete',c['key'],c['body'],trace[-1]))
        cases.append(dict(key=c['key'],frames=trace))
        if len(cases)%100==0:print(json.dumps(dict(progressCases=len(cases),frames=comparisons)),flush=True)
    paths=[Path(__file__),OUT/'check_native_event_corpus.py',OUT/'check_native_script_frame.py',OUT/'native_event_step_emulator.py',OUT/'native_scenario_end_emulator.py']
    sources=contract_sources(args,__file__);sources.update({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
    for path,digest in baseline.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('source-changed-during-check',path)
    sources.update(baseline)
    report=dict(managedNativeCases=len(cases),frameComparisons=comparisons,unsupportedCases=len(unsupported),mismatches=0,nativeGameCtlSha256=n.sha256,unityPlay=False,actualFpsMeasured=False,
        scope='Original GameCtl Update/10F20 against production CampaignPresentation native mode for the whitelist-supported EVT bodies, four supplied ownership/random contexts and explicit movement/timer controls. Compare next line, active/execution/test flags, emitted request bytes, map/profile calls and exact MAGIC/KILL export arguments after each Update. MAGIC target query supplies no target; KILL exports are captured boundaries, not executed here. MFC message helper, audio backend, map mutator, profile provider and transport are supplied boundaries. A dismissal acknowledges the whole message; this does not verify page rendering, physical clicks, trigger policy or real FPS.',
        sources=sources,logs=str(WORK),unsupportedKeys=unsupported)
    (WORK/'native-cases.json').write_text(json.dumps(cases,ensure_ascii=False,indent=2)+'\n')
    (OUT/'native-directed-frame-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','unsupportedKeys']},ensure_ascii=False))
if __name__=='__main__':main()
