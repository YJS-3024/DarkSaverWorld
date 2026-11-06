"""Actual UserUnit AI FIFO worker and commander/follower dispatch, bounded callees.
No unit spawning, target/damage policy or runtime0C route is introduced here.
"""
import hashlib,json,struct
from pathlib import Path
from native_ai_emulator import NativeAi,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Dispatch(NativeAi):
    def __init__(self):
        super().__init__(dict(width=12,height=12,units=[]));self.calls=[];self.clock_values=[100,103];self.clock_at=0;self.result=77
        for a in [0x10004990,0x100041f0]:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=a,end=a)
    def _dispatch(self,uc,address,size,data):
        if address in [0x10004990,0x100041f0]:
            a=self.args(2);self.calls.append(dict(kind='commander'if address==0x10004990 else'follower',unit=self.signed(a[0]),cursor=self.signed(a[1])));self.ret(self.result)
        elif self.hooks.get(address)in ['clock','_clock']:
            self.ret(self.clock_values[self.clock_at%len(self.clock_values)]);self.clock_at+=1
        else:super()._dispatch(uc,address,size,data)
    def call(self,address,args=()):
        stack=self.STACK+0xff000;self.uc.mem_write(stack,struct.pack('<'+'I'*(len(args)+1),self.STOP,*[x&0xffffffff for x in args]));self.uc.reg_write(UC_X86_REG_ESP,stack)
        self.uc.emu_start(address,self.STOP+1,timeout=1000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
    def prepare(self,head=0,tail=0,unit=401,cursor=-7,busy=9):
        self.calls=[];self.clock_at=0
        for i in range(100):self.write(0x1003d3b0+i*4,9999)
        for p,v in [(0x100e03e4,head),(0x100e03e8,tail),(0x10016020,cursor),(0x100e03e0,busy),(0x100e03ec,123),(0x100e03f0,10),(0x100e03f4,20),(0x1003d3a8,4321)]:self.write(p,v)
        self.write(0x1003d3b0+head*4,unit)
def main():
    n=Dispatch();roles=[];workers=[]
    try:
        for unit in [-32768,-2,-1,0,1,9,10,11,399,400,401,799,800,2999]:
            for cursor in [-7,-1,0,123]:
                n.prepare(cursor=cursor);n.call(0x10002420,[unit]);expected=[]if unit<0 else[dict(kind='commander'if unit%10==0 else'follower',unit=unit,cursor=cursor)]
                assert n.calls==expected and n.read(0x100e03e0)==0
                assert n.read(0x10016020)==(cursor&0xffffffff if unit<0 else 77)
                assert n.read(0x100e03f0)==(10 if unit<0 else 13)and n.read(0x100e03f4)==(20 if unit<0 else 21)
                assert n.clock_at==(0 if unit<0 else 2)
                roles.append(dict(unit=unit,cursor=cursor,calls=list(n.calls)))
        for head,tail in [(0,0),(0,1),(99,0),(99,1),(50,99)]:
            for unit in [-1,0,1,400,401,799]:
                n.prepare(head,tail,unit);before=bytes(n.uc.mem_read(0x1003d3b0,400));n.call(0x100023c0);expected=bytearray(before)
                if head!=tail:expected[head*4:head*4+4]=struct.pack('<i',-1)
                assert bytes(n.uc.mem_read(0x1003d3b0,400))==bytes(expected)
                assert n.read(0x100e03e4)==(head if head==tail else(head+1)%100)and n.read(0x100e03e8)==tail
                assert n.read(0x100e03e0)==(9 if head==tail else 0)and n.read(0x100e03ec)==(123 if head==tail else 0)
                assert n.read(0x1003d3a8)==(4321 if head==tail else unit&0xffffffff)
                assert len(n.calls)==(1 if head!=tail and unit>=0 else 0)
                workers.append(dict(head=head,tail=tail,unit=unit,nextHead=n.read(0x100e03e4),calls=list(n.calls)))
        n.prepare(99,1,400,cursor=-1);n.write(0x1003d3b0,401);n.call(0x100023c0);n.call(0x100023c0);n.call(0x100023c0)
        assert n.calls==[dict(kind='commander',unit=400,cursor=-1),dict(kind='follower',unit=401,cursor=77)]and n.read(0x100e03e4)==1
        # An entire100-slot ring with head==tail is empty to the actual worker.
        # Preserve this native behavior; do not invent a separate full bit.
        n.prepare(0,0,400);n.call(0x100023c0);assert not n.calls and n.read(0x1003d3b0)==400
    finally:n.close()
    evidence=ROOT/'분석/Ghidra/Saver_Late/MonsterAiDispatch/evidence.json';manifest=json.loads(evidence.read_text())
    for row in manifest['files']:assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest()==row['sha256']
    paths=[Path(__file__),OUT/'native_ai_emulator.py',evidence,*[ROOT/r['asset']for r in manifest['files']]]
    result=dict(roleCases=len(roles),workerCases=len(workers),wrapAndCursorChains=1,equalCursorEmptyCases=1,mismatches=0,unityPlay=False,runtime0CConnected=False,
        originalUserUnitSha256=manifest['originalUserUnitSha256'],sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},roles=roles,workers=workers,
        scope='Actual UserUnit023C0 worker and02420 role dispatch. Consume exactly one100-slot FIFO entry per invocation, including negative sentinel, wrap, equal-cursor no-op, carried return cursor and two supplied clock samples/counters. Commander04990/follower041F0 are captured callees, not executed by this check. Native soldier planner equivalence is a separate report; no full CommanderAI decision, server authorization, new runtime0C, timing/FPS or Unity Play claim.')
    (OUT/'native-ai-dispatch-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','roles','workers']},ensure_ascii=False))
if __name__=='__main__':main()
