"""Remove unsupported initial rosters; synchronize content and visual hashes.
--apply mutates only scenario Content/Visuals and current resource manifests.
Source scripts, maps, saved games and original analysis are not rewritten.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/_Game/Resources/Scenarios'


def digest(value):
    return hashlib.sha256(json.dumps(value,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()


def transform(data,key,original=None):
    result=json.loads(json.dumps(data))
    result['units']=original['units'] if original else []
    result['deploymentSource']='original-set-pos' if original else 'original-missing'
    result['deploymentNote']='Original SET/POS unit/type/coordinates.' if original else 'Initial SET/POS not recovered. Await server unit notification; no generated fallback.'
    result['provenance']='Original script/presentation data; initial deployment is independently identified by deploymentSource.'
    for field in ['placementAdjustments','statSentinels']:result.pop(field,None)
    for row in result.get('characters',[]):row.pop('wave',None)
    result['profileHash']=digest(dict(scenario=key,source=result['deploymentSource'],units=result['units']))
    result.pop('sourceHash',None);result['sourceHash']=digest(result)
    return result


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    updates=[]
    for path in sorted(BASE.glob('*/Content.json')):
        key=path.parent.name;before=json.loads(path.read_text(encoding='utf-8'))
        source=BASE/'Original'/str(int(key))/'Deployment.json'
        original=json.loads(source.read_text(encoding='utf-8')) if source.exists() else None
        after=transform(before,key,original)
        # Preserve all screenplay/presentation content and the map-entry configuration.
        for field in before:
            if field not in ['units','placementAdjustments','statSentinels','profileHash','sourceHash','provenance','characters','deploymentSource','deploymentNote']:
                assert after[field]==before[field],(key,field)
        updates.append((path,before,after))
    if args.apply:
        # Reject a stale visual binding before writing any content file.
        for path,before,after in updates:
            visual=path.with_name('Visuals.json')
            if visual.exists():assert json.loads(visual.read_text(encoding='utf-8'))['contentHash'] in (before['sourceHash'],after['sourceHash']),visual
        for path,before,after in updates:
            path.write_text(json.dumps(after,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
            visual=path.with_name('Visuals.json')
            if visual.exists():
                text=visual.read_text(encoding='utf-8');data=json.loads(text)
                assert data['contentHash'] in (before['sourceHash'],after['sourceHash']),visual
                visual.write_text(text.replace(before['sourceHash'],after['sourceHash']),encoding='utf-8')
        by_key={p.parent.name:a for p,b,a in updates}
        for manifest in [ROOT/'Tools/MapChecks/scenarios_02_17_resources.json',ROOT/'Tools/LateVersionChecks/new_scenarios_check.json']:
            data=json.loads(manifest.read_text(encoding='utf-8'))
            for row in data['scenarios']:
                content=by_key[row['key']];row.update(sourceHash=content['sourceHash'],units=len(content['units']),deploymentSource=content['deploymentSource'])
                row.pop('waves',None);row.pop('adjustedPlacements',None)
            manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    report=dict(applied=args.apply,contentFiles=len(updates),removedUnits=sum(max(0,len(b['units'])-len(a['units'])) for p,b,a in updates),
                remainingUnits=sum(len(a['units']) for p,b,a in updates),missingInitialRosters=[p.parent.name for p,b,a in updates if a['deploymentSource']=='original-missing'],
                originalResourceUnits=sum(len(json.loads(p.read_text())['units']) for p in (BASE/'Original').glob('*/Deployment.json')),
                changedFiles=sum(b!=a for p,b,a in updates),savedGamesModified=False,
                rows=[dict(key=p.parent.name,beforeCount=len(b['units']),afterCount=len(a['units']),deploymentSource=a['deploymentSource'],sourceHash=a['sourceHash']) for p,b,a in updates])
    target=ROOT/'Tools/ScenarioPlacementChecks/original-deployment-cleanup.json' if args.apply else Path('/tmp/original-deployment-cleanup-preview.json')
    if not args.apply or report['changedFiles'] or not target.exists():
        target.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='rows'},ensure_ascii=False))


if __name__=='__main__':main()
