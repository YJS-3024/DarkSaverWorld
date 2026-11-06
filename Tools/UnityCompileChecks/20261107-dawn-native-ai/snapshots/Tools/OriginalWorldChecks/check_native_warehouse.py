"""Original warehouse wire/presentation; no inferred local inventory tokens or storage rules."""
import hashlib,json,os,subprocess,itertools
from pathlib import Path
from native_warehouse_emulator import WarehouseNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-warehouse-checks')
def main():
 WORK.mkdir(exist_ok=True);native=WarehouseNative();queries=[];moves=[];contents=[];labels=[]
 for key in [0,1,255,256,65535]:
  packet=native.query(key);assert packet==[(b'\x97'+key.to_bytes(2,'little')).hex()];queries.append(dict(key=key,packet=packet[0]))
 for deposit,key,item,token in itertools.product([False,True],[0,256,65535],[1,200,32767],[0,255,32768,65535]):
  r=native.move(deposit,key,item,token);assert len(r['packets'])==1;moves.append(dict(deposit=deposit,key=key,item=item,token=token,packet=r['packets'][0]))
 for count,fee in itertools.product([0,1,2,12,13,40,128],[0,1,2**32-1]):
  rows=[([1,200,-1,32767][i%4],(i*513)&65535)for i in range(count)];known=[1,200];r=native.contents(rows,fee,known);assert r['rows']==[dict(item=item,token=token)for item,token in rows if item in known]and r['fee']==fee and r['loaded']==1;contents.append(dict(known=known,**r))
 full=native.move(False,1,1,0,28);assert not full['packets']and len(full['messages'])==1
 close=native.close();assert close==dict(closed=[37],packets=[])
 for fee,value,origin,depth in itertools.product([False,True],[0,1,123456,2**32-1],[(0,0),(39,20),(150,88)],[8,16]):
  x,y=origin;r=native.money_label(fee,value,x,y,depth);assert r['color']==(0 if fee else 255 if depth==8 else 65535)and r['alignment']==(0 if fee else 2);labels.append(dict(fee=fee,value=value,origin=list(origin),depth=depth,**r))
 (WORK/'cases.json').write_text(json.dumps(dict(queries=queries,moves=moves,contents=contents,full=full,close=close,labels=labels),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'NativeWarehouseChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'NativeWarehouseChecks.cs'));names=['UI/NativeWarehouseClient.cs','Entry/LocalServerManager.NativeWarehouse.cs']
 for p in [*[ROOT/'Assets/_Original/Runtime'/name for name in names],OUT/'ClientWindowManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2400:])
 report=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_warehouse_emulator.py',OUT/'NativeWarehouseChecks.cs',OUT/'ClientWindowManagerHost.cs',*[ROOT/'Assets/_Original/Runtime'/name for name in names],ROOT/'Assets/_Original/Runtime/World/WorldService.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs']
 evidence=ROOT/'분석/Ghidra/Saver_Late/Warehouse/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  source=ROOT/row['asset'];assert hashlib.sha256(source.read_bytes()).hexdigest()==row['sha256'];paths.append(source)
 report.update(nativeCodeExecuted=True,nativeQueries=len(queries),nativeMoves=len(moves),nativeContents=len(contents),nativeMoneyLabelBlocks=len(labels),nativeFullBagCases=1,nativeCloseCases=1,mismatches=0,logs=str(WORK),sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths});(OUT/'native-warehouse-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
