"""Differential execute original x86 AI and the current managed planner, with bounded I/O."""
import hashlib,itertools,json,random,subprocess
from pathlib import Path
import check_magic_projectiles as magic
from native_ai_emulator import NativeAi
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-soldier-ai-checks')
def unit(id,x,y,**extra):
    u=dict(id=id,job=100 if id<400 else 302,life=1,x=x,y=y,hp=100,maxHp=100,mp=0,maxMp=0,ap=20,maxAp=20,direction=1,
        movementBudget=6,movementType=0,attackRange=1,canMove=True,canAttack=True,canRest=True,canCommand=id==0,commandRange=3,troopOrder=2,nativeState=0)
    u.update(extra);return u

def vectors():
    terrain=[0]*810
    for i in range(2,810,3):terrain[i]=1
    def make(name,state=16,hp=100,mode=2,clock=0,hint=0,position=(4,5),enemies=((400,4,4),(401,3,5)),kind=0,attrs=None,costs=None,**extra):
        own=unit(1,*position,nativeState=state,hp=hp,movementType=kind,attackTargets=[e[0]for e in enemies]);own.update(extra)
        return dict(name=name,width=12,height=12,member=1,hint=hint,clock=clock,random=[1,0,2],attributes=attrs or [0]*144,terrain=costs or terrain,
            units=[unit(0,5,5,troopOrder=mode),own]+[unit(i,x,y)for i,x,y in enemies])
    cases=[]
    # Moving bit1 does not guard the entire native041F0 body. Exercise its
    # distinct AP-full,10 action gate and20 movement branches explicitly.
    for state,ap in itertools.product([1,17,33,49],[0,19,20]):
        cases.append(make(f'moving-bit-{state}-{ap}',state,100,2,ap=ap))
    for mode,state,hp in itertools.product([2,4,8],[0,16,32,48],[20,39,40,49,50,59,60,89,90,99,100]):cases.append(make(f'base-{mode}-{state}-{hp}',state,hp,mode))
    for clock,hp,state in itertools.product([0,2999,3000,9999],[80,89,90,100],[16,48]):cases.append(make(f'clock-{clock}-{hp}-{state}',state,hp,8,clock))
    for hint,state,hp in itertools.product([99,-1],[0,16,32,48],[20,59,60,99,100]):cases.append(make(f'hint-{hint}-{state}-{hp}',state,hp,2,hint=hint))
    for mode,state,position,enemies in itertools.product([2,4,8],[16,32,48],[(4,5),(1,5)], [(),((400,8,5),),((400,8,5),(401,5,8))]):
        cases.append(make(f'move-{mode}-{state}-{position}-{enemies}',state,100,mode,position=position,enemies=enemies))
    for kind,radius in itertools.product([0,1,2,5,8],[1,2,3]):
        attrs=[0]*144;attrs[5*12+5]=1;attrs[5*12+6]=1;costs=terrain.copy();costs[30+kind*3+2]=99
        cases.append(make(f'terrain-{kind}-{radius}',48,100,2,position=(4,5),enemies=((400,7,5),),kind=kind,attrs=attrs,costs=costs,attackRange=radius))
    for center in [(1,1),(1,10),(10,1),(10,10)]:
        case=make('edge-'+str(center),32,100,8,position=(1,5),enemies=())
        case['units'][0].update(x=center[0],y=center[1]);cases.append(case)
    rng=random.Random(20261102)
    for i in range(500):
        mode=rng.choice([2,4,8]);state=rng.choice([0,16,32,48,64,80,96,112]);hp=rng.randint(1,100)
        cells=[(x,y)for x in range(1,11)for y in range(1,11)if(x,y)!=(5,5)];rng.shuffle(cells);position=cells.pop()
        enemies=tuple((400+n,*cells.pop())for n in range(rng.randrange(5)))
        attrs=[rng.randrange(3)if rng.random()<.3 else 0 for _ in range(144)];costs=terrain.copy()
        for a in range(1,3):
            for kind in range(10):costs[a*30+kind*3+2]=rng.choice([1,2,3,4,99])
        case=make('random-'+str(i),state,hp,mode,clock=rng.choice([0,2999,3000,9000]),hint=rng.choice([0,-1,99]),position=position,enemies=enemies,kind=rng.randrange(10),attrs=attrs,costs=costs,attackRange=rng.randint(1,4),movementBudget=rng.randint(0,8),ap=rng.choice([0,19,20]))
        case['units'][0]['commandRange']=rng.randint(0,6);case['random']=[rng.randrange(32768)for _ in range(4)];cases.append(case)
    catalogs=[json.loads((ROOT/'Assets/_Original/Data/World/KaosiaWorld.json').read_text())['maps']]
    campaign=json.loads((ROOT/'Assets/_Original/Data/Campaign/CampaignCatalog.json').read_text())
    real_maps=catalogs[0]+[m['world']for m in campaign['maps']if m['kind']=='scenario']
    for world in real_maps:
        grid=world['map'];w,h=grid['width'],grid['height']
        if w>200 or h>200 or w<8 or h<8:continue
        attrs=grid['attributes'];costs=grid['terrain'];chosen=None
        for x,y in sorted(((x,y)for x in range(2,w-3)for y in range(2,h-2)),key=lambda p:abs(p[0]-w//2)+abs(p[1]-h//2)):
            cells=[(x,y),(x+1,y),(x+2,y)]
            if all(0<=attrs[ty*w+tx]<27 and costs[attrs[ty*w+tx]*30+2]not in [99,-99]for tx,ty in cells):chosen=(x,y);break
        if chosen is None:continue
        x,y=chosen
        for state in [16,48]:
            case=make('decoded-map-'+str(world['id'])+'-'+str(state),state,100,2,position=(x+1,y),enemies=((400,x+2,y),))
            case.update(width=w,height=h,attributes=attrs,terrain=costs);case['units'][0].update(x=x,y=y);cases.append(case)
    return cases

def compile_and_run(cases):
    WORK.mkdir(exist_ok=True);input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'SoldierAiDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'SoldierAiDifferential.cs'))
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2200:])
    return json.loads(output.read_text()),hashlib.sha256(input.read_bytes()).hexdigest()

