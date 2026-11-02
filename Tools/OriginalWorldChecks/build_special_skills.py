"""Append the native86 special-menu definitions to the shared, source-linked effect catalog."""
import csv,hashlib,json
from pathlib import Path
from PIL import Image
from build_world_assets import ROOT,BASE

def build():
    source=ROOT/'분석/Saver_Late/decoded/gameres/set.arc/magicptn.atr'
    raw={int(a[0]):a for line in source.read_text().splitlines()if(a:=line.split())and a[0].isdigit()}
    rows=list(csv.DictReader((BASE/'Data/Tables/Magictbl.csv').open(encoding='utf-8-sig')))
    skills=[r for r in rows if 10000<=int(r['번호'])<20000 or 30000<=int(r['번호'])<32000];assert len(skills)==86
    catalog=BASE/'Data/Combat/MagicProjectiles.json';data=json.loads(catalog.read_text());data['skills']=[int(r['번호'])for r in skills]
    data['shapes']=[s for s in data['shapes']if s['id']<10000]+[dict(id=int(r['번호']),pattern=int(r['패턴']),team=int(r['팀']),unit=int(r['유닛']),distance=int(r['거리']),range=int(r['범위']),name=r['이름'])for r in skills]
    patterns={p['id']:p for p in data['patterns']};images=[Image.open(BASE/'Art/Tutorial/Effects.png').convert('RGBA'),Image.open(BASE/'Art/Tutorial/Attackeffects.png').convert('RGBA')];crops=[]
    def strip(tokens,surface=0):
        s=dict(zip(['x','y','width','height','stepX','stepY','count'],map(int,tokens)));s.update(surface=surface,rawCount=s['count']);return s
    for identifier in sorted({int(r['패턴'])for r in skills}):
        r=raw[identifier];assert len(r)==19;t=int(r[16]);assert t in[1,2,6,7,8,9,11,12,14,15,16,19,20,21,24]
        p=dict(id=identifier,type=t,sound1=int(r[17]),sound2=int(r[18]),impact=strip(r[2:9],1 if t in[12,21]else 0),travel=strip(r[9:16],1 if t in[11,14,24]else 0))
        if t in[11,24]:p['travel']['count']=8
        if t in[2,7,8,9,21]:p['charge']=dict(x=0,y=128,width=40,height=40,stepX=40,stepY=0,count=5,rawCount=5,surface=0)
        if identifier in patterns:assert p==patterns[identifier],identifier
        patterns[identifier]=p
        parts=[]if t==16 else ['impact']
        if t in[1,2,11,14,24]:parts+=['travel']
        if 'charge'in p:parts+=['charge']
        for part in parts:
            s=p[part]
            for i in range(s['count']):
                x=s['x']+i*s['stepX'];y=s['y']+i*s['stepY'];w=s['width'];h=s['height'];image=images[s['surface']]
                assert 0<=x<x+w<=image.width and 0<=y<y+h<=image.height,(identifier,part,i)
                crops.append(dict(pattern=identifier,part=part,surface=s['surface'],frame=i,rect=[x,y,w,h],pixelSha256=hashlib.sha256(image.crop((x,y,x+w,y+h)).tobytes()).hexdigest()))
    # Hardcoded caster pieces, separate from the unused pattern16 target strip.
    rects=[(0,281,377,10,22),(0,291,377,2,22),(0,293,377,18,22),(0,281,377,22,10),(0,281,387,22,2),(0,281,389,22,10),
           (0,351,407,10,22),(0,361,407,2,22),(0,363,407,18,22),(0,351,407,22,10),(0,351,417,22,2),(0,351,419,22,10),
           (0,371,311,27,39),(0,251,315,28,44),(0,280,314,27,60),
           (1,618,648,69,72),(1,687,648,69,72),(1,756,648,69,72),(1,825,648,69,72),(1,618,576,69,72),(1,687,576,69,72)]
    for surface,x,y,w,h in rects:crops.append(dict(part='caster-piece',surface=surface,rect=[x,y,w,h],pixelSha256=hashlib.sha256(images[surface].crop((x,y,x+w,y+h)).tobytes()).hexdigest()))
    # Uniform beam rows/columns permit exact repetition through one stretched draw.
    for x,y,w,h in[(291,377,2,22),(361,407,2,22)]:assert images[0].crop((x,y,x+1,y+h)).tobytes()==images[0].crop((x+1,y,x+2,y+h)).tobytes()
    for x,y,w,h in[(281,387,22,2),(351,417,22,2)]:assert images[0].crop((x,y,x+w,y+1)).tobytes()==images[0].crop((x,y+1,x+w,y+2)).tobytes()
    data['patterns']=sorted(patterns.values(),key=lambda p:p['id']);catalog.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
    general=Path(__file__).with_name('magic-projectile-assets.json');audit=json.loads(general.read_text());audit['outputs'][0]['sha256']=hashlib.sha256(catalog.read_bytes()).hexdigest();general.write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n')
    report=dict(source=str(source.relative_to(ROOT)),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),skills=data['skills'],frameCrops=crops,
        families=[dict(type=t,skills=[s['id']for s in data['shapes']if s['id']in data['skills']and patterns[s['pattern']]['type']==t])for t in sorted({patterns[int(r['패턴'])]['type']for r in skills})],
        excluded=[dict(id=int(r['번호']),name=r['이름'],route='mercenary'if int(r['번호'])<29000 else'emotion')for r in rows if int(r['번호'])>=10000 and int(r['번호'])not in data['skills']],unityPlayExecuted=False)
    Path(__file__).with_name('special-skill-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print('special skills',len(skills),'frame audit',len(crops),'noncombat records',len(report['excluded']))
if __name__=='__main__':build()
