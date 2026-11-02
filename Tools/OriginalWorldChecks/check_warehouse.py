"""Warehouse terms, staged ownership changes, final payment and persistence contracts."""
import json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-warehouse-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')


def main():
    WORK.mkdir(exist_ok=True)
    run=subprocess.run([sys.executable,str(OUT/'check_bank.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'WarehouseChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'WarehouseChecks.cs'))
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        run=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(run.stdout+run.stderr)
        if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    result=json.loads(run.stdout);previous=json.loads((OUT/'bank-check.json').read_text())
    result.update({k:previous[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings','resultRegression','trainingRegression','growthRegression','gatewayRegression','campaignRegression','scenarioBattleRegression','exitRegression','revivalRegression','currencyRegression']});result.update(bankRegression=previous['assertions'],logs=str(WORK))
    (OUT/'warehouse-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
