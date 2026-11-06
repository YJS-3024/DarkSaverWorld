"""Actual EVT file-loader output versus production parser on every source file."""
import hashlib
import json
from pathlib import Path
import check_native_event_corpus as corpus
from check_native_battle_responses import contract_sources
from native_event_evidence import event_corpus_sources
from native_event_loader_emulator import NativeEventLoader

ROOT = corpus.ROOT
OUT = corpus.OUT
WORK = Path('/tmp/ds-native-event-loader')


def main():
    WORK.mkdir(exist_ok=True)
    files = sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*/*.evt.txt'))
    assert len(files) == 112
    controls = {
        'event-zero': 'EVENT 0\nGOLD 123\nEND',
        'blank-disables': 'EVENT 1\nGOLD 123\n\nEND',
        '999-disables': 'EVENT 1\nGOLD 123\n999\nEND',
        'EOF-no-phantom-blank': 'EVENT 1\nGOLD 123\n',
        'numeric-token': 'EVENT 1\n1 CHARPOS 1 2\n01 CHARPOS 2 3\n1. text\n+1 text\n-1 text\n0 text\nEND',
        'nested-header': 'EVENT 1\nGOLD 1\nEVENT 2\nGOLD 2\nEND',
        'duplicate-header': 'EVENT 1\nGOLD 1\nEND\nEVENT 1\nGOLD 2\nEND',
        'case-sensitive-header': 'event 1\nGOLD 1\nEND',
        'literal-commands': 'EVENT 1\ngold 1\nGOLD\t1\nEND',
        '99-action-slots': 'EVENT 1\n'+'ALL\n'*105+'END',
        'outside-actions': 'GOLD 1\nEVENT 1\nGOLD 2\nEND\nGOLD 3',
    }
    control_files = []
    for name,source in controls.items():
        path = WORK/(name+'.evt.txt')
        path.write_bytes(source.encode('utf-8'))
        control_files.append(path)
    parsed,_ = corpus.load_bodies(files+control_files)
    expected = {str(p): {} for p in files+control_files}
    for row in parsed:
        expected[row['file']][row['number']] = row['body']
    args = (corpus.WORK/'parser.rsp').read_text(encoding='utf-8')
    baseline = contract_sources(args,__file__) | event_corpus_sources()
    for path in [OUT/'native_event_loader_emulator.py',
                 ROOT/'분석/Ghidra/GameCtl/LoadScenarioEventActions.c']:
        baseline[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    native = NativeEventLoader()
    comparisons = []
    failures = []
    source_events = 0
    for path in files+control_files:
        try:
            actual = native.load(path.read_text(encoding='utf-8'),path.name.split('.')[0] if path in files else '01')
        except Exception as error:
            raise RuntimeError('Original EVT loader fixture: '+str(path)) from error
        wanted = expected[str(path)]
        bad = [dict(number=n,native=actual.get(n),managed=wanted.get(n))
               for n in actual.keys()|wanted.keys() if actual.get(n)!=wanted.get(n)]
        if path in files:
            source_events += len(actual)
        comparisons.append(dict(file=path.relative_to(ROOT).as_posix() if path in files else path.name,
                                events=len(actual),mismatches=bad))
        failures.extend(bad)
    refusal_cases = []
    for name,options in [('no-archive',dict(archive=False)),
                         ('extraction-failed',dict(extracted=False)),
                         ('file-open-failed',dict(opened=False))]:
        # Same native instance still contains the last loaded table: failure
        # paths must clear its active flags instead of retaining old events.
        before = native.load('EVENT 1\nGOLD 123\nEND')
        assert before == {1:['GOLD 123','END']}
        after = native.load('EVENT 1\nGOLD 456\nEND',**options)
        assert after == {},(name,after)
        refusal_cases.append(name)
    for path,digest in baseline.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('source-changed-during-check',path)
    result = dict(sourceFiles=len(files),sourceEventBodies=source_events,
                  parserControls=len(controls),loaderFailurePaths=refusal_cases,
                  mismatches=len(failures),unityPlay=False,originalGameCtlSha256=native.sha256,
                  sources=baseline,comparisons=comparisons,controlSources=controls,
                  scope='Actual13D60/F110/F310/F040 executes over all112 decoded source EVT files. '
                        'Production parser is compiled unchanged and compared by active event ID and '
                        'ordered body through first END. Includes zero/duplicate/nested headers, '
                        'literal case/space tokens, full decimal conditions, blank/999 disabling, '
                        '99 action slots and EOF; failed extraction/open clears old flags. '
                        'CString storage and decoded file/archive callbacks are supplied; '
                        'not actual archive extraction, server condition evaluation or Unity Play.')
    (OUT/'native-event-loader-check.json').write_bytes(
        (json.dumps(result,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
    print(json.dumps({k:v for k,v in result.items()if k not in ['sources','comparisons','controlSources']},ensure_ascii=False))
    assert not failures,failures[:1]


if __name__ == '__main__':
    main()
