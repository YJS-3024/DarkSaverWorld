"""Actual UserUnit friendly/ally3/ally4 and managed three-valued client relations."""
import hashlib,itertools,json,struct,subprocess
from pathlib import Path
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_EAX
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-battle-relations')
class Relations(NativeAi):
    def __init__(self):super().__init__(dict(width=1,height=1,units=[]))
    def _dispatch(self,uc,address,size,data):
        if address==self.exports['GetTeamID']:return
        super()._dispatch(uc,address,size,data)
    def invoke(self,entry,a,b):
        sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<3I',self.STOP,a&0xffffffff,b&0xffffffff));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(entry,self.STOP+1,timeout=1000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and self.uc.reg_read(UC_X86_REG_ESP)==sp+4
        return self.signed(self.uc.reg_read(UC_X86_REG_EAX))
def main():
    WORK.mkdir(exist_ok=True);cases=[];teams=[-99,-1,0,1,2,7,8,9,15,16,19,20]
    patterns=[[1]*21,[2]*21,[4]*21,[1,2,4,0,3,-1,2]*3]
    for a,b,local,r in itertools.product(teams,teams,[-99,0,1,7,19],patterns):cases.append(dict(first=0,second=400,teamA=a,teamB=b,localTeam=local,relations=r))
    for first,second in [(-1,400),(0,-1),(-32768,400),(-1,-1),(0,0),(2999,400)]:cases.append(dict(first=first,second=second,teamA=0,teamB=8,localTeam=0,relations=patterns[0]))
    inp=WORK/'cases.json';out=WORK/'managed.json';inp.write_text(json.dumps(cases,separators=(',',':')));sdk=magic.UNITY/'DotNetSdk';exe=WORK/'Relations.dll'
    refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))];sources=[ROOT/'Assets/_Original/Runtime/Combat/NativeBattleRelations.cs',OUT.resolve()/'NativeRelationDifferential.cs']
    args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe),*refs,*map(str,sources)];rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args))
    version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name;exe.with_suffix('.runtimeconfig.json').write_text(json.dumps(dict(runtimeOptions=dict(tfm='net8.0',framework=dict(name='Microsoft.NETCore.App',version=version)))))
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(out)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    managed=json.loads(out.read_text());n=Relations();differences=[]
    try:
        for c,m in zip(cases,managed):
            n.write(n.RUNTIME+max(0,c['first'])*0x2e0+0x64,c['teamA'])
            if c['second']!=c['first']:n.write(n.RUNTIME+max(0,c['second'])*0x2e0+0x64,c['teamB'])
            n.write(0x1003d540,c['localTeam'])
            for i,v in enumerate(c['relations']):n.write(0x1003d54c+i*4,v)
            actual=dict(friendly=bool(n.invoke(0x100024a0,c['first'],c['second'])),ally3=n.invoke(0x10002500,c['first'],c['second']),ally4=n.invoke(0x100025c0,c['first'],c['second']))
            actual['magic']=[bool(flags&1 and actual['ally3']or flags&2 and actual['ally4'])for flags in [0,1,2,3,34,35,-1,128]]
            if actual!=m:differences.append(dict(context=c,native=actual,managed=m))
    finally:n.close()
    (WORK/'differences.json').write_text(json.dumps(differences,indent=2)+'\n')
    paths=[Path(__file__),OUT/'native_ai_emulator.py',*sources,ROOT/'분석/Ghidra/Saver_Late/AiTargetSelectors/evidence.json'];report=dict(nativeContexts=len(cases),nativeHelperCalls=len(cases)*3,derivedMagicMasks=len(cases)*8,missingContextControls=6,mismatches=len(differences),unityPlay=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},logs=str(WORK),scope='Actual024A0/02500/025C0 and GetTeamID bodies across signed team/current-team/21-slot relation contexts. Magic low-bit eligibility composes these native return values; it is not a full CheckMagicTarget query here. Missing managed contexts deliberately remain unknown; no ID/team guess, B2 stale-tail policy or server alliance operation.')
    (OUT/'native-battle-relations-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}));assert not differences,differences[:3]
if __name__=='__main__':main()
