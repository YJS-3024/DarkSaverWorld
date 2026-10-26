"""Validate source music bindings, playable audio and exact WAV sample conversion."""
import csv,json,hashlib,struct,subprocess,wave,re
from pathlib import Path
from world_asset_baker import ROOT

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    folder=ROOT/'Assets/_Game/Resources/World/Audio';data=json.loads((folder/'Manifest.json').read_text())
    table={t[0]:t[1].lower() for line in (ROOT/'분석/Saver_Late/decoded/gameres/set.arc/midiset.txt').read_text().splitlines() if len(t:=line.split())==2 and t[0].isdigit()}
    assert data['version']==1 and len(data['regions'])==44 and len(data['sounds'])==20
    fallbacks={r['map']:r['inheritedFrom'] for r in data['regions'] if r['authored']};assert fallbacks=={'1111':'1110','1410':'1400','1500':'2000','2510':'2500'}
    clips=set();native=ROOT/'분석/Saver_Late/native_music'
    sources={p.name.lower():p for p in native.iterdir() if p.is_file()}
    for row in data['regions']:
        key=row['inheritedFrom'] or row['map']
        if key in table:assert row['midi'].lower()==table[key] and not row.get('lateBindingMissing',False)
        else:assert row.get('lateBindingMissing',False) and row['bindingProvenance'].startswith('legacy binding retained')
        assert sha(sources[row['midi'].lower()])==row['sourceSha256']
        path=ROOT/'Assets/_Game/Resources'/(row['clip']+'.ogg');assert sha(path)==row['audioSha256'];clips.add(path)
    for path in clips:
        stream=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','a:0','-show_entries','stream=codec_name,sample_rate,channels,duration','-of','json',str(path)],text=True))['streams'][0]
        assert stream['codec_name']=='vorbis' and int(stream['sample_rate'])==44100 and float(stream['duration'])>1
    frames=0
    for row in data['sounds']:
        source=ROOT/row['source'];path=ROOT/'Assets/_Game/Resources'/(row['clip']+'.wav')
        assert sha(source)==row['sourceSha256'] and sha(path)==row['audioSha256']
        with wave.open(str(source),'rb') as f:raw=f.readframes(f.getnframes())
        with wave.open(str(path),'rb') as f:
            assert (f.getnchannels(),f.getsampwidth(),f.getframerate())==(1,2,22050)
            assert f.readframes(f.getnframes())==struct.pack('<'+'h'*len(raw),*((x-128)*256 for x in raw))
        assert len(raw)==row['frames'];frames+=len(raw)
    paths=list(folder.rglob('*'))+[ROOT/'Assets/_Game/Runtime/Core/WorldAudioData.cs',ROOT/'Assets/_Game/Runtime/World/WorldAudioService.cs']
    guid_paths={}
    for meta in (ROOT/'Assets/_Game').rglob('*.meta'):
        match=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
        if match:guid_paths.setdefault(match[1],[]).append(meta)
    for path in paths:
        if path.suffix=='.meta':continue
        meta=Path(str(path)+'.meta');assert meta.exists(),path
        guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1];assert len(guid_paths[guid])==1
    report=dict(regionBindings=44,nativeBindings=sum(not r['authored'] and not r.get('lateBindingMissing',False) for r in data['regions']),legacyBindingsRetained=[r['map'] for r in data['regions'] if r.get('lateBindingMissing',False)],authoredFallbacks=fallbacks,uniqueBgmClips=len(clips),exactConvertedSounds=20,exactPcmFrames=frames,metadataUnique=True,scenarioMissingOriginal=['nco02.mid'],scope='Original binding/hash checks, ffprobe codec/duration, exact source PCM sample conversion; no Unity import or listening/playback verification.',unityPlay=False)
    (ROOT/'Tools/MapChecks/world_audio_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
if __name__=='__main__':main()
