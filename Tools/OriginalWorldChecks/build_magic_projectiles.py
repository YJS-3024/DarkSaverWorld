"""Convert every general magic record (<10000), literal source frames and native no-effect fallbacks."""
import csv,hashlib,json,re
from pathlib import Path
from PIL import Image
from build_world_assets import ROOT,BASE,metadata

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def build():
    source=ROOT/'분석/Saver_Late/decoded/gameres/set.arc/magicptn.atr';magic=BASE/'Data/Tables/Magictbl.csv';atlas=BASE/'Art/Tutorial/Effects.png';fx2=BASE/'Art/Tutorial/Attackeffects.png'
    raw={int(v[0]):v for line in source.read_text().splitlines()if(v:=line.split())and v[0].isdigit()}
    with magic.open(encoding='utf-8-sig',newline='')as f:rows=[r for r in csv.DictReader(f)if int(r['번호'])<10000]
    assert len(rows)==289
    shapes=[dict(id=int(r['번호']),name=r['이름'],pattern=int(r['패턴']),team=int(r['팀']),unit=int(r['유닛']),distance=int(r['거리']),range=int(r['범위']))for r in rows]
    images=[Image.open(atlas).convert('RGBA'),Image.open(fx2).convert('RGBA')];assert images[0].size==(999,1098)and images[1].size==(999,999)
    def strip(tokens,surface=0):
        v=dict(zip(['x','y','width','height','stepX','stepY','count'],map(int,tokens)));v['surface']=surface;v['rawCount']=v['count'];return v
    patterns=[];crops=[]
    for identifier in sorted({s['pattern']for s in shapes}):
        r=raw.get(identifier)
        if r is None:p=dict(id=identifier,type=0,sound1=-1,sound2=-1,missingSource=True)
        else:
            assert len(r)==19;p=dict(id=identifier,type=int(r[16]),sound1=int(r[17]),sound2=int(r[18]),impact=strip(r[2:9],1 if int(r[16])in[4,21,22]else 0),travel=strip(r[9:16],1 if int(r[16])==11 else 0))
        t=p['type'];assert t in [0,1,2,3,4,5,6,7,8,9,10,11,13,18,21,22]
        if t==11:p['travel']['count']=8
        if t in [2,3,4,7,8,9,13,21,22]:p['charge']=dict(x=0,y=128,width=40,height=40,stepX=40,stepY=0,count=5,rawCount=5,surface=0)
        patterns.append(p)
        parts=[]if t==0 else ['impact']+(['travel']if t in[1,2,3,4,10,11,13]else [])+(['charge']if 'charge'in p else [])
        for part in parts:
            s=p[part];image=images[s['surface']]
            for i in range(s['count']):
                x=s['x']+i*s['stepX'];y=s['y']+i*s['stepY'];w=s['width'];h=s['height'];assert 0<=x<x+w<=image.width and 0<=y<y+h<=image.height,(identifier,part,i)
                crops.append(dict(pattern=identifier,part=part,surface=s['surface'],frame=i,rect=[x,y,w,h],pixelSha256=hashlib.sha256(image.crop((x,y,x+w,y+h)).tobytes()).hexdigest()))
    circle_rects=[(0,560,72,23),(72,560,26,51),(98,560,83,34),(181,560,86,34),(267,560,26,51),(293,560,73,22),(360,560,144,5),(366,565,75,81),(441,565,71,83),(366,648,159,6),(513,565,84,72),(366,654,82,73)]
    circles=[dict(surface=1,rect=list(r),pixelSha256=hashlib.sha256(images[1].crop((r[0],r[1],r[0]+r[2],r[1]+r[3])).tobytes()).hexdigest())for r in circle_rects]
    spells=[s['id']for s in shapes];area=[s['id']for s in shapes if s['unit']!=8 and s['range']>0];groups=[s['id']for s in shapes if s['unit']==8]
    target=BASE/'Data/Combat/MagicProjectiles.json';target.write_text(json.dumps(dict(patterns=patterns,magic=spells,areaMagic=area,groupMagic=groups,shapes=shapes),ensure_ascii=False,indent=2)+'\n');metadata.meta(target,ROOT/'Assets')
    asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text();guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(target)+'.meta').read_text(),re.M)[1]
    text='\n'.join(s for s in text.splitlines()if not s.startswith('  magicProjectiles:'))+'\n';text+='  magicProjectiles: {fileID: 4900000, guid: '+guid+', type: 3}\n';asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name);report=json.loads(p.read_text());path=asset.relative_to(ROOT).as_posix()
        if path in report.get('sources',{}):report['sources'][path]=sha(asset)
        for row in report.get('outputs',[]):
            if row['asset']==path:row['sha256']=sha(asset)
        p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    report=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in[source,magic,atlas,fx2]},outputs=[dict(asset=target.relative_to(ROOT).as_posix(),sha256=sha(target))],patterns=[p['id']for p in patterns],supportedMagic=spells,areaMagic=area,groupMagic=groups,frameCrops=crops,circleCrops=circles,unityPlayExecuted=False,
        noEffectMagic=[s['id']for s in shapes if next(p for p in patterns if p['id']==s['pattern'])['type']==0],missingPatterns=[p['id']for p in patterns if p.get('missingSource')],
        families=[dict(pattern=p['id'],type=p['type'],magic=[s['id']for s in shapes if s['pattern']==p['id']])for p in patterns],
        policy='All289 general records including monster/item/event spells;151 special menu records remain separate. Literal late patterns and source-default no-effect branches; no inferred damage, costs, duration or initial ownership. Source snapshots determine permissions, stats and revival. Existing40ms mapping/global-RNG differences remain explicit.')
    Path(__file__).with_name('magic-projectile-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    from build_special_skills import build as build_skills
    build_skills()
    return report
if __name__=='__main__':
    r=build();print(json.dumps({k:r[k]for k in['missingPatterns','noEffectMagic','policy']}));print('spells',len(r['supportedMagic']),'frame records',len(r['frameCrops']))
