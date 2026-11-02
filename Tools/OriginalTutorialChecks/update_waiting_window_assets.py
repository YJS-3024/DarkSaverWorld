"""Update waiting-window LSC geometry/item names without rebuilding maps or lessons."""
import csv,hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_lsc
def build():
 target=BASE/'Data/Tutorial/Waiting1500.json';definition=json.loads(target.read_text());before=json.loads(target.read_text());sources=[]
 for window in definition['windows']:
  for mini in [False,True]:
   path=BASE/f'SourceData/Native/wlib/scene{window["scene"]}{"m"if mini else ""}.lsc.bytes';sources.append(path);data=read_lsc(path);l,t,r,b=data['region']
   if mini:window.update(minWidth=data['width'],minHeight=data['height'],minDragX=l,minDragY=t,minDragWidth=r-l,minDragHeight=b-t)
   else:
    assert (window['width'],window['height'])==(data['width'],data['height']);window.update(dragX=l,dragY=t,dragWidth=r-l,dragHeight=b-t)
 table=BASE/'Data/Tables/items.csv';sources.append(table);items={int(row['번호']):row for row in csv.DictReader(table.open(encoding='utf-8-sig'))};definition['items']=[dict(id=identifier,name=items[identifier]['이름'].replace('_',' '))for identifier in [1,762,777]]
 assert definition['map']==before['map']and definition['lines']==before['lines']
 assert all(after[key]==original[key]for after,original in zip(definition['windows'],before['windows'])for key in ['x','y','buttons'])
 target.write_text(json.dumps(definition,ensure_ascii=False,indent=2)+'\n');sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();report=dict(sources={p.relative_to(ROOT).as_posix():sha(p)for p in sources},outputs=[dict(asset=target.relative_to(ROOT).as_posix(),sha256=sha(target))],windows=len(definition['windows']),itemNames=definition['items'],scope='Literal full/minimized LSC drag bounds/dimensions and CSV names1/762/777. Existing tutorial map,36lines,button arrays and presentation origins preserved. No persistent items or use counts created.',unityPlay=False)
 manifest=Path(__file__).with_name('tutorial-assets.json');record=json.loads(manifest.read_text())
 for row in record['outputs']:
  if row['asset']==target.relative_to(ROOT).as_posix():row['sha256']=sha(target)
 manifest.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
 Path(__file__).with_name('waiting-window-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(windows=report['windows'],itemNames=report['itemNames']),ensure_ascii=False))
if __name__=='__main__':build()
