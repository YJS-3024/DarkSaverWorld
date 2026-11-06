"""Actual isolated Git autocrlf checkout; real index and original game files stay untouched."""
import hashlib,json,os,shutil,subprocess,sys,tempfile
from pathlib import Path
PKG=Path(__file__).resolve().parent;ROOT=PKG.parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    names=['Assets/_Original/README.md','Tools/CheckSupport/README.md','설계/08_클라이언트_정적_분석.md','설계/10_분석_적용_진행표.md','설계/테스트_검증_계획.md','Tools/OriginalWorldChecks/check_native_effect_table_parser.py','Tools/OriginalWorldChecks/check_native_alliance_history.py','Assets/_Original/Editor/Scenario/ScenarioEditorWindow.AiRules.cs','Assets/_Original/Data/Combat/MagicMenu.json','Assets/_Original/Runtime/Growth/MagicLoadout.cs','Assets/_Original/Runtime/Combat/NativeMagicPackets.cs','Tools/OriginalWorldChecks/native_magic_table_emulator.py','.gitattributes','Assets/_Original/Runtime/Combat/SoldierActions.cs','Assets/_Original/Runtime/Combat/SoldierMovement.cs',
      'Assets/_Original/Runtime/Combat/NativeAiRules.cs','Assets/_Original/Runtime/Combat/NativeScenarioScriptSuffix.cs',
      'Assets/_Original/Runtime/Combat/NativeBattleRelations.cs','Assets/_Original/Data/Campaign/NativeAiRules.json',
      'Assets/_Original/Data/Campaign/NativeAiRules.json.meta','Assets/_Original/Data/Campaign/NativeCampaignArt.asset',
      'Tools/run_original_checks.py','Tools/OriginalWorldChecks/native_ai_frame_chain.py',
      '분석/Ghidra/Saver_Late/AiFrameDispatch/GameCtl/read-only-export.log','분석/Ghidra/Saver_Late/AiFrameDispatch/GameCtl/references.txt',
      '분석/Ghidra/Saver_Late/RetreatConfirmation/1000e780.c',
      'Tools/UnityCompileChecks/20261107-dawn-native-ai/snapshots/Tools/OriginalWorldChecks/native-soldier-ai-check.json',
      'Tools/UnityCompileChecks/20261107-dawn-native-ai/snapshots/Assets/_Original/Runtime/Combat/SoldierActions.cs',
      'Tools/UnityCompileChecks/20261107-dawn-native-ai/README.md']
    # Exercise every owned source frozen by this package, not just a sample.
    frozen=json.loads((PKG/'development-verification.json').read_text(encoding='utf-8'))
    names=sorted({(ROOT/name).resolve().relative_to(ROOT).as_posix()for name in names}|{(ROOT/row['source']).resolve().relative_to(ROOT).as_posix()for row in frozen['snapshots']})
    index=Path(subprocess.check_output(['git','rev-parse','--git-path','index'],cwd=ROOT,text=True).strip());index=index if index.is_absolute()else ROOT/index
    before=sha(index);sources={name:sha(ROOT/name)for name in names};rows=[]
    env=dict(os.environ,GIT_CONFIG_NOSYSTEM='1',GIT_CONFIG_GLOBAL=os.devnull)
    with tempfile.TemporaryDirectory(prefix='ds-dawn-eol-')as temporary:
        target=Path(temporary)
        def git(*args):return subprocess.run(['git',*args],cwd=target,env=env,text=True,capture_output=True,check=True)
        git('init','-q');git('config','core.autocrlf','true')
        for name in names:
            path=target/name;path.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/name,path)
        (target/'control.txt').write_bytes(b'one\ntwo\n');git('add','--',*names,'control.txt')
        for name in names+['control.txt']:(target/name).unlink()
        git('checkout-index','-a','-f')
        for name in names:assert sha(target/name)==sources[name],name;rows.append(dict(source=name,sha256=sources[name]))
        assert(target/'control.txt').read_bytes()==b'one\r\ntwo\r\n'
    assert sha(index)==before,'Real Git index changed'
    report=dict(preservedFiles=len(names),coreAutocrlf=True,unscopedControlBecameCrLf=True,realRepositoryIndexUnchanged=True,windowsProcess=False,sources=sources,cases=rows,
      scope='Actual isolated Linux git checkout-index/core.autocrlf=true; source LF, exact native export logs/xrefs and frozen package -text retained. Not a Windows process or Unity import.')
    if'--check-only'not in sys.argv:(PKG/'source-eol-check.json').write_bytes((json.dumps(report,ensure_ascii=False,indent=2)+'\n').encode())
    print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']}))
if __name__=='__main__':main()
