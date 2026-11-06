"""Production15 mutation plan versus original interpreter on all supplied EVT bodies."""
import hashlib,json,subprocess,struct
from pathlib import Path
import check_magic_projectiles as magic
from native_event_step_emulator import NativeEventStep
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-immediate-events')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    WORK.mkdir(exist_ok=True);files=sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*/*.evt.txt'));assert len(files)==112
    input=WORK/'input.json';output=WORK/'managed.json';input.write_text(json.dumps([str(p)for p in files]))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'NativeImmediateEvents.dll';harness=OUT/'NativeImmediateEventsDifferential.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(input),str(output)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    cases=json.loads(output.read_text());mismatches=[];mutations=0;unsupported=[]
    for c in cases:
        if not c['supported']:unsupported.append(dict(file=c['file'],number=c['number']));continue
        n=NativeEventStep();n.setup(['END']);base=0x1006c120;n.write(base,1);empty=n.string(b'')
        assert len(c['body'])<=399
        for i in range(399):n.write(base+16+i*4,n.string(c['body'][i].encode('cp949'))if i<len(c['body'])else empty)
        n.uc.mem_write(0x0d040000,b'\x5c\x15\x01\0\0\0');n.run(0x1000eb10,[0x0d040000]);actual=[dict(name=name,args=[int(a)for a in text.split()])for name,text in n.mutations]
        mutations+=len(actual)
        if n.sent or n.events or n.profile or actual!=c['changes']:mismatches.append(dict(file=c['file'],number=c['number'],native=actual,managed=c['changes'],sent=n.sent,events=n.events,profile=n.profile))
    paths=[Path(__file__),harness,OUT/'native_event_step_emulator.py',*[OUT/name for name in ['native_scenario_end_emulator.py','native_result_kills_emulator.py','native_result_emulator.py','native_battle_status_emulator.py','native_battle_briefing_emulator.py','native_battle_options_emulator.py']],ROOT/'Assets/_Original/Runtime/Campaign/CampaignModel.cs',ROOT/'Assets/_Original/Runtime/Campaign/CampaignService.cs',*files]
    result=dict(sourceFiles=len(files),eventBodies=len(cases),nativeExecutedBodies=len(cases)-len(unsupported),deferredBodies=len(unsupported),nativeMutationCalls=mutations,mismatches=len(mismatches),unityPlay=False,originalGameCtlSha256=n.sha256,
        scope='All112 repository EVT files are parsed by the production Events parser and their supported bodies are copied into actual F710/15/interpreter execution. Production synchronous mutation plan versus original map helper calls; owner requests/dialogues/timers/profile calls must be absent. The source loader parser itself, map mutation helper geometry, actual Unity rendering and original server trigger policy are separate. Unsupported pre-ownership host operations are deferred, never silently applied.',
        sources={p.relative_to(ROOT).as_posix():sha(p)for p in paths},deferred=unsupported,logs=str(WORK))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,ensure_ascii=False,indent=2))
    (OUT/'native-immediate-events-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False))
    assert not mismatches,mismatches[:1]
if __name__=='__main__':main()
