"""Explicit user return-draft helper and ordinary bidirectional travel contracts."""
import hashlib,json,subprocess,sys
from pathlib import Path
import check_magic_projectiles as magic
from check_native_battle_responses import contract_sources
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-reverse-gateway-checks')
def main():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk'
    if not(old/'checks.rsp').is_file():
        prerequisite=subprocess.run([sys.executable,str(OUT/'check_gateways.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'prerequisite.log').write_text(prerequisite.stdout+prerequisite.stderr)
        if prerequisite.returncode:raise RuntimeError((prerequisite.stdout+prerequisite.stderr)[-2400:])
    exe=WORK/'ReverseGatewayChecks.dll';args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n"'+str(source)+'"\n'
    args+='\n"'+str(OUT.resolve()/'ReverseGatewayChecks.cs')+'"\n-main:ReverseGatewayEntry\n';rsp=WORK/'checks.rsp';rsp.write_text(args)
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
        if phase=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    result=json.loads(r.stdout);result['sources']=contract_sources(args,__file__)
    source=ROOT/'Assets/_Original/Editor/Scenario/ScenarioEditorWindow.Gateways.cs';result['sources'][source.relative_to(ROOT).as_posix()]=hashlib.sha256(source.read_bytes()).hexdigest()
    result['logs']=str(WORK);(OUT/'reverse-gateway-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
