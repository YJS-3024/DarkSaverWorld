"""Native single-target projectile families, sizes, timing and spell menu checks."""
import csv,hashlib,io,json,struct,subprocess,sys,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-magic-projectile-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')


def check_native(evidence_name='magic-projectile-native-evidence.json'):
    evidence=json.loads((OUT/evidence_name).read_text());inventory=json.loads((ROOT/evidence['sourceInventory']).read_text());count=0
    for source in evidence['sources']:
        paths=[Path(f['extractedFile'])for p in inventory['payloads']for f in p['files']if f.get('sha256')==source['sha256']]
        paths.append(ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver'/source['file'])
        path=next(p for p in paths if p.exists()and hashlib.sha256(p.read_bytes()).hexdigest()==source['sha256'])
        raw=path.read_bytes();pe=struct.unpack_from('<I',raw,60)[0];optional=pe+24;base=struct.unpack_from('<I',raw,optional+28)[0];sections=optional+struct.unpack_from('<H',raw,pe+20)[0]
        for fixture in source['fixtures']:
            rva=int(fixture['address'],16)-base;expected=bytes.fromhex(fixture['bytes']);actual=None
            for i in range(struct.unpack_from('<H',raw,pe+6)[0]):
                _,address,size,offset=struct.unpack_from('<IIII',raw,sections+i*40+8)
                if address<=rva and rva+len(expected)<=address+size:actual=raw[offset+rva-address:offset+rva-address+len(expected)];break
            assert actual==expected,(source['version'],source['file'],fixture['address']);count+=1
    return count


def check_general_assets(projectiles):
    from PIL import Image
    from build_magic_audio import arc_entries,lzh_decode,lzss_decode
    base=ROOT/'Assets/_Original'
    with (base/'Data/Tables/Magictbl.csv').open(encoding='utf-8-sig',newline='')as f:
        rows={int(r['번호']):r for r in csv.DictReader(f)if int(r['번호'])<10000}
    catalog=json.loads((base/'Data/Combat/MagicProjectiles.json').read_text())
    assert set(rows)==set(catalog['magic'])==set(projectiles['supportedMagic'])
    for shape in catalog['shapes']:
        if shape['id']>=10000:continue
        row=rows[shape['id']]
        assert [shape[k]for k in ['pattern','team','unit','distance','range']]==[int(row[k])for k in ['패턴','팀','유닛','거리','범위']]
    images=[Image.open(base/'Art/Tutorial/Effects.png').convert('RGBA'),Image.open(base/'Art/Tutorial/Attackeffects.png').convert('RGBA')]
    for record in projectiles['frameCrops']+projectiles['circleCrops']:
        x,y,w,h=record['rect'];pixels=images[record['surface']].crop((x,y,x+w,y+h)).tobytes()
        assert hashlib.sha256(pixels).hexdigest()==record['pixelSha256']
    assert len(projectiles['circleCrops'])==12 and len(projectiles['noEffectMagic'])==12 and projectiles['missingPatterns']==[64,65]
    audio=json.loads((OUT/'magic-audio-assets.json').read_text());source=ROOT/audio['source']
    assert hashlib.sha256(source.read_bytes()).hexdigest()==audio['sourceSha256']
    original={int(Path(name).stem):(lzh_decode if method==0 else lzss_decode)(data,size)for name,method,size,data in arc_entries(source)if name.lower().endswith('.wav')}
    assert len(original)==len(audio['clips'])==20
    for clip in audio['clips']:
        raw=original[clip['id']];assert hashlib.sha256(raw).hexdigest()==clip['sourceSha256']
        with wave.open(io.BytesIO(raw),'rb')as w:
            native=w.readframes(w.getnframes());width=w.getsampwidth()
            assert (w.getnchannels(),w.getframerate(),w.getnframes())==(clip['channels'],clip['rate'],clip['frames'])
        pcm=b''.join(struct.pack('<h',(v-128)<<8)for v in native)if width==1 else native
        converted=ROOT/clip['asset'];assert hashlib.sha256(converted.read_bytes()).hexdigest()==clip['sha256']
        with wave.open(str(converted),'rb')as w:assert w.getsampwidth()==2 and w.readframes(w.getnframes())==pcm
        assert hashlib.sha256(pcm).hexdigest()==clip['pcmSha256']
        assert 'normalize: 0' in Path(str(converted)+'.meta').read_text()
    used={p[k]for p in catalog['patterns']for k in ['sound1','sound2']if p[k]>0}
    assert used<={r['id']for r in audio['clips']}
    return dict(verifiedFrameCrops=len(projectiles['frameCrops'])+len(projectiles['circleCrops']),nativeSoundClips=len(audio['clips']),audibleSoundIds=sorted(used))


def main():
    WORK.mkdir(exist_ok=True)
    run=subprocess.run([sys.executable,str(OUT/'check_battle_magic.py')],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    run_contract()


def run_contract():
    WORK.mkdir(exist_ok=True)
    old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'MagicProjectileChecks.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'MagicProjectileChecks.cs'))
    args+='\n'+str(OUT.resolve()/'AllMagicChecks.cs')+'\n'
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(ROOT)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        run=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(run.stdout+run.stderr)
        if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2200:])
    evidence=json.loads((OUT/'class-assets.json').read_text())
    for path,digest in evidence['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    for row in evidence['outputs']:assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest()==row['sha256'],row['asset']
    metadata=json.loads((OUT/'magic-menu-assets.json').read_text())
    for path,digest in metadata['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    for row in metadata['outputs']:assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest()==row['sha256'],row['asset']
    assert metadata['rows']==440
    projectiles=json.loads((OUT/'magic-projectile-assets.json').read_text())
    for path,digest in projectiles['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
    for row in projectiles['outputs']:assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest()==row['sha256'],row['asset']
    assert len(projectiles['supportedMagic'])==289 and len(projectiles['areaMagic'])==116 and len(projectiles['groupMagic'])==107 and len(projectiles['frameCrops'])==409
    general_assets=check_general_assets(projectiles)
    native_count=check_native()
    assert evidence['size']==[540,435]and len(evidence['buttons'])==21
    assert [(b['x'],b['y'])for b in evidence['buttons']if b['id']==81]==[(143,394)]
    result=json.loads(run.stdout);previous=json.loads((OUT/'battle-magic-check.json').read_text())
    result.update({k:previous[k]for k in ['runtimeApiErrors','editorApiErrors','playerApiErrors','warnings','resultRegression','trainingRegression','growthRegression','gatewayRegression','campaignRegression','scenarioBattleRegression','exitRegression','revivalRegression','currencyRegression','bankRegression','warehouseRegression','mercenaryRegression','shopRegression','itemUseRegression','classChangeRegression','statAdjustmentRegression','magicLoadoutRegression']});result.update(battleMagicRegression=previous['assertions'],nativeInstructionFixtures=native_count,logs=str(WORK),**general_assets)
    (OUT/'magic-projectile-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
