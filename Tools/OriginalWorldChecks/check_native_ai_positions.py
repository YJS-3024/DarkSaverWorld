"""Actual seven-argument GetMovePos vs shared managed commander/follower geometry."""
import hashlib,itertools,json,random,struct,subprocess
from pathlib import Path
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EIP
import check_native_soldier_ai as soldier
ROOT=soldier.ROOT;OUT=soldier.OUT;WORK=Path('/tmp/ds-native-ai-position-checks')
def vectors():
    result=[]
    # Reuse the independently validated terrain/occupancy fixtures, but swap
    # faction roles:400 now seeks and attacks player0, not a >=10-only target.
    base=soldier.vectors()
    for i,c in enumerate(base):
        # Entry clock/UI/leader lifecycle belongs to041F0, not03480. In
        # particular an inactive leader record alone does not specify whether
        # an independently supplied mainmap still contains its old occupant.
        if c['name'].startswith(('last-move-clock-','owner-lifecycle-'))or i%3:continue
        c=dict(c);c['units']=[dict(u)for u in c['units']];mapping={1:400,0:410,**{u['id']:j for j,u in enumerate(c['units'][2:])}}
        for u in c['units']:
            old=u['id'];u['id']=mapping[old];u['team']=8 if old<2 else 0
            if u.get('nativeInfo'):
                info=dict(u['nativeInfo']);info['fields']=info['fields'].copy();info['fields'][0]=u['id'];info['fields'][24]=u['team'];u['nativeInfo']=info
            if old==1 and u.get('attackTargets')is not None:u['attackTargets']=[mapping[t]for t in u['attackTargets']]
        leader=c['units'][0];c.update(name='reversed-'+c['name'],member=400,cursor=-7,centerX=leader['x'],centerY=leader['y'],radius=max(2,leader['commandRange']),mode={2:1,4:2,8:3}[leader['troopOrder']]);result.append(c)
    terrain=[0]*810
    for i in range(2,810,3):terrain[i]=1
    for mode,hp,center,radius in itertools.product([-1,0,1,2,3,4],[49,50,100],[(5,5),(1,1),(20,21)],[0,3,12,20]):
        actor=soldier.unit(400,4,5,hp=hp,nativeState=48,attackTargets=[0],team=8)
        result.append(dict(name=f'boundary-{mode}-{hp}-{center}-{radius}',width=12,height=12,member=400,hint=0,cursor=13,centerX=center[0],centerY=center[1],radius=radius,mode=mode,random=[1,0,2],attributes=[0]*144,terrain=terrain,units=[actor,soldier.unit(0,8,5,team=0)]))
    for hp,state,point in itertools.product([0,1,100],[0,128],[(4,4),(8,5),(1,1)]):
        actor=soldier.unit(400,4,5,hp=99,nativeState=48,attackTargets=[0],team=8)
        target=soldier.unit(0,*point,hp=hp,nativeState=state,nativeActive=True,nativeEnabled=True,team=0)
        result.append(dict(name=f'target-lifecycle-{hp}-{state}-{point}',width=12,height=12,member=400,hint=0,cursor=0,centerX=5,centerY=5,radius=3,mode=2,random=[1,0,2],attributes=[0]*144,terrain=terrain,units=[actor,target]))
    # Unit ID groups used by reachability and received teams used by target
    # relations are distinct native inputs. Exercise intentionally different values.
    corridor_terrain=[0]*810
    for attribute in range(27):
        for kind in range(10):corridor_terrain[attribute*30+kind*3+2]=99 if attribute==1 else 1
    for actor_id,block_id,target_id,teams in [(400,401,0,(0,8,8)),(400,0,1,(8,8,0)),(1,2,400,(0,8,8)),(1,2,400,(0,0,8))]:
        for mode in [1,2,3]:
            attributes=[1]*144
            for x in range(1,11):attributes[5*12+x]=0
            actor=soldier.unit(actor_id,3,5,hp=20 if mode==1 else 100,nativeState=48,team=teams[0],
                attackTargets=[unit for unit,team in [(block_id,teams[1]),(target_id,teams[2])]if team!=teams[0]])
            result.append(dict(name=f'team-corridor-{actor_id}-{block_id}-{teams}-{mode}',width=12,height=12,member=actor_id,hint=0,cursor=13,
                centerX=9,centerY=5,radius=1,mode=mode,random=[1,0,2],attributes=attributes,terrain=corridor_terrain,
                units=[actor,soldier.unit(block_id,5,5,team=teams[1]),soldier.unit(target_id,9,5,team=teams[2])]))
    for original in [c for c in result if c['name'].startswith('team-corridor-')]:
        c=dict(original);c['name']='received-'+c['name'];c['units']=[dict(u)for u in original['units']]
        for u in c['units']:
            fields=[0]*44;fields[0]=u['id'];fields[24]=u['team'];u['nativeInfo']=dict(fields=fields,name='',className='')
        c['units'][0]['attackTargets']=None;result.append(c)
    return result
