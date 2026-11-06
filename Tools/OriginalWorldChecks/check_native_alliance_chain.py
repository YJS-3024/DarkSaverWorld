"""Bounded original B2 ABI chain and stale-local/return-byte consequences."""
import hashlib,json
from pathlib import Path
from native_alliance_chain import AllianceUser,AllianceGame
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent

def main():
    user=AllianceUser(dict(width=1,height=1,units=[]));game=AllianceGame(user);cases=[]
    try:
        user.write(0x1003d548,0x12345678)
        for count in [-1,0,1,5,20,21]:
            for team in [0,7,19]:
                for tail in [(20,0),(20,1),(20,2)]:
                    rows=[(i,i%4)for i in range(max(0,count))]
                    beforeLoopTeam=user.read(0x1003d548)
                    result=game.receive(rows,team,tail,count);actual=result['result'];expected=[1]*21
                    for key,kind in rows:
                        if kind in [1,2]:expected[key]=2 if kind==1 else 4
                    if 0<=count<21 and tail[1]in [1,2]:expected[tail[0]]=2 if tail[1]==1 else 4
                    assert actual==dict(localTeam=team,lastLoopTeam=team if count>=0 else beforeLoopTeam,relations=expected),(count,team,tail,actual,expected)
                    cases.append(dict(count=count,localTeam=team,controlledTail=list(tail),**result))
        # The input is not fully determined by the packet when declared count<21.
        for count in [0,1,5,20]:
            for team in [0,7,19]:
                sample=[r for r in cases if r['count']==count and r['localTeam']==team]
                assert[s['result']['relations'][20]for s in sample]==[1,2,4]
        # All21 explicit rows overwrite the whole86-byte by-value struct. The
        # extra setter read sees caller return lowWORD D540, so it does not write.
        for team in [0,7,19]:
            sample=[r for r in cases if r['count']==21 and r['localTeam']==team]
            assert all(r['result']==sample[0]['result']for r in sample)
    finally:user.close()
    evidence=ROOT/'분석/Ghidra/Saver_Late/AllianceLifecycle'
    sources=[Path(__file__),OUT/'native_alliance_chain.py',OUT/'check_native_alliance_metadata.py',OUT/'native_ai_emulator.py',OUT/'native_scenario_end_emulator.py',OUT/'native_result_kills_emulator.py',OUT/'native_result_emulator.py',OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_status_emulator.py',OUT/'native_battle_options_emulator.py']
    sources.extend(p for p in evidence.iterdir()if p.suffix in ['.c','.java','.txt','.log','.json'])
    report=dict(nativeChains=len(cases),controlledAmbiguousCountCases=36,completeStructCases=9,negativeCountCases=9,mismatches=0,unityPlay=False,runtimeB2Connected=False,callerReturnPc='1000d540',originalGameCtlSha256=game.sha256,originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},cases=cases,
      scope='Actual GameCtl EB10/B2 by-value arguments and actual UserUnit SetAllyInfo linked in separate original instances with literal real caller return PC. Supplied stale local pairs20/0,20/1,20/2 prove count0..20 is not a function of wire bytes alone. Count21 overwrites the full86-byte struct; extra setter kind reads real return lowWORD D540 and does not write. Negative count resets21 relations to1, updates localTeam, preserves lastLoopTeam. No original stack history recovery, arbitrary count/key memory safety, original DLL initialization/Windows process, UI/server alliance policy or runtimeB2 connection.')
    (OUT/'native-alliance-chain-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
