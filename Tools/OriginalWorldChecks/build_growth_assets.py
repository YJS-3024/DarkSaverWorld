"""Preserve source growth metadata; do not infer server XP/stat/point formulas."""
import csv
import hashlib
import json
import re
import uuid
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original'
TARGET=BASE/'Data/Growth/CharacterGrowth.json'


def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def meta(p):
    path=Path(str(p)+'.meta')
    if not path.exists():
        guid=uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+p.relative_to(ROOT/'Assets').as_posix()).hex
        body='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'if p.is_dir()else'TextScriptImporter:\n  externalObjects: {}\n'
        path.write_text('fileFormatVersion: 2\nguid: '+guid+'\n'+body)


def build():
    source=BASE/'SourceData/Decoded/gameres/set.arc'
    level=source/'levelabl.atr.txt';limits=source/'levelptl.atr.txt';skills=source/'solskill.atr.txt'
    unit_path=BASE/'Data/Tables/units_all.csv';magic_path=BASE/'Data/Tables/Magictbl.csv';item_path=BASE/'Data/Tables/items.csv'
    jobs={int(s.split()[0])for s in limits.read_text().splitlines()if s.strip()and not s.lstrip().startswith(';')}
    units={int(r['직업']):r for r in csv.DictReader(unit_path.open(encoding='utf-8-sig'))}
    magic_ids={int(r['번호'])for r in csv.DictReader(magic_path.open(encoding='utf-8-sig'))}
    skill_ids=magic_ids|{int(s.split()[0])for s in skills.read_text().splitlines()if s.split()and s.split()[0].isdigit()}
    rows={};trailing=[]
    def ids(raw,line):
        if raw in ['0','']:return [],True
        if raw.endswith(','):trailing.append(line)
        tokens=raw.rstrip(',').split(',');valid=all(t.isdigit()and int(t)>0 for t in tokens)
        return [int(t)for t in tokens if t.isdigit()and int(t)>0],valid
    for line,raw in enumerate(level.read_text().splitlines(),1):
        t=raw.split()
        if not t or not t[0].isdigit()or int(t[0])not in jobs:continue
        assert len(t)==18,(line,t)
        learned,valid_magic=ids(t[16],line);usable,valid_skills=ids(t[17],line)
        r=dict(job=int(t[0]),classLevel=int(t[1]),nextJob=int(t[12]),tablePoints=int(t[11]),magicLevelUp=int(t[10]),
            requiredExperience=int(t[15]),magic=learned,skills=usable,rawMagic=t[16],rawSkills=t[17],
            abilitiesResolved=valid_magic and valid_skills and set(learned)<=magic_ids and set(usable)<=skill_ids,sourceLines=[line])
        key=(r['job'],r['classLevel'])
        if key in rows:
            assert {k:v for k,v in rows[key].items()if k!='sourceLines'}=={k:v for k,v in r.items()if k!='sourceLines'},key
            rows[key]['sourceLines'].append(line)
        else:rows[key]=r
    def class_stage(job):
        match=re.search(r'(?:제\s*)?(\d+)\s*단계',units[job]['설명'].replace('_',' '))
        if not match:raise ValueError('Missing source class stage: '+str(job))
        return int(match.group(1))
    data=dict(version=1,sourceHash=sha(level),jobs=[dict(id=j,picture=int(units[j]['직그림']),kind=int(units[j]['병종']),stage=class_stage(j),name=units[j]['이름'].replace('_',' '))for j in sorted(jobs)],
        rows=list(rows.values()),magicIds=sorted(magic_ids),skillIds=sorted(skill_ids))
    TARGET.parent.mkdir(exist_ok=True);meta(TARGET.parent);TARGET.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');meta(TARGET)
    asset=BASE/'Data/World/NativeWorldArt.asset'
    world_path=BASE/'Data/World/KaosiaWorld.json'
    if world_path.exists():
        world=json.loads(world_path.read_text());items={int(r['번호']):r for r in csv.DictReader(item_path.open(encoding='utf-8-sig'))};flags=['보','비','기','수','창','궁','승','신','마','사']
        for item in world['items']:item['jobKinds']=[i for i,flag in enumerate(flags)if int(items[item['id']][flag])==1]
        world_path.write_text(json.dumps(world,ensure_ascii=False,indent=2)+'\n')
    if asset.exists():
        guid=re.search(r'^guid: ([0-9a-f]{32})$',Path(str(TARGET)+'.meta').read_text(),re.M)[1]
        reference='  growth: {fileID: 4900000, guid: '+guid+', type: 3}'
        text=asset.read_text()
        if re.search(r'^  growth:.*$',text,re.M):text=re.sub(r'^  growth:.*$',reference,text,flags=re.M)
        else:text=text.rstrip('\n')+'\n'+reference+'\n'
        asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name)
        if not p.exists():continue
        report=json.loads(p.read_text());changed=False
        for target in [asset,world_path]:
            if not target.exists():continue
            key=target.relative_to(ROOT).as_posix()
            if key in report.get('sources',{}):report['sources'][key]=sha(target);changed=True
            for output in report.get('outputs',[]):
                if output['asset']==key:output['sha256']=sha(target);changed=True
        if changed:p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    result=dict(jobs=len(jobs),rows=len(rows),sourceRows=sum(len(r['sourceLines'])for r in rows.values()),
        unresolvedRows=sum(not r['abilitiesResolved']for r in rows.values()),trailingDelimiterRows=len(set(trailing)),
        sources={p.relative_to(ROOT).as_posix():sha(p)for p in [level,limits,skills,unit_path,magic_path,item_path]},
        output=TARGET.relative_to(ROOT).as_posix(),outputSha256=sha(TARGET),
        policy='Metadata and source references only. No automatic XP threshold, class change, stat formula, point/money reward or ability-retention policy.')
    Path(__file__).with_name('growth-assets.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    return result


if __name__=='__main__':
    result=build();print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False))