def native(c):
    n=NativeAi(c)
    try:
        out=n.DATA+0x20000;cursor=out+32;n.write(cursor,c['cursor']);args=[out,c['member'],cursor,c['centerX'],c['centerY'],c['radius'],c['mode']];sp=n.STACK+0xff000
        n.uc.mem_write(sp,struct.pack('<8I',n.STOP,*[a&0xffffffff for a in args]));n.uc.reg_write(UC_X86_REG_ESP,sp)
        n.uc.emu_start(0x10003480,n.STOP+1,timeout=5000000,count=5000000)
        assert n.uc.reg_read(UC_X86_REG_EIP)==n.STOP and n.uc.reg_read(UC_X86_REG_ESP)==sp+4
        return dict(x=n.signed(n.read(out)),y=n.signed(n.read(out+4)),cursor=n.signed(n.read(cursor)),restHint=n.signed(n.read(n.RUNTIME+c['member']*0x2e0+0x1fc)),randomCalls=n.random_calls)
    finally:n.close()
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();inp=WORK/'cases.json';out=WORK/'managed.json';inp.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=soldier.magic.UNITY/'DotNetSdk';exe=WORK/'AiPositions.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeAiPositionDifferential.cs'))
    for p in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(p)not in args:args+='\n"'+str(p)+'"\n'
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(out)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    managed=json.loads(out.read_text());differences=[]
    for c,m in zip(cases,managed):
        expected=native(c);actual={k:m[k]for k in expected}
        if m['unavailable']or expected!=actual:differences.append(dict(name=c['name'],native=expected,managed=m))
    (WORK/'differences.json').write_text(json.dumps(differences,indent=2)+'\n')
    from check_native_battle_responses import contract_sources
    sources=contract_sources(args,__file__)
    for p in [OUT/'native_ai_emulator.py',OUT/'check_native_soldier_ai.py',ROOT/'분석/Ghidra/Saver_Late/AiMovementCompleteAbi/evidence.json']:sources[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    report=dict(cases=len(cases),entryOnlyContextsExcluded=sum(c['name'].startswith(('last-move-clock-','owner-lifecycle-'))for c in soldier.vectors()),receivedTeamMetadataCases=sum(c['name'].startswith('received-team-corridor-')for c in cases),receivedTeamCorridorCases=sum(c['name'].startswith('team-corridor-')for c in cases),reversedFactionCases=sum(c['name'].startswith('reversed-')for c in cases),mismatches=len(differences),unityPlay=False,sources=sources,logs=str(WORK),scope='Actual03480 GetMovePos with final mode argument, real SetArea/MoveAttack/nearest/reachability calculations; supplied attributes/team/rand/occupancy. Compare destination, cursor, actor1FC hint and random draw count, including enemy400 targeting player0. Entry-only clock/UI/leader lifecycle contexts are excluded from geometry reuse. A supplied stale native mainmap occupant is separate from an inactive unit record and is not inferred here. Not path packet bytes, complete AI behavior, server outcomes or rendered Play.')
    (OUT/'native-ai-positions-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}));assert not differences,differences[:3]
if __name__=='__main__':main()
