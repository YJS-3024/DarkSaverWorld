"""Isolated Git checkout-index/core.autocrlf=true; never stages the real repo."""
import hashlib,json,os,shutil,subprocess,sys,tempfile
from pathlib import Path
PKG=Path(__file__).resolve().parent;ROOT=PKG.parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    files=['.gitattributes','Assets/_Original/Runtime/Combat/SoldierActions.cs','Assets/_Original/Runtime/Combat/NativeBattleTeamNames.cs',
      '분석/Ghidra/Saver_Late/MonsterAiDispatch/100041f0.c','분석/Ghidra/Saver_Late/MonsterAiDispatch/100035e0.c',
      'Tools/OriginalWorldChecks/check_native_ai_dispatch.py','Tools/GhidraChecks/refresh_native_evidence_references.py',
      'Tools/UnityCompileChecks/20261106-night-victory-notice/snapshots/Tools/OriginalWorldChecks/native-directed-frame-check.json',
      'Tools/UnityCompileChecks/20261106-night-victory-notice/reproductions/moving-ai-before/original-planner.cs',
      'Tools/UnityCompileChecks/20261106-night-victory-notice/README.md']
    index=Path(subprocess.check_output(['git','rev-parse','--git-path','index'],cwd=ROOT,text=True).strip());index=index if index.is_absolute()else ROOT/index
    before=sha(index);sources={p:sha(ROOT/p)for p in files};results=[]
    env=dict(os.environ,GIT_CONFIG_NOSYSTEM='1',GIT_CONFIG_GLOBAL=os.devnull)
    with tempfile.TemporaryDirectory(prefix='ds-night-eol-')as temp:
        target=Path(temp)
        def git(*args):return subprocess.run(['git',*args],cwd=target,env=env,text=True,capture_output=True,check=True)
        git('init','-q');git('config','core.autocrlf','true')
        for name in files:
            p=target/name;p.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/name,p)
        (target/'control.txt').write_bytes(b'one\ntwo\n');git('add','--',*files,'control.txt')
        for name in files+['control.txt']:(target/name).unlink()
        git('checkout-index','-a','-f')
        for name in files:
            actual=sha(target/name);assert actual==sources[name],name;results.append(dict(source=name,sha256=actual))
        assert (target/'control.txt').read_bytes()==b'one\r\ntwo\r\n'
    assert sha(index)==before,'real repository index changed during isolated proof'
    report=dict(preservedFiles=len(files),coreAutocrlf=True,unscopedControlBecameCrLf=True,realRepositoryIndexUnchanged=True,windowsProcess=False,sources=sources,cases=results,
        scope='Actual isolated Linux Git checkout-index with core.autocrlf=true, current source LF rules and frozen night-package -text. Real repository index untouched. This is not a Windows process or Unity import.')
    if '--check-only'not in sys.argv:(PKG/'source-eol-check.json').write_bytes((json.dumps(report,ensure_ascii=False,indent=2)+'\n').encode())
    print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']}))
if __name__=='__main__':main()
