"""Original wet body crop/ripple, idle shadow and special980..998 walking clock."""
import hashlib,json,subprocess
from pathlib import Path
from check_native_body import NativeBody
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-body-scenery-checks')
class NativeScenery(NativeBody):
    def __init__(self,c):
        super().__init__(c);self.write(self.RUNTIME+0x68,c['appearance']);self.write(self.RUNTIME+0x1c,c['kind']);self.write(0x100e040c+c['appearance']*16,5)
    def _dispatch(self,uc,address,size,data):
        if self.hooks.get(address)=='dry-tile':return # Execute actual AC90 water predicate.
        super()._dispatch(uc,address,size,data)
    def run_scenery(self):
        c=self.case;u=c['units'][0]
        if c['moving']:self.invoke(0x1000bd00,[c['nativeDirection'],0,c['appearance'],c['phase'],u['x'],u['y'],self.DATA+0x6100,self.DATA+0x6104])
        else:self.invoke(0x1000b5d0,[0,u['x'],u['y']])
        result=self.frames;self.close();return result
def vectors():
    cases=[]
    for size in [32,64]:
        for key in ([100,899,979,980,981,998,999]if size==32 else[701,790,791]):
            for attr,kind in [(0,0),(6,0),(20,0),(134,0),(6,1)]:
                for moving in [False,True]:
                    for clock in [0,100,125,249,250,333,375,500]:
                        cases.append(dict(size=size,appearance=key,attribute=attr,kind=kind,moving=moving,direction=3,nativeDirection=1,phase=1 if moving else 0,clock=clock,width=12,height=12,attributes=[attr]*144,units=[dict(id=0,job=key,x=5,y=5,life=1,hp=100,maxHp=100,nativeState=0,movementType=kind)]))
    import copy
    for original in cases[::8]:
        for state in [16,32]:
            c=copy.deepcopy(original);c['units'][0]['nativeState']=state;cases.append(c)
    return cases
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeBodySceneryDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeBodySceneryDifferential.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[]
    for c,current in zip(cases,managed):
        native=NativeScenery(c).run_scenery()
        if native!=current:mismatches.append(dict(input=c,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeBodyFrames.cs','Tools/OriginalWorldChecks/NativeBodySceneryDifferential.cs','Tools/OriginalWorldChecks/check_native_body_scenery.py','Tools/OriginalWorldChecks/check_native_body.py','Tools/OriginalWorldChecks/native_feedback_renderer.py','Tools/OriginalWorldChecks/native_ai_emulator.py']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual UserUnit AC90/B5D0/BD00 for6/20 water modulo128, movement-kind1/key899 exemption,25/48px body cutoff,250 clock ripple,100 clock idle shadow/key981 exception,999 hide,980..998 walking modulo3. Full interior viewport/palette0; clipping partitions and palette changes are separate.',logs=str(WORK))
    (OUT/'native-body-scenery-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
