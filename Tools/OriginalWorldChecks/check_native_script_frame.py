"""Execute original Update's10F20 loop; command-yield cadence, not measured FPS."""
import hashlib,json,struct
from pathlib import Path
from native_event_step_emulator import NativeEventStep
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_EIP
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Frame(NativeEventStep):
    def hook(self,uc,at,size,data):
        if at==0x10010f20:self.scriptCalls+=1
        super().hook(uc,at,size,data)
    def frame(self,clock=1000):
        self.scriptCalls=0;self.clock=clock;stack=0x0f0ff000;self.uc.reg_write(UC_X86_REG_ESP,stack)
        self.uc.emu_start(0x10003bbf,0x10003be4,timeout=1000000,count=100000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==0x10003be4 and self.uc.reg_read(UC_X86_REG_ESP)==stack
        r=self.snapshot();r['scriptCalls']=self.scriptCalls;return r
    def step(self,clock=None):
        self.scriptCalls=0;return super().step(clock)
def main():
    cases=[]
    for subject in [-1,0,7]:
        n=Frame();n.setup(['UNKNOWN','UNKNOWN','END'],subject);rows=[n.frame()for _ in range(3)]
        assert [r['line']for r in rows]==([4,4,4]if subject<0 else[2,3,4])and[r['scriptCalls']for r in rows]==([3,0,0]if subject<0 else[1,1,1])
        cases.append(dict(kind='unknown-token-yields',subject=subject,frames=rows))
    for subject in [-1,0,7]:
        n=Frame();n.setup(['DO 500','GOLD 123','END'],subject);r=n.frame()
        assert r['scriptCalls']==(3 if subject<0 else 1)and r['line']==(4 if subject<0 else 2)
        if subject>=0:
            before=n.frame(1499);after=n.frame(1500);assert before['line']==2 and after['line']==3 and after['sent']==[[0x3a,123,0,0,0]]
        cases.append(dict(kind='skip-or-timer',subject=subject,first=r))
    n=Frame();n.setup(['FIRSTCLEAR','TEST','(','GOLD 123',')','END']);rows=[n.frame()for _ in range(4)]
    assert rows[0]['profile']==[['FIRSTCLEAR',True]]and rows[0]['scriptCalls']==1
    assert any(r['sent']==[[0x3a,123,0,0,0]]for r in rows)
    cases.append(dict(kind='condition-yield-loop',frames=rows))
    n=Frame();n.setup(['DO2 7','UNKNOWN','END']);first=n.frame();n.write(n.unit_address(7)+0x100,1);held=n.frame();n.write(n.unit_address(7)+0x100,0);release=n.frame();nextFrame=n.frame()
    assert first['line']==held['line']==2 and release['line']==3 and release['scriptCalls']==2 and nextFrame['line']==4
    cases.append(dict(kind='movement-release-reenters-same-update',frames=[first,held,release,nextFrame]))
    files=[Path(__file__),OUT/'native_event_step_emulator.py',OUT/'native_scenario_end_emulator.py',OUT/'native_result_kills_emulator.py']
    report=dict(nativeUpdateCases=len(cases),mismatches=0,originalGameCtlSha256=n.sha256,unityPlay=False,actualFpsMeasured=False,managedFrameEquivalenceTested=False,
        scope='Actual GameCtl Update03BBF..03BE4 calls10F20, stops at return1 and continues at return0. Eligible unknown commands yield; skipped owner commands run through in one update; DO uses signed clock; DO2 movement release returns0 so the next command runs in the same update. MFC/graphics/unit/profile/transport boundaries are supplied. This establishes command cadence only, not original FPS, wall-clock playback or current Unity presentation equivalence.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in files},cases=cases)
    (OUT/'native-script-frame-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
