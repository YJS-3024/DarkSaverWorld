"""Battle consumable contracts and native link/source audit; not actual Unity Play."""
import csv,hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-battle-item-checks')
def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'BattleItemChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    args+='\n'+str(OUT.resolve()/'AllMagicChecks.cs')+'\n'+str(OUT.resolve()/'BattleItemChecks.cs')+'\n-main:BattleItemEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    result=json.loads(r.stdout);audit=json.loads((OUT/'battle-item-assets.json').read_text());assert hashlib.sha256((ROOT/audit['source']).read_bytes()).hexdigest()==audit['sourceSha256'];assert hashlib.sha256((ROOT/audit['output']).read_bytes()).hexdigest()==audit['sha256']
    rows={int(r['번호']):(int(r['마법번']),int(r['내구']))for r in csv.DictReader((ROOT/audit['source']).open(encoding='utf-8-sig'))if int(r['장착위'])==7 and int(r['마법번'])>0}
    assert {r['item']:(r['magic'],r['rawDurability'])for r in audit['links']}==rows and len(rows)==33
    previous=json.loads((OUT/'special-skill-check.json').read_text())
    result.update(specialSkillRegression=previous['assertions'],generalMagicRegression=previous['generalMagicRegression'],nativeInstructionFixtures=magic.check_native('battle-item-native-evidence.json'),runtimeApiErrors=previous['runtimeApiErrors'],editorApiErrors=previous['editorApiErrors'],playerApiErrors=previous['playerApiErrors'],warnings=previous['warnings'],logs=str(WORK));(OUT/'battle-item-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
def main():
    WORK.mkdir(exist_ok=True)
    r=subprocess.run([sys.executable,str(OUT/'check_special_skills.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    run_contract()
if __name__=='__main__':main()
