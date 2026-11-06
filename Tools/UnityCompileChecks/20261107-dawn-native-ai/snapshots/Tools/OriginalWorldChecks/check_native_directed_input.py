"""Actual native mode1/mode2 map-input dispatch and production pointer pages."""
import hashlib,json,subprocess
from pathlib import Path
from native_event_step_emulator import NativeEventStep
from native_ai_emulator import UC_X86_REG_ECX
from check_picture_initialization import block
from check_native_battle_responses import contract_sources
from native_event_evidence import native_event_sources
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-directed-input')
class Input(NativeEventStep):
    def hook(self,uc,address,size,data):
        if address==0x10014220:
            self.actions.append('script-clear');return # Execute its actual +1C clear and ret12.
        elif address==0x10008ca0:self.actions.append('message-page');self.ret()
        elif address==0x10009890:self.actions.append(['map-action',self.args(3)]);self.ret(0,12)
        elif address==0x1000a6e0:self.actions.append('map-cancel');self.ret(0,12)
        elif address==0x10002fb0:self.actions.append(['key',self.args(1)[0]]);self.ret()
        else:super().hook(uc,address,size,data)
    def input(self,mode,script_wait,message,action):
        self.setup(['END'],mode=mode);self.actions=[];self.write(self.SCRIPT+0x1c,script_wait);self.write(0x0d020000,message);self.write(0x1006a70c,0x0d050000)
        address,args={'left':(0x10002e10,[123,234]),'right':(0x10002eb0,[123,234]),'right-up':(0x10002ee0,[123,234]),'key':(0x10002f50,[0x71])}[action]
        try:self.run(address,args)
        except Exception as error:raise RuntimeError((mode,script_wait,message,action,self.actions))from error
        return list(self.actions)
def main():
    WORK.mkdir(exist_ok=True);n=Input();native=[]
    for mode in [1,2]:
        for waiting in [False,True]:
            for visible in [False,True]:
                for action in ['left','right','right-up','key']:
                    actual=n.input(mode,waiting,visible,action);expected=[]
                    if action in ['left','right']:
                        if waiting:expected.append('script-clear')
                        if visible:expected.append('message-page')
                    if mode==1 and action=='left':expected.append(['map-action',[123,234,0]])
                    if mode==1 and action=='right-up':expected.append('map-cancel')
                    if action=='key':expected.append(['key',0x71])
                    assert actual==expected,(mode,waiting,visible,action,actual,expected);native.append(dict(mode=mode,waiting=waiting,message=visible,action=action,calls=actual))
    campaign=ROOT/'Assets/_Original/Runtime/Campaign/WorldSceneRoot.Campaign.cs';text=campaign.read_text();start=text.index('        private bool CampaignCutscene=>');end=text.index(';',start)+1
    paint=block(text,'private void PaintCampaignActors(')[1:-1];paint=paint[paint.index('            var dialogue='):]
    extracted=WORK/'ActualDirectedInput.cs';extracted.write_text('using DarkSaver.Original.Campaign;using UnityEngine;namespace DarkSaver.Original.World {public sealed partial class WorldSceneRoot {'+text[start:end]+block(text,'private void AcknowledgeDirectedPointer(')+'private void PaintExtractedMessage(bool presenting){'+paint+'}}}')
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'DirectedInput.dll';args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeDirectedUiHost.cs'))+'\n'+str(extracted)
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    baseline=contract_sources(args,__file__)|native_event_sources();baseline[campaign.relative_to(ROOT).as_posix()]=hashlib.sha256(campaign.read_bytes()).hexdigest()
    rsp=WORK/'checks.rsp';rsp.write_text(args);sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet'
    exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,cmd in [('compile',[str(dotnet),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(dotnet),str(exe)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    for path,digest in baseline.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('source-changed-during-check',path)
    report=json.loads(r.stdout);report.update(nativeInputCases=len(native),nativeCases=native,nativeGameCtlSha256=n.sha256,mismatches=0,sources=baseline,logs=str(WORK))
    (OUT/'native-directed-input-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','nativeCases']},ensure_ascii=False))
if __name__=='__main__':main()
