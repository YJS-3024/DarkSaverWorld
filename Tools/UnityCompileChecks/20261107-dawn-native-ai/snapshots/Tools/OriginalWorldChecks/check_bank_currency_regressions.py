"""Run existing currency/bank production contracts on current sources, without recursive campaign suites."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-bank-core-regressions')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';results={};env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name in ['CurrencyChecks','BankChecks']:
  exe=WORK/(name+'.dll');args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/(name+'.cs')));rsp=WORK/(name+'.rsp');rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
  for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
   p=subprocess.run(command,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'-'+phase+'.log')).write_text(p.stdout+p.stderr)
   if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2000:])
  results[name]=json.loads(p.stdout)
 paths=[Path(__file__),OUT/'CurrencyChecks.cs',OUT/'BankChecks.cs',ROOT/'Assets/_Original/Runtime/World/CurrencyService.cs',ROOT/'Assets/_Original/Runtime/World/BankTransferService.cs'];report=dict(results=results,unityPlay=False,mismatches=0,scope='Existing unmodified currency/bank contract harnesses compiled against current production account/service contracts. No inherited historical test count is copied into this report; groups stay separate.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
 (OUT/'bank-currency-regressions.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
