"""Actual042E0 spell-selection controls for all440 original magic/skill table rows.
Targets, CheckMagicTarget output and final sends are explicit call boundaries.
"""
import csv,hashlib,itertools,json,struct
from pathlib import Path
from check_native_commander_ai_gates import Commander
from native_ai_emulator import UC_HOOK_CODE
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Spells(Commander):
    def __init__(self):
        super().__init__();self.metadata={};self.trace=[];self.magic_result=1;self.magic_target=0;self.check_result=True;self.point=(8,9)
        for a in [0x10003a40,self.exports['CheckMagicTarget'],0x10005090]:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=a,end=a)
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and 'GetMagicTbl@@'in name:
            self.trace.append(['table',self.args(1)[0]]);p=self.DATA+0x40000
            for offset,key in [(0,'id'),(4,'mp'),(8,'distance'),(16,'team')]:self.write(p+offset,self.metadata[key])
            self.ret(p)
        elif address in [0x10003860,0x10003a40,0x10003c00]:
            p,actor,target,radius=self.args(4);kind={0x10003860:'enemy-area',0x10003a40:'ally-area',0x10003c00:'basic-area'}[address]
            self.trace.append([kind,actor,radius]);is_magic=kind!='basic-area';self.write(target,self.magic_target if is_magic else 0);uc.mem_write(p,struct.pack('<hhhh',actor,self.magic_target if is_magic else 0,1,2));self.ret(self.magic_result if is_magic else 1)
        elif address==self.exports['CheckMagicTarget']:
            p,magic,actor,x,y=self.args(5);self.trace.append(['check',magic,actor,x,y]);uc.mem_write(p,bytes(16));uc.mem_write(p,struct.pack('<hhhh',magic,actor,x,y));self.ret(int(self.check_result))
        elif address==0x10005090:
            p,mode=self.args(2);magic,actor,x,y=struct.unpack('<hhhh',uc.mem_read(p,8));self.packets.append(dict(kind='magic',source=actor,magic=magic,x=x,y=y,mode=mode));self.ret()
        else:super()._dispatch(uc,address,size,data)
    def run_spell(self,row,mp=None,status=0,point=(8,9),result=1,target=0,checked=True):
        p=self.prepare(99,roll=42,actions=[(100,2,row['id']),(0,1,-1)]);self.metadata=row;self.trace=[];self.point=point;self.magic_result=result;self.magic_target=target;self.check_result=checked
        self.write(p+0x28,row['mp']if mp is None else mp);self.write(p+0x104,status);self.write(p+0x70,5);self.write(p+0x74,6)
        other=self.RUNTIME+max(0,target)*0x2e0;self.write(other+0x70,point[0]);self.write(other+0x74,point[1])
        self.call(0x100042e0,[400,-1]);return dict(trace=list(self.trace),packets=list(self.packets),ap=self.read(p+0x60),state=self.read(p+0x100),order=self.read(p+0x108),randomCalls=self.random_calls)
def main():
    path=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';rows=[dict(id=int(r['번호']),mp=int(r['MP']),distance=int(r['거리']),team=int(r['팀']))for r in csv.DictReader(path.open(encoding='utf-8-sig'))];assert len(rows)==440
    n=Spells();cases=[]
    def compare(label,row,**options):
        actual=n.run_spell(row,**options);status=options.get('status',0);mp=options.get('mp',row['mp']);point=options.get('point',(8,9));target=options.get('target',0);result=options.get('result',1);checked=options.get('checked',True)
        eligible=not(status&0x80)and mp>=row['mp'];family='enemy-area'if row['team']%2==0 else'ally-area';trace=[]
        if not(status&0x80):trace.append(['table',row['id']])
        if eligible:trace.append([family,400,max(3,row['distance'])])
        valid_target=eligible and target>=0 and result!=0
        x,y=point
        if row['distance']in[-1,-3,-4]:x=(-1 if point[0]>5 else-2)if abs(point[1]-6)<abs(point[0]-5)else(-3 if point[1]<=6 else-4)
        if valid_target and y>0:trace.append(['check',row['id'],400,x,y])
        if valid_target and y>0 and checked:packets=[dict(kind='magic',source=400,magic=row['id'],x=x,y=y,mode=0)]
        elif valid_target:packets=[dict(kind='attack',source=400,target=target,sourceTerrain=1,targetTerrain=2)]if result==1 else[dict(kind='rest',source=400)]
        else:trace.append(['basic-area',400,1]);packets=[dict(kind='attack',source=400,target=0,sourceTerrain=1,targetTerrain=2)]
        expected=dict(trace=trace,packets=packets,ap=0,state=0,order=2,randomCalls=1)
        assert actual==expected,(label,row,options,expected,actual);cases.append(dict(label=label,metadata=row,options=options,native=actual))
    try:
        for row in rows:
            compare('metadata-exact-MP',row);compare('metadata-insufficient-MP',row,mp=row['mp']-1);compare('metadata-status80',row,status=0x80)
        base=dict(id=101,mp=5,distance=3,team=34)
        for distance,team in itertools.product([-4,-3,-2,-1,0,3,4,9],[-2,-1,0,1,34,35]):compare('range-and-parity',dict(base,distance=distance,team=team))
        for distance,point in itertools.product([-4,-3,-1,3],[(9,6),(1,6),(5,10),(5,2),(8,9),(2,3),(8,3),(2,9),(8,0),(5,6)]):compare('direction-tie-and-y0',dict(base,distance=distance),point=point)
        for result,target,checked in itertools.product([0,1,2],[-1,0],[False,True]):compare('target-check-fallback',base,result=result,target=target,checked=checked)
    finally:n.close()
    sources=[Path(__file__),OUT/'check_native_commander_ai_gates.py',OUT/'native_ai_emulator.py',path,ROOT/'분석/Ghidra/Saver_Late/MonsterCommanderAi/100042e0.c']
    report=dict(originalTableRows=len(rows),nativeSpellControlCases=len(cases),mismatches=0,unityPlay=False,runtimeCommanderAiConnected=False,originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},cases=cases,
      scope='Execute actual042E0 with all440 original metadata rows at exact/insufficient MP and status80, team parity/min3 lookup radius, directional X encoding/ties/Y0, negative/no target and build failure paths. Prove same-rand fallthrough and failed magic check -> basic attack/rest using its selected record. Target search, CheckMagicTarget output and sends are captured boundaries, not full selector/damage/AI/runtime0C or Unity Play.')
    (OUT/'native-commander-ai-spells-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']}))
if __name__=='__main__':main()
