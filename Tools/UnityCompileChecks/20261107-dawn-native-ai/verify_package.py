"""Frozen package verification; --snapshot-only allows later working source changes."""
import hashlib,json,subprocess,sys
from pathlib import Path
PKG=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def verify_preserved(root,data):
    preserved=data['preserved']
    for row in preserved['preexistingPyc']:assert sha(root/row['path'])==row['sha256'],row['path']
    # Compare against the recorded baseline commit, not current git-status:
    # committing unchanged task work must not invalidate its preservation proof.
    def git(*args):
        return subprocess.check_output(['git','-c','core.quotepath=false',*args],cwd=root).decode('utf-8').split('\0')
    scopes=preserved['protectedGitScopes']
    changed=set(git('diff','--name-only','--no-renames','-z',data['head'],'--',*scopes))
    changed.update(git('ls-files','--others','--exclude-standard','-z','--',*scopes));changed.discard('')
    assert changed==set(preserved['allowedProtectedChanges']),'Protected scope differs from frozen baseline: '+str(sorted(changed))
    for previous in preserved['previousPackages']:
        folder=root/'Tools/UnityCompileChecks'/previous['package'];inventory=folder/'package-inventory.json'
        assert sha(inventory)==previous['inventorySha256'],str(inventory)
        entries=read(inventory)['entries']
        if isinstance(entries,dict):entries=[dict(path=name,sha256=digest)for name,digest in entries.items()]
        assert len(entries)==previous['files']
        for row in entries:assert sha(folder/row['path'])==row['sha256'],(previous['package'],row['path'])
def main():
    ROOT=PKG.parents[2]
    current='--snapshot-only'not in sys.argv;inventory=read(PKG/'package-inventory.json');data=read(PKG/'development-verification.json')
    paths={p.relative_to(PKG).as_posix()for p in PKG.rglob('*')if p.is_file()and p.name!='package-inventory.json'}
    assert paths=={r['path']for r in inventory['entries']},'Package file inventory differs'
    for row in inventory['entries']:assert sha(PKG/row['path'])==row['sha256'],row['path']
    for row in data['snapshots']:
        assert sha(PKG/row['snapshot'])==row['sha256'],row['snapshot']
        if current:assert sha(ROOT/row['source'])==row['sha256'],'Current source changed: '+row['source']
    if current:
        for path,digest in data['codeSources'].items():assert sha(ROOT/path)==digest,path
        for row in data['snapshots']:
            path=PKG/row['snapshot']
            if path.suffix=='.json':
                report=read(path)
                if isinstance(report,dict):
                    for source,digest in report.get('sources',{}).items():assert sha(ROOT/source)==digest,'Current declared source changed: '+source
        verify_preserved(ROOT,data)
    for row in data['reproductions']:
        directory=PKG/row['path'];assert len(read(directory/('mismatches.json'if row['name']=='clock-before'else'differences.json')))==row['count'],row['name']
        manifest=directory/'original-manifest.json'
        if manifest.is_file():
            for name,digest in read(manifest).get('codeSha256',{}).items():assert sha(directory/name)==digest,(row['name'],name)
            for source in read(manifest).get('sourceSnapshots',[]):assert sha(directory/'source-before'/source['source'])==source['sha256'],source['source']
        if row['name']=='magic-attribute-before':
            old=read(directory/'source-before/Assets/_Original/Data/Combat/MagicMenu.json');now=read(PKG/'snapshots/Assets/_Original/Data/Combat/MagicMenu.json')
            for item in now['entries']:del item['attribute']
            assert old==now,'Magic catalog changed beyond the added attribute'
    controls=read(PKG/'package-preservation-check.json')
    assert controls['controlledCases']==len(controls['cases']) and controls['refusalCases']==sum(row['refused']for row in controls['cases'])
    for source,digest in controls['sources'].items():assert sha(PKG/source)==digest,'Preservation controls changed after execution: '+source
    if current:
        for source,digest in read(PKG/'source-eol-check.json')['sources'].items():assert sha(ROOT/source)==digest,'EOL source changed: '+source
    for state in (PKG/'runs').glob('*/compiler-hosts/*/compiler-host-preparation.json'):
        status=read(state);assert status['status']=='passed'
        for name,digest in status['files'].items():assert sha(state.parent/name)==digest,(state,name)
    print(json.dumps(dict(packageFiles=len(paths),snapshots=len(data['snapshots']),scriptSources=len(data['codeSources']),reports=data['integration']['reports'],frozenBytesMatch=True,currentSourcesChecked=current,unityPlay=False)))
if __name__=='__main__':main()
