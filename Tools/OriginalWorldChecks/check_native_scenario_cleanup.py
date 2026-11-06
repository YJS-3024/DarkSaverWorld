"""Actual visual linked-list clear and one-byte5B builder, distinguished from the0D FIFO."""
import hashlib,json
from pathlib import Path
from native_scenario_end_emulator import NativeScenarioEnd
from native_ai_emulator import UC_X86_REG_ECX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Cleanup(NativeScenarioEnd):
    def __init__(self):super().__init__();self.calls=[];self.confirming=False
    def hook(self,uc,at,size,data):
        if at==0x10008d20:return # Execute08D20 and091E0 unchanged.
        if at==0x100092b0:self.calls.append(['destroy',uc.reg_read(UC_X86_REG_ECX)]);self.ret()
        elif at==0x1000e2c0:
            p,count=self.args(2);self.calls.append(['send-boundary',list(uc.mem_read(p,count))]);self.ret()
        elif self.hooks.get(at,'')in ['??3@YAXPAX@Z','#825']:self.calls.append(['delete',self.args(1)[0]]);self.ret()
        else:super().hook(uc,at,size,data)
def main():
    cases=[]
    for count in [1,2,3,10,100]:
        n=Cleanup();head=0x0d060000
        for i in range(count):n.write(head+i*256+8,head+(i+1)*256 if i+1<count else 0)
        n.write(0x1006a70c,head);n.write(0x100936d0,2);n.uc.mem_write(0x100936d4,b'\x05\0\x07\0\x08\0\x09\0')
        before=bytes(n.uc.mem_read(0x100936d0,12));n.run(0x10008d20,[])
        assert n.read(0x1006a70c)==head+(count-1)*256
        assert n.calls==[v for i in range(count-1)for v in [['destroy',head+i*256],['delete',head+i*256]]]
        assert bytes(n.uc.mem_read(0x100936d0,12))==before
        cases.append(dict(nodesIncludingSentinel=count,deleted=count-1,eventFifoUnchanged=True))
    n=Cleanup();n.run(0x1000e750,[]);assert n.calls==[['send-boundary',[0x5b]]]
    keys=[]
    for key in [0x1b,0x0d]:
        for outcome in [1,3]:
            n=Cleanup();n.open();n.uc.mem_write(n.OBJECT,bytes([0xa5])*512);n.write(n.OBJECT,1);n.write(n.OBJECT+0x18,1);n.write(0x1006a708,n.OBJECT);n.write(0x1006a740,outcome)
            before=bytes(n.uc.mem_read(n.OBJECT,512));trace=n.key(key,[True]*4,[True]*4,existing=[8,28])
            assert trace==[] and bytes(n.uc.mem_read(n.OBJECT,512))==before and n.calls==[]
            keys.append(dict(key=key,outcome=outcome,callbacks=trace,noticeUnchanged=True))
    files=[Path(__file__),*[OUT/name for name in ['native_scenario_end_emulator.py','native_result_kills_emulator.py','native_result_emulator.py','native_battle_status_emulator.py','native_battle_briefing_emulator.py','native_battle_options_emulator.py','native_ai_emulator.py']],
        *[ROOT/'분석/Ghidra/Saver_Late/ScenarioEndControl'/name for name in ['10008d20.c','100091e0.c','1000e750.c','Saver-00414640.c','Saver-00412c80-key.c','Saver-00423500-key.c','Saver-00424ad0-key.c']],
        ROOT/'분석/Ghidra/Saver_Late/BattleWindows/10002fb0.c']
    report=dict(nativeVisualClearCases=len(cases),nativeLeaveBuilderCases=1,nativeEndKeyCases=len(keys),mismatches=0,unityPlay=False,originalGameCtlSha256=n.sha256,
        scope='Original08D20/091E0 remove linked visual nodes until the tail sentinel and preserve a populated0D FIFO. Destruction/free are captured boundaries; no GPU resource release claim. Actual E750/C600 appends one root5B byte and reaches captured E2C0 transport; mode/socket delivery is excluded. Actual GameCtl OnKeyDown ignores Escape/Enter with an end notice active; recorded Saver key handlers forward keys or close their own input child, not a newly invented end acknowledgement. Saver host restoration/key handling is static evidence only, not full Windows input execution. Do not call08D20 a script instruction drain or E750 the whole map cleanup.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in files},cases=cases,keyCases=keys,leavePacketHex='5b')
    (OUT/'native-scenario-cleanup-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
