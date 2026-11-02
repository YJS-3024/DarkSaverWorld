"""Native three-choice troop modes, explicit authority, atomic save; not Unity Play."""
import json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-troop-order-checks')
def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'TroopOrderChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    args+='\n'+str(OUT.resolve()/'AllMagicChecks.cs')+'\n'+str(OUT.resolve()/'TroopOrderChecks.cs')+'\n-main:TroopOrderEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    result=json.loads(r.stdout)
    previous=json.loads((OUT/'battle-item-check.json').read_text())
    result.update(battleItemRegression=previous['assertions'],specialSkillRegression=previous['specialSkillRegression'],generalMagicRegression=previous['generalMagicRegression'],nativeInstructionFixtures=magic.check_native('troop-order-native-evidence.json'),runtimeApiErrors=previous['runtimeApiErrors'],editorApiErrors=previous['editorApiErrors'],playerApiErrors=previous['playerApiErrors'],warnings=previous['warnings'],logs=str(WORK));(OUT/'troop-order-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
def main():
    WORK.mkdir(exist_ok=True)
    r=subprocess.run([sys.executable,str(OUT/'check_battle_items.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    run_contract()
if __name__=='__main__':main()
