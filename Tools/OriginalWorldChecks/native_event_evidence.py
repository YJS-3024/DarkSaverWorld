"""Source closure shared by the directed-event native comparison reports."""
import hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def native_event_sources():
    paths=[Path(__file__),ROOT/'분석/InstallerVersions/installer_inventory.json']
    paths += [OUT/name for name in ['native_ai_emulator.py','native_battle_options_emulator.py','native_battle_briefing_emulator.py',
        'native_battle_status_emulator.py','native_result_emulator.py','native_result_kills_emulator.py','native_scenario_end_emulator.py','native_event_step_emulator.py']]
    return {p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths}
def event_corpus_sources():
    paths=[OUT/name for name in ['check_native_event_corpus.py','NativeImmediateEventsDifferential.cs','NativeEventCorpusRequests.cs']]
    paths+=sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*/*.evt.txt'))
    assert len(paths)==115
    return native_event_sources()|{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths}
