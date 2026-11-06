"""Original0D queue and event-start subject data flow; no server event policy."""
import hashlib,json,struct
from pathlib import Path
from native_scenario_end_emulator import NativeScenarioEnd
from native_ai_emulator import UC_X86_REG_ECX
from check_native_script_frame import Frame
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Handoff(Frame):
    def __init__(self):super().__init__();self.scriptCalls=0
    def hook(self,uc,address,size,data):
        if 'GetTeamID@@'in self.hooks.get(address,''):self.ret(0) # Explicit supplied team, never ID/50.
        else:super().hook(uc,address,size,data)
    def deliver(self,event):
        self.uc.mem_write(0x0d040000,b'\x5c\x0d'+struct.pack('<hh',event,0));self.run(0x1000eb10,[0x0d040000])
    def observed_frame(self):
        r=self.frame();return dict(event=self.read(self.SCRIPT+16),line=r['line'],active=r['active'],calls=r['scriptCalls'],queued=self.read(0x100936d0),sent=r['sent'])
def handoff_cases():
    cases=[]
    for initial_unknown in [False,True]:
        for second_exists in [False,True]:
            n=Handoff();n.setup(['END']);n.write(n.SCRIPT+12,0)
            # Populate all100 empty sentinels: leaving zeroed unused records is
            # a different, invalid queue fixture after the shift operation.
            n.uc.mem_write(0x100936d4,b'\xff'*400);empty=n.string(b'')
            for number,lines,exists in [(1,['UNKNOWN','END']if initial_unknown else['END'],True),(2,['GOLD 123','END'],second_exists),(3,['GOLD 456','END'],True)]:
                base=0x1006c120+number*0x64c;n.write(base,int(exists))
                for i in range(399):n.write(base+16+i*4,n.string(lines[i].encode())if i<len(lines)else empty)
            for number in [1,2,3]:n.deliver(number)
            assert n.read(0x100936d0)==2
            frames=[]
            if initial_unknown:
                r=n.observed_frame();frames.append(r);assert r['event']==1 and r['line']==2 and r['calls']==1 and r['sent']==[]
            r=n.observed_frame();frames.append(r)
            assert r['calls']==1 and r['queued']==1 and r['sent']==[]
            if second_exists:
                assert r['event']==2 and r['line']==0 and r['active']==1 # Next body starts on the next Update.
                r=n.observed_frame();frames.append(r);assert r['sent']==[[0x3a,123,0,0,0]]
                r=n.observed_frame();frames.append(r);assert r['event']==3 and r['line']==0 and r['calls']==1 and r['queued']==0
            else:
                assert r['event']==1 and r['active']==0
                idle=n.observed_frame();frames.append(idle);assert idle['calls']==0 and idle['queued']==1 and idle['sent']==[]
                n.deliver(-1);assert n.read(n.SCRIPT+16)==3 and n.read(0x100936d0)==0
            r=n.observed_frame();frames.append(r);expected=([[0x3a,123,0,0,0]]if second_exists else[])+[[0x3a,200,1,0,0]];assert r['sent']==expected
            r=n.observed_frame();frames.append(r);assert r['active']==0 and r['queued']==0 and r['sent']==expected
            cases.append(dict(initialUnknown=initial_unknown,secondExists=second_exists,frames=frames,explicitQueuePumpAfterMissing=not second_exists))
    return cases
