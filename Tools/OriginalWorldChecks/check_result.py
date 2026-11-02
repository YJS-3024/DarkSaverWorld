"""Scenario result/return contracts plus source artwork and existing training regression."""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-scenario-result-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')


def main():
    WORK.mkdir(exist_ok=True)
    run=subprocess.run([sys.executable,str(OUT/'check_training.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'ResultChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'ResultChecks.cs'))
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        run=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(run.stdout+run.stderr)
        if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    result=json.loads(run.stdout);source=json.loads((OUT/'result-assets.json').read_text())
    for p,sha in source['sources'].items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha,p
    for item in source['outputs']:assert hashlib.sha256((ROOT/item['asset']).read_bytes()).hexdigest()==item['sha256']
    previous=json.loads((OUT/'training-check.json').read_text());result.update(runtimeApiErrors=previous['runtimeApiErrors'],editorApiErrors=previous['editorApiErrors'],playerApiErrors=previous['playerApiErrors'],warnings=previous['warnings'],trainingRegression=previous['assertions'],growthRegression=previous['growthRegression'],gatewayRegression=previous['gatewayRegression'],campaignRegression=previous['campaignRegression'],logs=str(WORK))
    (OUT/'result-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
