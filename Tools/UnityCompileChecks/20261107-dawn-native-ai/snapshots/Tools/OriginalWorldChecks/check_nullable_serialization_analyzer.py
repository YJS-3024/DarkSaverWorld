"""Run the installed Unity serialization analyzer on paired actual runtime API compilations.

No Editor import/Play or license bypass. Baseline differs only by removing the
NonSerialized attributes tested by check_nullable_contracts.py.
"""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-nullable-serialization-analyzer')
def main():
 WORK.mkdir(exist_ok=True);data=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data';sdk=data/'DotNetSdk';analyzer=data/'Tools/BuildPipeline/Unity.Analyzers.Common/Unity.Analyzers.Common.dll';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 template=(Path('/tmp/ds-original-entry-checks')/'runtime.rsp').read_text();base=[];sources=sorted((ROOT/'Assets/_Original/Runtime').rglob('*.cs'))
 for source in sources:
  text=source.read_text()
  if '[NonSerialized,DataMember'in text:
   target=WORK/'baseline'/source.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text.replace('[NonSerialized,DataMember','[DataMember'));base.append((source,target))
 results={}
 for variant in ['baseline','current']:
  args='\n'.join(line for line in template.splitlines()if not line.strip('"').startswith('-out:'))
  args+='\n-out:'+str(WORK/(variant+'.dll'))+'\n-analyzer:'+str(analyzer)+'\n-errorlog:'+str(WORK/(variant+'.sarif'))+',version=2.1\n'
  if variant=='baseline':
   for source,target in base:args=args.replace(str(source),str(target))
  rsp=WORK/(variant+'.rsp');rsp.write_text(args);p=subprocess.run([str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)],cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(variant+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2400:])
  diagnostics=[row for run in json.loads((WORK/(variant+'.sarif')).read_text())['runs']for row in run.get('results',[])]
  results[variant]=dict(errors=sum(row.get('level')=='error'for row in diagnostics),warnings=sum(row.get('level')=='warning'for row in diagnostics),uac1001=sum(row.get('ruleId')=='UAC1001'for row in diagnostics),otherRules=sorted({row.get('ruleId')for row in diagnostics if row.get('ruleId')!='UAC1001'}))
 expected=json.loads((OUT/'nullable-contract-check.json').read_text())['results']['current']['nullableFields']
 assert results['baseline']==dict(errors=0,warnings=expected,uac1001=expected,otherRules=[]),'Baseline did not reproduce precisely the production nullable warnings'
 assert results['current']==dict(errors=0,warnings=0,uac1001=0,otherRules=[]),'Current Unity serialization analyzer diagnostics remain'
 paths=[Path(__file__),OUT/'nullable-contract-check.json',*sources];report=dict(results=results,runtimeSources=len(sources),nativeUnityAnalyzer=dict(path=str(analyzer.relative_to(ROOT.parent)),sha256=hashlib.sha256(analyzer.read_bytes()).hexdigest()),mismatches=0,unityEditorImport=False,unityPlay=False,scope='Actual Unity6000.5.7f1 Unity.Analyzers.Common.dll used by Roslyn against all current runtime Unity API sources. Paired baseline removes only NonSerialized from DCS nullable fields; current compilation must have no diagnostics. Actual Editor import/plugin warnings/Play are separate and were not run.',logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
 (OUT/'nullable-serialization-analyzer.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
