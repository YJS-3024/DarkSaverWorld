"""Preserve native magic menu names/coordinates; do not generate learned/selected spells."""
import csv,hashlib,json,re
from pathlib import Path
from build_world_assets import ROOT,BASE,metadata


def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()


def build():
    source=BASE/'Data/Tables/Magictbl.csv';rows=[]
    with source.open(encoding='utf-8-sig',newline='')as f:
        for r in csv.DictReader(f):
            rows.append(dict(id=int(r['번호']),name=r['이름'].replace('_',' '),iconX=int(r['iconx']),iconY=int(r['icony']),mp=int(r['MP']),distance=int(r['거리']),range=int(r['범위']),team=int(r['팀']),pattern=int(r['패턴']),unit=int(r['유닛'])))
    assert len(rows)==440 and len({r['id']for r in rows})==len(rows)
    target=BASE/'Data/Combat/MagicMenu.json';target.write_text(json.dumps(dict(entries=rows),ensure_ascii=False,indent=2)+'\n');metadata.meta(target,ROOT/'Assets')
    asset=BASE/'Data/World/NativeWorldArt.asset';text=asset.read_text();guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(target)+'.meta').read_text(),re.M)[1]
    text='\n'.join(s for s in text.splitlines()if not s.startswith('  magicMenu:'))+'\n';text+='  magicMenu: {fileID: 4900000, guid: '+guid+', type: 3}\n';asset.write_text(text)
    for name in ['world-assets.json','gateway-assets.json','campaign-assets.json']:
        p=Path(__file__).with_name(name);report=json.loads(p.read_text());path=asset.relative_to(ROOT).as_posix()
        if path in report.get('sources',{}):report['sources'][path]=sha(asset)
        for row in report.get('outputs',[]):
            if row['asset']==path:row['sha256']=sha(asset)
        p.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    report=dict(sources={source.relative_to(ROOT).as_posix():sha(source)},outputs=[dict(asset=target.relative_to(ROOT).as_posix(),sha256=sha(target))],rows=len(rows),policy='Original names/icon pixel coordinates and display metadata only. No spell ownership, battle selection, damage or AP policy.',unityPlayExecuted=False)
    Path(__file__).with_name('magic-menu-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');return report


if __name__=='__main__':print(json.dumps(build(),ensure_ascii=False))
