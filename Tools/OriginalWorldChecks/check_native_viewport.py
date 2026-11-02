"""Execute actual F900/F940 and full unit blits at viewport and local-row9 edges."""
import copy,hashlib,json,subprocess
from pathlib import Path
from check_native_unit_frames import NativeUnit
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-viewport-checks')
class NativeBoundedUnit(NativeUnit):
    def __init__(self,c):
        super().__init__(c);self.invoke(0x1000f4a0,[c['left'],c['top']]);self.write(0x10016108,c['columns']);self.write(0x1001610c,c['rows'])
    def _dispatch(self,uc,address,size,data):
        if self.hooks.get(address)=='visible':return # Execute F900 instead of the old interior-only hook.
        super()._dispatch(uc,address,size,data)
    def run_bounded(self):
        frames=self.run_unit()
        # Original blits are viewport-relative; production frame origins are map pixels.
        for frame in frames:frame['x']+=self.case['left']*32;frame['y']+=self.case['top']*32
        return frames
def vectors():
    result=[]
    for left,top in [(0,0),(7,11)]:
        columns=20;rows=15
        points=[(left-1,top),(left,top),(left+columns-1,top),(left+columns,top),(left,top-1),(left,top+rows-1),(left,top+rows),(left+9,top+8),(left+9,top+9),(left+9,top+10)]
        for size,key in [(32,100),(64,701)]:
            for moving in [False,True]:
                for attr in [0,6]:
                    for direction,row in [(1,3),(8,2)]:
                        for x,y in points:
                            for part in range(6):
                                units=[dict(id=0,job=key,x=x,y=y,life=1,hp=100,maxHp=200,mp=45,maxMp=65,direction=0),dict(id=400,x=x+1,y=y,life=1,hp=50,maxHp=100,mp=7,maxMp=10,direction=1)]
                                result.append(dict(mode='frames',bounded=True,width=64,height=64,left=left,top=top,columns=columns,rows=rows,size=size,appearance=key,attribute=attr,kind=0,moving=moving,part=part,direction=row,nativeDirection=direction,phase=1 if moving else 0,clock=333,state=16,attack=True,number=True,attackCounter=7,numberCounter=11,deathCounter=0 if size==64 else 8,active=True,corpseAvailable=True,attributes=[attr]*4096,units=units))
    # Flight, hidden appearance, pure corpse and zero/narrow/ten-row surfaces.
    additions=[]
    for original in result:
        if original['left']==7 and original['part']==0 and original['nativeDirection']==1:
            if original['attribute']==6:
                c=copy.deepcopy(original);c['kind']=1;additions.append(c)
            if original['size']==32:
                c=copy.deepcopy(original);c['appearance']=999;additions.append(c)
            c=copy.deepcopy(original);c.update(state=128,attack=False,number=False);additions.append(c)
            for columns,rows in [(20,10),(1,1),(0,0)]:
                c=copy.deepcopy(original);c.update(columns=columns,rows=rows);additions.append(c)
    return result+additions
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeViewportDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeUnitFramesDifferential.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());assert len(managed['output'])==len(cases);mismatches=[];blits=0
    for i,(c,current)in enumerate(zip(cases,managed['output'])):
        native=NativeBoundedUnit(c).run_bounded();blits+=len(native)
        if native!=current:mismatches.append(dict(index=i,input=c,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeViewport.cs','Assets/_Original/Runtime/Combat/NativeBodyFrames.cs','Assets/_Original/Runtime/Combat/NativeUnitFrames.cs','Assets/_Original/Runtime/Combat/NativeBodyOrder.cs','Assets/_Original/Runtime/World/WorldSceneRoot.BodyLayers.cs','Tools/OriginalWorldChecks/NativeUnitFramesDifferential.cs','Tools/OriginalWorldChecks/check_native_viewport.py','Tools/OriginalWorldChecks/check_native_unit_frames.py','Tools/OriginalWorldChecks/check_native_body_scenery.py','Tools/OriginalWorldChecks/check_native_body.py','Tools/OriginalWorldChecks/native_feedback_renderer.py','Tools/OriginalWorldChecks/native_ai_emulator.py']
    result=dict(cases=len(cases),nativeBlits=blits,mismatches=len(mismatches),managedGuardAssertions=managed['assertions'],nativeCodeExecuted=True,unityPlayExecuted=False,nativeDllSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual SetMainView/F900/F940 and FCD0/B5D0/BD00/F970 with shifted origins, exclusive viewport edges and local row9.32/64px, movement/parts/water/flight/hidden/death/attack/numbers. Captures pre-device DirectDraw calls; actual device clipping, camera centering, Unity screen and wall-clock speed are not measured.',logs=str(WORK))
    (OUT/'native-viewport-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps({k:v for k,v in mismatches[0].items()if k!='input'}));raise SystemExit(1)
if __name__=='__main__':main()
