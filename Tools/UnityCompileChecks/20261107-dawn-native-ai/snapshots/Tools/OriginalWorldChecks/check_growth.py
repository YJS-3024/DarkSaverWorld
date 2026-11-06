"""Compile the real growth services and check persistence/replay/source boundaries."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-character-growth-checks')
UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')


def main():
    from check_native_battle_responses import contract_sources
    WORK.mkdir(exist_ok=True)
    run=subprocess.run([sys.executable,str(OUT/'check_gateways.py')],cwd=ROOT,text=True,capture_output=True)
    (WORK/'regression.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2500:])
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'GrowthChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'GrowthChecks.cs'))
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    run=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True);(WORK/'compile.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2500:])
    exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    run=subprocess.run([str(dotnet),str(exe),str(ROOT)],cwd=ROOT,text=True,capture_output=True);(WORK/'run.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2500:])
    result=json.loads(run.stdout);source=json.loads((OUT/'growth-assets.json').read_text())
    for path,sha in source['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,path
    assert hashlib.sha256((ROOT/source['output']).read_bytes()).hexdigest()==source['outputSha256']
    gateway=json.loads((OUT/'gateway-check.json').read_text());result.update(runtimeApiErrors=gateway['runtimeApiErrors'],editorApiErrors=gateway['editorApiErrors'],playerApiErrors=gateway['playerApiErrors'],warnings=gateway['warnings'],gatewayRegression=gateway['assertions'],campaignRegression=gateway['campaignRegression'],logs=str(WORK))
    result['sources']=contract_sources(args,__file__);result['sources'].update(source['sources']);result['sources'][source['output']]=source['outputSha256']
    builder=OUT/'build_growth_assets.py';result['sources'][builder.relative_to(ROOT).as_posix()]=hashlib.sha256(builder.read_bytes()).hexdigest()
    (OUT/'growth-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
