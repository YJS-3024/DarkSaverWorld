"""Check selected legacy field bytes, Unity asset bindings and version-specific GRS."""
import hashlib
import json
import re
import subprocess
from pathlib import Path

import build_campaign_assets as builder

ROOT=builder.ROOT
BASE=builder.BASE


def main():
    checks=0
    def check(condition,label):
        nonlocal checks
        checks+=1
        assert condition,label
    def read(path):return json.loads(path.read_text())
    def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
    def refs(text,field):
        block=re.search(r'^  '+field+r':\n((?:  - .*\n)*)',text,re.M)
        check(block is not None,'Unity asset field '+field)
        return re.findall(r'guid: ([0-9a-f]{32})',block.group(1))
    def guid(path):return re.search(r'^guid: ([0-9a-f]{32})$',Path(str(path)+'.meta').read_text(),re.M)[1]
    catalog=read(BASE/'Data/Campaign/CampaignCatalog.json');maps=read(BASE/'Data/Campaign/GatewayMaps.json')
    row=next(m for m in catalog['maps']if m['id']==1010);info=next(m for m in maps['maps']if m['id']==1010)
    original=ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/MAP'
    selected=ROOT/info['nativePath'];native=builder.read_mrc(original/'1010.mrc')
    check(selected.read_bytes()==(original/'1010.mrc').read_bytes(),'active map exactly matches preserved original')
    check(sha(selected)==row['hash']==info['hash']=='512f1fcd60ce30e19ada0d650c41efdf758432f66264c492d2c99fead74f30ac','map version/hash agreement')
    check(row['world']['sourceVersion']=='Saver_1.2'and info['legacy'],'runtime/editor select old graphics')
    check([native['width'],native['height']]==[160,160]and len(native['sprites'])==734,'old map dimensions/decorations')
    check(info['width']==160 and info['height']==160,'editor dimensions')
    check(row['world']['map']['attributes']==[0 if(t[3]&127)>27 else t[3]&127 for t in native['tiles']],'navigation uses selected source terrain')
    asset=(BASE/'Data/Campaign/NativeCampaignArt.asset').read_text()
    check(refs(asset,'maps')[row['nativeIndex']]==guid(selected),'runtime MRC reference uses legacy file')
    raw=bytes.fromhex(re.search(r'^  legacyGraphicIds: ([0-9a-f]+)$',asset,re.M)[1])
    ids=[int.from_bytes(raw[i:i+4],'little')for i in range(0,len(raw),4)]
    bindings=refs(asset,'legacyGraphics')
    expected=sorted({rid for key in builder.LEGACY_MAPS for rid in builder.read_mrc(original/(key+'.mrc'))['ids']})
    check(ids==expected and len(ids)==len(bindings),'all selected old GRS dependencies bound')
    inventory=read(ROOT/'분석/InstallerVersions/installer_inventory.json')
    known={f['sha256']for p in inventory['payloads']for f in p.get('files',[])if 'sha256'in f}
    for index,rid in enumerate(ids):
        copied=selected.parent/('mdsr'+str(rid)+'.grs.bytes');source=original/('mdsr'+str(rid)+'.grs')
        check(copied.read_bytes()==source.read_bytes(),'original GRS bytes '+str(rid))
        check(guid(copied)==bindings[index],'Unity GRS binding '+str(rid))
        check(sha(copied)in known,'installer provenance '+str(rid))
    late=BASE/'SourceData/Native/map/1010.mrc.bytes'
    check(sha(late)=='3edd35a2a5e876d43bdeb3a7a459fc5cf3bd6657aa8c3131ce749b0887189e16','late source retained')
    check((selected.parent/'mdsr24.grs.bytes').read_bytes()!=(BASE/'SourceData/Native/map/mdsr24.grs.bytes').read_bytes(),'changed GRS24 kept separate')
    original_catalog=json.loads(subprocess.check_output(['git','show','HEAD:Assets/_Original/Data/Campaign/CampaignCatalog.json'],cwd=ROOT))
    old={m['id']:m for m in original_catalog['maps']}
    for m in catalog['maps']:
        if m['key']not in builder.LEGACY_MAPS:check(m==old[m['id']],'other map retained '+str(m['id']))
    gateway_path=BASE/'Data/Campaign/UserGateways.json'
    check(gateway_path.read_bytes()==subprocess.check_output(['git','show','HEAD:'+gateway_path.relative_to(ROOT).as_posix()],cwd=ROOT),'user connections not migrated or replaced')
    editor=read(ROOT/'Tools/OriginalScenarioEditorChecks/editor-check.json')
    rendered=next(m for m in editor['rendered']if m['key']=='1010')
    image=Path('/tmp/ds-original-scenario-editor-checks/1010.rgba')
    check(rendered['width']==5120 and rendered['height']==5120 and sha(image)==rendered['sha256'],'actual C# rendering dimensions/hash')
    check('1010'in editor['nativePixelComparisons'],'independent decoder pixel comparison executed')
    result=dict(assertions=checks,map=1010,version='Saver_1.2',nativeSprites=734,legacyGrs=ids,
        renderedSha256=rendered['sha256'],editorMaps=editor['maps'],unityPlay=False,
        scope='Original bytes, navigation, per-version asset bindings and managed C# versus Python pixels; no Unity Play or historical timing validation.')
    Path(__file__).with_name('legacy-field-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(result,ensure_ascii=False))


if __name__=='__main__':main()
