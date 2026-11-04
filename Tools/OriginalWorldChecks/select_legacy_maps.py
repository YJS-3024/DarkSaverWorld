"""Select complete preserved1.2 map/GRS designs without rebuilding art or user gateways."""
import hashlib,json,re,csv,io
from pathlib import Path
import build_campaign_assets as builder
ROOT=builder.ROOT;BASE=builder.BASE;OUT=Path(__file__).parent
OLD=ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/MAP'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def main():
    catalog_path=BASE/'Data/Campaign/CampaignCatalog.json';gate_path=BASE/'Data/Campaign/GatewayMaps.json';asset=BASE/'Data/Campaign/NativeCampaignArt.asset'
    catalog=json.loads(catalog_path.read_text());gate=json.loads(gate_path.read_text())
    protected=[BASE/'Data/Campaign/UserGateways.json',*BASE.glob('Editor/ScenarioLayouts/*.json'),*BASE.rglob('*.meta'),* (BASE/'SourceData/Native').rglob('*.bytes')]
    before={p:sha(p)for p in protected};audit=[]
    for info in gate['maps']:
        old=OLD/(info['key']+'.mrc');late=BASE/'SourceData/Native/map'/(info['key']+'.mrc.bytes')
        if not old.exists():audit.append(dict(key=info['key'],status='old-map-unavailable'));continue
        m=builder.read_mrc(old);missing=[i for i in m['ids']if not(OLD/f'mdsr{i}.grs').exists()]
        if missing:audit.append(dict(key=info['key'],status='old-graphics-unavailable',missingGraphics=missing));continue
        reference=late if late.exists()else ROOT/info['nativePath']
        different=sha(old)!=sha(reference)
        graphics=[i for i in m['ids']if (BASE/f'SourceData/Native/map/mdsr{i}.grs.bytes').exists()and sha(OLD/f'mdsr{i}.grs')!=sha(BASE/f'SourceData/Native/map/mdsr{i}.grs.bytes')]
        selected=info['key']in builder.LEGACY_MAPS
        already_old=info.get('legacy')and sha(ROOT/info['nativePath'])==sha(old)
        assert selected or already_old or not different and not graphics,('Complete changed old design was omitted',info['key'])
        audit.append(dict(key=info['key'],status='selected-old'if selected else'old-equivalent',geometryDiffers=different,graphicsDiffers=graphics,oldSha256=sha(old)))
    assert {r['key']for r in audit if r['status']=='selected-old'}==builder.LEGACY_MAPS
    copied=builder.prepare_legacy_maps()
    unchanged={m['id']:json.dumps(m,sort_keys=True)for m in catalog['maps']if m['key']not in builder.LEGACY_MAPS}
    for row in catalog['maps']:
        if row['key']not in builder.LEGACY_MAPS:continue
        p=builder.map_path(row['key']);m=builder.read_mrc(p);row['hash']=sha(p);row['world']['sourceVersion']='Saver_1.2'
        row['world']['map'].update(width=m['width'],height=m['height'],attributes=[0 if(t[3]&127)>27 else t[3]&127 for t in m['tiles']],renderAttributes=[t[3]for t in m['tiles']])
        info=next(r for r in gate['maps']if r['key']==row['key']);info.update(width=m['width'],height=m['height'],hash=sha(p),legacy=True,nativePath=p.relative_to(ROOT).as_posix())
    # Runtime and editor resolve the same version. Preserve all index ordering.
    text=asset.read_text();original_asset=text
    def ref(p):return '{fileID: 4900000, guid: '+builder.world.guid(p)+', type: 3}'
    block=re.search(r'^  maps:\n((?:  - .*\n)*)',text,re.M);lines=block[1].splitlines()
    for row in catalog['maps']:
        if row['key']in builder.LEGACY_MAPS:lines[row['nativeIndex']]='  - '+ref(builder.map_path(row['key']))
    text=text[:block.start(1)]+'\n'.join(lines)+'\n'+text[block.end(1):]
    graphics=sorted({p for _,p in copied if p.name.startswith('mdsr')},key=lambda p:int(p.name[4:].split('.')[0]))
    text=re.sub(r'^  legacyGraphics:\n(?:  - .*\n)*','',text,flags=re.M);text=re.sub(r'^  legacyGraphicIds:.*\n','',text,flags=re.M)
    text+='  legacyGraphics:\n'+''.join('  - '+ref(p)+'\n'for p in graphics)
    text+='  legacyGraphicIds: '+''.join(int(p.name[4:].split('.')[0]).to_bytes(4,'little').hex()for p in graphics)+'\n'
    def other_fields(value):
        return re.sub(r'^  (?:maps|legacyGraphics|legacyGraphicIds):[^\n]*\n(?:  - .*\n)*','',value,flags=re.M)
    assert other_fields(original_asset)==other_fields(text),'Unrelated campaign art fields changed'
    # Validate authored gateway coordinates against the new dimensions without rewriting them.
    settings=json.loads((BASE/'Data/Campaign/UserGateways.json').read_text());infos={r['id']:r for r in gate['maps']}
    for g in settings.get('gates',[]):
        for name in ['fromMap','targetMap']:
            assert g.get(name)in infos,('Unknown authored gateway map',g.get('id'),name)
        for map_id,x,y,w,h,version in [(g['fromMap'],g['x'],g['y'],g.get('width',0)or 1,g.get('height',0)or 1,g.get('fromHash')),
                (g['targetMap'],g['spawnX'],g['spawnY'],g.get('spawnWidth',0)or 1,g.get('spawnHeight',0)or 1,g.get('targetHash'))]:
            info=infos[map_id]
            assert 0<=x and 0<=y and w>0 and h>0 and x+w<=info['width'] and y+h<=info['height'],('Authored gateway outside old geometry; edit before switching',g['id'])
            assert version==info['hash'],('Authored gateway targets another map version; preserve and review manually',g['id'])

    write(catalog_path,catalog);write(gate_path,gate);asset.write_text(text)
    table=BASE/'Data/Tables/scene_maps.csv'
    rows=list(csv.DictReader(io.StringIO(table.read_text(encoding='utf-8-sig'))));columns=list(rows[0])
    for row in rows:
        info=next((m for m in gate['maps']if m['key']==row['map_key']),None)
        if info is not None:
            row.update(width_tiles=str(info['width']),height_tiles=str(info['height']),map_asset_path=info['nativePath'])
    with table.open('w',encoding='utf-8-sig',newline='') as stream:
        writer=csv.DictWriter(stream,fieldnames=columns);writer.writeheader();writer.writerows(rows)

    assert all(sha(p)==h for p,h in before.items()),'Existing source/meta/user settings changed'
    assert all(json.dumps(m,sort_keys=True)==unchanged[m['id']]for m in catalog['maps']if m['key']not in builder.LEGACY_MAPS)
    outputs={str(p.relative_to(ROOT)):sha(p)for p in [catalog_path,gate_path,asset]}
    # Several art builders append independent fields to this shared ScriptableObject.
    # Record the verified map-only postprocess in every manifest declaring it;
    # do not rerun unrelated artwork builders or rewrite texture import metadata.
    manifests=sorted(set(OUT.glob('*assets.json'))|set(OUT.glob('*-art.json')))
    for manifest in manifests:
        d=json.loads(manifest.read_text());declared=d.get('outputs',[])
        changed=False
        for key,value in outputs.items():
            if key in d.get('sources',{}):d['sources'][key]=value;changed=True
            if isinstance(declared,dict):
                if key in declared:declared[key]=value;changed=True
            else:
                for row in declared:
                    if row.get('asset')==key:row['sha256']=value;changed=True
        if not changed:continue
        if manifest.name in ['campaign-assets.json','gateway-assets.json']:
            for original,target in copied:
                d['sources'][str(original.relative_to(ROOT))]=sha(original);d['sources'][str(target.relative_to(ROOT))]=sha(target)
        d['legacyMapSelection']='Tools/OriginalWorldChecks/legacy-map-selection.json';write(manifest,d)
    result=dict(selected=sorted(builder.LEGACY_MAPS,key=int),maps=audit,outputs=outputs,
        sources={str(p.relative_to(ROOT)):sha(p)for p in [Path(__file__),OUT/'build_campaign_assets.py',*[o for o,t in copied],*[t for o,t in copied]]},
        protectedFiles=len(before),userGatewaysUnchanged=True,unityPlay=False,
        scope='Exact preserved1.2 MRC and matching GRS where design differs; missing old files retain late version. Equivalent designs keep existing references. No new portals/NPC/rosters/save migration.')
    write(OUT/'legacy-map-selection.json',result);print(json.dumps(dict(selected=result['selected'],statuses={s:sum(r['status']==s for r in audit)for s in sorted({r['status']for r in audit})},protected=len(before))))
if __name__=='__main__':main()
