"""Native sound ARC to lossless PCM16 clips, preserving all samples and the original rate."""
import hashlib,io,json,re,struct,sys,wave
from pathlib import Path
from build_world_assets import ROOT,BASE,metadata
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from build_late_saver_catalog import arc_entries
from analyze_client import lzh_decode,lzss_decode

def build():
    source=BASE/'SourceData/Native/gameres/sound.arc.bytes';out=BASE/'Art/Combat/MagicSounds';out.mkdir(parents=True,exist_ok=True);records=[]
    for name,method,size,packed in arc_entries(source):
        if not name.lower().endswith('.wav'):continue
        raw=(lzh_decode if method==0 else lzss_decode)(packed,size)
        with wave.open(io.BytesIO(raw),'rb')as w:channels=w.getnchannels();width=w.getsampwidth();rate=w.getframerate();frames=w.getnframes();samples=w.readframes(frames)
        assert width in[1,2];pcm=b''.join(struct.pack('<h',(v-128)<<8)for v in samples)if width==1 else samples
        path=out/(str(int(Path(name).stem))+'.wav')
        with wave.open(str(path),'wb')as w:w.setnchannels(channels);w.setsampwidth(2);w.setframerate(rate);w.writeframes(pcm)
        guid=metadata.meta(path,ROOT/'Assets');meta=Path(str(path)+'.meta')
        meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 0\n    quality: 1\n    conversionMode: 0\n  platformSettingOverrides: {}\n  forceToMono: 0\n  normalize: 0\n  preloadAudioData: 1\n  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n  userData: Native sample-preserving PCM conversion\n  assetBundleName:\n  assetBundleVariant:\n')
        with wave.open(str(path),'rb')as w:assert w.getframerate()==rate and w.getnframes()==frames and w.readframes(frames)==pcm
        records.append(dict(id=int(Path(name).stem),asset=str(path.relative_to(ROOT)),guid=guid,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),sourceSha256=hashlib.sha256(raw).hexdigest(),pcmSha256=hashlib.sha256(pcm).hexdigest(),rate=rate,frames=frames,channels=channels))
    records.sort(key=lambda r:r['id']);asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text();lines=text.splitlines();result=[];skip=False
    for line in lines:
        if line.startswith('  magicSoundIds:')or line.startswith('  magicSounds:'):skip=True;continue
        if skip and line.startswith('  -'):continue
        skip=False;result.append(line)
    result+=['  magicSoundIds:']+['  - '+str(r['id'])for r in records]+['  magicSounds:']+['  - {fileID: 8300000, guid: '+r['guid']+', type: 3}'for r in records];asset.write_text('\n'.join(result)+'\n')
    digest=hashlib.sha256(asset.read_bytes()).hexdigest();key=str(asset.relative_to(ROOT))
    for name in['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name);r=json.loads(p.read_text())
        if key in r.get('sources',{}):r['sources'][key]=digest
        for row in r.get('outputs',[]):
            if row['asset']==key:row['sha256']=digest
        p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
    report=dict(source=str(source.relative_to(ROOT)),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),clips=records,unityPlayExecuted=False,policy='All native PCM samples preserved; no normalization/resampling. One non-looping AudioSource per ID; no listening/wall-clock claim.')
    Path(__file__).with_name('magic-audio-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print('native clips',len(records))
if __name__=='__main__':build()
