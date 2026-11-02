"""Execute actual UserUnit attack/rest allocation, byte/WORD writers and send-boundary length."""
import hashlib,json,struct,subprocess
from pathlib import Path
from check_native_feedback import NativeFeedback
from native_ai_emulator import UC_HOOK_CODE
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-outbound-packet-checks')
class NativeOutgoing(NativeFeedback):
    def __init__(self,case):
        super().__init__(case);self.sent=[]
        for address,name in [(0x10004f70,'send-bytes'),(0x10011896,'bounded-new'),(0x1001188a,'bounded-delete')]:
            self.hooks[address]=name;self._hook_add(UC_HOOK_CODE,self._dispatch,begin=address,end=address)
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name in ['send-attack','send-rest']:return # Execute actual original builders, not planner send stubs.
        if name=='bounded-new':
            count=self.args(1)[0];assert count in [4,10];self.ret(self.DATA+0x70000);return
        if name=='bounded-delete':self.ret();return
        if name=='send-bytes':
            ptr,count=self.args(2);assert count in [4,10];self.sent.append(bytes(self.uc.mem_read(ptr,count)).hex());self.ret();return
        super()._dispatch(uc,address,size,data)
    def run_packet(self):
        c=self.case
        if c['operation']=='rest':self.invoke(0x100051e0,[c['source']])
        else:
            self.uc.mem_write(self.DATA+0x6000,struct.pack('<HHHH',c['source'],c['target'],c['sourceTerrain'],c['targetTerrain']));self.invoke(0x10005240,[self.DATA+0x6000])
        assert len(self.sent)==1;result=self.sent[0];self.close();return result

def vectors():
    cases=[]
    for source in [0,1,999,2999]:
        for a in [0,1,127,128,255,32767,32768,65535]:
            for b in [0,1,127,128,255,32767,32768,65535]:cases.append(dict(operation='attack',source=source,target=400,sourceTerrain=a,targetTerrain=b,width=12,height=12,units=[]))
        cases.append(dict(operation='rest',source=source,target=-1,sourceTerrain=-1,targetTerrain=-1,width=12,height=12,units=[]))
    return cases

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'OutboundPacketDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'OutboundPacketDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());mismatches=[]
    for case,current in zip(cases,managed):
        native=NativeOutgoing(case).run_packet()
        if native!=current:mismatches.append(dict(input=case,native=native,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeBattlePackets.cs','Tools/OriginalWorldChecks/OutboundPacketDifferential.cs','Tools/OriginalWorldChecks/check_native_outbound_packets.py','Tools/OriginalWorldChecks/native_ai_emulator.py','Tools/OriginalWorldChecks/check_native_feedback.py','Assets/_Original/Runtime/Combat/TrainingCombat.cs']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual native10005240/100051E0 and native byte/WORD append functions execute. Allocation/free and final post-message send boundary are isolated; compare complete connected-mode application bytes/length. No host network, socket framing or guessed costs/other action formats.',logs=str(WORK))
    (OUT/'native-outbound-packet-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
