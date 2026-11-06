"""Exercise reference refresh on isolated metadata, including late refusals.

Repository evidence, reports, game assets and saved profiles remain read-only.
This does not execute the referenced native/managed tests again.
"""
import contextlib
import io
import json
import tempfile
from pathlib import Path
import refresh_native_evidence_references as refresh

ROOT = refresh.ROOT
OUT = ROOT / 'Tools/OriginalWorldChecks'


def main():
    originals = {name: (refresh.BASE / name / 'evidence.json').read_bytes()
                 for name in refresh.REFERENCES}
    original_base = refresh.BASE
    assertions = 0
    with tempfile.TemporaryDirectory(prefix='ds-evidence-refresh-') as temporary:
        refresh.BASE = Path(temporary)
        try:
            def prepare():
                for name, raw in originals.items():
                    target = refresh.BASE / name / 'evidence.json'
                    target.parent.mkdir(exist_ok=True)
                    target.write_bytes(raw)

            def snapshot():
                return {name: (refresh.BASE / name / 'evidence.json').read_bytes()
                        for name in originals}

            def stale_earlier_reference():
                # Force a visible earlier update. Identical current metadata
                # would hide a premature write in the late-refusal controls.
                name = next(iter(refresh.REFERENCES))
                field = next(iter(refresh.REFERENCES[name]))
                target = refresh.BASE / name / 'evidence.json'
                data = json.loads(target.read_bytes())
                data[field]['sha256'] = '0' * 64
                target.write_bytes(json.dumps(data,ensure_ascii=False).encode('utf-8'))

            prepare()
            stale_earlier_reference()
            with contextlib.redirect_stdout(io.StringIO()):
                refresh.main()
            for name, fields in refresh.REFERENCES.items():
                before = json.loads(originals[name])
                after = json.loads(snapshot()[name])
                for field, filename in fields.items():
                    report = OUT / filename
                    assert after[field] == dict(report=report.relative_to(ROOT).as_posix(),
                                                sha256=refresh.sha(report))
                    after[field] = before[field]
                    assertions += 1
                assert after == before, 'Refresh modified non-reference evidence: ' + name
                assertions += 1

            refusals = []
            # Corrupt the LAST manifest so earlier metadata would already have
            # been written if refresh were not validating the whole batch first.
            last = next(reversed(refresh.REFERENCES))
            for mutation in ['evidence-hash', 'reference-path']:
                prepare()
                stale_earlier_reference()
                target = refresh.BASE / last / 'evidence.json'
                changed = json.loads(target.read_bytes())
                if mutation == 'evidence-hash':
                    changed['files'][0]['sha256'] = '0' * 64
                else:
                    field = next(iter(refresh.REFERENCES[last]))
                    changed[field]['report'] = 'unapproved-retarget.json'
                target.write_text(json.dumps(changed), encoding='utf-8')
                before = snapshot()
                try:
                    with contextlib.redirect_stdout(io.StringIO()):
                        refresh.main()
                except AssertionError as error:
                    refusals.append(dict(mutation=mutation,reason=str(error)))
                else:
                    raise AssertionError('Invalid metadata was accepted: ' + mutation)
                assert snapshot() == before, 'Partial refresh before late refusal'
                assertions += 1
        finally:
            refresh.BASE = original_base

    for name, raw in originals.items():
        assert (original_base / name / 'evidence.json').read_bytes() == raw
        assertions += 1
    sources = {path.relative_to(ROOT).as_posix(): refresh.sha(path)
               for path in [Path(__file__), Path(refresh.__file__),
                            *[original_base / name / 'evidence.json' for name in originals]]}
    result = dict(assertions=assertions,manifests=len(originals),
                  lateRefusals=refusals,repositoryManifestsUnchanged=True,
                  forcedEarlierReferenceChange=True,
                  testsRerunByThisCheck=False,unityPlay=False,sources=sources,
                  scope='Actual metadata refresh over isolated manifest copies. '
                        'Current report/source verification, exact reference-only changes, '
                        'and no partial writes on late bad evidence/retarget refusal. '
                        'No game files or original evidence are mutated.')
    (OUT / 'native-evidence-refresh-guard-check.json').write_text(
        json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='sources'},ensure_ascii=False))


if __name__ == '__main__':
    main()
