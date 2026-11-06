"""Source execution evidence for native end-control; managed ingress is checked separately."""
import hashlib,json,struct
from pathlib import Path
from native_scenario_end_emulator import NativeScenarioEnd
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
OPEN=[[0x4ca,8,0,0],[0x4d0,8,110,10]]
def main():
    native=NativeScenarioEnd();ends=[];ack=[];confirms=[]
    for flag in [0,1,2,255]:
        for outcome in [-1,0,1,2,3,4,32767]:
            for tutorial in [0,1]:
                r=native.begin(flag,outcome,tutorial);assert r['events'][0]==['SendMessageA',[0,0x4dc,1,0]]
                if flag==1:
                    if tutorial:assert r['actions']==[]and r['callbacks']==[]and r['events'].count(['game-status',0])==2
                    elif outcome==1:assert r['actions']==[['script-boundary','DEE']]and r['callbacks']==[[0x4cb,i,0,0]for i in range(10)]and['visual-list-clear-boundary']in r['events']
                    else:assert r['actions']==[['leave-request-builder-boundary']]and['PostMessageA',[0,0x590,0,0]]in r['events']and['PostMessageA',[0,0x4cd,0,0]]in r['events']
                elif outcome in [1,3]:
                    text='테스트님이 승리하셨습니다.'if outcome==1 else'주지휘관이 죽었습니다.'
                    assert ['dialogue',[40,130,561,65],text]in r['events']and r['noticeMode']==1 and r['noticeOutcome']==(1 if outcome==1 else 0)and r['callbacks']==[]
                else:assert r['callbacks']==OPEN and r['noticeMode']==0
                ends.append(dict(flag=flag,outcome=outcome,tutorial=tutorial,native=r))
    for tutorial in [0,1]:
        for mode in [0,1,2]:
            for position in [0,3]:
                r=native.acknowledge(tutorial,mode,position)
                close=not tutorial and(mode==0 or mode==1 and position+3>=6)
                assert r['callbacks']==(OPEN if close else[])and r['active']==(0 if close else 1)
                if mode==1:assert r['firstLine']==(position if tutorial and position+3>=6 else position+3)
                ack.append(dict(tutorial=tutorial,mode=mode,position=position,native=r))
    for outcome in [1,2,3,4]:
        for tutorial in [0,1]:
            for control in [0,1]:
                native.events=[];r=native.confirm(outcome,8,tutorial,1,control)
                if control!=0 or tutorial:assert r['actions']==[]
                elif outcome==1:assert ['script-boundary','DEE']in r['actions']
                else:assert ['leave-request-builder-boundary']in r['actions']
                confirms.append(dict(outcome=outcome,tutorial=tutorial,control=control,native=r))
    team_notices=[]
    for rows in [[],[(1,'다른팀')],[(0,'검사')],[(0,'첫째'),(0,'마지막')],[(i,'팀'+str(i))for i in range(1,10)]+[(0,'열째')],[(i,'팀'+str(i))for i in range(1,21)]+[(0,'끝')],[(0,'A\0B')]]:
        packet=b'\x5c\x60'+struct.pack('<h',len(rows))+b''.join(struct.pack('<hB',team,len(name.encode('cp949')))+name.encode('cp949')for team,name in rows)
        expected=next((name.split('\0')[0]for team,name in reversed(rows)if team==0),'previous')
        received=native.receive_teams(packet,0);assert received==expected
        r=native.begin(0,1,0,None)
        assert ['dialogue',[40,130,561,65],expected+'님이 승리하셨습니다.']in r['events']
        team_notices.append(dict(rows=rows,localName=received,native=r))
    paths=[Path(__file__),OUT/'native_scenario_end_emulator.py',OUT/'native_result_kills_emulator.py',OUT/'native_result_emulator.py',OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_options_emulator.py',ROOT/'Assets/_Original/Runtime/Combat/NativeBattlePacketService.cs']
    result=dict(nativeEndCases=len(ends),nativeAcknowledgeCases=len(ack),nativeConfirmCases=len(confirms),nativeTeamToWinNoticeCases=len(team_notices),mismatches=0,unityPlay=False,productionFlowExercised=False,originalSha256=native.sha256,
        scope='Exact GameCtlEB10->D150 and08CA0 plus result8 confirm with supplied current result/winner/tutorial globals. CString, dialogue construction, UserUnit status, script load/drain, cleanup and Win32 calls are captured boundaries. Original execution evidence only: production61 flow not exercised; no inferred server send order, rewards or Unity UI equivalence claim.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},endCases=ends,acknowledgeCases=ack,confirmCases=confirms,teamNoticeCases=team_notices)
    (OUT/'native-scenario-end-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','endCases','acknowledgeCases','confirmCases','teamNoticeCases']},ensure_ascii=False))
if __name__=='__main__':main()
