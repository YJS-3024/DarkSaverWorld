"""Register only guide-confirmed Kaosia consumables and mage-house stock."""
import csv,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
guide=ROOT/'분석/Saver_Late/screenshots/added_kaosia_shop_reference.csv';catalog=ROOT/'분석/Saver_Late/unity_catalogs/items.csv';target=ROOT/'Assets/_Game/Resources/World/KaosiaShopStock.json'
rows=list(csv.DictReader(guide.open(encoding='utf-8-sig')));items={int(r['번호']):r for r in csv.DictReader(catalog.open(encoding='utf-8-sig'))};stock=json.loads(target.read_text());added=[]
for source,key,position,count in [('닥세_리소스/카오시아_소모품점.png','KaosiaMagicTools',7,23),('닥세_리소스/카오시아마법사의집.png','Interior2800',10,6)]:
 offers=[]
 for row in rows:
  if row['reference']!=source:continue
  id=int(row['nativeIds']);item=items[id];assert int(item['장착위'])==position and int(item['가격'])>0
  offers.append(dict(itemId=id,buyPrice=int(item['가격']),sellPrice=-1))
 assert len(offers)==count and len({o['itemId'] for o in offers})==count
 entry=next((s for s in stock['shops'] if s['key']==key),None)
 if entry is None:entry=dict(key=key);stock['shops'].append(entry)
 entry.update(evidence='Kaosia guide membership: '+source+'; active late item table purchase prices; original resale price unconfirmed.',offers=offers)
 added.append(dict(key=key,count=count,source=source,position=position,ids=[o['itemId'] for o in offers]))
target.write_text(json.dumps(stock,ensure_ascii=False,indent=2)+'\n')
report=dict(guideSha256=hashlib.sha256(guide.read_bytes()).hexdigest(),catalogSha256=hashlib.sha256(catalog.read_bytes()).hexdigest(),updates=added,shops={s['key']:len(s['offers']) for s in stock['shops']},resalePricesConfirmed=False,accessoryStockConfirmed=False,actualUnityPlay=False)
Path(__file__).with_name('kaosia_shop_stock_update.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print('PASS: 23 consumables + 6 mage-house items; existing weapon/armor/accessory stock preserved.')
