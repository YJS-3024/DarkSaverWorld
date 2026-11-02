"""Native health/mana/attack display contracts, frame crops and existing regression."""
import hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-battle-feedback-checks')
def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'BattleFeedbackChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    for name in ['AllMagicChecks.cs','MemberCommandChecks.cs','RegroupChecks.cs','SoldierActionChecks.cs','BattleFeedbackChecks.cs']:args+='\n'+str(OUT.resolve()/name)

    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    args+='\n-main:BattleFeedbackEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    result=json.loads(r.stdout);assets=json.loads((OUT/'feedback-assets.json').read_text());from PIL import Image
    assert hashlib.sha256((ROOT/assets['source']).read_bytes()).hexdigest()==assets['sourceSha256']
    for record in assets['images'].values():assert hashlib.sha256((ROOT/record['asset']).read_bytes()).hexdigest()==record['sha256']
    for crop in assets['frames']:
        image=Image.open(ROOT/assets['images'][str(crop['surface'])]['asset']).convert('RGBA');x,y,w,h=crop['rect'];assert hashlib.sha256(image.crop((x,y,x+w,y+h)).tobytes()).hexdigest()==crop['pixelSha256']
    api=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    for path,digest in api['codeSha256'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    result.update(verifiedFrameCrops=len(assets['frames']),nativeInstructionFixtures=magic.check_native('battle-feedback-native-evidence.json'),**{k:api[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},apiReport='Tools/OriginalEntryChecks/entry-check.json',currentApiCodeMatched=True,logs=str(WORK))
    (OUT/'battle-feedback-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
def main():
    WORK.mkdir(exist_ok=True);r=subprocess.run([sys.executable,str(OUT/'check_battle_ticks.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    run_contract()
if __name__=='__main__':main()
