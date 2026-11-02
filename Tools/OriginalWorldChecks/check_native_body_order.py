"""Execute original first/second unit passes and all five large-body crops."""
import hashlib,json,random,struct,subprocess
from pathlib import Path
from check_native_body_scenery import NativeScenery
from native_ai_emulator import UC_HOOK_CODE
from check_native_feedback import NativeFeedback
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-body-order-checks')
class NativeOrder(NativeFeedback):
    def __init__(self,c):
        super().__init__(c);self.draws=[];self.stage=1
        self.uc.mem_write(0x100e42a0,b'\xff'*(200*200*4))
        for u in sorted(c['units'],key=lambda u:u['id']):
            self.write(self.RUNTIME+u['id']*0x2e0+0x68,u['job'])
            if u.get('nativeActive',True)and u.get('nativeEnabled',True):self.write(0x100e42a0+(u['x']*200+u['y'])*4,u['id'])
        for p,v in [(0x100e4290,c['left']),(0x100e428c,c['top']),(0x10016108,c['columns']),(0x1001610c,c['rows'])]:self.write(p,v)
        self.hooks[0x1000fcd0]='capture-body';self._hook_add(UC_HOOK_CODE,self._dispatch,begin=0x1000fcd0,end=0x1000fcd0)
    def _dispatch(self,uc,address,size,data):
        if self.hooks.get(address)=='capture-body':self.draws.append(dict(id=self.args(1)[0],part=self.read(0x1010b3dc),stage=self.stage));self.ret();return
        super()._dispatch(uc,address,size,data)
    def run(self):
        self.invoke(0x10010ee0,[1]);self.stage=2;self.invoke(0x10010c00,[]);result=self.draws;self.close();return result
class NativeParts(NativeScenery):
    def __init__(self,c):super().__init__(c);self.write(0x1010b3dc,c['part'])
def vectors():
    result=[];rng=random.Random(1700)
    # Occupied/empty flags, ordinary/large/flying, overlapping native slots and viewport edges.
    for i in range(256):
        width=12;height=12;attrs=[rng.choice([0,6,20,127,128,134,148,255])for _ in range(width*height)]
        units=[dict(id=j,x=rng.randrange(1,10),y=rng.randrange(1,10),job=rng.choice([100,701,790,899,999]),movementType=rng.choice([0,1,2]),nativeActive=rng.random()>.1,nativeEnabled=rng.random()>.1)for j in range(12)]
        result.append(dict(mode='plan',width=width,height=height,attributes=attrs,units=units,left=i%3,top=i%4,columns=8,rows=7))
    for attr in [0,128,134,255]:
        for job in [100,701]:
            for kind in [0,1]:
                c=dict(mode='plan',width=12,height=12,attributes=[0]*144,units=[dict(id=0,x=5,y=5,job=job,movementType=kind),dict(id=1,x=6,y=5,job=701,movementType=0),dict(id=2,x=5,y=6,job=701,movementType=0)],left=0,top=0,columns=12,rows=12)
                for x,y in [(5,5),(6,5),(5,6)]:c['attributes'][y*12+x]=attr
                result.append(c)
    for size in [32,64]:
        for attr,kind in [(0,0),(6,0),(20,0),(134,0),(6,1)]:
            for moving in [False,True]:
                for part in range(6):
                    for direction,row in [(1,3),(3,0),(8,2)]:
                        for clock in [100,333]:
                            key=701 if size==64 else 100
                            result.append(dict(mode='frames',width=12,height=12,size=size,appearance=key,attribute=attr,kind=kind,moving=moving,part=part,direction=row,nativeDirection=direction,phase=1,clock=clock,attributes=[attr]*144,units=[dict(id=0,job=key,x=5,y=5,nativeState=32)]))
    return result
def check_map_attributes():
    totals=[];sources={}
    for file,group in [('Assets/_Original/Data/World/KaosiaWorld.json','town'),('Assets/_Original/Data/Campaign/CampaignCatalog.json','campaign')]:
        catalog=json.loads((ROOT/file).read_text());sources[file]=hashlib.sha256((ROOT/file).read_bytes()).hexdigest()
        for row in catalog['maps']:
            current=row if group=='town'else row['world'];key=str(row['id'])if group=='town'else row['key']
            base='KaosiaLegacy'if current['sourceVersion']=='Saver_1.2'else 'Native/map'
            path=ROOT/'Assets/_Original/SourceData'/base/(key+'.mrc.bytes');raw=path.read_bytes()
            count=struct.unpack_from('<i',raw)[0];offset=4+count*4;width,height,_=struct.unpack_from('<iii',raw,offset);cells=width*height
            attributes=list(raw[offset+12+cells*2:offset+12+cells*7][3::5])
            assert current['map']['renderAttributes']==attributes,(key,'literal attributes')
            assert current['map']['attributes']==[0 if(a&127)>27 else a&127 for a in attributes],(key,'navigation changed')
            totals.append(dict(map=key,group=group,cells=cells,flagged=sum(a>=128 for a in attributes)))
            sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(raw).hexdigest()
    (WORK/'map-attributes.json').write_text(json.dumps(totals,indent=2))
    return dict(maps=len(totals),cells=sum(t['cells']for t in totals),flagged=sum(t['flagged']for t in totals),navigationPreserved=True),sources
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeBodyOrderDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeBodyOrderDifferential.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[]
    for index,(c,current)in enumerate(zip(cases,managed)):
        native=NativeOrder(c).run()if c['mode']=='plan'else NativeParts(c).run_scenery()
        if native!=current:mismatches.append(dict(index=index,input=c,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeBodyOrder.cs','Assets/_Original/Runtime/Combat/NativeBodyFrames.cs','Tools/OriginalWorldChecks/NativeBodyOrderDifferential.cs','Tools/OriginalWorldChecks/check_native_body_order.py','Tools/OriginalWorldChecks/check_native_body_scenery.py','Tools/OriginalWorldChecks/check_native_body.py','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/native_feedback_renderer.py']
    attributes,mapSources=check_map_attributes()
    result=dict(cases=len(cases),planCases=sum(c['mode']=='plan'for c in cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,mapAttributes=attributes,nativeDllSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},mapSources=mapSources,scope='Actual10010EE0 stage1/10010C00 draw scheduling and1000B5D0/BD00 parts0..5 body/shadow/ripple/readiness blits. Interior viewport, supplied clock, palette0; screen-edge clipping/process-clock phase and repeated attack overlays are outside this comparison.',logs=str(WORK))
    (OUT/'native-body-order-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','mapSources']}))
    if mismatches:print(json.dumps({k:v for k,v in mismatches[0].items()if k!='input'}));raise SystemExit(1)
if __name__=='__main__':main()
