"""Actual commander042E0/04810/02EB0 with supplied target/path boundaries."""
import hashlib,json,struct
from pathlib import Path
from native_ai_emulator import NativeAi,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Commander(NativeAi):
    TABLE=0x100d99a0
    def __init__(self):
        super().__init__(dict(width=12,height=12,units=[],random=[0]));self.phase='action';self.calls=[];self.destination=(-1,-1);self.path_ok=False
        for a in [0x10003860,0x10003c00,0x10003480,0x1000e310,0x100042e0]:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=a,end=a)
    def _dispatch(self,uc,address,size,data):
        if address==0x100042e0:
            if self.phase=='move':self.calls.append(['action',*self.args(2)]);self.ret(-1)
            return
        if address in [0x10003860,0x10003c00]:
            p,unit,target,radius=self.args(4);self.calls.append(['target38'if address==0x10003860 else'target3c',unit,radius])
            self.write(target,401);uc.mem_write(p,struct.pack('<hhhh',unit,401,3,4));self.ret(1)
        elif address==0x10003480:
            p,unit,cursor,x,y,radius,move_mode=self.args(7);signed=lambda z:z if z<2**31 else z-2**32
            self.calls.append(['region',unit,signed(x),signed(y),signed(radius),move_mode]);self.write(cursor,42);uc.mem_write(p,struct.pack('<ii',*self.destination));self.ret(p)
        elif address==0x1000e310:self.calls.append(['path',*self.args(4)[1:]]);self.ret(int(self.path_ok))
        elif address==0x10004ff0:self.calls.append(['send-move']);self.ret()
        else:super()._dispatch(uc,address,size,data)
    def call(self,entry,args):
        sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*[v&0xffffffff for v in args]));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(entry,self.STOP+1,timeout=1000000,count=200000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and self.uc.reg_read(UC_X86_REG_ESP)==sp+4
        return self.signed(self.uc.reg_read(UC_X86_REG_EAX))
    def prepare(self,hp=100,state=16,active=1,found=True,roll=0,actions=()):
        self.packets=[];self.calls=[];self.random_calls=0;self.case['random']=[roll];self.phase='action'
        for i in range(100):self.write(self.TABLE+i*0x110,999)
        self.uc.mem_write(self.RUNTIME+400*0x2e0,bytes(0x2e0));p=self.RUNTIME+400*0x2e0
        for offset,value in [(0,active),(4,1),(0x10,400),(0x1c,0),(0x20,hp),(0x24,100),(0x28,0),(0x30,1),(0x60,20),(0x70,5),(0x74,6),(0x100,state),(0x108,1234)]:self.write(p+offset,value)
        self.uc.mem_write(self.TABLE,bytes(0x110));self.write(self.TABLE,400 if found else 999);self.write(self.TABLE+4,2);self.write(self.TABLE+0x1c,len(actions))
        for i,(weight,kind,magic)in enumerate(actions):
            for j,value in enumerate([weight,kind,magic]):self.write(self.TABLE+0x20+i*12+j*4,value)
        return p

def main():
    n=Commander();actions=[];moves=[]
    try:
        for hp in [29,30,99,100]:
            for flags in [0,1,16,17,32,48]:
                for found in [False,True]:
                    p=n.prepare(hp,flags,found=found);result=n.call(0x100042e0,[400,-1]);eligible=bool(flags&16)and not(flags&1)
                    rest=eligible and found and hp<30
                    assert result==-1 and n.packets==([dict(kind='rest',source=400)]if rest else[])
                    assert n.read(p+0x60)==(0 if rest else 20)and n.read(p+0x100)==(flags&~16 if rest else flags)
                    assert n.read(p+0x108)==(2 if eligible and found else 4 if eligible else 1234)
                    assert n.random_calls==(1 if eligible and found else 0)
                    actions.append(dict(hp=hp,flags=flags,found=found,rest=rest))
        for roll in [0,1,49,50,51,99]:
            p=n.prepare(99,roll=roll,actions=[(50,0,-1),(50,1,-1)]);n.call(0x100042e0,[400,0]);rest=roll<=50
            assert n.packets==([dict(kind='rest',source=400)]if rest else[dict(kind='attack',source=400,target=401,sourceTerrain=3,targetTerrain=4)])
            assert n.read(p+0x60)==0 and n.read(p+0x100)==0 and n.random_calls==1
            actions.append(dict(weightedRoll=roll,selected='rest'if rest else'attack'))
        for roll in [0,1]:
            n.prepare(99,roll=roll,actions=[(0,0,-1),(100,1,-1)]);n.call(0x100042e0,[400,0]);assert n.packets[0]['kind']==('rest'if roll==0 else'attack')
        for roll in [0,49,99]:
            n.prepare(99,roll=roll,actions=[(100,2,-1),(0,1,-1)]);n.call(0x100042e0,[400,0]);assert n.packets[0]['kind']=='attack'and n.random_calls==1
        for mode,order in __import__('itertools').product([-1,0,1,2],[0,2,4,8]):
            for target_active in [False,True]:
                for radius in [3,8,12]:
                    for destination,path_ok in [((-1,-1),False),((5,6),True),((7,8),False),((7,8),True)]:
                        p=n.prepare(state=48);n.write(n.TABLE+4,order);n.phase='move';n.destination=destination;n.path_ok=path_ok
                        for offset,value in [(8,mode),(12,10),(16,11),(20,12),(24,410)]:n.write(n.TABLE+offset,value)
                        other=n.RUNTIME+410*0x2e0;n.uc.mem_write(other,bytes(0x2e0))
                        for offset,value in [(0,int(target_active)),(4,int(target_active)),(0x4c,radius),(0x70,20),(0x74,21)]:n.write(other+offset,value)
                        result=n.call(0x10004810,[400,-7]);region=(10,11,12)if mode==0 else(20,21,max(7,radius))if mode==1 and target_active else(5,6,20)
                        expected_mode={4:2,2:1,8:3}.get(order,1)if mode==0 or mode==1 and target_active else 2
                        assert n.calls[0]==['region',400,*region,expected_mode]and n.calls[-1]==['action',400,42]and result==-1
                        path=destination[0]>=0 and destination[1]>=0 and destination!=(5,6)
                        assert sum(c[0]=='path'for c in n.calls)==int(path)and sum(c[0]=='send-move'for c in n.calls)==int(path and path_ok)
                        assert n.read(p+0x100)==(0 if path else 48)and n.read(p+0x60)==20
                        moves.append(dict(mode=mode,order=order,moveMode=expected_mode,targetActive=target_active,targetRange=radius,destination=destination,pathSuccess=path_ok,region=region))
    finally:n.close()
    evidence=ROOT/'분석/Ghidra/Saver_Late/MonsterCommanderAi/evidence.json';data=json.loads(evidence.read_text());paths=[Path(__file__),OUT/'native_ai_emulator.py',evidence]
    corrected=ROOT/'분석/Ghidra/Saver_Late/AiFileLoaderAndMoveMode/evidence.json';paths.append(corrected)
    for row in data['files']+json.loads(corrected.read_text())['files']:
        p=ROOT/row['asset'];assert hashlib.sha256(p.read_bytes()).hexdigest()==row['sha256'];paths.append(p)
    report=dict(actionGateCases=48,weightedBoundaryCases=8,invalidMagicFallbackCases=3,movementGateCases=len(moves),mismatches=0,unityPlay=False,runtimeCommanderAiConnected=False,
        originalUserUnitSha256=data['originalUserUnitSha256'],sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},actions=actions,moves=moves,
        scope='Actual02EB0 unit-key lookup and042E0/04810 bodies. Source30percent strict low-HP rest, missing AI->order4, rand%100 cumulative <= (including zero weight), invalid negative-magic fallback without reroll; AREA/follow/default20 region and minimum7 follow range; actual seventh GetMovePos argument, state2/4/8->mode1/2/3, fallback forces2; destination/path-failure flag clearing and action-after-movement call. Target selection/path construction, final sends and nested action are explicit hosts. No complete magic/AI policy, damage, spawning, runtime0C or Unity Play claim.')
    (OUT/'native-commander-ai-gates-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','actions','moves']},ensure_ascii=False))
if __name__=='__main__':main()
