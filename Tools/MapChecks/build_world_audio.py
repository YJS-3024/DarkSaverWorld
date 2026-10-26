"""Native world BGM and lossless sound conversion; four missing map bindings explicit."""
import csv,json,hashlib,re,wave,struct,subprocess
from pathlib import Path
from world_asset_baker import ROOT,directory,text_asset
from bake_scenario01_music import bake
OUT=ROOT/'Assets/_Game/Resources/World/Audio'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    directory(OUT);directory(OUT/'Music');directory(OUT/'Sounds')
    legacy_table={r['map_id']:r['midi'] for r in csv.DictReader((ROOT/'닥세_리소스/분석결과/맵_BGM_연결.csv').open(encoding='utf-8-sig'))}
    table={t[0]:t[1] for line in (ROOT/'분석/Saver_Late/decoded/gameres/set.arc/midiset.txt').read_text().splitlines() if len(t:=line.split())==2 and t[0].isdigit()}
    audit=json.loads((ROOT/'Tools/MapChecks/shop_interiors_resources.json').read_text())
    maps=set(['1000','1002','1003','1004','1010','1011','1012','1020','1030','1040','1050','1060','1070','1080','1090','1100','1110','1111','1200','1300','1400','1410'])|{r['nativeMap'] for r in audit['nativeInteriorMaps']}
    fallback={'1111':'1110','1410':'1400','1500':'2000','2510':'2500'};bindings=[]
    source=ROOT/'분석/Saver_Late/native_music'
    filenames={p.name.lower():p for p in source.iterdir() if p.is_file()}
    for key in sorted(maps):
        inherited=fallback.get(key);lookup=inherited or key;missing=lookup not in table;midi=table.get(lookup,legacy_table.get(lookup));track=Path(midi).stem.lower()
        shared=ROOT/'Assets/_Game/Resources/Scenarios/Shared/Music'/(track+'.ogg')
        clip='Scenarios/Shared/Music/'+track if shared.exists() else 'World/Audio/Music/'+track
        output=ROOT/'Assets/_Game/Resources'/(clip+'.ogg');native=filenames[midi.lower()]
        if not output.exists():bake([native.name],OUT/'Music',source=source)
        bindings.append(dict(map=key,clip=clip,midi=midi,sourceSha256=sha(native),audioSha256=sha(output),authored=inherited is not None,inheritedFrom=inherited or '',sourceVersion='Saver_Late',lateBindingMissing=missing,bindingProvenance='legacy binding retained: absent from late midiset' if missing else 'explicit late midiset'))
    template=(ROOT/'Assets/_Game/Art/Converted/Audio/Magic/Magic_21.wav.meta').read_text()
    sounds=[]
    for native in sorted((ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/sound').iterdir()):
        if native.suffix.lower()!='.wav':continue
        key=int(native.stem);output=OUT/'Sounds'/f'{key:02}.wav'
        with wave.open(str(native),'rb') as f:
            assert (f.getnchannels(),f.getsampwidth(),f.getframerate())==(1,1,22050);pcm=f.readframes(f.getnframes())
        converted=struct.pack('<'+'h'*len(pcm),*((v-128)*256 for v in pcm))
        with wave.open(str(output),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(22050);f.writeframes(converted)
        from build_scenario01 import guid
        output.with_name(output.name+'.meta').write_text(re.sub(r'^guid: [a-f0-9]+','guid: '+guid(output),template,flags=re.M))
        sounds.append(dict(id=key,clip=f'World/Audio/Sounds/{key:02}',source=str(native.relative_to(ROOT)),sourceSha256=sha(native),audioSha256=sha(output),frames=len(pcm)))
    jobs=[dict(job=int(r['직업']),appearance=int(r['직그림'])) for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/units_all.csv').open(encoding='utf-8-sig'))]
    text_asset(OUT/'Manifest.json',dict(version=1,regions=bindings,sounds=sounds,jobs=jobs))
    report=dict(regionBindings=len(bindings),nativeBindings=sum(not r['authored'] and not r['lateBindingMissing'] for r in bindings),authoredFallbacks=[r for r in bindings if r['authored']],soundFiles=len(sounds),midiRenderer='FluidSynth/TimGM; original score, adapted synthesizer timbre',soundConversion='exact unsigned8 -> signed16, 22050Hz mono',automaticEffects='battle magic and supported scenario spell cues + sound1 on death excluding native appearance700..799; current singleplayer presentation timing',notAssigned='Unconfirmed attack/movement/shop/UI sound IDs are not invented.',unityPlay=False)
    (ROOT/'Tools/MapChecks/world_audio_resources.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
if __name__=='__main__':main()
