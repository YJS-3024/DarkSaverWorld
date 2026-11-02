"""Native dry-tile static/walking body source size, position, direction and clock phase."""
import hashlib,json,subprocess
from pathlib import Path
from native_feedback_renderer import Renderer
from native_ai_emulator import UC_HOOK_CODE
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-body-checks')
class NativeBody(Renderer):
    def __init__(self,case):
        super().__init__(case);self.hooks[0x1000ac90]='dry-tile';self._hook_add(UC_HOOK_CODE,self._dispatch,begin=0x1000ac90,end=0x1000ac90)
        u=case['units'][0];base=self.RUNTIME+u['id']*0x2e0;job=701 if case['size']==64 else 100
        self.write(base+0x18,job);self.write(base+0x68,job);self.write(base+0x148,case['nativeDirection']);self.write(0x100e040c+job*16,5);self.write(0x10016108,20);self.write(0x1001610c,15)
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='skip-body':return # Execute the actual native body renderer in this fixture.
        if name=='dry-tile':self.ret(0);return
        if name and 'GetCharAbility'in name:self.ret(self.DATA+0x70000);return
        super()._dispatch(uc,address,size,data)
    def run_body(self):
        c=self.case;u=c['units'][0]
        if c['moving']:self.invoke(0x1000bd00,[c['nativeDirection'],u['id'],701 if c['size']==64 else 100,c['phase'],u['x'],u['y'],self.DATA+0x6100,self.DATA+0x6104])
        else:self.invoke(0x1000b5d0,[u['id'],u['x'],u['y']])
        result=[f for f in self.frames if f['surface']==4];self.close();return result

def vectors():
    cases=[];rows={1:3,2:2,3:0,4:1,5:3,6:3,7:2,8:2}
    for size in [32,64]:
        for moving in [False,True]:
            for direction,row in rows.items():
                for clock in [0,124,125,249,250,332,333,375,665,666,998,999,1332,1999]:
                    for phase in [0,2]if moving else[0]:
                        units=[dict(id=0,x=5,y=5,life=1,hp=100,maxHp=200,mp=45,maxMp=65,direction=row,nativeState=0)]
                        cases.append(dict(name=f'body-{size}-{moving}-{direction}-{clock}-{phase}',unit=0,size=size,moving=moving,nativeDirection=direction,direction=row,phase=phase,clock=clock,width=12,height=12,units=units))
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeBodyDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeBodyDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());mismatches=[]
    for case,current in zip(cases,managed):
        native=NativeBody(case).run_body()
        if native!=current:mismatches.append(dict(name=case['name'],native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeBodyFrames.cs','Tools/OriginalWorldChecks/NativeBodyDifferential.cs','Tools/OriginalWorldChecks/check_native_body.py','Tools/OriginalWorldChecks/native_feedback_renderer.py','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/check_native_feedback.py','Assets/_Original/Runtime/Combat/BattleFeedbackFrames.cs']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual native1000B5D0 idle and1000BD00 walking body blits for32/64px sheets, eight directions, ordinary dry/full viewport tiles and supplied clock values. Palette0/body surface provided; water/terrain split/partial clipping, shadow markers, special979..998 body cycle and real-time process-clock phase are outside this comparison.',logs=str(WORK))
    (OUT/'native-body-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
