"""Offline Kaosia source/scene/resource audit; no Unity import or Play claim."""
import csv,hashlib,json,re
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]

def behaviours(scene):
    env=UnityPy.load(str(ROOT/'Assets/_Game/Scenes/Runtime'/(scene+'.unity')))
    rows=[]
    for obj in env.objects:
        if obj.type.name=='MonoBehaviour':
            data=obj.read_typetree(); rows.append((data.get('m_EditorClassIdentifier','').split('.')[-1],data,obj))
    return rows

def main():
    core=behaviours('GameCore');town=behaviours('Town_Runtime')
    panel,owner=next((d,o) for c,d,o in core if c=='TownServicesPanel')
    expected={'progression':'Combat/ExperienceProgression.asset','hireSkills':'Jobs/MercenaryHireCatalog.asset','mercenaryJobs':'Jobs/MercenaryJobCatalog.asset'}
    resolved={}
    for field,path in expected.items():
        ref=panel[field];assert ref['m_FileID']>0 and ref['m_PathID']!=0
        raw=bytes(owner.assets_file.externals[ref['m_FileID']-1].guid).hex()
        # UnityPy's external GUID bytes expose each packed hex pair reversed.
        guid=''.join(raw[i:i+2][::-1] for i in range(0,32,2))
        target=ROOT/'Assets/_Game/Runtime'/path
        if not target.exists():
            target=next(p.with_suffix('') for p in (ROOT/'Assets/_Game/Runtime').rglob('*.asset.meta') if ('guid: '+guid) in p.read_text())
        assert ('guid: '+guid) in target.with_name(target.name+'.meta').read_text()
        resolved[field]=str(target.relative_to(ROOT))
    transition=next(d for c,d,o in core if c=='BattleTransitionController')
    assert len(transition['formationCatalogs'])==4
    item_panel=next(d for c,d,o in core if c=='ItemCatalogPanel');assert item_panel['catalog']['m_PathID']!=0
    portal=next(d for c,d,o in town if c=='ScenePortal2D');assert portal['targetScene']=='WorldField1010'
    assert any(c=='TrainingEntrance2D' and d['playerJobs']['m_PathID']!=0 for c,d,o in town)
    native_npcs=sum(c=='TownNpcInteraction' for c,d,o in town)
    data=json.loads((ROOT/'Assets/_Game/Resources/Town/Quests.json').read_text());canonical=dict(data);canonical.pop('sourceHash')
    assert data['sourceHash']==hashlib.sha256(json.dumps(canonical,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    items=list(csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/items.csv').open(encoding='utf-8-sig')))
    growth=list(csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/experience_progression.csv').open(encoding='utf-8-sig')))
    spans=[max(int(r['level']) for r in growth if r['jobId']==job) for job in ['100','105','115']]
    assert spans==[5,10,15] and sum(spans)==30
    stock=[r for r in items if int(r['번호'])>0 and int(r['장착위']) in [1,2,3,4,5,8]
           and 0<=int(r['장착렙'])<=sum(spans) and r['가격'].isdigit() and int(r['가격'])>0
           and all(r[k] in ['0','1'] for k in ['보','비','기','수','창','궁','승','신','마','사'])
           and any(r[k]=='1' for k in ['보','비','기','수','창','궁','승','신','마','사'])]
    assert len(stock)==202
    for quest in data['rows']:
        start,end=[(ROOT/'분석/Saver_1.2/decoded/all_arc'/p).read_text(encoding='cp949') for p in quest['sources']]
        normalize=lambda value:re.sub(r'\s+','',value)
        for key in ['dialogue','prompt','yes','no']:assert normalize(quest[key]) in normalize(start)
        for key in ['ending','result']:assert normalize(quest[key]) in normalize(end)
        if quest['itemId']:
            match=[r for r in items if r['이름']=='팔찌'];assert len(match)==1 and int(match[0]['번호'])==quest['itemId']==929
    assert [(q['id'],q['gold'],q['itemId']) for q in data['rows']]==[(1,20000,0),(2,0,929)]
    for asset in ['TownShop/NativeTownShop','TownInventory/NativeTownInventory','TownCharacterInfo/NativeTownCharacterInfo','CharacterPortraits/CharacterPortraits']:
        assert (ROOT/'Assets/_Game/Resources/UI'/(asset+'.asset')).exists()
    report=dict(scope='Offline serialized scene links, source dialogue/reward identity and resource existence. Not Unity execution.',
        source_quest_hash=data['sourceHash'],linked_tables=resolved,formation_catalogs=4,source_quest_ids=[1,2],source_rewards={'1':'20000 GOLD','2':'item929 팔찌'},
        quest_access='Town services board; source NPC positions are not invented',npc_interaction_objects=native_npcs,
        npc_placement_evidence='닥세_리소스/분석결과/카오시아_NPC_배치검증.json',
        serialized_shop_offers=len(panel['shopOffers']),shop_catalog='Runtime ItemCatalogPanel fallback; empty scene stock reconstructed by KaosiaShopRules, preserving explicitly configured lists',
        stock_basis='User confirms class tiers1-3; exact individual original stock not recovered. Local reconstruction uses required base level0-30, positive source prices, wearable weapons/armor only.',
        runtime_reconstructed_stock_count=len(stock),runtime_stock_ids=[int(r['번호']) for r in stock],
        purchase_price_policy='Use source item table price as local purchase quote',buyback_price_policy='Unknown; sellPrice=-1 disables selling',
        unresolved=['Original Kaosia NPC coordinates/service mapping','Exact individual original seller stock and buyback prices','Original starting funds/items','Other scenario quest objectives and server-only adjudication'],
        preserved=['Native item/equipment UI','Class change and growth/hiring tables','Formation and field/training/scenario travel','Character-slot saves and tutorial world spawn21,21'])
    (Path(__file__).parent/'town_bindings_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PASS town links, source quests1/2 and rewards; unresolved source-only data recorded separately.')
if __name__=='__main__':main()
