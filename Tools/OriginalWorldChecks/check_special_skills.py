"""Native86 special-menu execution contracts; actual Unity Play is intentionally separate."""
import hashlib,json,subprocess,sys
from pathlib import Path
from PIL import Image
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-special-skill-checks')
def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'SpecialSkillChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    args+='\n'+str(OUT.resolve()/'AllMagicChecks.cs')+'\n'+str(OUT.resolve()/'SpecialSkillChecks.cs')+'\n-main:SpecialSkillEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    result=json.loads(r.stdout);audit=json.loads((OUT/'special-skill-assets.json').read_text());assert len(audit['skills'])==86 and len(audit['excluded'])==65
    assert hashlib.sha256((ROOT/audit['source']).read_bytes()).hexdigest()==audit['sourceSha256']
    images=[Image.open(ROOT/'Assets/_Original/Art/Tutorial/Effects.png').convert('RGBA'),Image.open(ROOT/'Assets/_Original/Art/Tutorial/Attackeffects.png').convert('RGBA')]
    for f in audit['frameCrops']:
        x,y,w,h=f['rect'];assert hashlib.sha256(images[f['surface']].crop((x,y,x+w,y+h)).tobytes()).hexdigest()==f['pixelSha256']
    previous=json.loads((OUT/'magic-projectile-check.json').read_text())
    result.update(verifiedFrameCrops=len(audit['frameCrops']),nativeInstructionFixtures=magic.check_native('special-skill-native-evidence.json'),generalMagicRegression=previous['assertions'],runtimeApiErrors=previous['runtimeApiErrors'],editorApiErrors=previous['editorApiErrors'],playerApiErrors=previous['playerApiErrors'],warnings=previous['warnings'],logs=str(WORK));(OUT/'special-skill-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
def main():
    WORK.mkdir(exist_ok=True)
    r=subprocess.run([sys.executable,str(OUT/'check_magic_projectiles.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    run_contract()
if __name__=='__main__':main()
