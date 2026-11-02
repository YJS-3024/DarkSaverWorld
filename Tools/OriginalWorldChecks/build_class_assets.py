"""Reassemble native scene23 class/ability frame. No generated candidates or class rules."""
import hashlib,json,re,tempfile
from pathlib import Path
from PIL import Image
from build_world_assets import BASE,ROOT,metadata,key
from decode_visuals import read_lsc,read_ui_library


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def build():
    source=BASE/'SourceData/Native/wlib';target=BASE/'Art/World';target.mkdir(exist_ok=True)
    inputs=[source/(n+'.bytes')for n in ['scene23.lsc','scene23m.lsc','waitlib.lce','waitlib.ltn']]
    layouts={n:read_lsc(source/(n+'.lsc.bytes'))for n in ['scene23','scene23m']}
    outputs=[]
    with tempfile.TemporaryDirectory()as temp:
        libs={}
        for ext in ['lce','ltn']:
            p=Path(temp)/('waitlib.'+ext);p.write_bytes((source/('waitlib.'+ext+'.bytes')).read_bytes());libs[ext]=read_ui_library(p)[:2]
        for name,filename in [('scene23','ClassAdjustment.png'),('scene23m','ClassAdjustmentMinimized.png')]:
            d=layouts[name];frame=Image.new('RGBA',(d['width'],d['height']))
            for v in d['decor']:
                a=libs['lce'][0][v['id']];piece=libs['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
                for y in range(v['repeat_y']):
                    for x in range(v['repeat_x']):frame.alpha_composite(piece,(v['x']+x*a['width'],v['y']+y*a['height']))
            p=target/filename;key(frame).save(p);outputs.append(p)
        d=layouts['scene23'];controls=[]
        for v in d['buttons']:
            assert v['kind']=='button';a=libs['ltn'][0][v['id']];controls.append(dict(id=v['id'],x=v['x'],y=v['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
    assert (d['width'],d['height'])==(540,435)and len(controls)==21
    data=BASE/'Data/World/ClassAdjustment.json';data.write_text(json.dumps(dict(scene=23,width=d['width'],height=d['height'],buttons=controls),ensure_ascii=False,indent=2)+'\n');outputs.append(data)
    for p in outputs:metadata.meta(p,ROOT/'Assets')
    asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text()
    for field,p,kind in [('classWindow',outputs[0],2800000),('classMinimized',outputs[1],2800000),('classLayout',data,4900000)]:
        guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
        text='\n'.join(s for s in text.splitlines()if not s.startswith('  '+field+':'))+'\n';text+='  '+field+': {fileID: '+str(kind)+', guid: '+guid+', type: 3}\n'
    asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name);r=json.loads(p.read_text());path=asset.relative_to(ROOT).as_posix()
        if path in r.get('sources',{}):r['sources'][path]=sha(asset)
        for out in r.get('outputs',[]):
            if out['asset']==path:out['sha256']=sha(asset)
        p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
    report=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in inputs},outputs=[dict(asset=p.relative_to(ROOT).as_posix(),sha256=sha(p))for p in outputs],
        size=[540,435],buttons=controls,unityPlayExecuted=False,policy='Literal original scene23/m frame and controls; same waitlib button atlas. Candidates/points/approved growth are local authority data, not inferred server rules.')
    Path(__file__).with_name('class-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');return report


if __name__=='__main__':print(json.dumps(build(),ensure_ascii=False))
