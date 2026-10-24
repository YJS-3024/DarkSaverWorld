"""Cross-audit complete ability routing, native presentation metadata, hire data and emote pixels."""
import csv,json,hashlib,re
from pathlib import Path
from PIL import Image
import UnityPy
from world_asset_baker import ROOT

def main():
    source=ROOT/'닥세_리소스/분석결과/표_Magictbl.csv'
    original={int(t[0]):t for row in csv.DictReader(source.open(encoding='utf-8-sig')) for t in [json.loads(row['tokens'])]}
    coverage=json.loads((ROOT/'Tools/MapChecks/all_abilities_check.json').read_text())
    assert coverage['sourceCatalogSha256']==hashlib.sha256(source.read_bytes()).hexdigest()
    entries={r['id']:r for r in coverage['entries']};assert len(entries)==439 and entries.keys()==original.keys()
    assert coverage['combat']==374 and coverage['hiring']==50 and coverage['emotes']==15
    presentation=UnityPy.load(str(ROOT/'Assets/_Game/Resources/Effects/NativeMagicPresentationCatalog.asset'))
    metadata=next(o.read_typetree()['entries'] for o in presentation.objects if o.type.name=='MonoBehaviour')
    assert {r['id'] for r in metadata}==original.keys()
    patterns={int(t[0]):t for raw in csv.DictReader((ROOT/'닥세_리소스/분석결과/표_MagicPtn.csv').open(encoding='utf-8-sig')) for t in [json.loads(raw['tokens'])]}
    silent_visual=[]
    for row in metadata:
        pattern=patterns[int(original[row['id']][14])]
        assert row['family']==int(pattern[16]) and row['sound1']==int(pattern[17]) and row['sound2']==int(pattern[18])
        if entries[row['id']]['route']=='Combat':
            assert row['family']!=99
            if row['family']==0:silent_visual.append(row['id'])
    assert silent_visual==list(range(1101,1107))
    hires={int(r['특수스킬']):r for r in csv.DictReader((ROOT/'분석/Saver_1.2/converted/solskill.csv').open(encoding='utf-8-sig'))}
    jobs={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/mercenaries.csv').open(encoding='utf-8-sig'))};recruits=set()
    for row in entries.values():
        assert row['mp']==int(original[row['id']][5])
        if row['route']=='Hiring':
            hire=hires[row['id']];assert int(hire['용병고용수'])>0
            for key in hire['용병고용종수'].split(','):
                if not key.strip():continue
                job=jobs[int(key)];assert int(job['체력수'])>0 and int(job['가격'])>=0 and int(job['마력수'])==-1;recruits.add(int(key))
    # Every playable class ability is either a catalog spell or an existing hire entry.
    class_rows=[r for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/experience_progression.csv').open(encoding='utf-8-sig')) if 100<=int(r['jobId'])<=169]
    class_ids=set()
    for row in class_rows:
        for key in ('learnedMagic','usableSkills'):
            for value in row[key].split(','):
                if value.strip() and int(value)>0:class_ids.add(int(value))
    assert class_ids <= original.keys()|hires.keys()
    out=ROOT/'Assets/_Game/Resources/Effects/Emotes';emotes=json.loads((out/'Manifest.json').read_text())['entries'];proof=json.loads((ROOT/'Tools/MapChecks/emote_resources.json').read_text())
    assert {e['id'] for e in emotes}=={r['id'] for r in entries.values() if r['route']=='Emote'}
    atlas_path=ROOT/proof['source'];assert hashlib.sha256(atlas_path.read_bytes()).hexdigest()==proof['sourceSha256'];atlas=Image.open(atlas_path).convert('RGBA')
    timing=list(csv.DictReader((ROOT/'닥세_리소스/분석결과/특수기_패턴99_프레임시간.csv').open(encoding='utf-8-sig')));pixels=0
    for row in emotes:
        frames=sorted([r for r in timing if int(r['pattern_id'])==int(original[row['id']][14])],key=lambda r:int(r['frame']))
        assert len(row['frames'])==len(frames) and row['limit']==int(frames[0]['stop_counter'])
        assert row['delays']==[int(f['delay_ms']) for f in frames]
        for path,frame in zip(row['frames'],frames):
            rect=tuple(int(frame[k]) for k in ('left','top','right','bottom'));expected=atlas.crop(rect)
            expected.putdata([(r,g,b,0 if (r,g,b)==(0,0,0) else a) for r,g,b,a in expected.getdata()])
            assert Image.open(ROOT/'Assets/_Game/Resources'/(path+'.png')).convert('RGBA').tobytes()==expected.tobytes();pixels+=1
    paths=list(out.rglob('*'))+[ROOT/'Assets/_Game/Runtime'/p for p in ['Combat/SinglePlayerAbilityRules.cs','Combat/LocalCombatAuthority.Abilities.cs','Combat/BattleTransitionController.Emotes.cs','Network/LocalEmotePackets.cs','World/WorldEmoteVisual.cs']]
    guids={}
    for meta in (ROOT/'Assets/_Game').rglob('*.meta'):
        match=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
        if match:guids.setdefault(match[1],[]).append(meta)
    for path in paths:
        if path.suffix=='.meta':continue
        meta=Path(str(path)+'.meta');assert meta.exists(),path
        guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1];assert len(guids[guid])==1
    report=dict(total=439,combatMagic=289,combatSpecials=85,hiring=50,hireJobs=len(recruits),emotes=15,originalEmoteFrames=pixels,classAbilityIds=len(class_ids),presentationMetadata=len(metadata),nativeNoGraphicCombatIds=silent_visual,checks=coverage['checks'],allClassIdsRouted=True,originalEmotePixelsPreserved=True,unityPlay=False,scope='Source catalog and class/hire linkage, native metadata, emote pixels/timing, GUIDs; core combat execution in all_abilities_check.json. No Unity import/playback or original-server formula validation.')
    (ROOT/'Tools/MapChecks/all_abilities_resources_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
if __name__=='__main__':main()
