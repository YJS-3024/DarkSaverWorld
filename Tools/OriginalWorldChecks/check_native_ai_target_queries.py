"""Actual three AI target queries: explicit relationship boundary, native lifecycle tests."""
import hashlib,itertools,json,struct,subprocess
from pathlib import Path
from native_ai_emulator import NativeAi,UC_HOOK_CODE,UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
from check_native_soldier_ai import unit
from check_native_battle_responses import contract_sources
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-ai-target-query-checks')
class Targets(NativeAi):
    def __init__(self,c):
        super().__init__(dict(width=12,height=12,units=c['units'],random=c['random']));self.permitted=c['permitted']
        for a in [0x10002500,0x100024a0]:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=a,end=a)
    def _dispatch(self,uc,address,size,data):
        if address in [0x10002500,0x100024a0]:
            target,source=self.args(2);valid=target in self.permitted;self.ret(int(not valid)if address==0x10002500 else int(valid))
        else:super()._dispatch(uc,address,size,data)
    def select(self,c):
        p=self.DATA+0x1000;t=p+32;args=[p,c['source'],t,c['radius']];sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<5I',self.STOP,*args));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start([0x10003860,0x10003c00,0x10003a40][c['family']],self.STOP+1,timeout=5000000,count=5000000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
        return dict(target=self.signed(self.read(t)),draws=self.random_calls,valid=self.uc.reg_read(UC_X86_REG_EAX))
def vectors():
    cases=[]
    for family,hp,state,active,enabled,radius,draw in itertools.product(range(3),[0,1,100],[0,128],[False,True],[False,True],[1,3],[0,1]):
        actor=unit(400,5,5,nativeActive=True,nativeEnabled=True);target=unit(0,6,5,hp=hp,nativeState=state,nativeActive=active,nativeEnabled=enabled)
        other=unit(410,5,6,nativeActive=True,nativeEnabled=True)
        cases.append(dict(name=f'{family}-{hp}-{state}-{active}-{enabled}-{radius}-{draw}',family=family,source=400,radius=radius,random=[draw],permitted=[0,410,400],units=[actor,target,other]))
    return cases
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();inp=WORK/'cases.json';out=WORK/'managed.json';inp.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'AiTargets.dll';sdk=magic.UNITY/'DotNetSdk';args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeAiTargetDifferential.cs'))
    for p in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(p)not in args:args+='\n"'+str(p)+'"\n'
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(out)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    managed=json.loads(out.read_text());differences=[]
    for c,m in zip(cases,managed):
        n=Targets(c)
        try:actual=n.select(c)
        finally:n.close()
        current={k:m[k]for k in actual}
        if actual!=current:differences.append(dict(name=c['name'],native=actual,managed=current))
    (WORK/'differences.json').write_text(json.dumps(differences,indent=2)+'\n')
    sources=contract_sources(args,__file__)
    for p in [OUT/'native_ai_emulator.py',OUT/'check_native_soldier_ai.py']:sources[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    report=dict(nativeCases=len(cases),mismatches=len(differences),unityPlay=False,sources=sources,logs=str(WORK),scope='Actual03860 enemy diamond,03C00 enemy terrain,03A40 friendly diamond scan and uniform random draw. Explicit per-fixture ally3/024A0 eligibility is the supplied relationship boundary, not recovered B2 policy. Native active/enabled/80 versus HP0, target0 and source-center exclusion compared. No target HP weighting, server damage, full AI or Unity Play claim.')
    (OUT/'native-ai-target-queries-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}));assert not differences,differences[:4]
if __name__=='__main__':main()
