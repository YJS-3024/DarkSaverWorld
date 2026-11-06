"""Keep Unity's unsupported nullable fields out of its serializer without changing account JSON."""
import hashlib,json,os,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-nullable-contract-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet');base=(old/'contracts.rsp').read_text();paths=[]
 for source in (ROOT/'Assets/_Original/Runtime').rglob('*.cs'):
  text=source.read_text()
  if re.search(r'\[NonSerialized,DataMember[^\n]* public (?:int|uint|ulong|long|bool)\?',text):
   target=WORK/'baseline'/source.relative_to(ROOT);target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text.replace('[NonSerialized,DataMember','[DataMember'));paths.append((source,target))
 results={}
 for variant in ['baseline','current']:
  exe=WORK/(variant+'.dll');args=base.replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NullableContractChecks.cs'))
  if variant=='baseline':
   args+='\n-define:BASELINE\n'
   for source,target in paths:args=args.replace(str(source),str(target))
  rsp=WORK/(variant+'.rsp');rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
  for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
   p=subprocess.run(command,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(variant+'-'+phase+'.log')).write_text(p.stdout+p.stderr)
   if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2500:])
  results[variant]=json.loads(p.stdout)
 assert results['current']['serializedCasesSha256']==results['baseline']['serializedCasesSha256'],'NonSerialized changed actual DCS save JSON'
 report=dict(results=results,mismatches=0,unityPlay=False,unityImport=False,scope='Paired compilations of the current production contracts, differing only in NonSerialized attributes. Byte-identical serializer case payloads and field coverage; Unity Editor UAC1001 warning disappearance requires actual Editor import.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [Path(__file__),OUT/'NullableContractChecks.cs',*[p for p,_ in paths]]})
 (OUT/'nullable-contract-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:{a:b for a,b in v.items()if a not in ['fields','scope']}for k,v in results.items()}))
if __name__=='__main__':main()
