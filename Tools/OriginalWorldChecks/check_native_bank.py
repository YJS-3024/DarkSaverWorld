"""Original bank protocol/input and production receipt/session lifecycle checks."""
import hashlib,json,os,subprocess,itertools,sys
from pathlib import Path
from native_bank_emulator import BankNative
from native_mercenary_window_emulator import MercenaryNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-bank-checks')
def main():
 WORK.mkdir(exist_ok=True);n=BankNative();query=n.query();assert query==dict(packets=['93'],loaded=0);balances=[];amounts=[];keys=[];controls=[]
 for value in [0,1,255,65535,2**32-1,2**32,2**63,2**64-1]:
  result=n.balance(value);assert result==dict(value=value,loaded=1);balances.append(result)
 texts=['','0','1','200','-1','+123',' 42','123suffix','wrong','\t-12tail','2147483647','-2147483648','0000000012','+','  ']
 for deposit in [False,True]:
  for text in texts:
   result=n.amount(deposit,text);assert result['closed']==[35]and len(result['packets'])==1;amounts.append(dict(deposit=deposit,text=text,packet=result['packets'][0]))
  for key in [0,13,27,65,112]:
   result=n.amount(deposit,'12',key);assert len(result['packets'])==int(key==13)and result['closed']==([35]if key in [13,27]else[]);keys.append(dict(deposit=deposit,key=key,**result))
 for control,wallet,savings,loaded in itertools.product([0,1,2,3],[0,1,2**32-1,2**32,2**32+17,2**64-1],[0,1,2**64-1],[False,True]):
  result=n.open_amount(control,wallet,savings,loaded);expected_open=loaded and(control==0 and (wallet&0xffffffff)>0 or control==1 and savings>0)
  assert bool(result['opened'])==expected_open and result['closed']==([34]if control==2 else[])
  if expected_open:assert result['deposit']==int(control==0)
  controls.append(dict(control=control,wallet=wallet,savings=savings,loaded=loaded,**result))
 labels=[];renderer=MercenaryNative()
 for wallet,savings,loaded,depth,origin in itertools.product([0,1,2**32-1,2**32+17,2**64-1],[0,1,2**32-1,2**32+23,2**64-1],[False,True],[8,16],[(0,0),(55,90)]):
  x,y=origin;labels.append(dict(wallet=wallet,savings=savings,loaded=loaded,depth=depth,x=x,y=y,expected=renderer.bank_labels(wallet,savings,loaded,x,y,depth)))
 (WORK/'cases.json').write_text(json.dumps(dict(query=query,balances=balances,amounts=amounts,keys=keys,controls=controls,labels=labels),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeBankChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeBankChecks.cs'))
 names=['UI/NativeCurrencyText.cs','UI/NativeBankClient.cs','World/LocalServerManager.Currency.cs','World/LocalServerManager.Bank.cs','Entry/LocalServerManager.NativeBank.cs']
 for p in [*[ROOT/'Assets/_Original/Runtime'/name for name in names],OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2000:])
 r=json.loads(p.stdout);r.update(nativeCodeExecuted=True,nativeQueryCases=1,nativeBalanceCases=len(balances),nativeAmountCases=len(amounts),nativeKeyCases=len(keys),nativeButtonCases=len(controls),nativeBankLabelCases=len(labels),nativeBankLabelCalls=sum(len(row["expected"])for row in labels),mismatches=0,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_bank.py',OUT/'native_bank_emulator.py',OUT/'native_mercenary_window_emulator.py',OUT/'native_shop_details_emulator.py',OUT/'NativeBankChecks.cs',OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs',*[ROOT/'Assets/_Original/Runtime'/name for name in names],ROOT/'Assets/_Original/Runtime/World/CurrencyService.cs',ROOT/'Assets/_Original/Runtime/World/BankTransferService.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Bank.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs']});(OUT/'native-bank-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
