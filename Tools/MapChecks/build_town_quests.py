"""Bind source duty dialogue/rewards to scenario01, without inventing NPC positions."""
import csv, hashlib, json, re
from pathlib import Path
from build_scenario01 import ROOT, directory, guid

def main():
    source = ROOT / '분석/Saver_1.2/catalogs'
    quests = list(csv.DictReader((source / 'quests.csv').open(encoding='utf-8-sig')))
    items = list(csv.DictReader((source / 'items.csv').open(encoding='utf-8-sig')))
    rows = []
    for quest_id, name in [(1,'케인'),(2,'타란')]:
        start = next(r for r in quests if r['questId'] == str(quest_id) and r['stage'] == '1')
        end = next(r for r in quests if r['questId'] == str(quest_id) and r['stage'] == '3')
        assert start['npcId'] == end['npcId']
        result = end['result']; gold = 0; item = 0
        if m := re.fullmatch(r'(\d+) GOLD를 얻었습니다\.', result): gold = int(m[1])
        else:
            matches = [r for r in items if result == r['이름'] + '를 얻었습니다.' or result == r['이름'] + '을 얻었습니다.']
            assert len(matches) == 1, result
            item = int(matches[0]['번호'])
        rows.append(dict(id=quest_id,npcId=int(start['npcId']),npcName=name,dialogue=start['dialogue'],prompt=start['prompt'],yes=start['yes'],no=start['no'],
                         ending=end['dialogue'],result=result,gold=gold,itemId=item,sources=[start['source'],end['source']]))
    data = dict(version=1,rows=rows)
    data['sourceHash'] = hashlib.sha256(json.dumps(data,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    out = ROOT / 'Assets/_Game/Resources/Town';directory(out)
    path = out / 'Quests.json';path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
    path.with_name(path.name+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(path)+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    print('Baked source quests1/2; rewards gold20000 and exact-name item929; NPC placement excluded.')
if __name__ == '__main__':main()
