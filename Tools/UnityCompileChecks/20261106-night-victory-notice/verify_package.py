"""Verify frozen bytes; --snapshot-only permits later repository source changes."""
import hashlib,json,sys
from pathlib import Path
PKG=Path(__file__).resolve().parent;ROOT=PKG.parents[2]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def main():
    current='--snapshot-only'not in sys.argv;inventory=read(PKG/'package-inventory.json');data=read(PKG/'development-verification.json')
    for row in inventory['entries']:
        p=PKG/row['path'];assert p.is_file()and sha(p)==row['sha256'],row['path']
    files={p.relative_to(PKG).as_posix()for p in PKG.rglob('*')if p.is_file()and p.name!='package-inventory.json'}
    assert files=={r['path']for r in inventory['entries']},'package inventory differs from its files'
    for row in data['snapshots']:
        target=PKG/row['snapshot'];assert sha(target)==row['sha256'],row['snapshot']
        if current:assert sha(ROOT/row['source'])==row['sha256'],'current source report differs: '+row['source']
    if current:
        for source,digest in data['codeSources'].items():assert sha(ROOT/source)==digest,'current code differs: '+source
        # Verify declared report inputs too, not only the report file itself.
        for row in data['snapshots']:
            p=PKG/row['snapshot']
            if p.suffix=='.json':
                report=read(p)
                for source,digest in report.get('sources',{}).items():
                    assert sha(ROOT/source)==digest,'current declared input differs: '+source
    reproduction=PKG/'reproductions/moving-ai-before';initial=read(reproduction/'initial-report.json')
    assert initial['mismatches']==8 and len(read(reproduction/'differences.json'))==8 and len(read(reproduction/'target-cases.json'))==12
    assert sha(reproduction/'original-planner.cs')==initial['sources']['Assets/_Original/Runtime/Combat/SoldierActions.cs']
    assert sha(reproduction/'initial-check.py')==initial['sources']['Tools/OriginalWorldChecks/check_native_soldier_ai.py']
    print(json.dumps(dict(packageFiles=len(files),snapshots=len(data['snapshots']),scriptSources=len(data['codeSources']),sourceCurrentChecked=current,
        frozenBytesMatch=True,initialEightDiscrepanciesPreserved=True,unityPlay=False)))
if __name__=='__main__':main()
