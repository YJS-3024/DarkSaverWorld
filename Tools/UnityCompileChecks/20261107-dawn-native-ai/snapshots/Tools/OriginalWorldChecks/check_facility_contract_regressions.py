"""Run existing facility harnesses directly; do not copy inherited recursive-suite counts."""
import hashlib,json,os,subprocess
from pathlib import Path
from check_native_battle_responses import contract_sources
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-facility-contract-regressions')
def main():
 WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-original-entry-checks');sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet');results={};compiled_sources={}
 names=['ShopChecks','MercenaryChecks','RevivalChecks','WarehouseChecks']
 for name in names:
  exe=WORK/(name+'.dll');args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/(name+'.cs')));rsp=WORK/(name+'.rsp');rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
  for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
   p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'-'+phase+'.log')).write_text(p.stdout+p.stderr)
   if p.returncode:raise RuntimeError(name+': '+(p.stdout+p.stderr)[-2400:])
  results[name]=json.loads(p.stdout);compiled_sources.update(contract_sources(args,__file__))
 paths=[Path(__file__),*[OUT/(n+'.cs')for n in names],*[ROOT/'Assets/_Original/Runtime/World'/n for n in ['WarehouseTransferService.cs','ShopTradeService.cs','MercenaryTransferService.cs','TempleRevivalService.cs','WorldModel.cs']],ROOT/'Assets/_Original/Runtime/Growth/CommanderRoster.cs']
 report=dict(results=results,mismatches=0,unityPlay=False,logs=str(WORK),scope='Direct current-production executions of existing shop/hire/revival/warehouse harnesses; warehouse adds remaining-use conservation cases. Explicit fixture money/fees/capacity are never game defaults. Group counts stay separate.',sources=compiled_sources|{str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
 (OUT/'facility-contract-regressions.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:{a:b for a,b in v.items()if a!='scope'}for k,v in results.items()}))
if __name__=='__main__':main()
