"""Refresh report references only after all referenced source reports are current.

Never reruns tests, changes evidence/source-file hashes, or repairs report inputs.
All declared manifests are validated before the first metadata write.
"""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'분석/Ghidra/Saver_Late'
REFERENCES={
 'ScenarioEndControl':{'nativeExecution':'native-scenario-end-check.json','cleanupExecution':'native-scenario-cleanup-check.json','runtimeBridge':'native-result-contract-check.json','productionPlanComparison':'native-end-stage-check.json'},
 'EventQueue':{'nativeExecution':'native-event-queue-check.json','interpreterStepExecution':'native-event-steps-check.json','interpreterCorpusExecution':'native-event-corpus-check.json','updateLoopExecution':'native-script-frame-check.json','runtime15Comparison':'native-immediate-events-check.json','runtime15Contracts':'scenario-progress-check.json'},
 'MagicRedirect':{'nativeExecution':'native-magic-redirect-check.json','targetOracle':'native-magic-target-catalog-check.json','combinedImportBridge':'native-magic-redirect-catalog-check.json','runtimeSelection':'native-magic-redirect-selection-check.json','runtimeContracts':'native-magic-check.json'},
 'AuxiliaryPackets':{'nativeExecution':'native-auxiliary-packets-check.json','allianceMetadataExecution':'native-alliance-metadata-check.json','monsterActivationExecution':'native-monster-activation-check.json','runtime20Comparison':'native-victory-notice-check.json','runtime66Comparison':'native-scenario-suffix-check.json'},
 'RetreatConfirmation':{'nativeExecution':'native-auxiliary-packets-check.json'},
 'DirectedEvents':{'runtimeContracts':'native-directed-event-check.json','frameComparison':'native-directed-frame-check.json','controlExports':'native-directed-control-check.json','messageFormatting':'native-campaign-message-check.json','inputComparison':'native-directed-input-check.json','substringSemantics':'native-message-substring-check.json','fileLoaderComparison':'native-event-loader-check.json'}}
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def verify_files(manifest):
 for row in manifest['files']:
  path=ROOT/row['asset'];assert path.is_file()and sha(path)==row['sha256'],'Changed native evidence; preserve and review: '+row['asset']
def verify_report(path):
 data=json.loads(path.read_text(encoding='utf-8'));assert data.get('mismatches',0)==0,path
 assert data.get('sources'), 'No declared source proof: '+str(path)
 for source,digest in data['sources'].items():
  target=ROOT/source;assert target.is_file()and sha(target)==digest,'Stale report; rerun its real producer: '+path.name+' / '+source
 return data
def main():
 prepared=[]
 for name,refs in REFERENCES.items():
  path=BASE/name/'evidence.json';data=json.loads(path.read_text(encoding='utf-8'));verify_files(data)
  for field,filename in refs.items():
   report=ROOT/'Tools/OriginalWorldChecks'/filename
   # Preserve the established paths too: this tool cannot silently retarget evidence.
   previous=data[field]['report'];assert previous==report.relative_to(ROOT).as_posix(),(name,field,previous)
   verify_report(report);data[field]=dict(report=previous,sha256=sha(report))
  prepared.append((path,data))
 for path,data in prepared:path.write_bytes((json.dumps(data,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
 print(json.dumps(dict(manifests=len(prepared),references=sum(len(v)for v in REFERENCES.values()),sourceHashesRewritten=False,testsExecuted=False)))
if __name__=='__main__':main()