class Events(NativeScenarioEnd):
    SCRIPT=0x0d030000
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if address==0x1000f710 and self.captureStart:
            self.started.append(self.args(1)[0]);self.ret(0,4)
        elif name and 'GetTeamID@@'in name:
            value=self.args(1)[0];value=value-2**32 if value>=2**31 else value;self.teams.append(value);self.ret(17)
        else:super().hook(uc,address,size,data)
    def queue(self,count,busy,event,subject):
        self.open();self.captureStart=True;self.started=[];self.teams=[]
        self.write(0x10093864,self.SCRIPT);self.uc.mem_write(self.SCRIPT,bytes(2048));self.write(self.SCRIPT+12,busy)
        self.write(0x100936d0,count)
        original=[(i,100+i)for i in range(count)]
        for i in range(100):self.uc.mem_write(0x100936d4+4*i,struct.pack('<hh',*(original[i]if i<count else(-1,-1))))
        self.uc.mem_write(0x0d040000,b'\x5c\x0d'+struct.pack('<hh',event,subject));self.run(0x1000eb10,[0x0d040000])
        actual=self.read(0x100936d0);rows=[struct.unpack('<hh',self.uc.mem_read(0x100936d4+4*i,4))for i in range(actual)]
        return dict(count=actual,rows=rows,started=self.started)
    def start(self,event,subject,exists):
        self.open();self.captureStart=False;self.started=[];self.teams=[];empty=self.string(b'')
        self.uc.mem_write(self.SCRIPT,bytes([0xa5])*2048)
        entry=0x1006c120+event*0x64c;self.write(entry,exists)
        for i in range(399):self.write(entry+16+i*4,empty)
        self.uc.reg_write(UC_X86_REG_ECX,self.SCRIPT);self.run(0x1000f710,[(event&65535)|((subject&65535)<<16)])
        signed=lambda p:struct.unpack('<i',self.uc.mem_read(p,4))[0]
        return dict(event=signed(self.SCRIPT+16),subject=signed(self.SCRIPT+20),team=signed(self.SCRIPT+24),active=signed(self.SCRIPT+12),teams=self.teams,
                    initialized=[signed(self.SCRIPT+p)for p in [4,0x1c,0x34,0x20,0x2c,0x24]])
def main():
    n=Events();queues=[];starts=[]
    for count in [0,1,2,99,100]:
        for busy in [0,1]:
            for event in [-1,0,1]:
                r=n.queue(count,busy,event,2999);expected=[(i,100+i)for i in range(count)]
                if count<100 and event!=-1:expected.append((event,2999))
                started=[]
                if not busy and expected:
                    a,b=expected.pop(0);started=[(a&65535)|((b&65535)<<16)]
                assert r==dict(count=len(expected),rows=expected,started=started),(count,busy,event,r)
                queues.append(dict(count=count,busy=busy,event=event,after=r))
    for event in [0,1,99]:
        for subject in [-32768,-2,-1,0,1,2999,32767]:
            for exists in [0,1]:
                r=n.start(event,subject,exists)
                if exists:assert r==dict(event=event,subject=subject,team=-1 if subject==-1 else 17,active=1,teams=[]if subject==-1 else[subject],initialized=[1,0,0,0,-1,-1]),r
                else:assert r==dict(event=-1515870811,subject=-1515870811,team=-1515870811,active=-1515870811,teams=[],initialized=[-1515870811]*6),r
                starts.append(dict(event=event,subject=subject,exists=exists,after=r))
    handoffs=handoff_cases()
    paths=[Path(__file__),*[OUT/name for name in ['native_scenario_end_emulator.py','native_result_kills_emulator.py','native_result_emulator.py','native_battle_briefing_emulator.py','native_battle_status_emulator.py','native_battle_options_emulator.py','native_event_step_emulator.py','check_native_script_frame.py','native_ai_emulator.py']]]
    report=dict(nativeQueueCases=len(queues),nativeEventStartCases=len(starts),nativeUpdateHandoffCases=len(handoffs),mismatches=0,unityPlay=False,runtime0DConnected=False,originalGameCtlSha256=n.sha256,
        scope='Actual EB10->D8A0->F070 two-signed-WORD queue and actual F710 event-start state. Queue cases capture F710; start cases execute399 CString copies with supplied strings/GetTeamID. Four cases combine actual receipt, F710, Update loop and selected UNKNOWN/GOLD/END bodies: END can start the next event but its body waits until the next Update; missing event leaves the remaining queue idle until another queue pump. Full100 sentinel slots are initialized explicitly. Not all10F20 commands, original Windows process, server trigger policy or runtime0D implementation.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},queueCases=queues,eventStarts=starts,updateHandoffs=handoffs)
    (OUT/'native-event-queue-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','queueCases','eventStarts','updateHandoffs']},ensure_ascii=False))
if __name__=='__main__':main()
