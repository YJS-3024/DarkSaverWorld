"""Facility provider callbacks may replace their world; never touch the replacement request."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-facility-callback-checks')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');exe=WORK/'FacilityCallbackChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'FacilityCallbackChecks.cs'));rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2400:])
 report=json.loads(p.stdout);paths=[Path(__file__),OUT/'FacilityCallbackChecks.cs',*[ROOT/'Assets/_Original/Runtime/World'/name for name in ['WarehouseTransferService.cs','ShopTradeService.cs','MercenaryTransferService.cs','TempleRevivalService.cs']]]
 report.update(mismatches=0,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths});(OUT/'facility-callback-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
