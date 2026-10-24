"""Convert all15 original pattern99 emotes, preserving frames/delay metadata."""
import csv,json,hashlib
import UnityPy
from collections import defaultdict
from PIL import Image
from world_asset_baker import ROOT,directory,png,text_asset

def main():
    source=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/etc3/fx3.bmp';image=Image.open(source).convert('RGBA')
    out=ROOT/'Assets/_Game/Resources/Effects/Emotes';directory(out)
    groups=defaultdict(list)
    for row in csv.DictReader((ROOT/'닥세_리소스/분석결과/특수기_패턴99_프레임시간.csv').open(encoding='utf-8-sig')):groups[int(row['pattern_id'])].append(row)
    # Repair the historical derived CSV/metadata shift in the final pattern115.
    patterns={int(t[0]):t for row in csv.DictReader((ROOT/'닥세_리소스/분석결과/표_MagicPtn.csv').open(encoding='utf-8-sig')) for t in [json.loads(row['tokens'])]}
    converted=ROOT/'분석/Saver_1.2/converted/MagicPtn.csv';lines=converted.read_text(encoding='utf-8-sig').splitlines()
    for index,line in enumerate(lines):
        if line.startswith('"115",'):lines[index]='"'+'","'.join(patterns[115])+'"'
    converted.write_text('\n'.join(lines)+'\n',encoding='utf-8-sig')
    metadata=ROOT/'Assets/_Game/Resources/Effects/NativeMagicPresentationCatalog.asset';env=UnityPy.load(str(metadata))
    obj=next(o for o in env.objects if o.type.name=='MonoBehaviour');data=obj.read_typetree();changed=False
    for row in data['entries']:
        if row['id']==29015:
            expected=dict(pattern=115,family=99,frames1=int(patterns[115][8]),frames2=int(patterns[115][15]),sound1=int(patterns[115][17]),sound2=int(patterns[115][18]))
            changed=any(row[k]!=v for k,v in expected.items());row.update(expected)
    if changed:obj.save_typetree(data);metadata.write_bytes(env.file.save())
    entries=[];proof=[]
    for pattern,rows in sorted(groups.items()):
        paths=[];delays=[]
        for row in sorted(rows,key=lambda x:int(x['frame'])):
            rect=tuple(int(row[k]) for k in ['left','top','right','bottom']);frame=image.crop(rect)
            frame.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in frame.getdata()])
            key=f'{pattern}_{row["frame"]}';png(out/(key+'.png'),frame);paths.append('Effects/Emotes/'+key);delays.append(int(row['delay_ms']))
            proof.append(dict(texture=key,rect=rect,rgbaSha256=hashlib.sha256(frame.tobytes()).hexdigest()))
        entries.append(dict(id=29001+pattern-101,frames=paths,delays=delays,limit=int(rows[0]['stop_counter']),offsetX=int(rows[0]['draw_x_offset']),offsetY=int(rows[0]['draw_y_offset'])))
    assert len(entries)==15;text_asset(out/'Manifest.json',dict(entries=entries))
    (ROOT/'Tools/MapChecks/emote_resources.json').write_text(json.dumps(dict(entries=15,frames=len(proof),source=str(source.relative_to(ROOT)),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),originalFrameTiming=True,placement='singleplayer above-character anchor; original full-screen playback not validated',pixels=proof,unityPlay=False),ensure_ascii=False,indent=2)+'\n');print('Built15 native emotes:',len(proof),'frames')
if __name__=='__main__':main()
