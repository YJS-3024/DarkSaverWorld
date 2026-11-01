"""Select the preserved Saver_1.2 field without regenerating unrelated imported artwork."""
import hashlib
import json
from pathlib import Path

import build_campaign_assets as builder

ROOT=builder.ROOT
BASE=builder.BASE


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path,value):
    path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n')


def main():
    # Do not run the complete artwork builder: retain the user's existing texture import settings.
    protected=[BASE/'Data/Campaign/UserGateways.json',BASE/'SourceData/Native/map/1010.mrc.bytes']
    protected+=list(BASE.rglob('*.meta'))
    before={p:digest(p)for p in protected}
    copied=builder.prepare_legacy_maps()
    path=builder.map_path('1010');m=builder.read_mrc(path)
    catalog_path=BASE/'Data/Campaign/CampaignCatalog.json'
    catalog=json.loads(catalog_path.read_text());row=next(v for v in catalog['maps']if v['id']==1010)
    unchanged={v['id']:json.dumps(v,sort_keys=True)for v in catalog['maps']if v['id']!=1010}
    row['hash']=digest(path);row['world']['sourceVersion']='Saver_1.2'
    nav=row['world']['map'];nav['width']=m['width'];nav['height']=m['height']
    nav['attributes']=[0 if(t[3]&127)>27 else t[3]&127 for t in m['tiles']]
    write_json(catalog_path,catalog)
    gateway_path=BASE/'Data/Campaign/GatewayMaps.json'
    gateways=json.loads(gateway_path.read_text());g=next(v for v in gateways['maps']if v['id']==1010)
    g.update(width=m['width'],height=m['height'],hash=digest(path),legacy=True,nativePath=path.relative_to(ROOT).as_posix())
    write_json(gateway_path,gateways)
    asset=BASE/'Data/Campaign/NativeCampaignArt.asset';text=asset.read_text()
    def ref(p):return '{fileID: 4900000, guid: '+builder.world.guid(p)+', type: 3}'
    start=text.index('  maps:\n')
    # A Unity YAML sequence uses the same indentation as its field, so find the next field explicitly.
    end=text.index('  graphics:\n',start)
    lines=text[start:end].splitlines();lines[row['nativeIndex']+1]='  - '+ref(path)
    text=text[:start]+'\n'.join(lines)+'\n'+text[end:]
    graphics=sorted({p for _,p in copied if p.name.startswith('mdsr')},key=lambda p:int(p.name[4:].split('.')[0]))
    # Keep this tool repeatable and preserve all existing fields, textures and metadata.
    lines=text.splitlines();filtered=[];skip=False
    for line in lines:
        if line.startswith('  legacyGraphics:'):skip=True;continue
        if skip and line.startswith('  - '):continue
        skip=False
        if not line.startswith('  legacyGraphicIds:'):filtered.append(line)
    text='\n'.join(filtered)+'\n  legacyGraphics:\n'+''.join('  - '+ref(p)+'\n'for p in graphics)
    text+='  legacyGraphicIds: '+''.join(int(p.name[4:].split('.')[0]).to_bytes(4,'little').hex()for p in graphics)+'\n'
    asset.write_text(text)
    # Update the existing evidence manifests for only the resources changed here.
    campaign_report=Path(__file__).with_name('campaign-assets.json');report=json.loads(campaign_report.read_text())
    for original,target in copied:
        for p in [original,target]:report['sources'][p.relative_to(ROOT).as_posix()]=digest(p)
    for output in report['outputs']:
        if output['asset']in [catalog_path.relative_to(ROOT).as_posix(),asset.relative_to(ROOT).as_posix()]:
            output['sha256']=digest(ROOT/output['asset'])
    write_json(campaign_report,report)
    gateway_report=Path(__file__).with_name('gateway-assets.json');report=json.loads(gateway_report.read_text())
    for p in [catalog_path,path]:report['sources'][p.relative_to(ROOT).as_posix()]=digest(p)
    for output in report['outputs']:
        if output['asset']==gateway_path.relative_to(ROOT).as_posix():output['sha256']=digest(gateway_path)
    write_json(gateway_report,report)
    assert all(digest(p)==h for p,h in before.items()),'Existing metadata/source/user gateways changed'
    assert all(json.dumps(v,sort_keys=True)==unchanged[v['id']]for v in catalog['maps']if v['id']!=1010)
    late=builder.read_mrc(BASE/'SourceData/Native/map/1010.mrc.bytes')
    result=dict(map=1010,sourceVersion='Saver_1.2',mapPath=path.relative_to(ROOT).as_posix(),
        mapSha256=digest(path),size=[m['width'],m['height']],nativeSprites=len(m['sprites']),lateSprites=len(late['sprites']),
        legacyGraphics=[dict(id=int(p.name[4:].split('.')[0]),path=p.relative_to(ROOT).as_posix(),sha256=digest(p))for p in graphics],
        otherCampaignMapsUnchanged=len(unchanged),preservedExistingMetadata=len(before)-2,userGatewaysUnchanged=True,
        originalLateMapUnchanged=True,unityPlay=False)
    write_json(Path(__file__).with_name('legacy-field-version.json'),result)
    print(json.dumps({k:v for k,v in result.items()if k!='legacyGraphics'}))


if __name__=='__main__':main()
