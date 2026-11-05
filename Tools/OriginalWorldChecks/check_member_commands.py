"""Native pure-regroup destination selection and automatic authority movement; not Unity Play."""
import csv,hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-member-command-checks')

def check_assets():
    base=ROOT/'Assets/_Original';catalog=json.loads((base/'Data/Combat/OriginalCombat.json').read_text())
    rows=list(csv.DictReader((base/'Data/Tables/units_all.csv').open(encoding='utf-8-sig')))
    assert catalog['version']==4 # Added source base stats; preserve original movement-field assertions below.
    assert {j['job']:j['rawMovementType']for j in catalog['jobs']}=={int(r['직업']):int(r['병종'])for r in rows}
    return dict(jobMovementTypes=len(rows))

def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'MemberCommandChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    args+='\n'+str(OUT.resolve()/'AllMagicChecks.cs')+'\n'+str(OUT.resolve()/'MemberCommandChecks.cs')+'\n-main:MemberCommandEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    result=json.loads(r.stdout);previous=json.loads((OUT/'regroup-check.json').read_text())
    api=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    for path,digest in api['codeSha256'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    assert {p.relative_to(ROOT).as_posix()for p in (ROOT/'Assets/_Original/Runtime').rglob('*.cs')}<=set(api['codeSha256'])
    result.update(regroupRegression=previous['assertions'],nativeInstructionFixtures=magic.check_native('member-command-native-evidence.json'),
        **{k:api[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},apiReport='Tools/OriginalEntryChecks/entry-check.json',currentApiCodeMatched=True,**check_assets(),logs=str(WORK))
    (OUT/'member-command-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))

def main():
    WORK.mkdir(exist_ok=True)
    r=subprocess.run([sys.executable,str(OUT/'check_regroup.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    run_contract()

if __name__=='__main__':main()
