"""Original68 target-query -> mode2 request -> Magic continuation boundaries.
Target calculation is supplied; its general eligibility is not re-proved here.
"""
import csv,hashlib,json,struct
from pathlib import Path
from check_native_magic_packets import NativeMagicClientOutgoing
from native_ai_emulator import UC_X86_REG_EAX,UC_X86_REG_ESP,UC_X86_REG_EIP
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Redirect(NativeMagicClientOutgoing):
    def dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and 'CheckMagicTarget@@'in name:
            ptr,*args=self.args(5);signed=lambda v:v-2**32 if v>=2**31 else v;self.queries.append(list(map(signed,args)))
            c=self.case
            if c['ok']:
                result=c.get('targetResult',c)
                if result['targets']:uc.mem_write(self.DATA+0x6100,struct.pack('<'+'h'*len(result['targets']),*result['targets']))
                uc.mem_write(ptr,struct.pack('<hhhhIB3xI',result['magic'],result['source'],result['x'],result['y'],0,len(result['targets']),self.DATA+0x6100 if result['targets']else 0))
            value=int(c['ok'])
        elif name and 'Magic@@'in name:
            a,b,ignored,count,p=self.args(5);word=lambda v:(v&65535)-65536 if v&32768 else v&65535
            self.effects.append(dict(magic=word(a),source=word(a>>16),x=word(b),y=word(b>>16),targets=list(struct.unpack('<'+'h'*(count&255),uc.mem_read(p,(count&255)*2)))));value=0
        else:return super().dispatch(uc,address,size,data)
        sp=uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);uc.reg_write(UC_X86_REG_EAX,value);uc.reg_write(UC_X86_REG_ESP,sp+4);uc.reg_write(UC_X86_REG_EIP,pc)
    def receive(self,c):
        self.case=c;self.sent=[];self.effects=[];self.queries=[]
        for at,key in [(0,'id'),(8,'distance'),(12,'range'),(16,'team'),(20,'unit'),(0x170,'attribute')]:self.uc.mem_write(self.DATA+0xa000+at,struct.pack('<i',c['metadata'][key]))
        self.uc.mem_write(0x1001a8ec,struct.pack('<i',c['gameMode']));self.uc.mem_write(self.DATA,b'\x5c\x68'+struct.pack('<hhhh',c['magic'],c['source'],c['x'],c['y']))
        sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<II',self.STOP,self.DATA));self.uc.reg_write(UC_X86_REG_ESP,sp);self.uc.emu_start(0x1000eb10,self.STOP+1,timeout=5000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
        return dict(query=self.queries,requests=self.sent,effects=self.effects)
def main():
    table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv'
    with table.open(encoding='utf-8-sig',newline='')as f:rows={int(r['번호']):r for r in csv.DictReader(f)}
    cases=[];native=Redirect();builder=NativeMagicClientOutgoing()
    for magic in [101,301,4001,10101]:
        r=rows[magic];metadata={key:int(r[column])for key,column in [('id','번호'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}
        for ok in [False,True]:
            for targets in [[],[400],[0,1,400]]:
                for mode in [0,0x200,0x400,0x800]:
                    c=dict(magic=magic,source=0,x=10,y=11,ok=ok,targets=targets,gameMode=mode,metadata=metadata)
                    actual=native.receive(c);packet=builder.run_cast(dict(metadata=metadata,source=0,x=10,y=11,mode=2,targets=targets))if ok and mode in [0x200,0x400]else None
                    expected=dict(query=[[magic,0,10,11]],requests=[packet]if packet else[],effects=[dict(magic=magic,source=0,x=10,y=11,targets=targets)]if ok else[])
                    assert actual==expected,(c,actual,expected);cases.append(dict(input=c,native=actual))
    for magic,source,x,y in [(-32768,-32768,-32768,32767),(-1,-1,-1,-1),(101,2999,32767,-32768),(32767,32767,0,0)]:
        c=dict(magic=magic,source=source,x=x,y=y,ok=False,targets=[],gameMode=0x400,metadata=cases[0]['input']['metadata']);actual=native.receive(c)
        assert actual==dict(query=[[magic,source,x,y]],requests=[],effects=[]);cases.append(dict(input=c,native=actual))
    native.close();builder.close()
    paths=[Path(__file__),table,*[OUT/name for name in ['check_native_magic_packets.py','check_native_battle_decoder.py','check_native_move_hints.py','native_ai_emulator.py']]]
    report=dict(nativeRedirectCases=len(cases),mismatches=0,unityPlay=False,runtime68Connected=False,originalGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',
        scope='Actual EB10->DFB0 four signed WORD reads and native E4C0 mode2 request builder, then captured Magic import. CheckMagicTarget success and returned target capsule are supplied boundaries; actual table metadata is used. Failed target query stops both paths. Successful zero-target capsule still reaches Magic. Game mode gates outgoing bytes but not the Magic continuation. Reference request is the separately invoked original builder, not a production68 implementation or a general target-policy proof.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},cases=cases)
    (OUT/'native-magic-redirect-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
