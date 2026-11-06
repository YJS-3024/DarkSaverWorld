"""Execute every repository EVT body in original10F20 with explicit host boundaries.

This is an interpreter transcript, not a runtime0D bridge, server trigger oracle,
message renderer, magic target hit, audio playback or original frame-rate check.
"""
import collections
import hashlib
import json
import subprocess
from pathlib import Path

import check_magic_projectiles as magic
from native_event_step_emulator import NativeEventStep

ROOT=magic.ROOT
OUT=magic.OUT
WORK=Path('/tmp/ds-native-event-corpus')


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


class Corpus(NativeEventStep):
    def __init__(self,random_value):
        super().__init__()
        self.heap=0x0d060000
        self.random_value=random_value
        self.messages=[]
        self.boundaries=[]

    def hook(self,uc,address,size,data):
        name=self.hooks.get(address,'')
        if address==0x100109e0:
            # Capture the complete MESSAGE helper argument, not its rendering.
            self.messages.append(self.text(self.args(1)[0]).decode('cp949'))
            self.ret(0,4)
        elif name=='time':
            pointer=self.args(1)[0]
            if pointer:self.write(pointer,123456)
            self.ret(123456)
        elif name=='srand':self.ret()
        elif name=='rand':self.ret(self.random_value)
        elif 'dmKillChar@@'in name:
            self.boundaries.append(['dmKillChar',self.args(1)[0]])
            self.ret()
        elif 'CheckMagicTarget@@'in name:
            self.boundaries.append(['CheckMagicTarget-no-target',self.args(4)])
            self.ret(0)
        elif name=='#823' or name.startswith('??2@'):
            count=self.args(1)[0]
            assert 0<count<=2048,count
            pointer=self.heap
            self.heap+=(count+15)&~15
            assert self.heap<0x0d080000
            self.ret(pointer)
        else:
            if name=='SendMessageA':
                args=self.args(4)
                if args[1]==0x591:
                    self.boundaries.append(['PLAYMIDI',args[2],self.text(args[3]).decode('cp949')])
            super().hook(uc,address,size,data)

    def execute(self,body,requests,team=0):
        self.setup(body,team=team)
        self.write(0x1001a50c,1)
        self.write(0x1001a508,1)
        steps=[]
        request_comparisons=0
        for _ in range(len(body)*4+10):
            line=self.read(self.SCRIPT+0x34) or 1
            command=body[line-1]if 0<line<=len(body)else ''
            packet=requests.get(command)if team==0 and self.read(0x1001a50c)!=0 else None
            sent=len(self.sent)
            result=self.step(self.clock)
            # Compare each emitted operation to the existing production packet
            # builder, at the exact line and native TEST/ownership state.
            assert self.sent[sent:]==([packet]if packet is not None else []),(command,team,self.sent[sent:],packet)
            if packet is not None:request_comparisons+=1
            steps.append(dict(line=result['line'],yielded=bool(result['returnValue']),
                execution=result['execution'],testResult=result['testResult']))
            if not result['active']:break
            # Supply immediate user dismissal; its UI/timing is outside this
            # transcript. The original interpreter still handles DO clock gates.
            self.write(self.SCRIPT+0x1c,0)
            self.write(self.SCRIPT+0x30,0)
            self.write(0x0d020000,0)
            if result['waiting']==1:
                self.clock=(self.clock+max(0,result['waitValue'])+1)&0xffffffff
        else:raise AssertionError(('event-did-not-end',body,result))
        return dict(steps=steps,final=result,messages=self.messages,boundaries=self.boundaries,requestComparisons=request_comparisons)


def load_bodies(files):
    """Compile/reexecute the existing production Events-parser harness."""
    WORK.mkdir(exist_ok=True)
    old=Path('/tmp/ds-npc-gateway-checks')
    assert (old/'checks.rsp').is_file() and (old/'GatewayChecks.runtimeconfig.json').is_file(), 'Prepare the same work directory with check_gateways.py before this check.'
    exe=WORK/'EventCorpusParser.dll'
    harness=OUT/'NativeImmediateEventsDifferential.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(
        str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    args+='\n'+str(OUT.resolve()/'NativeEventCorpusRequests.cs')+'\n/main:NativeEventCorpusRequests\n'
    response=WORK/'parser.rsp'
    response.write_text(args)
    input_path=WORK/'input.json'
    input_path.write_text(json.dumps([str(p)for p in files]))
    output=WORK/'bodies.json'
    sdk=magic.UNITY/'DotNetSdk'
    exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(response)]),
            ('parse',[str(sdk/'dotnet'),str(exe),str(input_path),str(output)])]:
        run=subprocess.run(command,cwd=ROOT,text=True,capture_output=True)
        (WORK/(phase+'.log')).write_text(run.stdout+run.stderr)
        assert run.returncode==0,(phase,(run.stdout+run.stderr)[-2400:])
    return json.loads(output.read_text()),json.loads(Path(str(output)+'.requests.json').read_text())


