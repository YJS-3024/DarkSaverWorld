"""Audit configured stock against the actual Unity item catalog; not Unity playback."""
import csv,json
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
stock=json.loads((ROOT/'Assets/_Game/Resources/World/KaosiaShopStock.json').read_text());env=UnityPy.load(str(ROOT/'Assets/_Game/Runtime/Items/ItemCatalog.asset'));data=next(o.read_typetree() for o in env.objects if o.type.name=='MonoBehaviour');items={int(e['numericValues'][0]):e for e in data['entries']}
expected={'KaosiaWeaponShop':22,'KaosiaArmorShop':20,'KaosiaAccessoryShop':0,'KaosiaMagicTools':23,'Interior2800':6};seen=set();total=0
for shop in stock['shops']:
 key=shop['key'];assert key in expected and key not in seen;seen.add(key);assert len(shop['offers'])==expected[key];ids=set()
 for offer in shop['offers']:
  id=offer['itemId'];assert id not in ids;ids.add(id);item=items[id];assert offer['buyPrice']==item['numericValues'][16] and offer['buyPrice']>0 and offer['sellPrice']==-1;total+=1
 if key in ['KaosiaMagicTools','Interior2800']:
  source='닥세_리소스/카오시아_소모품점.png' if key=='KaosiaMagicTools' else '닥세_리소스/카오시아마법사의집.png'
  guide=list(csv.DictReader((ROOT/'분석/Saver_Late/screenshots/added_kaosia_shop_reference.csv').open(encoding='utf-8-sig')))
  assert ids=={int(row['nativeIds']) for row in guide if row['reference']==source}
  assert all(items[id]['numericValues'][3]==(7 if key=='KaosiaMagicTools' else 10) for id in ids)
 print(key,len(ids),'valid items/prices')
assert total==71 and seen==set(expected)
print('PASS 71 offers against actual Unity item asset; accessory deferred by user; resale price unknown. Static only.')
