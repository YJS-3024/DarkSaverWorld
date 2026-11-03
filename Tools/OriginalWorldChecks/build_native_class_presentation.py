"""Source class labels and embedded forbidden-drop bitmap only; no candidates/points/costs or authored map changes."""
import csv,hashlib,json,re,uuid,sys,io
from PIL import Image
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def denied_bitmap():
 sys.path.insert(0,str(ROOT/'Tools/CombatApChecks/python-deps'));import pefile
 inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());expected='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689';source=Path(next(f['extractedFile']for payload in inventory['payloads']for f in payload['files']if f.get('sha256')==expected));assert sha(source)==expected
 pe=pefile.PE(str(source));entry=next(e for t in pe.DIRECTORY_ENTRY_RESOURCE.entries if t.id==2 for e in t.directory.entries if e.id==154);leaf=entry.directory.entries[0];d=leaf.data.struct;raw=pe.get_data(d.OffsetToData,d.Size);image=Image.open(io.BytesIO(raw)).convert('RGBA');assert image.size==(32,32)
 pixels=[(r,g,b,0 if r==g==b==0 else 255)for r,g,b,a in image.get_flattened_data()];image.putdata(pixels)
 return image,dict(nativeSha256=expected,type=2,id=154,language=leaf.id,dibSha256=hashlib.sha256(raw).hexdigest(),rgbaSha256=hashlib.sha256(image.tobytes()).hexdigest())
def main():
 source=ROOT/'Assets/_Original/Data/Tables/units_all.csv';entries=[dict(job=int(row['직업']),name=row['이름'].replace('_',' '),description=row['설명'].replace('_',' '))for row in csv.DictReader(source.open(encoding='utf-8-sig'))]
 p=ROOT/'Assets/_Original/Resources/OriginalClassLabels.json';p.write_text(json.dumps(dict(entries=entries),ensure_ascii=False,separators=(',',':'))+'\n');meta=Path(str(p)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+p.relative_to(ROOT/'Assets').as_posix()).hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 image,bitmap=denied_bitmap();denied=ROOT/'Assets/_Original/Resources/OriginalClassDropDenied.png';image.save(denied)
 from build_world_assets import metadata
 metadata.meta(denied,ROOT/'Assets');outputs=[dict(asset=q.relative_to(ROOT).as_posix(),sha256=sha(q))for q in [denied,Path(str(denied)+'.meta')]]
 report=dict(entries=len(entries),resource=p.relative_to(ROOT).as_posix(),sha256=sha(p),outputs=outputs,embeddedBitmap=bitmap,resourceGuid=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1],sources={q.relative_to(ROOT).as_posix():sha(q)for q in [Path(__file__),source]},scope='Original ability name/description fields with existing underscore-to-space presentation. Does not define class candidates, costs, points or automatic transitions.')
 (OUT/'native-class-presentation-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
