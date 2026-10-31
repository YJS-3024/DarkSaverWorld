"""Build the requested Kaosia presentation; preserve source/version/policy boundaries.

No old _Game scripts, economy defaults or invented monster deployments are copied.
"""
import csv
import hashlib
import importlib.util
import json
import re
import shutil
import sys
import tempfile
from decimal import Decimal
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Assets/_Original'; OUT = BASE / 'Art/World'; DATA = BASE / 'Data/World'
sys.path.insert(0, str(ROOT / '닥세_리소스/분석도구'))
from decode_visuals import read_mrc, read_grs, grs_image, read_lsc, read_ui_library
spec = importlib.util.spec_from_file_location('original_meta', ROOT / 'Tools/OriginalRestart/reset_assets.py')
metadata = importlib.util.module_from_spec(spec); spec.loader.exec_module(metadata)


def sha(raw): return hashlib.sha256(raw).hexdigest()
def guid(p): return re.search(r'^guid: ([a-f0-9]{32})$', Path(str(p) + '.meta').read_text(), re.M)[1]
def number(text, default=-1):
    if not text: return default
    value = Decimal(text); assert value == value.to_integral_value(); return int(value)
def key(im):
    im = im.convert('RGBA'); im.putdata([(*p[:3], 0 if p[:3] == (0, 0, 0) else 255) for p in im.getdata()]); return im


