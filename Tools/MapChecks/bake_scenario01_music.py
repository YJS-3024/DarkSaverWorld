"""Render the two original MIDI files to Unity Ogg clips using installed GM SF2.
The score is original; FluidSynth/TimGM timbre is a local playback adaptation,
not a recording or a reconstruction of the original Windows synthesizer.
"""
import ctypes as C
import ctypes.util
from pathlib import Path
import re
import subprocess
import tempfile
import uuid

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/_Game/Resources/Scenarios/01/Music'
SF2 = Path('/usr/share/sounds/sf2/TimGM6mb.sf2')
NS = uuid.UUID('23b3f21a-85ac-47cb-a63f-c304722b82c8')


def bake(filenames=None, output=None):
    OUT = output or globals()["OUT"]
    library = ctypes.util.find_library('fluidsynth')
    if not library or not SF2.exists():
        raise RuntimeError('Install libfluidsynth and TimGM6mb SoundFont before baking audio.')
    api = C.CDLL(library)
    def bind(name, result, args):
        f = getattr(api, name); f.restype = result; f.argtypes = args; return f
    ptr, integer, string = C.c_void_p, C.c_int, C.c_char_p
    settings_new = bind('new_fluid_settings', ptr, [])
    settings_int = bind('fluid_settings_setint', integer, [ptr, string, integer])
    settings_num = bind('fluid_settings_setnum', integer, [ptr, string, C.c_double])
    settings_str = bind('fluid_settings_setstr', integer, [ptr, string, string])
    synth_new = bind('new_fluid_synth', ptr, [ptr])
    sfload = bind('fluid_synth_sfload', integer, [ptr, string, integer])
    player_new = bind('new_fluid_player', ptr, [ptr])
    player_add = bind('fluid_player_add', integer, [ptr, string])
    play = bind('fluid_player_play', integer, [ptr])
    status = bind('fluid_player_get_status', integer, [ptr])
    renderer_new = bind('new_fluid_file_renderer', ptr, [ptr])
    render = bind('fluid_file_renderer_process_block', integer, [ptr])
    delete_renderer = bind('delete_fluid_file_renderer', None, [ptr])
    delete_player = bind('delete_fluid_player', None, [ptr])
    delete_synth = bind('delete_fluid_synth', None, [ptr])
    delete_settings = bind('delete_fluid_settings', None, [ptr])
    OUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUT.with_name(OUT.name + '.meta')
    if not folder_meta.exists(): folder_meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid5(NS, str(OUT.relative_to(ROOT))).hex + '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    source = ROOT / '분석/Saver_1.2/extracted/Saver_Files/Saver/BGM'
    template = (ROOT / 'Assets/_Game/Art/Converted/Audio/Magic/Magic_21.wav.meta').read_text()
    for filename in (filenames or ['Nco01.mid', 'ncr04.mid']):
        with tempfile.TemporaryDirectory(prefix='ds-midi-') as directory:
            wave = Path(directory) / 'music.wav'
            settings = settings_new()
            settings_num(settings, b'synth.sample-rate', 44100)
            settings_int(settings, b'synth.cpu-cores', 1)
            settings_str(settings, b'player.timing-source', b'sample')
            settings_str(settings, b'audio.file.name', str(wave).encode())
            settings_str(settings, b'audio.file.type', b'wav')
            synth = synth_new(settings)
            player = player_new(synth)
            renderer = None
            try:
                if sfload(synth, str(SF2).encode(), 1) < 0 or player_add(player, str(source / filename).encode()) < 0:
                    raise RuntimeError('Unable to load MIDI/SoundFont.')
                renderer = renderer_new(synth)
                if not renderer or play(player) < 0: raise RuntimeError('Unable to start MIDI render.')
                count = 0
                while status(player) == 1:
                    if render(renderer) < 0: raise RuntimeError('MIDI renderer failed.')
                    count += 1
                    if count > 44100 * 600 / 64: raise RuntimeError('MIDI render exceeds ten-minute bound.')
                for _ in range(1400): render(renderer)  # Preserve synth release tail.
            finally:
                if renderer: delete_renderer(renderer)
                delete_player(player); delete_synth(synth); delete_settings(settings)
            output = OUT / (Path(filename).stem.lower() + '.ogg')
            subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wave), '-c:a', 'libvorbis', '-q:a', '4', str(output)], check=True)
            meta = output.with_name(output.name + '.meta')
            if not meta.exists(): meta.write_text(re.sub(r'^guid: [0-9a-f]+', 'guid: ' + uuid.uuid5(NS, str(output.relative_to(ROOT))).hex, template, flags=re.M))
            print(output.name, output.stat().st_size)


if __name__ == '__main__':
    bake()
