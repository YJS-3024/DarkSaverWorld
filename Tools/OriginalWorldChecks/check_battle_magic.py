"""Approved fireball combat requests/resources and fixed-endpoint presentation checks."""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-battle-magic-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')


def main():
    WORK.mkdir(exist_ok=True)
    run=subprocess.run([sys.executable,str(OUT/'check_magic_loadout.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    run_contract()


def run_contract():
    WORK.mkdir(exist_ok=True)
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'BattleMagicChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'BattleMagicChecks.cs'))
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        run=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(run.stdout+run.stderr)
        if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    evidence=json.loads((OUT/'class-assets.json').read_text())
    for path,digest in evidence['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    for row in evidence['outputs']:assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest()==row['sha256'],row['asset']
    metadata=json.loads((OUT/'magic-menu-assets.json').read_text())
    for path,digest in metadata['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    for row in metadata['outputs']:assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest()==row['sha256'],row['asset']
    assert metadata['rows']==440
    assert evidence['size']==[540,435]and len(evidence['buttons'])==21
    assert [(b['x'],b['y'])for b in evidence['buttons']if b['id']==81]==[(143,394)]
    result=json.loads(run.stdout);previous=json.loads((OUT/'magic-loadout-check.json').read_text())
    result.update({k:previous[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings','resultRegression','trainingRegression','growthRegression','gatewayRegression','campaignRegression','scenarioBattleRegression','exitRegression','revivalRegression','currencyRegression','bankRegression','warehouseRegression','mercenaryRegression','shopRegression','itemUseRegression','classChangeRegression','statAdjustmentRegression']});result.update(magicLoadoutRegression=previous['assertions'],logs=str(WORK))
    (OUT/'battle-magic-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
