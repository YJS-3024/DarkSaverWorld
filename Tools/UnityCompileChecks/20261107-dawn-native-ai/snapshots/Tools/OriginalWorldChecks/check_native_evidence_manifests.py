"""Validate native evidence files, current report references and declared bridge states."""
import hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'Tools/GhidraChecks'))
from refresh_native_evidence_references import REFERENCES,BASE,verify_files,verify_report,sha

def main():
 sources={};reports=[];files=0
 for name,refs in REFERENCES.items():
  manifest=BASE/name/'evidence.json';data=json.loads(manifest.read_text(encoding='utf-8'));verify_files(data);sources[manifest.relative_to(ROOT).as_posix()]=sha(manifest);files+=len(data['files'])
  for field,filename in refs.items():
   path=OUT/filename;report=verify_report(path);row=data[field]
   assert row==dict(report=path.relative_to(ROOT).as_posix(),sha256=sha(path)),(name,field)
   sources[path.relative_to(ROOT).as_posix()]=sha(path);sources.update(report['sources']);reports.append(row['report'])
 supplemental=['AiFileLoaderAndMoveMode','AiMovementCompleteAbi','AiTargetSelectors','AiClockAndReset','ScenarioScriptSelection','Scalar40Consumers','SoldierControlLifecycle','MonsterCommanderAi','AiFrameDispatch','AllianceLifecycle','AiFollowerFrames','AiTickDispatch','MagicTableProjection']
 supplemental_files=0
 for name in supplemental:
  manifest=BASE/name/'evidence.json';data=json.loads(manifest.read_text(encoding='utf-8'));verify_files(data)
  sources[manifest.relative_to(ROOT).as_posix()]=sha(manifest);supplemental_files+=len(data['files'])
  for row in data['files']:sources[row['asset']]=row['sha256']
 intermediate=json.loads((BASE/'AiFileLoaderAndMoveMode/evidence.json').read_text(encoding='utf-8'))
 assert intermediate['supersededForMovementAbi']=='분석/Ghidra/Saver_Late/AiMovementCompleteAbi/evidence.json'
 route=ROOT/'분석/Saver_Late/gamectl_packet_routes.json';inventory=verify_report(route);declared={r['opcode']for r in inventory['routes']if r['commonIngressDeclared']}
 assert {'5C0D','5C15','5C20','5C66','5C61','5C68'}<=declared and '5CB2'not in declared
 assert json.loads((BASE/'ScenarioEndControl/evidence.json').read_text(encoding='utf-8'))['runtime61Connected']is True
 assert json.loads((BASE/'MagicRedirect/evidence.json').read_text(encoding='utf-8'))['runtime68Connected']is True
 events=json.loads((BASE/'EventQueue/evidence.json').read_text(encoding='utf-8'));assert events['runtime15Connected']is True and events['runtime0DConnected']is True
 directed=json.loads((BASE/'DirectedEvents/evidence.json').read_text(encoding='utf-8'));assert directed['runtime0DConnected']is True and directed['unityPlay']is False
 auxiliary=json.loads((BASE/'AuxiliaryPackets/evidence.json').read_text(encoding='utf-8'));assert auxiliary['runtime20Connected']is True and auxiliary['runtimeConnectedSubtypes']==['20','66'] and auxiliary['runtime66Connected']is True and auxiliary['runtime0CConnected']is False
 for p in [Path(__file__),ROOT/'Tools/GhidraChecks/refresh_native_evidence_references.py',route]:sources[p.relative_to(ROOT).as_posix()]=sha(p)
 result=dict(manifests=len(REFERENCES),evidenceFiles=files,supplementalManifests=len(supplemental),supplementalEvidenceFiles=supplemental_files,currentReportReferences=len(reports),mismatches=0,unityPlay=False,testsRerunByThisAudit=False,
  scope='Declared native exports preserve their recorded bytes; all referenced native/managed reports match current declared source hashes and manifest pointers;0D/15/20/61/66/68 bridge flags agree with the current common route inventory;B2 remains unconnected. Directed-event tests retain their explicit team, target, formatting and input boundaries. This audits existing successful evidence and does not rerun it or establish whole-client/Unity/Windows equivalence.',sources=sources,reports=reports)
 (OUT/'native-evidence-manifest-check.json').write_bytes((json.dumps(result,ensure_ascii=False,indent=2)+'\n').encode('utf-8'));print(json.dumps({k:v for k,v in result.items()if k not in ['sources','reports']},ensure_ascii=False))
if __name__=='__main__':main()