def main():
    cases=vectors();managed,digest=compile_and_run(cases);mismatches=[];native=[]
    for case,current in zip(cases,managed):
        expected=NativeAi(case).run()
        for packet in expected['packets']:packet.pop('nativePoints',None)
        actual={k:current[k]for k in ['packets','state','hint','ap','randomCalls']};native.append(expected)
        if actual!=expected:mismatches.append(dict(name=case['name'],native=expected,managed=actual))
    (WORK/'native.json').write_text(json.dumps(native,indent=2));(WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/SoldierActions.cs','Assets/_Original/Runtime/Combat/SoldierMovement.cs','Assets/_Original/Runtime/Combat/RegroupMovement.cs','Assets/_Original/Runtime/Combat/MemberCommands.cs','Assets/_Original/Runtime/Combat/CommandRange.cs','Assets/_Original/Runtime/Tutorial/TutorialScript.cs','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/SoldierAiDifferential.cs','Tools/OriginalWorldChecks/check_native_soldier_ai.py']
    result=dict(cases=len(cases),mismatches=len(mismatches),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},decodedMapCases=sum(c['name'].startswith('decoded-map-')for c in cases),casesSha256=digest,unityPlayExecuted=False,
        nativeCodeExecuted=True,scope='Actual bounded x86 ExecuteSoldierAI/GetMovePos/MoveAttack/target selection/SetArea/pathbacktracker; map attributes/team/clock/rand and empty MFC string/path-presentation queue/send boundaries hooked. Compare purpose/destination/target/flags/hint/AP/random draws, not path packet bytes or gameplay playback.',logs=str(WORK))
    (OUT/'native-soldier-ai-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:
        print(json.dumps(mismatches[:4],indent=2));raise SystemExit(1)
if __name__=='__main__':main()
