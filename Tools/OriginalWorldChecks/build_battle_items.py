"""Native equip-position7 -> item magic links. Durability is metadata, never use-count policy."""
import csv,hashlib,json,re
from pathlib import Path
from build_world_assets import ROOT,BASE,metadata

def build():
    source=BASE/'Data/Tables/items.csv';rows=[dict(item=int(r['번호']),magic=int(r['마법번']),rawDurability=int(r['내구']))for r in csv.DictReader(source.open(encoding='utf-8-sig'))if int(r['장착위'])==7 and int(r['마법번'])>0];assert len(rows)==33
    target=BASE/'Data/Combat/BattleItems.json';target.write_text(json.dumps(dict(entries=rows),ensure_ascii=False,indent=2)+'\n');guid=metadata.meta(target,ROOT/'Assets')
    asset=BASE/'Data/World/NativeWorldArt.asset';text='\n'.join(line for line in asset.read_text().splitlines()if not line.startswith('  battleItems:'))+'\n  battleItems: {fileID: 4900000, guid: '+guid+', type: 3}\n';asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name);r=json.loads(p.read_text());key=str(asset.relative_to(ROOT));digest=hashlib.sha256(asset.read_bytes()).hexdigest()
        if key in r.get('sources',{}):r['sources'][key]=digest
        for row in r.get('outputs',[]):
            if row['asset']==key:row['sha256']=digest
        p.write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n')
    report=dict(source=str(source.relative_to(ROOT)),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),output=str(target.relative_to(ROOT)),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),links=rows,policy='33 native position7 links only. Cost, consumption, remaining uses and final health require explicit authority; raw durability is not initial remaining uses.',unityPlayExecuted=False)
    Path(__file__).with_name('battle-item-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print('native consumable links',len(rows))
if __name__=='__main__':build()
