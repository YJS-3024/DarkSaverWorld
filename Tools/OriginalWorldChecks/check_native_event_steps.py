"""Original event wait/ownership and synchronous15 execution; no production0D claim."""
import hashlib,json,struct
from pathlib import Path
from native_event_step_emulator import NativeEventStep
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def main():
    waits=[];movement=[];ownership=[];batches=[]
    for subject in [-1,0,7]:
        for mode in [1,2]:
            for duration in [-1,0,1,500,2147483647]:
                for delta in [0,1,499,500,2147483647,2147483648]:
                    n=NativeEventStep();n.setup(['DO '+str(duration),'UNKNOWN','END'],subject,mode);first=n.step();r=n.step(1000+delta)
                    difference=(delta+2**31)%2**32-2**31
                    held=subject>=0 and duration>=0 and difference<duration
                    assert r['line']==(2 if held else 3),(subject,mode,duration,delta,r)
                    waits.append(dict(subject=subject,mode=mode,duration=duration,delta=delta,line=r['line'],held=held))
    for token in ['DO2','WAITMOVEEND']:
        for subject in [-1,0,7]:
            n=NativeEventStep();n.setup([token+' 7','UNKNOWN','END'],subject,moving=());first=n.step()
            n.write(n.unit_address(7)+0x100,1);held=n.step(9999)
            assert held['line']==(3 if subject<0 else 2),(token,subject,held)
            n.write(n.unit_address(7)+0x100,0);released=n.step(9999);assert released['line']==(4 if subject<0 else 2)
            if subject>=0:assert n.step(9999)['line']==3
            waits.append(dict(command=token,subject=subject,heldLine=held['line'],releasedLine=released['line']))
    for mode in [1,2]:
        for subject in [-1,0,7]:
            for flags in [0,1,2,3,0x100,0xffffffff]:
                n=NativeEventStep();n.setup(['UNKNOWN','END'],subject,mode)
                if subject>=0:n.write(n.unit_address(subject)+0x100,flags)
                r=n.step();blocked=mode==1 and subject>=0 and flags&1!=0
                assert r['line']==(1 if blocked else 2),(mode,subject,flags,r)
                movement.append(dict(mode=mode,subject=subject,flags=flags,line=r['line']))
    commands={'GOLD 123':[0x3a,123,0,0,0],'USEITEM 398':[0x49,142,1],'GETGENERAL 100':[0x3c,100,0],'SCENECLEAR':[0x5c,0x20,1],'SCENECLEAR ALONE':[0x5c,0x20,2]}
    for mode in [1,2]:
        for subject in [-1,0,7]:
            for team in [0,7]:
                for command,packet in commands.items():
                    n=NativeEventStep();n.setup([command,'END'],subject,mode,team);r=n.step()
                    allowed=subject>=0 and(mode!=1 or team==0)
                    assert r['sent']==([packet]if allowed else[]),(mode,subject,team,command,r)
                    ownership.append(dict(mode=mode,subject=subject,team=team,command=command,sent=r['sent']))
    # Execute the actual15 dispatcher, allocation, F710399-slot copy and entire
    # selected bodies. Only map-change helpers are captured; their geometry has
    # an independent original MRC mutation check.
    lines=['DO 500','MESSAGE 1 COM0 0 COM0 test','GOLD 123','USEITEM 398','GETGENERAL 100','FIRSTCLEAR','SAVECLEAR','CHANGESPRITE 1 2 3 4 5 6','CHANGETILE2 1 2 3 4 5 6 7','END']
    for count in [0,1,2,5]:
        n=NativeEventStep();n.setup(['END']);empty=n.string(b'')
        for event in range(count):
            base=0x1006c120+event*0x64c;n.write(base,1)
            for i in range(399):n.write(base+16+i*4,n.string(lines[i].encode('cp949'))if i<len(lines)else empty)
        packet=b'\x5c\x15'+struct.pack('<h',count)+b''.join(struct.pack('<h',event)for event in range(count))
        n.uc.mem_write(0x0d040000,packet);n.run(0x1000eb10,[0x0d040000]);r=n.snapshot()
        assert r['sent']==[]and r['events']==[]and r['profile']==[]and r['unitReads']==[]and len(r['mutations'])==2*count,r
        if count:assert r['active']==0 and r['subject']==-1 and r['team']==-1 and r['waiting']==0
        batches.append(dict(count=count,native=r))
    paths=[Path(__file__),OUT/'native_event_step_emulator.py',*[OUT/name for name in ['native_scenario_end_emulator.py','native_result_kills_emulator.py','native_result_emulator.py','native_battle_status_emulator.py','native_battle_briefing_emulator.py','native_battle_options_emulator.py']]]
    result=dict(nativeWaitCases=len(waits),nativeMovementGateCases=len(movement),nativeOwnershipCases=len(ownership),nativeSynchronousBatchCases=len(batches),mismatches=0,originalGameCtlSha256=n.sha256,unityPlay=False,production0DConnected=False,
        scope='Exact10F20 command stepping and EB10->DCF0->F710->10F20 synchronous15 batches. Signed32 clock subtraction, subject/target movement flag, event versus scene ownership guard and five outgoing commands execute in original code. Actual15 skips interactive messages, waits and owner requests with subject=-1 but calls both map mutation helpers. MFC strings/graphics, supplied unit records/team, allocation, profile helpers, transport and map mutation helpers are documented boundaries. Not all10F20 commands or actual frame speed; no runtime0D or original server policy claim.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},waits=waits,movement=movement,ownership=ownership,batches=batches)
    (OUT/'native-event-steps-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','waits','movement','ownership','batches']},ensure_ascii=False))
if __name__=='__main__':main()
