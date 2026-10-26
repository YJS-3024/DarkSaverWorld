"""Apply explicit later midiset bindings and render changed MIDI scores.
Preserves documented missing-map fallbacks. No claim of original synth timbre or listening QA.
"""
import hashlib,json,sys,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
from bake_scenario01_music import bake
native=Path(sys.argv[1])/'Saver_Files/saver/BGM';source=ROOT/'분석/Saver_Late/native_music';source.mkdir(exist_ok=True)
files={p.name.lower():p for p in native.iterdir() if p.suffix.lower()=='.mid'}
for p in files.values():shutil.copyfile(p,source/p.name)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
lines=(ROOT/'분석/Saver_Late/decoded/gameres/set.arc/midiset.txt').read_text().splitlines();bindings={}
for line in lines:
 t=line.split()
 if len(t)==2 and t[0].isdigit():bindings[t[0]]=t[1]
declared_rows=int(lines[0]);parsed_rows=len(bindings) # Preserve the original 47/50 discrepancy; current region bindings precede the trailing rows.
path=ROOT/'Assets/_Game/Resources/World/Audio/Manifest.json';data=json.loads(path.read_text());rendered={};changes=[];missing_bindings=[]
old_dir=ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/BGM';old_files={p.name.lower():p for p in old_dir.iterdir() if p.is_file()}
for row in data['regions']:
 key=row['inheritedFrom'] if row['authored'] else row['map'];missing=key not in bindings;midi=bindings.get(key,row['midi']);
 if missing:missing_bindings.append(dict(map=row['map'],lookup=key,retainedMidi=midi))
 track=Path(midi).stem.lower();native_file=files[midi.lower()];shared=ROOT/f'Assets/_Game/Resources/Scenarios/Shared/Music/{track}.ogg';clip=f'Scenarios/Shared/Music/{track}' if shared.exists() else f'World/Audio/Music/{track}';output=ROOT/'Assets/_Game/Resources'/(clip+'.ogg')
 old=old_files.get(midi.lower());changed=not output.exists() or old is None or sha(old)!=sha(native_file)
 if changed and track not in rendered:
  bake([native_file.name],output.parent,source=source);rendered[track]=dict(sourceSha256=sha(native_file),audioSha256=sha(output))
 if row['midi'].lower()!=midi.lower():changes.append(dict(map=row['map'],before=row['midi'],after=midi))
 row.update(midi=midi,clip=clip,sourceSha256=sha(native_file),audioSha256=sha(output),sourceVersion='Saver_Late',lateBindingMissing=missing,bindingProvenance='legacy binding retained: absent from late midiset' if missing else 'explicit late midiset')
tables=json.loads((ROOT/'분석/Saver_Late/tables.json').read_text());data['jobs']=[dict(job=int(r['values'][0]),appearance=int(r['values'][1])) for r in tables['ability.atr']['rows']];data['sourceVersion']='Saver_Late';path.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n')
report=dict(midisetDeclaredRows=declared_rows,midisetParsedRows=parsed_rows,regionBindings=len(data['regions']),changedBindings=changes,legacyBindingsRetained=missing_bindings,rendered=rendered,unitSoundBindings=len(data['jobs']),renderer='FluidSynth/TimGM; adapted timbre, no listening or original playback verification',unityPlay=False)
(ROOT/'Tools/LateVersionChecks/audio_update_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report))
