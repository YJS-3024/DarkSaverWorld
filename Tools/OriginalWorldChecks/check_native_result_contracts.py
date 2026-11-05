"""Actual native result ingress and scenario save/exit contracts."""
import hashlib,json,subprocess
from pathlib import Path
import check_magic_projectiles as magic
from check_native_battle_responses import manager_guard,contract_sources
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-result-contract-checks')
def main():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeResultChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'ResultChecks.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    for name in ['NativeResultChecks.cs','NativeResponseManagerHost.cs']:args+='\n'+str(OUT.resolve()/name)
    for name in ['LocalServerManager.Training.cs','LocalServerManager.NativeBattlePackets.cs','LocalServerManager.NativeBattleResponses.cs','LocalServerManager.BattleItems.cs']:args+='\n'+str(ROOT/'Assets/_Original/Runtime/Entry'/name)
    args+='\n'+str(manager_guard(WORK))+'\n-main:NativeResultEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    result=json.loads(r.stdout);api=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    for path,digest in api['codeSha256'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    result.update(sources=contract_sources(args,__file__),currentApiCodeMatched=True,**{key:api[key]for key in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings']},logs=str(WORK))
    (OUT/'native-result-contract-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
