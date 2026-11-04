"""Verify current UI/client evidence hashes without inventing a Unity Play result.

Run the named focused checks first. This only consolidates successful, current
reports, checks their declared sources and the all-source API compile, and audits
managed-script GUIDs. Counts remain separate because many fixtures overlap.
"""
import hashlib,json,re
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
 names=['native-font-check.json','native-settings-check.json','native-memo-check.json','native-memo-transfer-check.json','native-candidate-data-check.json','runtime-resource-bindings-check.json','native-world-text-check.json','native-window-placement-check.json','native-chat-check.json','native-chat-composer-check.json','native-private-chat-check.json','native-item-caption-check.json','native-self-info-check.json','player-identity-check.json','native-commander-check.json','commander-adjustments-check.json','client-window-assets-check.json','../OriginalTutorialChecks/drag-lifecycle-check.json','../OriginalTutorialChecks/tutorial-check.json','../OriginalTutorialChecks/dialogue-presentation-check.json','commander-name-check.json']
 names+=['pixel-import-check.json','native-bank-check.json','bank-currency-regressions.json','item-drag-context-check.json','nullable-contract-check.json','facility-callback-check.json','facility-contract-regressions.json','native-warehouse-check.json']
 names+=['nullable-serialization-analyzer.json','callback-session-regressions.json']
 names+=['native-shop-check.json']
 names+=['native-world-message-check.json']
 names+=['native-shop-details-check.json','native-hire-list-check.json','native-mercenary-check.json','native-window-drag-check.json']
 names+=['native-battle-options-check.json','native-class-adjustments-check.json','native-class-presentation-check.json']
 names+=['picture-initialization-check.json','legacy-maps-check.json']
 names+=['character-reset-check.json','map-pointer-check.json','native-temple-alignment-check.json','temple-alignment-check.json','training-tier-check.json','native-tile-attribute-check.json','native-body-order-check.json']
 reports=[];checked={};stale=[]
 for name in names:
  path=OUT/name;report=json.loads(path.read_text());assert report.get('mismatches',0)==0,name
  for source,digest in report.get('sources',{}).items():
   target=ROOT/source;actual=sha(target)if target.is_file()else None
   if actual!=digest:stale.append(dict(report=name,source=source))
   else:checked[source]=actual
  counts={key:value for key,value in report.items()if isinstance(value,int)and not isinstance(value,bool)}
  if 'nativeInheritedClickCases'in report:counts['nativeInheritedClickCases']=len(report['nativeInheritedClickCases'])
  nested={key:dict(assertions=value.get('assertions'),scope=value.get('scope'))for key,value in report.get('results',{}).items()}
  reports.append(dict(report=path.relative_to(ROOT).as_posix(),sha256=sha(path),counts=counts,groups=nested,scope=report.get('scope','Focused owned-adjustment regression groups; see the source report.')))
 assert not stale,'Stale verification sources; rerun the relevant check: '+json.dumps(stale,ensure_ascii=False)
 artifacts=[]
 for name in ['native-shop-details-art.json','native-mercenary-catalog-art.json','native-facility-windows-art.json','native-class-presentation-art.json','native-candidate-cache-art.json']:
  path=OUT/name;artifact=json.loads(path.read_text());resource=ROOT/artifact['resource'];assert sha(resource)==artifact['sha256'],'Generated resource changed: '+str(resource)
  meta=Path(str(resource)+'.meta');text=meta.read_text();assert re.search(r'^guid: ([a-f0-9]{32})$',text,re.M)[1]==artifact['resourceGuid']and 'TextScriptImporter:'in text,'Native TextAsset metadata mismatch: '+str(meta)
  for source,digest in artifact['sources'].items():assert sha(ROOT/source)==digest,'Resource inputs changed; regenerate: '+source;checked[source]=digest
  artifacts.append(dict(report=path.relative_to(ROOT).as_posix(),resource=artifact['resource'],sha256=artifact['sha256'],guid=artifact['resourceGuid']))
 path=ROOT/'Tools/OriginalEntryChecks/entry-check.json';api=json.loads(path.read_text())
 assert all(api[key]==0 for key in ['runtimeApiErrors','editorApiErrors','playerApiErrors'])and all(value==0 for value in api['warnings'].values()),'API compile failure/warnings'
 for source,digest in api['codeSha256'].items():assert sha(ROOT/source)==digest,'API source changed after compile: '+source
 # The compiler report must cover every current runtime and editor source too.
 code=sorted((ROOT/'Assets/_Original/Runtime').rglob('*.cs'))+sorted((ROOT/'Assets/_Original/Editor').rglob('*.cs'))
 assert {p.relative_to(ROOT).as_posix()for p in code}==set(api['codeSha256']),'API compilation source coverage mismatch'
 asset_sources={p.relative_to(ROOT).as_posix()for p in (ROOT/'Assets').rglob('*.cs')}
 assert asset_sources==set(api['codeSha256']),'C# assets exist outside the declared API compilation scope'
 # A report with unchanged old inputs must not silently omit newly generated PNGs.
 from apply_pixel_import_policy import texture_paths
 pixel_path=OUT/'pixel-import-check.json';pixel=json.loads(pixel_path.read_text());policy_path=OUT/'pixel-import-policy.json';policy=json.loads(policy_path.read_text())
 assert pixel['generationReportSha256']==sha(policy_path),'Pixel metadata policy changed after verification'
 assert {ROOT/row['asset']for row in policy['textures']}==set(texture_paths()),'Current generated textures are missing from the importer verification'
 assert pixel['textures']==len(policy['textures']),'Pixel importer texture coverage mismatch'
 resource=json.loads((OUT/'runtime-resource-bindings-check.json').read_text());from check_runtime_resource_bindings import inventory
 assert inventory()[2]==resource['inventorySha256'],'Runtime resource/GUID inventory changed; rerun resource binding check'
 guids={}
 for source in code:
  meta=Path(str(source)+'.meta');assert meta.is_file(),str(meta);m=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M);assert m and m[1]not in guids,'Missing/duplicate script GUID: '+str(source);guids[m[1]]=source.relative_to(ROOT).as_posix()
 evidence=ROOT/'분석/Ghidra/Saver_Late/ClientWindows/evidence.json';original=json.loads(evidence.read_text())
 for row in original['functions']+[original['assembly']]:assert sha(ROOT/row['asset'])==row['sha256'],'Native static evidence changed: '+row['asset']
 assertions=0
 for folder in ['Runtime/UI','Runtime/World','Runtime/Entry','Runtime/Growth','Runtime/Tutorial']:
  for path in (ROOT/'Assets/_Original'/folder).glob('*.cs'):
   assert all(line==line.rstrip()for line in path.read_text().splitlines()),'Trailing source whitespace: '+str(path);assertions+=1
 result=dict(checkedAtUtc=datetime.now(timezone.utc).isoformat(),unityPlay=False,unityEditorTestRunner=False,physicalInput=False,audioListening=False,scope='Current declared source hashes, complete runtime/editor/Player API coverage, successful focused reports, source GUID uniqueness and static-evidence integrity. Counts are not summed; reports execute different isolated native/managed scopes. No full transitive/native-process/Windows/Unity-screen/IME/audio/server-delivery equivalence claim. Player saves and authored world/gateway/monster data are not test inputs.',reports=reports,api=dict(report='Tools/OriginalEntryChecks/entry-check.json',sha256=sha(ROOT/'Tools/OriginalEntryChecks/entry-check.json'),runtimeSources=api['runtimeSources'],editorSources=api['editorSources'],errors=0,warnings=0,codeSha256=api['codeSha256']),sourceAudit=dict(declaredSources=len(checked),scriptGuids=len(guids),assetCSharpSources=len(asset_sources),pixelTextures=pixel['textures'],staticFunctions=len(original['functions']),whitespaceFiles=assertions),staticEvidence=dict(report=evidence.relative_to(ROOT).as_posix(),sha256=sha(evidence)),toolSha256=sha(Path(__file__)))
 result['resourceArtifacts']=artifacts
 result['pixelImportAudit']=dict(report=pixel_path.relative_to(ROOT).as_posix(),sha256=sha(pixel_path),policySha256=sha(policy_path),textures=pixel['textures'],unityImport=False)
 (OUT/'client-windows-integration-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(reports=len(reports),resourceArtifacts=len(artifacts),api=result['api']|{'codeSha256':None},sourceAudit=result['sourceAudit'],unityPlay=False),ensure_ascii=False))
if __name__=='__main__':main()
