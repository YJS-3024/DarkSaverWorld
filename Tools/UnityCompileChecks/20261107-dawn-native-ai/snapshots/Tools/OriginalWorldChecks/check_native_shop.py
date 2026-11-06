"""Direct ordinary-shop drag wire versus separate house-purchase confirmation evidence."""
import hashlib,json,os,subprocess,itertools
from pathlib import Path
from native_shop_emulator import ShopNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-shop-checks')
def main():
 WORK.mkdir(exist_ok=True);native=ShopNative();packets=[];allow=[]
 for item in [0,1,99,100,199,200,299,300,399,718,32767]:
  r=native.drop(True,item);assert len(r['packets'])==1 and not r['closed'];packets.append(dict(buy=True,item=item,token=0,packet=r['packets'][0]))
 for base,item,token in itertools.product([0,100,200,300],[0,1,99],[0,255,32768,65535]):
  item+=base;r=native.drop(False,item,token,base);assert len(r['packets'])==1 and not r['closed'];packets.append(dict(buy=False,item=item,token=token,packet=r['packets'][0]))
 for base,item in itertools.product([0,100,200,300],[-1,0,1,99,100,199,200,299,300,399,400,718]):allow.append(dict(base=base,item=item,allowed=native.allows(item,base)))
 full=native.drop(True,1,count=28);assert not full['packets']and full['messages']==['가지고 있는게 너무 많아 살 수 없습니다.']
 wrong=native.drop(False,100,0,0);assert not wrong['packets']and wrong['messages']==['여기서 팔 수 있는 물건이 아닙니다.']
 (WORK/'cases.json').write_text(json.dumps(dict(packets=packets,allow=allow,full=full,wrong=wrong),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeShopChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeShopChecks.cs'));names=['UI/NativeItemDragContext.cs','UI/NativeWarehouseClient.cs','UI/NativeShopClient.cs','UI/NativeFacilityWindows.cs','Entry/LocalServerManager.NativeShop.cs','World/LocalServerManager.Shop.cs']
 for path in [*[ROOT/'Assets/_Original/Runtime'/n for n in names],OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs']:args+='\n"'+str(path)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2400:])
 report=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_shop_emulator.py',OUT/'NativeShopChecks.cs',OUT/'ClientWindowManagerHost.cs',OUT/'IdentityManagerHost.cs',*[ROOT/'Assets/_Original/Runtime'/n for n in names],ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs'];evidence=ROOT/'분석/Ghidra/Saver_Late/Shop/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];paths.append(path)
 report.update(nativeCodeExecuted=True,nativeBuyCases=11,nativeSaleCases=len(packets)-11,nativeCategoryCases=len(allow),nativeFullBagCases=1,nativeRejectedSaleCases=1,mismatches=0,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths});(OUT/'native-shop-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
