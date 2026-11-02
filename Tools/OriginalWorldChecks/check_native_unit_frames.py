"""Actual UserUnit FCD0 with live body/movement and full staged draw-call order."""
import hashlib,json,struct,subprocess
from pathlib import Path
from check_native_body_scenery import NativeScenery
from check_native_body_order import NativeOrder,vectors as body_vectors
from native_ai_emulator import UC_HOOK_CODE
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-unit-frame-checks')
class NativePasses(NativeOrder):
    def __init__(self,c):
        super().__init__(c)
        for addr,name in [(0x10010700,'magic-pass'),(0x1000f970,'number-pass')]:self.hooks[addr]=name;self._hook_add(UC_HOOK_CODE,self._dispatch,begin=addr,end=addr)
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='magic-pass':self.draws.append(dict(id=-1,part=0,stage=3));self.ret();return
        if name=='number-pass':self.draws.append(dict(id=self.args(1)[0],part=0,stage=4));self.ret();return
        super()._dispatch(uc,address,size,data)
    def run(self):
        self.invoke(0x10010ee0,[1]);self.stage=2;self.invoke(0x10010ee0,[2]);result=self.draws;self.close();return result
class NativeUnit(NativeScenery):
    def run_unit(self):
        c=self.case;b=self.RUNTIME
        for u in c['units']:
            self.write(self.RUNTIME+u['id']*0x2e0+0x28,u['mp']);self.write(self.RUNTIME+u['id']*0x2e0+0x2c,u['maxMp'])
        if c['attack']:
            self.uc.mem_write(self.DATA+0x6000,struct.pack('<hhh',400,0,-1));self.invoke(self.exports['AttackChar'],[self.DATA+0x6000]);self.write(b+0x158,c['attackCounter'])
        if c['number']:
            self.invoke(self.exports['SetHpDisplay'],[75<<16]);self.write(b+0x15c,c['numberCounter'])
        state=c['state']|(1 if c['moving']else 0)|(4 if c['attack']and c['attackCounter']<49 else 0)|(64 if c['number']and c['numberCounter']<27 else 0)
        self.write(b+0x100,state);self.write(b+0x11c,c['nativeDirection']);self.write(b+0x170,c['phase']);self.write(b+0x164,c['deathCounter']);self.write(b,int(c['active']));self.write(0x1010b3dc,c['part'])
        if not c['corpseAvailable']:self.write(0x100e040c+c['appearance']*16,0)
        self.invoke(0x1000fcd0,[0]);result=self.frames;self.close();return result
def vectors():
    result=[c for c in body_vectors()if c['mode']=='plan']
    for size,key in [(32,100),(64,701)]:
        for moving in [False,True]:
            for attr in [0,6]:
                for part in range(6):
                    for state,attack,number,ac,nc,active in [(16,False,False,0,0,True),(16,True,False,7,0,True),(16,True,True,7,11,True),(144,True,True,7,11,True),(128,False,False,0,0,True),(128,True,True,49,27,True),(16,True,True,7,11,False)]:
                        units=[dict(id=0,job=key,x=5,y=5,life=1,hp=100,maxHp=200,mp=45,maxMp=65,direction=0),dict(id=400,x=6,y=5,life=1,hp=50,maxHp=100,mp=7,maxMp=10,direction=1)]
                        result.append(dict(mode='frames',width=12,height=12,size=size,appearance=key,attribute=attr,kind=0,moving=moving,part=part,direction=0,nativeDirection=3,phase=1 if moving else 0,clock=333,state=state,attack=attack,number=number,attackCounter=ac,numberCounter=nc,deathCounter=0 if size==64 else 8,active=active,corpseAvailable=True,attributes=[attr]*144,units=units))
    import copy
    for original in result[272:]:
        if original['state']==128 and not original['attack']and original['part']==0:
            c=copy.deepcopy(original);c['corpseAvailable']=False;result.append(c)
    for size in [32,64]:
        template=next(c for c in result if c['mode']=='frames'and c['size']==size and c['moving']and c['attack']and c['number']and c['active']and c['state']==16 and c['attribute']==0)
        for direction,row in {1:3,2:2,3:0,4:1,5:3,6:3,7:2,8:2}.items():
            for phase in [0,1,2]:
                c=copy.deepcopy(template);c.update(nativeDirection=direction,direction=row,phase=phase,clock=100,part=3);result.append(c)
    for moving in [False,True]:
        template=next(c for c in result if c['mode']=='frames'and c['size']==32 and c['moving']==moving and c['attack']and c['number']and c['active']and c['state']==16)
        c=copy.deepcopy(template);c['appearance']=999;result.append(c)
    return result
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeUnitFramesDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeUnitFramesDifferential.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed['output'])==len(cases);mismatches=[];blits=0
    for i,(c,current)in enumerate(zip(cases,managed['output'])):
        native=NativePasses(c).run()if c['mode']=='plan'else NativeUnit(c).run_unit();blits+=len(native)
        if native!=current:mismatches.append(dict(index=i,input=c,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeUnitFrames.cs','Assets/_Original/Runtime/Combat/NativeBodyOrder.cs','Assets/_Original/Runtime/Combat/BattleFeedbackFrames.cs','Assets/_Original/Runtime/Combat/NativeBodyFrames.cs','Tools/OriginalWorldChecks/NativeUnitFramesDifferential.cs','Tools/OriginalWorldChecks/check_native_unit_frames.py','Tools/OriginalWorldChecks/check_native_body_order.py','Tools/OriginalWorldChecks/check_native_body_scenery.py','Tools/OriginalWorldChecks/check_native_body.py','Tools/OriginalWorldChecks/native_feedback_renderer.py','Assets/_Original/Runtime/World/WorldSceneRoot.BodyLayers.cs','Assets/_Original/Runtime/World/WorldSceneRoot.BattleFeedback.cs','Assets/_Original/Runtime/World/WorldSceneRoot.Combat.cs']
    result=dict(cases=len(cases),planCases=sum(c['mode']=='plan'for c in cases),nativeCallsOrBlits=blits,mismatches=len(mismatches),managedGuardAssertions=managed['assertions'],nativeCodeExecuted=True,unityPlayExecuted=False,nativeDllSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual10010EE0 first/second unit passes, magic boundary/final number scan and FCD0 with real B5D0/BD00 bodies, attack/numeric/death. Current-life guards and missing corpse texture. Supplied clock/phase, interior viewport/palette0; no actual Unity pixels or original wall-clock claim.',logs=str(WORK))
    (OUT/'native-unit-frame-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps({k:v for k,v in mismatches[0].items()if k!='input'}));raise SystemExit(1)
if __name__=='__main__':main()
