"""Reassemble original Glib result report and button states; no synthetic rewards."""
import hashlib,json,re,sys,tempfile
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT,metadata,key
from build_combat_assets import script_meta
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_lsc,read_ui_library


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def build():
    source=BASE/'SourceData/Native/glib';target=BASE/'Art/World';target.mkdir(exist_ok=True)
    layout=read_lsc(source/'gscene8.lsc.bytes');inputs=[source/'gscene8.lsc.bytes',source/'gamelib.lce.bytes',source/'gamelib.ltn.bytes',ROOT/'닥세_리소스/ㅇㅋㅇ1.jpg']
    with tempfile.TemporaryDirectory()as temp:
        libraries={}
        for ext in ['lce','ltn']:
            p=Path(temp)/('gamelib.'+ext);p.write_bytes((source/('gamelib.'+ext+'.bytes')).read_bytes());libraries[ext]=read_ui_library(p)[:2]
        frame=Image.new('RGBA',(layout['width'],layout['height']))
        for d in layout['decor']:
            a=libraries['lce'][0][d['id']];pic=libraries['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
            for y in range(d['repeat_y']):
                for x in range(d['repeat_x']):frame.alpha_composite(pic,(d['x']+x*a['width'],d['y']+y*a['height']))
        controls=[]
        for d in layout['buttons']:
            assert d['kind']=='button';a=libraries['ltn'][0][d['id']]
            controls.append(dict(id=d['id'],x=d['x'],y=d['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
        background=target/'ScenarioResult.png';buttons=target/'ScenarioResultButtons.png'
        key(frame).save(background);key(libraries['ltn'][1]).save(buttons)
    assert (layout['width'],layout['height'])==(434,344)
    assert len(controls)==1 and (controls[0]['id'],controls[0]['x'],controls[0]['y'],controls[0]['width'],controls[0]['height'])==(19,372,311,46,19)
    data=BASE/'Data/World/ScenarioResult.json';data.write_text(json.dumps(dict(scene=8,width=layout['width'],height=layout['height'],buttons=controls),ensure_ascii=False,indent=2)+'\n')
    for p in [background,buttons,data]:metadata.meta(p,ROOT/'Assets')
    for p in [BASE/'Runtime/Campaign/CampaignResult.cs',BASE/'Runtime/Campaign/CampaignResultService.cs',BASE/'Runtime/World/WorldSceneRoot.Result.cs']:script_meta(p)
    asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text()
    for field,p,kind in [('resultWindow',background,2800000),('resultButtons',buttons,2800000),('resultLayout',data,4900000)]:
        guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
        text='\n'.join(s for s in text.splitlines()if not s.startswith('  '+field+':'))+'\n'
        text+='  '+field+': {fileID: '+str(kind)+', guid: '+guid+', type: 3}\n'
    asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name);report=json.loads(p.read_text());path=asset.relative_to(ROOT).as_posix()
        if path in report.get('sources',{}):report['sources'][path]=sha(asset)
        for o in report.get('outputs',[]):
            if o['asset']==path:o['sha256']=sha(asset)
        p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    report=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in inputs},outputs=[dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for p in [background,buttons,data]],
        size=[layout['width'],layout['height']],buttons=controls,unityPlayExecuted=False,
        policy='Original gscene8 frame/button coordinates and screenshot reference origin (110,10), XP/kill summary labels. Multi-ally scrolling is Unity adaptation; report values are authoritative display data, not reward instructions.')
    Path(__file__).with_name('result-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');return report


if __name__=='__main__':print(json.dumps(build(),ensure_ascii=False))
