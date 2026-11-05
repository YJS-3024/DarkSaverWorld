"""Native50/51 units and60 team names: codec and service/manager ingress."""
import hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-unit-snapshot-checks')
def run_contract():
    from check_native_battle_responses import manager_guard,contract_sources
    from check_training_test_authority import focus_host_code
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeUnitChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    for name in ['AllMagicChecks.cs','BattleItemChecks.cs','MemberCommandChecks.cs','RegroupChecks.cs','SoldierActionChecks.cs','BattleLifecycleChecks.cs','BattleFeedbackChecks.cs','BattleTickChecks.cs','NativeBattlePacketChecks.cs','NativeMovementChecks.cs','NativeMagicChecks.cs','NativeUnitChecks.cs','NativeResponseManagerHost.cs']:args+='\n'+str(OUT.resolve()/name)
    for name in ['LocalServerManager.Training.cs','LocalServerManager.NativeBattleResponses.cs','LocalServerManager.NativeBattlePackets.cs','LocalServerManager.BattleItems.cs']:args+='\n'+str(ROOT/'Assets/_Original/Runtime/Entry'/name)
    args+='\n'+str(manager_guard(WORK))
    focus,focus_sources=focus_host_code();focus_file=WORK/'FocusReceiver.cs'
    focus_file.write_text('using DarkSaver.Original.World;using DarkSaver.Original.Entry;'+focus)
    args+='\n'+str(focus_file)
    args+='\n-main:NativeUnitEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    result=json.loads(r.stdout);api=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    for p,h in api['codeSha256'].items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h,p
    result.update(**{k:api[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},apiReport='Tools/OriginalEntryChecks/entry-check.json',currentApiCodeMatched=True,sources=contract_sources(args,__file__),logs=str(WORK),scope='Actual50/51/60 codec, service and manager partials; atomic unit snapshots, profile/presentation separation, session-only team names, rollback and persistence. Managed component boundary; no Unity Play or inferred server roster/growth rules.')
    for source in focus_sources+[OUT/'check_training_test_authority.py']:result['sources'][source.relative_to(ROOT).as_posix()]=hashlib.sha256(source.read_bytes()).hexdigest()
    (OUT/'native-unit-snapshot-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
def main():
    WORK.mkdir(exist_ok=True);r=subprocess.run([sys.executable,str(OUT/'check_gateways.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
    if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    run_contract()
if __name__=='__main__':main()
