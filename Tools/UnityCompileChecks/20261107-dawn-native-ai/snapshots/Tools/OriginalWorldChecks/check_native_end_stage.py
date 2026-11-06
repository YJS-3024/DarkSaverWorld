"""Production61 end plan versus original GameCtl receipt traces, independent of UI fixtures."""
import hashlib,json,subprocess
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-end-stage')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    oracle=OUT/'native-scenario-end-check.json';original=json.loads(oracle.read_text())
    assert original['mismatches']==0
    for path,digest in original['sources'].items():assert sha(ROOT/path)==digest,path
    cases=[c for c in original['endCases']if c['tutorial']==0];assert len(cases)==28
    WORK.mkdir(exist_ok=True);input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,ensure_ascii=False))
    sdk=magic.UNITY/'DotNetSdk';exe=WORK/'NativeEndStage.dll'
    source=ROOT/'Assets/_Original/Runtime/Campaign/CampaignResult.cs';harness=OUT/'NativeEndStageDifferential.cs'
    from check_picture_initialization import block
    suffix_source=ROOT/'Assets/_Original/Runtime/Combat/NativeScenarioScriptSuffix.cs';suffix=WORK/'ActualScenarioSuffixValue.cs'
    suffix.write_text('namespace DarkSaver.Original.World {'+block(suffix_source.read_text(),'public static class OriginalScenarioScriptSuffix')+'}')
    refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
    rsp=WORK/'checks.rsp';rsp.write_text('\n'.join(['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+[str(source),str(suffix),str(harness.resolve())]))
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(input),str(output)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((Path('/tmp/ds-npc-gateway-checks')/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2200:])
    mismatch=[];actual=json.loads(output.read_text());assert len(actual)==len(cases)
    for i,(c,stage)in enumerate(zip(cases,actual)):
        n=c['native'];expected=3 if n['actions']else 1 if any(e[0]=='dialogue'for e in n['events'])else 2
        if stage!=expected:mismatch.append(dict(index=i,expected=expected,actual=stage))
    result=dict(cases=len(cases),mismatches=len(mismatch),nativeCodeExecutedInOracle=True,productionPlanExecuted=True,unityPlayExecuted=False,
        originalSha256=original['originalSha256'],oracleSha256=sha(oracle),sources={p.relative_to(ROOT).as_posix():sha(p)for p in [Path(__file__),harness,source,suffix_source,OUT/'check_picture_initialization.py',oracle]},
        scope='Production61 exact BYTE decoder (unchanged pure suffix class extracted for the result-record dependency; no service stub) and outcome/flag plan versus28 original GameCtl D150 receipt traces with tutorial off. Stage3 denotes direct end control; the full service additionally distinguishes victory DEE from nonvictory cleanup and safely defers unknown tables. Managed ingress/save/UI tests are separate. No server delivery order, rewards, pixels, timing measurement or Unity Play claim.',logs=str(WORK))
    (OUT/'native-end-stage-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    assert not mismatch,mismatch
if __name__=='__main__':main()
