"""Execute actual native-end presentation/input bodies with bounded managed UI objects."""
import hashlib,json,subprocess
from pathlib import Path
import check_magic_projectiles as magic
from check_picture_initialization import block
from check_native_battle_responses import contract_sources
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-end-ui')
def main():
    WORK.mkdir(exist_ok=True);result=ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Result.cs';world=ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs'
    text=result.read_text();start=text.index('        private bool WaitingScenarioResult=>');end=text.index(';',start)+1
    waiting=text.index('        private bool WaitingNativeEnd=>');waiting_end=text.index(';',waiting)+1
    source=WORK/'ActualEndUi.cs';source.write_text('using DarkSaver.Original.Campaign;using UnityEngine;using UnityEngine.UI;namespace DarkSaver.Original.World {public sealed partial class WorldSceneRoot {'
        +text[waiting:waiting_end]
        +text[start:end]+block(text,'private void PaintNativeEndNotice(')
        +'private void HandleExtractedEndInput(){'+block(world.read_text(),'if(WaitingNativeEnd)')+'}}}')
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'NativeEndUiChecks.dll';harness=OUT/'NativeEndUiHost.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))+'\n'+str(source)
    for p in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2600:])
    report=json.loads(r.stdout);sources=contract_sources(args,__file__)
    for p in [result,world]:sources[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    report.update(sources=sources,logs=str(WORK));(OUT/'native-end-ui-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
