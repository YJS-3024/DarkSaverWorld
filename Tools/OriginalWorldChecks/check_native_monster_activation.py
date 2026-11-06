"""Original setmonsterai and actual AIStart queue mutation; never a monster spawn table."""
import hashlib,json,struct
from pathlib import Path
from check_native_alliance_metadata import Teams
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Activation(Teams):
    def _dispatch(self,uc,address,size,data):
        if address==self.exports.get('AIStart'):return
        super()._dispatch(uc,address,size,data)
def main():
    ai=[];queues=[];n=Activation(dict(width=12,height=12,units=[]));map_record=n.DATA+0x20000
    n.write(0x1010b3e4,map_record);n.write(map_record+0x88,12);n.write(map_record+0x8c,14)
    try:
        for unit in [1,9,11,399,400,401,799,800,2999]:
            head=unit//10*10
            for active in [0,1]:
                for enabled in [0,1]:
                    for head_active in [0,1]:
                        for head_enabled in [0,1]:
                            if head==unit and(active!=head_active or enabled!=head_enabled):continue
                            ids=list(dict.fromkeys([unit,head]));before={};expected={}
                            for key in ids:
                                p=n.RUNTIME+key*0x2e0;n.uc.mem_write(p,bytes([0xa5])*0x2e0);n.write(p,active if key==unit else head_active);n.write(p+4,enabled if key==unit else head_enabled)
                                n.write(p+0x108,1234);before[key]=bytes(n.uc.mem_read(p,0x2e0));copy=bytearray(before[key])
                                if n.read(p)!=0 and n.read(p+4)!=0:copy[0x108:0x10c]=struct.pack('<i',2)
                                expected[key]=bytes(copy)
                            n.call(n.exports['setmonsterai'],struct.pack('<i',unit))
                            assert all(bytes(n.uc.mem_read(n.RUNTIME+key*0x2e0,0x2e0))==expected[key]for key in ids)
                            ai.append(dict(unit=unit,commander=head,active=active,enabled=enabled,commanderActive=head_active,commanderEnabled=head_enabled))
        for unit in [0,1,10,399,400,799,800,2999]:
            for occupant in [-1,0,799]:
                for existing in [False,True]:
                    for cursor in [0,99]:
                        p=n.RUNTIME+unit*0x2e0;n.uc.mem_write(p,bytes(0x2e0));n.write(p+0x70,5);n.write(p+0x74,6)
                        n.write(0x1003d5a0+(5*200+6)*4,occupant)
                        for i in range(100):n.write(0x1003d3b0+i*4,-1)
                        if existing:n.write(0x1003d3b0,unit)
                        n.write(0x100e03e4,0);n.write(0x100e03e8,cursor);before=bytes(n.uc.mem_read(p,0x2e0));expected=[-1]*100
                        if existing:expected[0]=unit
                        allowed=unit<800 and(unit>399 or unit%10!=0)and occupant>=0 and not existing
                        if allowed:expected[cursor]=unit
                        n.call(n.exports['AIStart'],struct.pack('<i',unit));actual=[n.signed(n.read(0x1003d3b0+i*4))for i in range(100)]
                        assert actual==expected and n.read(0x100e03e8)==((cursor+1)%100 if allowed else cursor),(unit,occupant,existing,cursor,allowed,[v for v in actual if v>=0],n.read(0x100e03e8))
                        assert n.read(0x100e4298)==12 and n.read(0x100e429c)==14 and bytes(n.uc.mem_read(p,0x2e0))==before
                        queues.append(dict(unit=unit,occupant=occupant,alreadyQueued=existing,cursor=cursor,enqueued=allowed))
        windows=[]
        for head in [0,99]:
            for tail in [0,1,99]:
                for existing_index in [-1,head,tail,50]:
                    unit=401;p=n.RUNTIME+unit*0x2e0;n.write(p+0x70,0);n.write(p+0x74,0);n.write(0x1003d5a0,0)
                    expected=[-1]*100
                    if existing_index>=0:expected[existing_index]=unit
                    for i,value in enumerate(expected):n.write(0x1003d3b0+i*4,value)
                    n.write(0x100e03e4,head);n.write(0x100e03e8,tail);inside=[]
                    for offset in range(100):
                        index=(head+offset)%100;inside.append(index)
                        if index==tail:break
                    queued=existing_index not in inside
                    if queued:expected[tail]=unit
                    n.call(n.exports['AIStart'],struct.pack('<i',unit))
                    assert[n.signed(n.read(0x1003d3b0+i*4))for i in range(100)]==expected and n.read(0x100e03e8)==((tail+1)%100 if queued else tail)
                    windows.append(dict(head=head,tail=tail,existingIndex=existing_index,enqueued=queued))
        for i in range(100):n.write(0x1003d3b0+i*4,-1)
        n.write(0x100e03e4,0);n.write(0x100e03e8,0);n.write(0x1003d5a0,0)
        for unit in range(400,501):
            p=n.RUNTIME+unit*0x2e0;n.write(p+0x70,0);n.write(p+0x74,0);n.call(n.exports['AIStart'],struct.pack('<i',unit))
        assert n.read(0x100e03e8)==1 and n.read(0x1003d3b0)==500 and[n.read(0x1003d3b0+i*4)for i in range(1,100)]==list(range(401,500))
    finally:n.close()
    paths=[Path(__file__),OUT/'check_native_alliance_metadata.py',OUT/'native_ai_emulator.py',ROOT/'분석/Ghidra/Saver_Late/AuxiliaryPackets/UserUnit-100015a0.c',ROOT/'분석/Ghidra/Saver_Late/AuxiliaryPackets/UserUnit-10002310.c']
    report=dict(nativeAiStateCases=len(ai),nativeAiQueueCases=len(queues),nativeRingWrapCases=1,nativeLiveWindowCases=len(windows),mismatches=0,unityPlay=False,runtime0CConnected=False,
        originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',
        scope='Actual UserUnit setmonsterai mutates only108=2 for active/enabled selected unit and decimal commander. Actual AIStart copies current map dimensions and searches the live head-to-tail range and enqueues eligible IDs into a100-entry ring if its current cell has a nonnegative occupant. Player commander IDs0/10/etc and IDs>=800 are excluded from this enqueue; no job/HP/count/coordinate creation. Whole736-byte selected/commander records and ring wrap are checked. Map pointer/occupancy and old records are explicit inputs; no AI execution, target selection, damage, respawn or runtime0C implementation claim.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},aiStateCases=ai,queueCases=queues,liveWindowCases=windows)
    (OUT/'native-monster-activation-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','aiStateCases','queueCases','liveWindowCases']},ensure_ascii=False))
if __name__=='__main__':main()
