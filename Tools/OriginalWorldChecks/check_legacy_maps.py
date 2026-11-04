"""Verify exact old designs, per-version binding, navigation and preserved scenario data."""
import csv,hashlib,io,json,re,subprocess
from pathlib import Path
import build_campaign_assets as builder
ROOT=builder.ROOT;BASE=builder.BASE;OUT=Path(__file__).parent

def main():
    checks=0
    def check(value,label):
        nonlocal checks
        checks+=1
        assert value,label
    def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
    def load(p):return json.loads(p.read_text())
    report=load(OUT/'legacy-map-selection.json');catalog=load(BASE/'Data/Campaign/CampaignCatalog.json');gate=load(BASE/'Data/Campaign/GatewayMaps.json')
    for name,digest in report['sources'].items():check(sha(ROOT/name)==digest,'selection source '+name)
    for name,digest in report['outputs'].items():check(sha(ROOT/name)==digest,'selection output '+name)
    old=ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/MAP';text=(BASE/'Data/Campaign/NativeCampaignArt.asset').read_text()
    def refs(field):return re.findall(r'guid: ([0-9a-f]+)',re.search(r'^  '+field+r':\n((?:  - .*\n)*)',text,re.M)[1])
    def guid(p):return re.search(r'^guid: ([0-9a-f]+)$',Path(str(p)+'.meta').read_text(),re.M)[1]
    ids=bytes.fromhex(re.search(r'^  legacyGraphicIds: (.*)$',text,re.M)[1]);ids=[int.from_bytes(ids[i:i+4],'little')for i in range(0,len(ids),4)]
    graphics=refs('legacyGraphics');maps=refs('maps');check(len(ids)==len(graphics)==len(set(ids)),'unique old GRS bindings')
    table={r['map_key']:r for r in csv.DictReader(io.StringIO((BASE/'Data/Tables/scene_maps.csv').read_text(encoding='utf-8-sig')))}
    for key in report['selected']:
        row=next(m for m in catalog['maps']if m['key']==key);info=next(m for m in gate['maps']if m['key']==key);source=old/(key+'.mrc');p=ROOT/info['nativePath'];m=builder.read_mrc(source)
        check(p.read_bytes()==source.read_bytes() and sha(p)==row['hash']==info['hash'],'original map '+key)
        check(row['world']['sourceVersion']=='Saver_1.2' and info['legacy'],'version '+key)
        check([m['width'],m['height']]==[info['width'],info['height']]==[row['world']['map']['width'],row['world']['map']['height']],'dimensions '+key)
        check(row['world']['map']['attributes']==[0 if(t[3]&127)>27 else t[3]&127 for t in m['tiles']],'collision '+key)
        check(row['world']['map']['renderAttributes']==[t[3]for t in m['tiles']],'raw render attributes '+key)
        check(maps[row['nativeIndex']]==guid(p),'runtime reference '+key)
        check(table[key]['map_asset_path']==info['nativePath'] and int(table[key]['width_tiles'])==m['width'] and int(table[key]['height_tiles'])==m['height'],'editor CSV '+key)
        for rid in m['ids']:
            copied=p.parent/f'mdsr{rid}.grs.bytes'
            check(copied.read_bytes()==(old/f'mdsr{rid}.grs').read_bytes() and graphics[ids.index(rid)]==guid(copied),'original graphics '+key+'/'+str(rid))
    previous=json.loads(subprocess.check_output(['git','show','HEAD:Assets/_Original/Data/Campaign/CampaignCatalog.json'],cwd=ROOT));prev={m['key']:m for m in previous['maps']}
    for row in catalog['maps']:
        check(row['units']==prev[row['key']]['units']and row['entries']==prev[row['key']]['entries'],'placement not migrated '+row['key'])
        if row['key']not in report['selected']:check(row==prev[row['key']],'other campaign map unchanged '+row['key'])
    p=BASE/'Data/Campaign/UserGateways.json';check(p.read_bytes()==subprocess.check_output(['git','show','HEAD:'+p.relative_to(ROOT).as_posix()],cwd=ROOT),'user portals unchanged')
    editor=load(ROOT/'Tools/OriginalScenarioEditorChecks/editor-check.json')
    check(set(report['selected'])<=set(editor['nativePixelComparisons']),'independent old map pixel comparisons')
    result=dict(assertions=checks,selected=report['selected'],mapsAudited=len(report['maps']),nativePixelComparisons=editor['nativePixelComparisons'],unityPlay=False,
        sources={str(p.relative_to(ROOT)):sha(p)for p in [Path(__file__),OUT/'legacy-map-selection.json',ROOT/'Tools/OriginalScenarioEditorChecks/editor-check.json']},
        scope='Preserved1.2 bytes/navigation/version bindings and production C# versus independent pixels. No historical portal positions, NPC placement or save coordinate migration.')
    (OUT/'legacy-maps-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
