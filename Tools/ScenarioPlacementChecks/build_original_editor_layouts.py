"""Build editor layouts from literal SET/POS joins; never synthesize an absent roster.
Does not touch runtime Content.json or scene files. Existing edited layouts require --reset.
"""
import argparse
import csv
import hashlib
import io
import json
import re
import sys
import uuid
from pathlib import Path
from PIL import Image
import yaml

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/_Game/Editor/BattleLayouts'
SOURCE=ROOT/'분석/Saver_Late'


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def meta(path,folder=False):
    p=Path(str(path)+'.meta')
    if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,path.relative_to(ROOT).as_posix()).hex+'\n'+('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n' if folder else ''),encoding='utf-8')


def source_units(scenario,stats):
    result=[]
    for u in scenario['units']:
        s=stats[u['job']]
        result.append(dict(id=u['unit'],job=u['job'],name=u['name'],x=u['x'],y=u['y'],
                          hp=int(s['체력수']),mp=int(s['마력수']),attack=int(s['공력수']),defense=int(s['방력수']),
                          magicAttack=int(s['마공수']),magicDefense=int(s['마방수']),experience=int(s['획득경'])))
    return result


def protect_existing(path,expected):
    if path.exists():
        existing=json.loads(path.read_text(encoding='utf-8'))
        if existing.get('placementEdited') or existing.get('units')!=expected:
            raise FileExistsError(str(path)+' has edits; --reset required')


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--reset',action='store_true');args=parser.parse_args()
    native=json.loads((SOURCE/'scenario_monster_placements.json').read_text(encoding='utf-8'))
    maps={Path(m['path']).stem:m for m in json.loads((SOURCE/'maps.json').read_text(encoding='utf-8'))}
    stats={int(r['직업']):r for r in csv.DictReader((SOURCE/'unity_catalogs/monsters.csv').open(encoding='utf-8-sig'))}
    scenes=list((ROOT/'Assets/_Game/Scenes/Maps').glob('*.unity'))
    # Check every destination before writing any file; preserve both UI and external edits.
    if not args.reset:
        for scenario in native['scenarios']:
            protect_existing(OUT/f"Original{scenario['scenario']}.json",source_units(scenario,stats))
    for folder in [OUT,OUT/'Art']:folder.mkdir(exist_ok=True,parents=True);meta(folder,True)
    jobs={u['job']:u['picture'] for s in native['scenarios'] for u in s['units']}
    inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text(encoding='utf-8'));late=next(v for v in inventory['payloads'] if 'saver.exe' in v['containers'])
    sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
    import build_late_saver_catalog as catalog
    import analyze_client as decoder
    sheets={};picture_hashes={}
    for file in late['files']:
        if not re.fullmatch(r'gameres/char\d+\.arc',file['file']):continue
        for name,method,size,packed in catalog.arc_entries(Path(file['extractedFile'])):
            match=re.fullmatch(r'(\d+)r\.bmp',name,re.I)
            if not match or int(match[1]) not in jobs.values():continue
            raw=(decoder.lzh_decode if method==0 else decoder.lzss_decode)(packed,size)
            assert len(raw)==size
            picture=int(match[1]);assert picture not in sheets
            sheet=Image.open(io.BytesIO(raw)).convert('RGB');cell=sheet.width//3
            # Two original sheets have98 columns: three32px frames plus2 unused columns.
            assert sheet.width-3*cell in (0,1,2) and sheet.height>=cell*4
            frame=sheet.crop((0,cell,cell,cell*2)).convert('RGBA')
            pixels=frame.get_flattened_data() if hasattr(frame,'get_flattened_data') else frame.getdata()
            frame.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else 255) for r,g,b,a in pixels])
            sheets[picture]=frame;picture_hashes[picture]=hashlib.sha256(raw).hexdigest()
    assert set(jobs.values())==set(sheets)
    template=yaml.safe_load((ROOT/'Assets/_Game/Resources/Scenarios/Shared/Characters/301_Down_0.png.meta').read_text(encoding='utf-8'))
    art=[]
    for job,picture in sorted(jobs.items()):
        frame=sheets[picture];target=OUT/'Art'/f'{job}.png';frame.save(target)
        m=json.loads(json.dumps(template));guid=uuid.uuid5(uuid.NAMESPACE_URL,target.relative_to(ROOT).as_posix()).hex;m['guid']=guid
        imp=m['TextureImporter'];imp['internalIDToNameTable']=[];imp['spriteSheet'].update(sprites=[],nameFileIdTable={},spriteID=guid)
        imp['spriteAlignment']=0;imp['spritePivot']={'x':.5,'y':.5};imp['spritePixelsToUnits']=32
        imp['userData']='Original SET ability picture '+str(picture)+'; down frame0; editor only.'
        target.with_suffix('.png.meta').write_text(yaml.safe_dump(m,sort_keys=False,allow_unicode=True),encoding='utf-8')
        art.append(dict(job=job,picture=picture,sheetSha256=picture_hashes[picture],path=target.relative_to(ROOT).as_posix(),sha256=sha(target)))
    index=[];written=0
    for scenario in native['scenarios']:
        key=scenario['scenario'];m=maps[key];candidates=[p for p in scenes if p.name.startswith('Map_'+key+'_')];assert len(candidates)==1,key
        scene=candidates[0];title=scene.stem.split('_',2)[2].strip(' ;') or key
        positions=[];pos=ROOT/Path(scenario['set']).with_suffix('.pos')
        for line in pos.read_text(encoding='utf-8').splitlines():
            tokens=line.split(';',1)[0].split()
            if len(tokens)==3 and all(t.isdigit() for t in tokens):
                unit,x,y=map(int,tokens)
                if unit<400:positions.append(dict(id=unit,x=x,y=y))
        entry=next(p for p in positions if p['id']==0)
        units=source_units(scenario,stats)
        target=OUT/f'Original{key}.json';row=dict(version=2,scenarioKey='Original'+key,width=m['width'],height=m['height'],entryX=entry['x'],entryY=entry['y'],
            profileHash=hashlib.sha256(json.dumps(units,ensure_ascii=False,sort_keys=True).encode()).hexdigest(),sourceHash=sha(ROOT/scenario['set']),
            placementEdited=False,provenance='Literal original SET/POS join; not proof of automatic native runtime loading',
            sourceScenario=key,sourceSet=scenario['set'],sourcePos=pos.relative_to(ROOT).as_posix(),
            sourceRows=[dict(id=u['unit'],setLine=u['setLine'],posLine=u['posLine'],extra=u['extra']) for u in scenario['units']],
            statsBasis='Ability base values for editor inspection; not server final combat values',
            partyPositions=positions,units=units,intro=[],outro=[],events=[],characters=[],traversalCells=[])
        target.write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');meta(target);written+=len(units)
        index.append(dict(key=key,title=title,mapScene=scene.stem,width=m['width'],height=m['height'],path=target.relative_to(ROOT).as_posix()))
    target=OUT/'OriginalIndex.json';target.write_text(json.dumps(dict(maps=index),ensure_ascii=False,indent=2)+'\n',encoding='utf-8');meta(target)
    report=dict(maps=len(index),units=written,artworkJobs=len(art),artworkPictures=len(sheets),artwork=art,
                sourceSha256=sha(SOURCE/'scenario_monster_placements.json'),sourcePairsPreserved=True,
                runtimeContentChanged=False,scenesChanged=False,unityGuiExecuted=False)
    (ROOT/'Tools/ScenarioPlacementChecks/original-editor-layouts.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Built',len(index),'original layouts /',written,'units /',len(art),'job sprites; no runtime Content or scenes changed')


if __name__=='__main__':main()
