"""Exercise training contracts with isolated authority fixtures and existing regression suite."""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-training-combat-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')


def main():
    WORK.mkdir(exist_ok=True)
    run=subprocess.run([sys.executable,str(OUT/'check_growth.py')],cwd=ROOT,text=True,capture_output=True)
    (WORK/'regression.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2000:])
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'TrainingChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'TrainingChecks.cs'))
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        run=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(run.stdout+run.stderr)
        if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2000:])
    result=json.loads(run.stdout);source=json.loads((OUT/'combat-assets.json').read_text())
    for path,sha in source['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==sha,path
    assert hashlib.sha256((ROOT/source['output']).read_bytes()).hexdigest()==source['outputSha256']
    growth=json.loads((OUT/'growth-check.json').read_text());result.update(runtimeApiErrors=growth['runtimeApiErrors'],editorApiErrors=growth['editorApiErrors'],playerApiErrors=growth['playerApiErrors'],warnings=growth['warnings'],growthRegression=growth['assertions'],gatewayRegression=growth['gatewayRegression'],campaignRegression=growth['campaignRegression'],logs=str(WORK))
    (OUT/'training-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
