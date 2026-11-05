"""Extract original job/XP facts without inventing training spawns or server combat rules."""
import csv,json,re
from pathlib import Path
from build_growth_assets import BASE,ROOT,meta,sha


def script_meta(path):
    meta(path)
    p=Path(str(path)+'.meta');text=p.read_text()
    if 'TextScriptImporter:' in text:
        p.write_text(text.replace('TextScriptImporter:\n  externalObjects: {}\n',
            'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'))


def build():
    source=BASE/'Data/Tables/units_all.csv'
    rows=list(csv.DictReader(source.open(encoding='utf-8-sig')))
    jobs=[dict(job=int(r['직업']),picture=int(r['직그림']),name=r['이름'].replace('_',' '),category=r['category'],
        baseExperience=int(r['획득경']),rawAttackRange=int(r['공거리']),rawMoveTick=int(r['이틱감']),
        rawAttackTick=int(r['공틱감']),rawRestTick=int(r['휴틱감']),rawCommandRange=int(r['지범']),rawMovementType=int(r['병종']),rawMagicTick=int(r['마틱감']),rawSpecialMovementType=int(r['특병종']))for r in rows]
    # Preserve source base fields without interpreting upper limits as current combat maxima.
    base_fields=dict(hp='체력수',mp='마력수',attack='공력수',defense='방력수',magicAttack='마공수',magicDefense='마방수',
        speed='속도',movement='이동력',accuracy='명중률',evasion='회피율',critical='크릭율',magicAccuracy='마명율',magicEvasion='마회율',hpLimit='체력한',mpLimit='마력한',attackLimit='공력한',defenseLimit='방력한',magicAttackLimit='마공한',magicDefenseLimit='마방한',commandAttack='지공',commandDefense='지방')
    for job,row in zip(jobs,rows):job['baseStats']={key:int(row[column])for key,column in base_fields.items()}
    item_source=BASE/'Data/Tables/items.csv'
    items=[dict(item=int(r['번호']),name=r['이름'].replace('_',' '),equipType=int(r['장착위']),rawCommandRange=int(r['지휘범위']))
        for r in csv.DictReader(item_source.open(encoding='utf-8-sig')) if int(r['지휘범위'])!=0]
    assert len({j['job']for j in jobs})==len(jobs)
    target=BASE/'Data/Combat/OriginalCombat.json';target.parent.mkdir(exist_ok=True);meta(target.parent)
    target.write_text(json.dumps(dict(version=4,jobs=jobs,commandRangeItems=items),ensure_ascii=False,indent=2)+'\n');meta(target)
    for folder in [BASE/'Runtime/Combat']:
        meta(folder)
        for p in folder.glob('*.cs'):script_meta(p)
    for p in [BASE/'Runtime/Entry/LocalServerManager.Training.cs',BASE/'Runtime/World/WorldSceneRoot.Combat.cs']:script_meta(p)
    asset=BASE/'Data/World/NativeWorldArt.asset'
    if asset.exists():
        guid=re.search(r'^guid: ([0-9a-f]{32})$',Path(str(target)+'.meta').read_text(),re.M)[1]
        reference='  combat: {fileID: 4900000, guid: '+guid+', type: 3}'
        text=asset.read_text()
        if re.search(r'^  combat:.*$',text,re.M):text=re.sub(r'^  combat:.*$',reference,text,flags=re.M)
        else:text=text.rstrip('\n')+'\n'+reference+'\n'
        asset.write_text(text)
    # Preserve the existing evidence manifests when attaching a new catalogue reference.
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name)
        if not p.exists():continue
        report=json.loads(p.read_text());key=asset.relative_to(ROOT).as_posix()
        if key in report.get('sources',{}):report['sources'][key]=sha(asset)
        for output in report.get('outputs',[]):
            if output['asset']==key:output['sha256']=sha(asset)
        p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    result=dict(jobs=len(jobs),monsterJobs=sum(j['category']=='monster'for j in jobs),
        monstersWithExperience=sum(j['category']=='monster'and j['baseExperience']>0 for j in jobs),
        commandRangeItems=len(items),sources={p.relative_to(ROOT).as_posix():sha(p) for p in [source,item_source]},output=target.relative_to(ROOT).as_posix(),outputSha256=sha(target),
        policy='Source facts only. No seeded roster, damage/AP/XP allocation/AI/respawn formula. Local notice contracts are not original wire opcodes.')
    Path(__file__).with_name('combat-assets.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    return result


if __name__=='__main__':print(json.dumps(build(),ensure_ascii=False))
