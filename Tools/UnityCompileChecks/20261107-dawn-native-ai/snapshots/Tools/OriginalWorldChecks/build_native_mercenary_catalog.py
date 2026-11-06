"""Source learned-skill hire catalog, no economy defaults or initial skill grants."""
import csv,hashlib,json,re,uuid
from pathlib import Path
from build_native_shop_details import integer,rows
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def data():
 source=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/solskill.atr.txt';skills=[]
 for line in source.read_text(encoding='utf-8-sig').splitlines():
  fields=line.split()
  if fields and fields[0].isdigit():skills.append(dict(id=int(fields[0]),maximum=int(fields[4]),jobs=list(map(int,fields[3].split(',')))))
 equipment=[dict(id=integer(row['번호']),skill=integer(row['마법번']),boost=integer(row['마렙치']))for row in rows()if integer(row['마법번'])>19999 and integer(row['마렙치'])!=0]
 jobs={job for skill in skills for job in skill['jobs']};units=[]
 for row in csv.DictReader((ROOT/'Assets/_Original/Data/Tables/units_all.csv').open(encoding='utf-8-sig')):
  if int(row['직업'])in jobs:units.append(dict(job=int(row['직업']),picture=int(row['직그림']),hp=integer(row['체력수']),attack=integer(row['공력수']),defense=integer(row['방력수']),price=integer(row['가격']),name=row['이름'].replace('_',' ')))
 assert {unit['job']for unit in units}==jobs
 return dict(skills=skills,equipment=equipment,units=units)
def main():
 catalog=data();path=ROOT/'Assets/_Original/Resources/OriginalMercenarySkills.json';path.write_text(json.dumps(catalog,ensure_ascii=False,separators=(',',':'))+'\n');meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 inputs=[Path(__file__),ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/solskill.atr.txt',ROOT/'Assets/_Original/Data/Tables/items.csv',ROOT/'Assets/_Original/Data/Tables/units_all.csv'];report=dict(skills=len(catalog['skills']),equipmentModifiers=len(catalog['equipment']),units=len(catalog['units']),resource=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),resourceGuid=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1],scope='Native hire skills/maximums, worn-item modifiers and unit names/pictures/table prices. No learned skills, gold/fees or hire approval invented.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in inputs});(OUT/'native-mercenary-catalog-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