def main():
    files=sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*/*.evt.txt'))
    assert len(files)==112
    bodies,requests=load_bodies(files)
    assert len(bodies)==1247
    # These spellings may be handled by server/file-loader code, but do not
    # dispatch an operation from10F20. They are not aliases for similarly named
    # client opcodes (GET_GENERAL versus GETGENERAL, MASSAGE versus MESSAGE).
    noops=['GETITEM 300','REGEN 2','CLASSUP','SET_DUTY_STEP 2 2','ENDING_DEMO B',
        'NEXTSCENE 03','COM_GENERAL 164 155 158 159 0','GET_GENERAL 109',
        'MASSAGE 1 ME 0 0 text','WARPCHAR 15 10','CHANGETILE CLEAR','CHARONCE',
        'SAVE','FX1 CLEAR','COMMANDER','ONCE','CHAR','ALL','INF']
    noop_cases=[]
    for execution in [0,1]:
        native=Corpus(0)
        native.setup(['UNKNOWN','END'])
        native.write(0x1001a50c,execution)
        expected=native.step()
        for command in noops:
            native=Corpus(0)
            native.setup([command,'END'])
            native.write(0x1001a50c,execution)
            result=native.step()
            assert result==expected and not native.messages and not native.boundaries,(command,execution,result)
            noop_cases.append(dict(command=command,execution=execution,native=result))
    transcript=[]
    summaries=[]
    counts=collections.Counter()
    failure=[]
    random_divergences=0
    context_checks=0
    inputs=collections.Counter(line.split()[0]for body in bodies for line in body['body'])
    for index,body in enumerate(bodies):
        # Both endpoints straddle all RANDOM50 branches; foreign ownership is
        # an independently supplied context, never inferred from a unit ID.
        variants=[]
        for team,random_value in [(0,0),(0,100),(7,0)]:
            try:
                native=Corpus(random_value)
                trace=native.execute(body['body'],requests,team)
                final=trace['final']
                assert final['active']==0
                if team!=0:
                    assert final['sent']==[] and trace['messages']==[] and trace['boundaries']==[],trace
                    assert final['profile']==[] and final['events']==[],trace
                counts['steps']+=len(trace['steps'])
                counts['messageBoundaries']+=len(trace['messages'])
                counts['mapMutationBoundaries']+=len(final['mutations'])
                counts['sentClientRequests']+=len(final['sent'])
                counts['productionRequestByteComparisons']+=trace['requestComparisons']
                counts.update('request-'+bytes(packet).hex()for packet in final['sent'])
                counts.update('boundary-'+event[0]for event in trace['boundaries'])
                digest=hashlib.sha256(json.dumps(trace,ensure_ascii=False,sort_keys=True).encode()).hexdigest()
                transcript.append(dict(file=body['file'],number=body['number'],team=team,randomValue=random_value,trace=trace))
                variants.append(trace)
                summaries.append(dict(fileIndex=files.index(Path(body['file'])),event=body['number'],team=team,
                    randomValue=random_value,steps=len(trace['steps']),messageBoundaries=len(trace['messages']),
                    sent=[bytes(packet).hex()for packet in final['sent']],mutations=len(final['mutations']),
                    traceSha256=digest))
            except Exception as error:
                failure.append(dict(file=body['file'],number=body['number'],team=team,randomValue=random_value,error=str(error)))
                raise
        # Pre-owner map operations must agree even when local personal requests
        # and messages are suppressed by the supplied foreign-team context.
        assert variants[0]['final']['mutations']==variants[1]['final']['mutations']==variants[2]['final']['mutations']
        context_checks+=1
        if (variants[0]['final']['sent'],variants[0]['messages'])!=(variants[1]['final']['sent'],variants[1]['messages']):
            random_divergences+=1
        if index%200==0:print(json.dumps(dict(executedBodies=index+1)),flush=True)
    transcript_path=WORK/'transcripts.json'
    transcript_path.write_text(json.dumps(transcript,ensure_ascii=False,indent=2)+'\n')
    paths=[Path(__file__),OUT/'NativeImmediateEventsDifferential.cs',OUT/'NativeEventCorpusRequests.cs',OUT/'native_event_step_emulator.py',
        *[OUT/name for name in ['native_scenario_end_emulator.py','native_result_kills_emulator.py','native_result_emulator.py',
        'native_battle_status_emulator.py','native_battle_briefing_emulator.py','native_battle_options_emulator.py','native_ai_emulator.py']],
        ROOT/'Assets/_Original/Runtime/Campaign/CampaignModel.cs',ROOT/'Assets/_Original/Runtime/Campaign/CampaignService.cs',*files]
    report=dict(sourceFiles=len(files),eventBodies=len(bodies),nativeBodyExecutions=len(summaries),
        standaloneNoopCases=len(noop_cases),inputCommands=sum(inputs.values()),nativeCounters=dict(counts),
        crossContextMutationCases=context_checks,randomizedBodyDivergences=random_divergences,
        distinctProductionRequestCommands=len(requests),
        mismatches=len(failure),originalGameCtlSha256=native.sha256,unityPlay=False,production0DIngressExecuted=False,
        productionParserExecuted=True,originalFileLoaderExecuted=False,magicTargetHitBranchExecuted=False,
        scope='Actual10F20 on every production-parsed repository EVT body for supplied local-team rand0/rand100 and foreign-team rand0; every emitted request compared to production packet bytes at the exact native line/TEST/owner state. Timers are advanced and user dismissal is supplied. MESSAGE helper, kill helper, no-target magic check, map geometry, MFC/graphics/profile/transport/audio/units/allocation remain explicit boundaries. Transcript control/requests only: not all indirect callees, original0D loader/FIFO integration, server conditions/awards, actual playback, input, original FPS or current Unity frame equivalence.',
        contexts=[dict(subject=0,mode=1,localTeam=0,team=team,rand=random_value,time=123456)
            for team,random_value in [(0,0),(0,100),(7,0)]],
        sources={p.relative_to(ROOT).as_posix():sha(p)for p in paths},inputTokenCounts=dict(inputs),
        files=[p.relative_to(ROOT).as_posix()for p in files],cases=summaries,noopCases=noop_cases,
        transcriptSha256=sha(transcript_path),logs=str(WORK))
    (OUT/'native-event-corpus-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({key:value for key,value in report.items()if key not in ['sources','files','cases','noopCases','inputTokenCounts']},ensure_ascii=False))
    assert not failure


if __name__=='__main__':main()
