"""Real preceding B2 stack bytes, not a guessed zero or controlled tail refill."""
import hashlib,json,struct
from pathlib import Path
from native_alliance_chain import AllianceUser,AllianceGame
from unicorn import UC_HOOK_MEM_WRITE
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def main():
    cases=[]
    for count in [0,1,5,20]:
        for team in [0,7,19]:
            for kind in [1,2]:
                user=AllianceUser(dict(width=1,height=1,units=[]));game=AllianceGame(user)
                try:
                    full=[(0,0)]*21;full[count]=(20,kind)
                    game.receive(full,team) # Actual first receipt fills every original local pair.
                    short=game.receive([(0,0)]*count,team,tail=None)
                    payload=bytes.fromhex(short['argumentsHex']);retained=struct.unpack('<hh',payload[2+count*4:6+count*4])
                    assert retained==(20,kind),(count,team,kind,retained)
                    expected=[1]*21;expected[20]=2 if kind==1 else 4
                    assert short['result']==dict(localTeam=team,lastLoopTeam=team,relations=expected)
                    cases.append(dict(declaredCount=count,localTeam=team,retainedPriorPair=list(retained),priorFullRows=full,result=short['result']))
                finally:user.close()
    # At count20 another actual receiver can reuse bytes at the end of that
    # local frame. Record exact retained bytes; do not infer a network tail.
    interleaved=[]
    for opcode,payload in [(0x40,struct.pack('<h',-1)),(0x5c,b'\x00')]:
        user=AllianceUser(dict(width=1,height=1,units=[]));game=AllianceGame(user)
        try:
            rows=[(0,0)]*21;rows[20]=(20,1);game.receive(rows,0)
            game.uc.mem_write(0x0d040000,bytes([0x5c,opcode])+payload);game.run(0x1000eb10,[0x0d040000])
            writes=[]
            def trace(uc,access,address,size,value,data):
                if size==4:writes.append(dict(address=address,before=user.read(address),value=value))
            handle=user.uc.hook_add(UC_HOOK_MEM_WRITE,trace,begin=0x1003d54c,end=0x1007ffff)
            try:short=game.receive([(0,0)]*20,0,tail=None)
            finally:user.uc.hook_del(handle)
            raw=bytes.fromhex(short['argumentsHex']);key,kind=struct.unpack('<hh',raw[82:86])
            expected=[1]*21
            outside=None
            if kind in [1,2]:
                value=2 if kind==1 else 4
                if 0<=key<21:expected[key]=value
                else:
                    # The real setter does not bounds-check the extra pair.
                    # This VM image contains the destination; record the actual
                    # out-of-table write rather than silently discarding it.
                    address=0x1003d54c+key*4
                    assert user.read(address)==value,(hex(address),key,kind)
                    observed=[w for w in writes if w['address']==address]
                    assert len(observed)==1 and observed[0]['value']==value and observed[0]['before']!=value,observed
                    outside=dict(key=key,address=f'{address:08X}',previousValue=observed[0]['before'],writtenValue=value,actualWriteHookObserved=True)
            assert short['result']['relations']==expected
            interleaved.append(dict(opcode=f'5C{opcode:02X}',retainedPair=[key,kind],relations=expected,outOfRelationTableWrite=outside))
        finally:user.close()
    assert any(c['retainedPair']!=[20,1]for c in interleaved),'A real intervening receiver must demonstrate stack-history sensitivity'
    paths=[Path(__file__),OUT/'native_alliance_chain.py',OUT/'check_native_alliance_metadata.py',OUT/'native_ai_emulator.py',OUT/'native_scenario_end_emulator.py',OUT/'native_result_kills_emulator.py',OUT/'native_result_emulator.py',OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_status_emulator.py',OUT/'native_battle_options_emulator.py']
    report=dict(nativeHistoryChains=len(cases),interveningReceiverChains=len(interleaved),mismatches=0,unityPlay=False,runtimeB2Connected=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},cases=cases,interleaved=interleaved,
      scope='Actual GameCtl EB10/B2 and real by-value UserUnit setter preserve the real caller return PC. Full21 receipt followed by shorter0/1/5/20 receipt inherits the original local pair without any B2 prefill on the second call.24 histories and2 intervening40/5C receivers demonstrate current wire alone cannot determine the extra relation write; bounded VM out-of-table writes are recorded rather than suppressed. The attempted66 interleaving is excluded because this host did not prepare its CString allocation boundary; separate66 checks remain independent. Stack entry addresses/initial image/sequence controlled; not original Windows caller history, arbitrary counts/key safety, guessed all-zero tail, server alliance policy or runtimeB2 integration.')
    (OUT/'native-alliance-history-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
