"""Actual0D MAGIC continuation and actual UserUnit KILL export semantics.

Target capsules are supplied to CheckMagicTarget, independently of its geometry
proof. No actual server trigger, damage formula, audio or Windows process.
"""
import csv,hashlib,json,struct
from pathlib import Path
from check_native_directed_frames import Oracle
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EIP
from native_event_evidence import native_event_sources
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Magic(Oracle):
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address,'')
        if 'CheckMagicTarget@@'in name:
            pointer,*args=self.args(5);signed=lambda v:v-2**32 if v>=2**31 else v
            self.query=list(map(signed,args));c=self.case
            if not c['ok']:self.ret(0);return
            if c['targets']:uc.mem_write(0x0d0a0000,struct.pack('<'+'h'*len(c['targets']),*c['targets']))
            uc.mem_write(pointer,struct.pack('<hhhhIB3xI',c['magic'],c['selectedSource'],c['selectedX'],c['selectedY'],0,len(c['targets']),0x0d0a0000 if c['targets']else 0));self.ret(1)
        elif 'GetMagicTbl@@'in name:self.ret(0x0d0b0000)
        elif 'Magic@@'in name:
            a,b,_,count,pointer=self.args(5);word=lambda v:(v&65535)-65536 if v&32768 else v&65535
            self.order.append('effect');self.effects.append(dict(magic=word(a),source=word(a>>16),x=word(b),y=word(b>>16),targets=list(struct.unpack('<'+'h'*(count&255),uc.mem_read(pointer,(count&255)*2)))if count&255 else[]));self.ret()
        elif address==0x1000e2c0:
            self.order.append('request');super().hook(uc,address,size,data)
        else:super().hook(uc,address,size,data)
    def execute_magic(self,c,metadata,game_mode):
        self.case=c;self.setup(['MAGIC '+str(c['magic'])+' '+str(c['source'])+' '+str(c['x'])+' '+str(c['y']),'END'])
        self.query=None;self.effects=[];self.order=[];self.write(0x1001a8ec,game_mode)
        for at,key in [(0,'id'),(8,'distance'),(12,'range'),(16,'team'),(20,'unit'),(0x170,'attribute')]:self.write(0x0d0b0000+at,metadata[key])
        r=self.frame();assert r['line']==2 and r['active']==1
        self.frame();return dict(query=self.query,requests=self.sent,effects=self.effects,order=self.order)
def native_kill(kind,unit,state,active):
    case=dict(width=5,height=5,units=[dict(id=0,x=1,y=1,hp=17,maxHp=30,nativeState=state,nativeActive=active,ap=7,maxAp=20)])
    n=NativeAi(case);sp=n.STACK+0xff000;n.uc.mem_write(sp,struct.pack('<II',n.STOP,unit&0xffffffff));n.uc.reg_write(UC_X86_REG_ESP,sp)
    n.uc.emu_start(n.exports[kind],n.STOP+1,timeout=1000000,count=10000);assert n.uc.reg_read(UC_X86_REG_EIP)==n.STOP
    value=dict(active=n.read(n.RUNTIME),state=n.read(n.RUNTIME+0x100),hp=n.read(n.RUNTIME+0x20),ap=n.read(n.RUNTIME+0x60),occupant=n.occupied[(1,1)])
    n.close();return value
def main():
    managed=json.loads((OUT/'native-directed-event-check.json').read_text())
    for path,digest in managed['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv'
    with table.open(encoding='utf-8-sig',newline='')as f:rows={int(r['번호']):r for r in csv.DictReader(f)}
    cases=[]
    for fixture in managed['directedMagicRequests']:
        row=rows[fixture['magic']];metadata={key:int(row[column])for key,column in [('id','번호'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
        for ok in [False,True]:
            for mode in [0,0x200,0x400,0x800]:
                c=dict(fixture,ok=ok);n=Magic(0);actual=n.execute_magic(c,metadata,mode)
                expected=dict(query=[c['magic'],c['source'],c['x'],c['y']],requests=[c['request']]if ok and mode in [0x200,0x400]else[],
                    effects=[dict(magic=c['magic'],source=c['selectedSource'],x=c['selectedX'],y=c['selectedY'],targets=c['targets'])]if ok else[],
                    order=['request','effect']if ok and mode in [0x200,0x400]else['effect']if ok else[])
                assert actual==expected,(c,mode,actual,expected);cases.append(dict(input=c,gameMode=mode,native=actual))
    lifecycle=[]
    for kind in ['dmKillChar','dmKillChar2']:
        for unit in [-2,-1,0]:
            for state in [0,1,4,0x40,0x80,0x81,0xffffffff]:
                for active in [False,True]:
                    actual=native_kill(kind,unit,state,active)
                    expected=dict(active=int(active)if unit<0 or kind=='dmKillChar2'or state&0x80 else 0,state=state|0x80 if unit>=0 and kind=='dmKillChar2'else state,hp=17,ap=7,
                        occupant=-1 if unit>=0 and kind=='dmKillChar'and not state&0x80 else 0)
                    assert actual==expected,(kind,unit,state,active,actual,expected);lifecycle.append(dict(kind=kind,unit=unit,state=state,active=active,after=actual))
    paths=[Path(__file__),OUT/'native-directed-event-check.json',table,OUT/'check_native_directed_frames.py',OUT/'check_native_event_corpus.py',OUT/'native_event_step_emulator.py',OUT/'native_ai_emulator.py']
    report=dict(nativeMagicCases=len(cases),nativeKillExportCases=len(lifecycle),productionMode1RequestComparisons=len(managed['directedMagicRequests']),mismatches=0,unityPlay=False,
        originalGameCtlSha256=n.sha256,originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',
        scope='Actual GameCtl mode1 MAGIC dispatcher, CheckMagicTarget supplied capsule, native E4C0 and captured Magic import. Native request bytes compare with four production0D ingress/service fixtures; mode gating and request-before-effect order are observed. Actual UserUnit dmKillChar/dmKillChar2 exports execute over explicit runtime/grid records, including negative IDs, corpse flag and unchanged HP/AP. MFC strings/allocation and grid writes are boundaries. Not a server trigger, damage, XP, death-to-ghost or Unity rendering proof.',
        sources=native_event_sources()|{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},magicCases=cases,lifecycleCases=lifecycle)
    (OUT/'native-directed-control-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','magicCases','lifecycleCases']},ensure_ascii=False))
if __name__=='__main__':main()