def main():
    OUT.mkdir(parents=True, exist_ok=True); DATA.mkdir(parents=True, exist_ok=True)
    report = dict(sources={}, outputs=[], sourceScope='User-confirmed43x43 Kaosia exterior; late native interiors; explicit local routing and photo/user staff references.',
                  policy='Unknown starting funds, bank/storage fees, resale, revival and hiring approval remain disabled.', actualUnityPlay=False)
    def source(path):
        report['sources'][path.relative_to(ROOT).as_posix()] = sha(path.read_bytes()); return path
    def native(name, old=False):
        if old:
            p = source(ROOT / '분석/Saver_1.2/extracted/Saver_Files/Saver/MAP' / name)
            copied = BASE / 'SourceData/KaosiaLegacy' / (name + '.bytes'); copied.parent.mkdir(exist_ok=True)
            if copied.exists(): assert copied.read_bytes() == p.read_bytes()
            else: shutil.copyfile(p, copied)
            metadata.meta(copied, ROOT / 'Assets'); output(copied, p.relative_to(ROOT).as_posix()+' original bytes'); return copied
        return source(BASE / 'SourceData/Native' / (name + '.bytes'))
    def output(path, origin):
        metadata.meta(path, ROOT / 'Assets'); report['outputs'].append(dict(asset=path.relative_to(ROOT).as_posix(), sha256=sha(path.read_bytes()), source=origin)); return path
    def save(name, im, origin):
        path = OUT / (name + '.png'); im.save(path); return output(path, origin)
    battle_map = json.loads(source(BASE / 'Data/Tutorial/Tutorial301.json').read_text())['map']
    maps = []; grounds = []; decorations = []; decoration_ids = {}; caches = {}
    # Grid coordinates are top-down native indices, not the earlier Unity Y-up centers.
    routes = [
        (2000,'무기점',163,40,35,40,38), (2100,'방어구점',141,34,29,34,32),
        (2200,'장신구점',166,28,36,28,39), (2300,'소모품점',75,28,9,28,12),
        (2400,'은행',68,37,9,37,11), (2510,'창고',99,29,18,29,21),
        (2600,'신전',67,10,8,10,11), (2800,'마법사의 집',156,8,34,8,36),
        (2900,'용병의 집',104,38,18,38,20), (101,'수련장',103,8,19,8,21)]
    for map_id in [1000] + [r[0] for r in routes]:
        old = map_id == 1000
        p = native('1000.mrc' if old else f'map/{map_id}.mrc', old); m = read_mrc(p)
        grs = {}
        for rid in m['ids']:
            cachekey = (old, rid)
            if cachekey not in caches: caches[cachekey] = read_grs(native(f'mdsr{rid}.grs' if old else f'map/mdsr{rid}.grs', old))
            grs[rid] = caches[cachekey]
        im = Image.new('RGBA', (m['width'] * 32, m['height'] * 32))
        for i, (rid, group, detail) in enumerate(zip(m['a'], m['b'], m['tiles'])):
            x, y = i % m['width'] * 32, i // m['width'] * 32; g = grs[rid]['groups'][group]
            im.alpha_composite(grs_image(g['palette'], g['images'][0]), (x, y))
            if detail[1] != 255:
                g = grs[detail[0]]['groups'][detail[1]]; frame = g['tiles'][detail[2]]['frames'][0]
                im.alpha_composite(grs_image(g['palette'], g['images'][frame], True), (x, y))
        placements = []
        for index, s in enumerate(m['sprites']):
            k = (old, s['resource'], s['index'], s['frame'])
            if k not in decoration_ids:
                g = grs[s['resource']]['sprites'][s['index']]; picture = grs_image(g['palette'], g['images'][s['frame']], True)
                decoration_ids[k] = len(decorations); decorations.append(save('Decoration' + str(len(decorations)), picture, f'{p.relative_to(ROOT)} sprite{index}; literal source GRS frame'))
            picture = Image.open(decorations[decoration_ids[k]])
            placements.append(dict(picture=decoration_ids[k], x=s['x']*32, y=s['y']*32, width=picture.width, height=picture.height))
        title = '카오시아' if old else next(r[1] for r in routes if r[0] == map_id)
        row = dict(id=map_id, title=title, sourceVersion='Saver_1.2' if old else 'Saver_Late', training=map_id == 101,
                   ground=len(grounds), map=dict(width=m['width'], height=m['height'], attributes=[0 if (t[3]&127)>27 else t[3]&127 for t in m['tiles']],
                   terrain=battle_map['terrain'], abilities=battle_map['abilities']), decorations=placements, npcs=[], portals=[])
        # Staff positions are source-canvas origins; customer counter cells are separate.
        if map_id in [2000,2100,2200,2300,2800,2900,2400,2510]:
            kind = 1 if map_id in [2000,2100,2200,2300,2800] else 2 if map_id == 2400 else 3 if map_id == 2510 else 4
            appearances = [(807,4,4),(810,15,4)] if map_id in [2000,2100,2200,2300,2800] else [(810,15,4)] if map_id == 2400 else [(807,4,4)]
            for appearance,x,y in appearances:
                row['npcs'].append(dict(id=f'{map_id}-{appearance}', name=title+' 주인' if kind==1 else title+' 관리인', picture=appearance,x=x,y=y,kind=kind,
                    counter=[dict(x=c,y=6) for c in range(x-1,x+2)],evidence='photo' if map_id in [2100,2400,2510] else 'user-confirmed shop match' if map_id in [2000,2200,2300,2800] else 'local placement using original appearance'))
        if map_id == 2600:
            row['npcs']=[dict(id='2600-818',name='신전 관리인',picture=818,x=9,y=5,kind=5,counter=[dict(x=x,y=7) for x in [8,9,10]],evidence='photo; original canvas origin288,160')]
        if old:
            for target,title,building,x,y,rx,ry in routes:
                assert m['sprites'][building]['resource'] == 20
                row['portals'].append(dict(id='enter-'+str(target),targetMap=target,spawnX=10,spawnY=7 if target==101 else (18 if target in [2600,2800] else 13),
                    returnX=rx,returnY=ry,cells=[dict(x=x,y=y),dict(x=x+1,y=y)]))
            report['routes']=[dict(target=r[0],title=r[1],nativeObjectIndex=r[2],door=[r[3],r[4]],returnCell=[r[5],r[6]],evidence='native sign/object + explicit local connection; not original server coordinates') for r in routes]
        elif map_id != 101:
            route = next(r for r in routes if r[0] == map_id)
            row['portals']=[dict(id='exit-'+str(map_id),targetMap=1000,spawnX=route[5],spawnY=route[6],returnX=route[5],returnY=route[6],cells=[dict(x=9,y=m['height']-1),dict(x=10,y=m['height']-1)])]
        grounds.append(save('Ground'+str(map_id), im, str(p.relative_to(ROOT))+' base/detail first frames; decorations separate in source order')); maps.append(row)
    rows = list(csv.DictReader(source(BASE/'Data/Tables/items.csv').open(encoding='utf-8-sig')))
    units={int(r['직업']):r for r in csv.DictReader(source(BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig'))}
    job_flags=['보','비','기','수','창','궁','승','신','마','사']
    items=[dict(id=int(r['번호']),picture=number(r['그림1']),equipType=number(r['장착위']),requiredLevel=number(r['장착렙']),
                jobs=[job for job,u in units.items() if job in (100,101,103) and 0<=number(u['병종'])<len(job_flags) and number(r[job_flags[number(u['병종'])]])==1],
                name=r['이름'].replace('_',' '),description=r['설명'].replace('_',' '),price=number(r['가격']),sellKnown=False,sellPrice=0) for r in rows]
    guide = list(csv.DictReader(source(ROOT/'분석/Saver_Late/screenshots/added_kaosia_shop_reference.csv').open(encoding='utf-8-sig')))
    shops=[]
    for map_id, stem, expected in [(2000,'카오시아_무기점',22),(2100,'카오시아방어구점',20),(2300,'카오시아_소모품점',23),(2800,'카오시아마법사의집',6)]:
        ids=[]
        for row in guide:
            if Path(row['reference']).stem!=stem:continue
            candidates=list(map(int,row['nativeIds'].split(';')))
            if len(candidates)>1:
                candidates=[i for i in candidates if next(p['price'] for p in items if p['id']==i)==number(row['guidePrice'])]
            assert len(candidates)==1,row;ids.append(candidates[0])
        assert len(ids)==len(set(ids))==expected,(stem,len(ids))
        shops.append(dict(map=map_id,items=ids,evidence=stem+' guide membership; active native table prices'))
    shops.append(dict(map=2200,items=[],evidence='Unconfirmed stock; user deferred accessories'))
    units={int(r['직업']):r for r in csv.DictReader(source(BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig'))}
    hires=[]
    for row in csv.DictReader(source(ROOT/'Tools/OriginalInputChecks/mercenary_class_stock.csv').open(encoding='utf-8-sig')):
        if not row['mercenaryIds']:continue
        for job,limit in zip(map(int,row['mercenaryIds'].split(',')),map(int,row['maximumPerJob'].split(','))):
            u=units[job];hires.append(dict(commander=int(row['classId']),job=job,picture=int(u['직그림']),limit=limit,price=number(u['가격']),name=u['이름'].replace('_',' ')))
    ids=sorted({100,101,103,807,810,818,898}|{r['picture'] for r in hires});pictures=[]
    for picture in ids:
        path=next((BASE/'Art/Archives/gameres').glob(f'char*.arc/{picture}r.bmp.png'));pictures.append(save('Picture'+str(picture),key(Image.open(source(path))),path.relative_to(ROOT).as_posix()+' original directional sheet'))
    windows=[];windowpaths=[]
    with tempfile.TemporaryDirectory(prefix='ds-world-wlib-') as temp:
        libraries={}
        for ext in ['lce','ltn','lnm']:
            p=Path(temp)/('waitlib.'+ext);p.write_bytes(native('wlib/waitlib.'+ext).read_bytes());libraries[ext]=read_ui_library(p)[:2]
        buttons=save('Buttons',key(libraries['ltn'][1]),'Native Wlib three-state controls')
        for scene in [9,10,17,18,20,27,29]:
            d=read_lsc(native(f'wlib/scene{scene}.lsc'));im=Image.new('RGBA',(d['width'],d['height']));controls=[]
            for v in d['decor']:
                a=libraries['lce'][0][v['id']];p=libraries['lce'][1].crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
                for y in range(v['repeat_y']):
                    for x in range(v['repeat_x']):im.alpha_composite(p,(v['x']+x*a['width'],v['y']+y*a['height']))
            for v in d['buttons']:
                a=libraries['ltn' if v['kind']=='button' else 'lnm'][0][v['id']]
                if v['kind']=='button':controls.append(dict(id=v['id'],x=v['x'],y=v['y'],sx=a['x'],sy=a['y'],width=a['width'],height=a['height']))
            windows.append(dict(scene=scene,width=d['width'],height=d['height'],buttons=controls));windowpaths.append(save('Window'+str(scene),key(im),f'Wlib/scene{scene} literal source; buttons separate'))
    item_atlas=save('Items',key(Image.open(source(BASE/'Art/Archives/gameres/item.arc/item.bmp.png'))),'Native item.bmp;100 columns of32px')
    # World UI copies each original32px cell into its own cached icon texture.
    item_meta=Path(str(item_atlas)+'.meta');item_meta.write_text(item_meta.read_text().replace('isReadable: 0','isReadable: 1'))
    definition=dict(maps=maps,shops=shops,items=items,hires=hires,windows=windows,rules=dict(initialGoldKnown=False,initialBankKnown=False,bankFeeKnown=False,storageRuleKnown=False,reviveRuleKnown=False,hireRuleKnown=False,dismissalRuleKnown=False))
    data=DATA/'KaosiaWorld.json';data.write_bytes((json.dumps(definition,ensure_ascii=False,indent=2)+'\n').encode());output(data,'Source maps/items/hire lists + explicitly classified routing and staff')
    for p in BASE.rglob('*'):
        if (p.suffix=='.cs' or p.is_dir()) and not Path(str(p)+'.meta').exists():metadata.meta(p,ROOT/'Assets',folder=p.is_dir())
    def ref(p,kind=2800000):return '{fileID: '+str(kind)+', guid: '+guid(p)+', type: 3}'
    text='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid(BASE/'Runtime/World/NativeWorldArt.cs')+', type: 3}\n  m_Name: NativeWorldArt\n'
    text+='  definition: '+ref(data,4900000)+'\n  buttons: '+ref(buttons)+'\n  items: '+ref(item_atlas)+'\n  battle: {fileID: 11400000, guid: '+guid(BASE/'Data/Tutorial/NativeTutorialArt.asset')+', type: 2}\n'
    for field,paths in [('grounds',grounds),('decorations',decorations),('pictures',pictures),('windows',windowpaths)]:text+='  '+field+':\n'+''.join('  - '+ref(p)+'\n' for p in paths)
    text+='  pictureIds: '+''.join(i.to_bytes(4,'little',signed=True).hex() for i in ids)+'\n'
    asset=DATA/'NativeWorldArt.asset';asset.write_text(text);metadata.meta(asset,ROOT/'Assets');Path(str(asset)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(asset)+'\nNativeFormatImporter:\n  mainObjectFileID: 11400000\n')
    scene=BASE/'Scenes/Start.unity';text=scene.read_text();world_guid=guid(BASE/'Runtime/World/WorldSceneRoot.cs')
    if 'guid: '+world_guid not in text:
        text=text.replace('  - component: {fileID: 2004}\n','  - component: {fileID: 2004}\n  - component: {fileID: 2005}\n',1)
        component='--- !u!114 &2005\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_GameObject: {fileID: 2000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+world_guid+', type: 3}\n  m_Name:\n  m_EditorClassIdentifier:\n  art: {fileID: 11400000, guid: '+guid(asset)+', type: 2}\n'
        text=text.replace('--- !u!1660057539',component+'--- !u!1660057539',1);scene.write_text(text)
    report.update(maps=len(maps),staff=sum(len(m['npcs']) for m in maps),decorations=len(decorations),shopCounts={s['map']:len(s['items']) for s in shops},hireOffers=len(hires))
    (ROOT/'Tools/OriginalWorldChecks/world-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ['maps','staff','decorations','shopCounts','hireOffers']},ensure_ascii=False))


if __name__=='__main__':main()
