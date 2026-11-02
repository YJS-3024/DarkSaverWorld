"""Original command radius facts and live snapshot contracts; does not execute Unity Play."""
import csv,hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-command-range-checks')

def check_assets():
    base=ROOT/'Assets/_Original';catalog=json.loads((base/'Data/Combat/OriginalCombat.json').read_text())
    jobs=list(csv.DictReader((base/'Data/Tables/units_all.csv').open(encoding='utf-8-sig')))
    items=list(csv.DictReader((base/'Data/Tables/items.csv').open(encoding='utf-8-sig')))
    assert {j['job']:j['rawCommandRange']for j in catalog['jobs']}=={int(r['직업']):int(r['지범'])for r in jobs}
    assert {i['item']:(i['rawCommandRange'],i['equipType'],i['name'])for i in catalog['commandRangeItems']}=={
        int(r['번호']):(int(r['지휘범위']),int(r['장착위']),r['이름'].replace('_',' '))for r in items if int(r['지휘범위'])!=0}
    from PIL import Image
    board=Image.open(base/'Art/Tutorial/Board.png').convert('RGBA');crops=[]
    for rect in [(303,300,335,303),(300,303,303,335)]:
        pixels=board.crop(rect);assert pixels.getbbox() is not None
        crops.append(dict(rect=list(rect),pixelSha256=hashlib.sha256(pixels.tobytes()).hexdigest()))
    return dict(jobRanges=len(jobs),itemBonuses=len(catalog['commandRangeItems']),nativeOutlineCrops=crops)

def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'CommandRangeChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    args+='\n'+str(OUT.resolve()/'AllMagicChecks.cs')+'\n'+str(OUT.resolve()/'CommandRangeChecks.cs')+'\n-main:CommandRangeEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    result=json.loads(r.stdout);previous=json.loads((OUT/'troop-order-check.json').read_text())
    result.update(troopOrderRegression=previous['assertions'],nativeInstructionFixtures=magic.check_native('command-range-native-evidence.json'),
        **{k:previous[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},**check_assets(),logs=str(WORK))
    (OUT/'command-range-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))

def main():
    WORK.mkdir(exist_ok=True)
    r=subprocess.run([sys.executable,str(OUT/'check_troop_orders.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    run_contract()

if __name__=='__main__':main()
