"""Original B2 alliance metadata ABI and UserUnit team lookup; no runtime policy."""
import hashlib,json,struct
from pathlib import Path
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
from native_scenario_end_emulator import NativeScenarioEnd
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Receiver(NativeScenarioEnd):
    def hook(self,uc,at,size,data):
        if 'SetAllyInfo@@'in self.hooks.get(at,''):
            stack=uc.reg_read(UC_X86_REG_ESP);raw=bytes(uc.mem_read(stack+4,86));count=struct.unpack('<h',raw[:2])[0]
            self.received=dict(count=count,pairs=[list(struct.unpack('<hh',raw[2+i*4:6+i*4]))for i in range(max(0,count))],localTeam=self.read(stack+92));self.ret()
        else:super().hook(uc,at,size,data)
class Teams(NativeAi):
    def _dispatch(self,uc,address,size,data):
        if address==self.exports.get('GetTeamID'):return # Execute real lookup rather than the AI oracle boundary.
        super()._dispatch(uc,address,size,data)
    def call(self,address,payload):
        stack=self.STACK+0xff000;self.uc.mem_write(stack-256,bytes(512));self.uc.mem_write(stack,struct.pack('<I',self.STOP)+payload);self.uc.reg_write(UC_X86_REG_ESP,stack)
        self.uc.emu_start(address,self.STOP+1,timeout=1000000,count=100000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
        return self.read(0x1003d540),self.read(0x1003d548),[self.read(0x1003d54c+i*4)for i in range(21)]
def main():
    receiver=Receiver();receipts=[];lookups=[];setters=[];extra=[]
    for count in [0,1,5,20,21]:
        for team in [0,7,19]:
            rows=[(i,(i%4))for i in range(count)];packet=b'\x5c\xb2'+struct.pack('<h',count)+b''.join(struct.pack('<hh',*r)for r in rows)
            receiver.write(0x1006a738,team);receiver.uc.mem_write(0x0d040000,packet);receiver.run(0x1000eb10,[0x0d040000])
            assert receiver.received==dict(count=count,pairs=[list(r)for r in rows],localTeam=team),receiver.received
            receipts.append(dict(count=count,localTeam=team))
    n=Teams(dict(width=1,height=1,units=[]))
    try:
        for unit in [0,7,400,2999]:
            for team in [-32768,-1,0,7,19,20,32767]:
                n.write(n.RUNTIME+unit*0x2e0+0x64,team);n.call(n.exports['GetTeamID'],struct.pack('<i',unit));actual=n.signed(n.uc.reg_read(UC_X86_REG_EAX));assert actual==team
                lookups.append(dict(unit=unit,receivedTeam=team))
        for count in [0,1,5,20]:
            for team in [0,7,19]:
                rows=[(i,i%4)for i in range(count)]+[(0,0)]*(21-count);blob=struct.pack('<h',count)+b''.join(struct.pack('<hh',*r)for r in rows)+b'\0\0'+struct.pack('<i',team)
                a,b,actual=n.call(n.exports['SetAllyInfo'],blob);expected=[1]*21
                for key,kind in rows[:count+1]:
                    if kind in [1,2]:expected[key]=2 if kind==1 else 4
                assert a==team and b==team and actual==expected
                setters.append(dict(count=count,localTeam=team,relations=actual))
        # A controlled possible stale tail proves the extra consumer iteration.
        # These bytes are explicit test input, not recovered original stack data.
        for count in [0,1,5,20]:
            rows=[(0,0)]*21;rows[count]=(17,1);blob=struct.pack('<h',count)+b''.join(struct.pack('<hh',*r)for r in rows)+b'\0\0'+struct.pack('<i',0)
            _,_,actual=n.call(n.exports['SetAllyInfo'],blob);assert actual[17]==2
            extra.append(dict(declaredPairs=count,controlledTailIndex=count,observedRelation=actual[17]))
    finally:n.close()
    paths=[Path(__file__),OUT/'native_ai_emulator.py',OUT/'native_scenario_end_emulator.py',ROOT/'분석/Ghidra/Saver_Late/AuxiliaryPackets/1000d4c0.c',ROOT/'분석/Ghidra/Saver_Late/AuxiliaryPackets/UserUnit-10004a40.c',ROOT/'분석/Ghidra/Saver_Late/AuxiliaryPackets/UserUnit-10001580.c',ROOT/'분석/Ghidra/Saver_Late/AuxiliaryPackets/1000d4c0.asm.txt']
    report=dict(nativeB2ReceiptCases=len(receipts),nativeTeamLookupCases=len(lookups),nativeAllianceSetterCases=len(setters),nativeControlledTailCases=len(extra),mismatches=0,unityPlay=False,runtimeB2Connected=False,
        originalGameCtlSha256=receiver.sha256,originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',
        scope='Actual GameCtl B2 receiver passes count and WORD pairs as86 bytes padded to88 plus explicit current-team DWORD. Actual UserUnit GetTeamID reads received runtime+64, never unit/50. Actual SetAllyInfo initializes21 relations to1 and maps supplied kind1/2 to2/4. Its count+1 loop also consumes a controlled stale-tail fixture; do not reproduce unknown original stack data or claim safe arbitrary counts. Setter kind names, real other-user UI/server policy and runtimeB2 integration are excluded.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},receipts=receipts,lookups=lookups,setters=setters,controlledTail=extra)
    (OUT/'native-alliance-metadata-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','receipts','lookups','setters','controlledTail']},ensure_ascii=False))
if __name__=='__main__':main()
