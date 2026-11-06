"""Generate only source item property text; do not rebuild world/stock/save assets."""
import csv,hashlib,json,re,uuid
from decimal import Decimal
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
#44B1C0 reads34..6C, skips70 (magic-level bonus), then74..80.
FIELDS=[('이동','이동력'),('체력','체력'),('공격','공격력'),('방어','방어력'),('공범위','공격범위'),('마법력','마법력'),('마법공','마법공격력'),('마법방','마법방어력'),('마법범위','마법범위'),('지휘범위','지휘범위'),('명중률','명중률'),('회피율','회피율'),('크컬율','필살률'),('마명률','마법명중률'),('마회율','마법회피율'),('이틱감','이동속도향상'),('공틱감','공격속도향상'),('휴틱감','휴식속도향상'),('마틱감','마법속도향상')]
def integer(value):
 n=Decimal(value);assert n==n.to_integral_value();return int(n)
def rows():return list(csv.DictReader((ROOT/'Assets/_Original/Data/Tables/items.csv').open(encoding='utf-8-sig',newline='')))
def main():
 source=ROOT/'Assets/_Original/Data/Tables/items.csv';items=[]
 for row in rows():
  text=''.join(label+': '+str(integer(row[column]))+(''if column=='마틱감'else' ')for column,label in FIELDS if integer(row[column])!=0);items.append(dict(id=int(row['번호']),properties=text))
 assert len({r['id']for r in items})==len(items)
 path=ROOT/'Assets/_Original/Resources/OriginalShopDetails.json';path.write_text(json.dumps(dict(items=items),ensure_ascii=False,separators=(',',':'))+'\n');meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 report=dict(items=len(items),fields=len(FIELDS),resource=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),resourceGuid=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1],scope='Display text only from existing original item table; source stock/prices/owned items/world maps/save files not rewritten.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [Path(__file__),source]})
 (OUT/'native-shop-details-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
