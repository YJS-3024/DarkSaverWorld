"""Raw basic-action responses and existing packet/visual/combat regressions."""
import hashlib,json,shlex,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-battle-response-checks')
def manager_guard(work):
    from check_picture_initialization import block
    source=(ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs').read_text()
    path=work/'ManagerContext.cs'
    path.write_text('using DarkSaver.Original.World;namespace DarkSaver.Original.Entry {public sealed partial class LocalServerManager {'+block(source,'private bool IsCurrentWorldReply(')+'}}')
    return path
def contract_sources(arguments,script):
    paths={ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs',Path(__file__),Path(script),OUT/'check_picture_initialization.py'}
    for line in arguments.splitlines():
        # Compiler response-file quotes are not shell escaping: preserve Windows backslashes.
        for token in shlex.split(line,posix=False):
            value=token.strip('"');path=Path(value)
            if value.endswith('.cs') and path.is_file() and ROOT in path.parents:paths.add(path)
    return {p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sorted(paths)}
def run_contract():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeBattleResponseChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    for name in ['AllMagicChecks.cs','MemberCommandChecks.cs','RegroupChecks.cs','SoldierActionChecks.cs','BattleFeedbackChecks.cs','BattleTickChecks.cs','BattleLifecycleChecks.cs','NativeBattleResponseChecks.cs']:args+='\n'+str(OUT.resolve()/name)
    for name in ['LocalServerManager.Training.cs','LocalServerManager.MemberCommands.cs','LocalServerManager.NativeBattleResponses.cs']:args+='\n'+str(ROOT/'Assets/_Original/Runtime/Entry'/name)
    args+='\n'+str(OUT.resolve()/'NativeResponseManagerHost.cs')
    args+='\n'+str(manager_guard(WORK))
    args+='\n-main:NativeBattleResponseEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    result=json.loads(r.stdout);api=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    for path,digest in api['codeSha256'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    result.update(**{k:api[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},apiReport='Tools/OriginalEntryChecks/entry-check.json',currentApiCodeMatched=True,sources=contract_sources(args,__file__),logs=str(WORK),scope='Actual basic response parser/service and manager partials; compiled source fingerprints include the original context guard. Managed Unity boundary host; no Unity Play or server damage formula.')
    (OUT/'native-battle-response-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
def main():
    WORK.mkdir(exist_ok=True);r=subprocess.run([sys.executable,str(OUT/'check_native_battle_packets.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    run_contract()
if __name__=='__main__':main()
