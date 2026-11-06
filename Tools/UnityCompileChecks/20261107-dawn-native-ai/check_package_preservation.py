"""Exercise preservation refusals in an isolated Git repository, never user saves."""
import copy,hashlib,importlib.util,json,subprocess,sys,tempfile
from pathlib import Path
HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('frozen_verifier',HERE/'verify_package.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    cases=[]
    with tempfile.TemporaryDirectory(prefix='ds-package-preservation-')as temporary:
        root=Path(temporary)
        def git(*args):return subprocess.check_output(['git',*args],cwd=root,stderr=subprocess.PIPE,text=True).strip()
        def write(name,value):
            p=root/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(value);return p
        git('init','-q');git('config','user.name','Isolated verification');git('config','user.email','fixture@example.invalid')
        base=write('Protected/base.dat',b'original');pyc=write('cache/original.pyc',b'original pycache')
        previous=write('Tools/UnityCompileChecks/prior/result.json',b'{"original":true}\n')
        inventory=write('Tools/UnityCompileChecks/prior/package-inventory.json',(json.dumps(dict(entries=[dict(path=previous.name,sha256=sha(previous))]))+'\n').encode())
        git('add','.');git('commit','-qm','isolated baseline');head=git('rev-parse','HEAD')
        allowed=write('Protected/allowed.json',b'new data')
        data=dict(head=head,preserved=dict(preexistingPyc=[dict(path='cache/original.pyc',sha256=sha(pyc))],protectedGitScopes=['Protected'],allowedProtectedChanges=['Protected/allowed.json'],previousPackages=[dict(package='prior',files=1,inventorySha256=sha(inventory))]))
        def accept(name,record=data):module.verify_preserved(root,record);cases.append(dict(name=name,refused=False))
        def refuse(name,record=data):
            try:module.verify_preserved(root,record)
            except AssertionError:cases.append(dict(name=name,refused=True))
            else:raise AssertionError('Incorrectly accepted '+name)
        accept('unchanged allowed untracked data');git('add','Protected/allowed.json');accept('unchanged allowed staged data');git('commit','-qm','isolated task data');accept('same data committed after recorded baseline')
        base.write_bytes(b'changed');refuse('protected existing bytes changed');base.write_bytes(b'original')
        extra=write('Protected/unexpected.dat',b'new');refuse('unexpected protected untracked file');extra.unlink()
        pyc.write_bytes(b'changed');refuse('preexisting pycache changed');pyc.write_bytes(b'original pycache')
        original=previous.read_bytes();previous.write_bytes(b'{"changed":true}\n');refuse('previous package result changed');previous.write_bytes(original)
        original=inventory.read_bytes();inventory.write_bytes(b'{}');refuse('previous inventory changed');inventory.write_bytes(original)
        changed=copy.deepcopy(data);changed['preserved']['previousPackages'][0]['files']=2;refuse('previous package file count disagrees',changed)
        write('Unrelated/note.txt',b'allowed unrelated task note');accept('outside protected scopes is handled by separate source snapshots')
    report=dict(controlledCases=len(cases),refusalCases=sum(c['refused']for c in cases),cases=cases,realRepositoryMutated=False,unityPlay=False,sources={p.name:sha(p)for p in[Path(__file__),HERE/'verify_package.py']},scope='Actual isolated Git baseline/stage/later-commit protection, prior file/inventory/count and preexisting pycache tamper refusals. Does not edit or test failure saves, Unity or the real Git index.')
    if '--check-only'not in sys.argv:(HERE/'package-preservation-check.json').write_bytes((json.dumps(report,ensure_ascii=False,indent=2)+'\n').encode())
    print(json.dumps({k:v for k,v in report.items()if k not in ['cases','sources']}))
if __name__=='__main__':main()
