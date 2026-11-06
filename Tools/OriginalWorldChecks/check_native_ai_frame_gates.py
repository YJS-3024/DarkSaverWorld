"""Execute original GameCtl full Update entry with controlled AI/interpreter results."""
import hashlib,itertools,json
from pathlib import Path
from native_ai_frame_emulator import NativeAiFrame
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def main():
    native=NativeAiFrame();cases=[]
    for ready,mode,active,phase,yields in itertools.product([0,1],[-1,0,1,2,3],[0,1],[0,1,2],[1,3]):
        result=native.frame(ready,mode,active,phase,yields);modal=not ready and mode==2
        calls=['script']*yields if modal else['ai']if phase==1 and mode!=2 else[]
        assert result==dict(calls=calls,returned=1,counterDelta=0 if modal else 1),(ready,mode,active,phase,yields,result)
        cases.append(dict(mapReady=ready,scriptMode=mode,scriptActive=active,phase=phase,scriptReturnsZeroBefore=yields-1,native=result))
    evidence=ROOT/'분석/Ghidra/Saver_Late/AiFrameDispatch/evidence.json';data=json.loads(evidence.read_text())
    sources=[Path(__file__),OUT/'native_ai_frame_emulator.py',OUT/'native_battle_options_emulator.py',OUT/'native_ai_emulator.py',evidence]
    for row in data['files']:
        path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];sources.append(path)
    report=dict(nativeUpdateCases=len(cases),mismatches=0,unityPlay=False,actualFpsMeasured=False,runtime0CConnected=False,originalGameCtlSha256=native.sha256,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},cases=cases,
        scope='Actual GameCtl03840 full entry/return, SEH shell and switch gates. Valid script object and mode/active/map-ready supplied; unused CString lifetimes, aiexcute and thiscall10F20 return sequence are captured boundaries. Compare AI calls, script-pump count, return and global frame-counter delta. Active is not an outer Update gate; supplied zero-return sequences exercise the caller loop, not actual inactive script semantics. No phase3..9 rendering, source-null undefined state, full scheduler/callee execution, Windows/Unity Play or FPS/timing measurement claim.')
    (OUT/'native-ai-frame-gates-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
