"""Source-backed CheckMagicTarget traces for every actual spell/skill table row.
This is a native target oracle, not server authorization, damage or a production68 path.
"""
import copy,csv,hashlib,json
from collections import Counter
from pathlib import Path
from check_native_magic_packets import NativeLevelTargets
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class CatalogTargets(NativeLevelTargets):
    def _dispatch(self,uc,address,size,data):
        # Unit8 frees its allocated list when every selected member is inactive.
        if self.hooks.get(address)=='#825':self.ret()
        else:super()._dispatch(uc,address,size,data)
UNITS=[dict(id=0,x=10,y=10,team=0),dict(id=1,x=9,y=10,team=0),dict(id=2,x=10,y=9,team=0),dict(id=3,x=9,y=9,team=0),dict(id=4,x=8,y=10,team=0,hp=0),dict(id=8,x=10,y=8,team=0),dict(id=9,x=9,y=8,team=0),dict(id=400,x=11,y=10,team=8),dict(id=401,x=10,y=11,team=8),dict(id=402,x=11,y=11,team=8,nativeActive=False),dict(id=403,x=12,y=10,team=8,nativeEnabled=False),dict(id=404,x=10,y=12,team=8,hp=0),dict(id=410,x=13,y=10,team=8),dict(id=411,x=10,y=13,team=8)]
def main():
    path=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv'
    with path.open(encoding='utf-8-sig',newline='')as f:rows=list(csv.DictReader(f))
    cases=[];zero_hp=0;empty_group=0;kinds=Counter()
    for row in rows:
        metadata={key:int(row[column])for key,column in [('id','번호'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
        kinds[(metadata['unit'],metadata['distance'])]+=1
        points=[(-1,-1),(-2,-1),(-3,-1),(-4,-1)]if metadata['distance']in[-1,-3,-4]and metadata['unit']!=8 else[(10,10),(11,10),(0,0),(23,23)]
        for point in points:
            case=dict(width=24,height=24,source=0,metadata=metadata,units=copy.deepcopy(UNITS))
            actual=CatalogTargets(case).targets(magic=metadata['id'],point=point,details=True)
            if actual['valid']:
                units={u['id']:u for u in case['units']};targets=actual['targets']
                assert all(units[u].get('nativeActive',True)and units[u].get('nativeEnabled',True)for u in targets),(metadata,point,actual)
                assert actual['magic']==metadata['id']and actual['source']==0
                zero_hp+=sum(units[u].get('hp',100)==0 for u in targets)
                if metadata['unit']==8:
                    selected=next(u for u in case['units']if(u['x'],u['y'])==point)
                    expected=[u['id']for u in case['units']if selected['id']//10*10<=u['id']<selected['id']//10*10+9 and u.get('nativeActive',True)and u.get('nativeEnabled',True)]
                    assert targets==sorted(expected)and(actual['x'],actual['y'])==(-1,-1)
                else:
                    assert targets==sorted(targets,key=lambda i:(units[i]['x'],units[i]['y']))
                    assert(actual['x'],actual['y'])==point
                    if metadata['distance']>=0:
                        center=(10,10)if metadata['distance']==0 else point
                        assert all(abs(units[i]['x']-center[0])+abs(units[i]['y']-center[1])<=max(0,metadata['range'])for i in targets)
            cases.append(dict(metadata=metadata,point=point,native=actual))
        if metadata['unit']==8:
            units=[copy.deepcopy(UNITS[0]),dict(id=400,x=11,y=10,team=8,nativeActive=False)]
            actual=CatalogTargets(dict(width=24,height=24,source=0,metadata=metadata,units=units)).targets(magic=metadata['id'],point=(11,10),details=True)
            assert actual==dict(valid=True,magic=metadata['id'],source=0,x=-1,y=-1,targets=[]),(metadata,actual)
            empty_group+=1;cases.append(dict(metadata=metadata,point=[11,10],fixture='inactive-group',native=actual))
    sources=[Path(__file__),path,*[OUT/name for name in ['check_native_magic_packets.py','native_ai_emulator.py','check_native_battle_decoder.py','check_native_move_hints.py']]]
    report=dict(tableRows=len(rows),nativeTargetCases=len(cases),zeroHpTargetOccurrences=zero_hp,emptyGroupSuccessCases=empty_group,mismatches=0,unityPlay=False,runtime68Connected=False,originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',
        scope='Every actual Magictbl row executes original UserUnit CheckMagicTarget with four controlled target/direction points; Unit8 adds a completely inactive selected group. Native grid, current unit flags/teams and exact table rows are supplied. Allocation, GetMagicTbl/GetTeamID/map access use existing bounded oracle boundaries. Record ordering, output coordinates, active/enabled filtering and valid empty-group success are checked. Not all possible maps/alliance states, operating eligibility, damage/costs, production68 routing, whole client or Unity Play.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},unitDistanceKinds=[dict(unit=u,distance=d,rows=n)for(u,d),n in sorted(kinds.items())],cases=cases)
    (OUT/'native-magic-target-catalog-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases','unitDistanceKinds']},ensure_ascii=False))
if __name__=='__main__':main()
