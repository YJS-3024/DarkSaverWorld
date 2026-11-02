"""Exact Compress.ReadArcBmp2 palette-index remap for the73 original player BMPs."""
import hashlib,json,re,uuid
from pathlib import Path
from PIL import Image
from build_world_assets import ROOT,BASE,metadata,key
OUT=Path(__file__).parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def remap(image,style):
    assert image.mode=='P'and 0<=style<4
    result=image.copy();data=bytes(result.tobytes());result.frombytes(bytes(v+16*style if 160<=v<=175 else v for v in data));return result
def build():
    sources=sorted((ROOT/'분석/Saver_Late/decoded_art/gameres').glob('char*.arc/*r.bmp'))
    sources=[p for p in sources if p.stem[:-1].isdigit()and int(p.stem[:-1])<300];assert len(sources)==73
    dest=BASE/'Art/Combat/Palettes';dest.mkdir(exist_ok=True);metadata.meta(dest,ROOT/'Assets',True)
    template=Path(str(BASE/'Art/Tutorial/Attackeffects.png')+'.meta').read_text();jobs=[];styles=[];guids=[];records=[]
    for source in sources:
        image=Image.open(source);assert image.mode=='P'and image.width%4==0;job=int(source.stem[:-1])
        archived=BASE/'Art/Archives/gameres'/source.relative_to(ROOT/'분석/Saver_Late/decoded_art/gameres');archived=Path(str(archived)+'.png')
        assert image.convert('RGB').tobytes()==Image.open(archived).convert('RGB').tobytes(),source
        for style in [1,2,3]:
            rgba=key(remap(image,style));target=dest/f'{job}_{style}.png';rgba.save(target)
            guid=uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+target.relative_to(BASE).as_posix()).hex
            Path(str(target)+'.meta').write_text(re.sub(r'^guid: .*$', 'guid: '+guid,template,flags=re.M))
            jobs.append(job);styles.append(style);guids.append(guid);records.append(dict(asset=target.relative_to(ROOT).as_posix(),sha256=sha(target),job=job,style=style,pixelSha256=hashlib.sha256(rgba.tobytes()).hexdigest(),source=source.relative_to(ROOT).as_posix(),sourceSha256=sha(source)))
    asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text()
    for name in ['paletteJobs','paletteStyles','palettePictures']:text=re.sub(r'^  '+name+r':\n(?:  - .*\n)*','',text,flags=re.M)
    text+='  paletteJobs:\n'+''.join(f'  - {j}\n'for j in jobs)+'  paletteStyles:\n'+''.join(f'  - {s}\n'for s in styles)+'  palettePictures:\n'+''.join('  - {fileID: 2800000, guid: '+g+', type: 3}\n'for g in guids)
    if not re.search(r'^  ownPalette:',text,re.M):text+='  ownPalette: 0\n'
    asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=OUT/name;report=json.loads(p.read_text());path=asset.relative_to(ROOT).as_posix()
        if path in report.get('sources',{}):report['sources'][path]=sha(asset)
        for row in report.get('outputs',[]):
            if row['asset']==path:row['sha256']=sha(asset)
        p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    result=dict(outputs=records,jobs=len(sources),variants=len(records),policy='Original eight-bit BMP palette; ReadArcBmp2 shifts only pixel indicesA0..AF by16*style1..3. Other pixels/palette colors and black transparency preserved. Original team-color preferences0/3/2; no ID-based alliance guess.',unityPlayExecuted=False)
    (OUT/'body-palette-assets.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');return result
if __name__=='__main__':
    result=build();print(json.dumps({k:v for k,v in result.items()if k!='outputs'},ensure_ascii=False))
