"""All1212 current table items versus original44B1C0; native stored-line splitting."""
import hashlib,itertools,json,os,random,struct,subprocess
from pathlib import Path
from native_shop_details_emulator import ShopDetailsNative,item_record
from build_native_shop_details import integer,rows
from native_font_emulator import Font
from native_shop_selection_emulator import ShopSelectionNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-shop-details-checks')
def main():
 WORK.mkdir(exist_ok=True);native=ShopDetailsNative();cases=[];items=rows()
 for i,row in enumerate(items):
  level=[0,1,10,99,32767][i%5];kind=i%10;flags=['보','비','기','수','창','궁','승','신','마','사'];item=dict(id=int(row['번호']),name=row['이름'].replace('_',' '),description=row['설명'].replace('_',' '),price=integer(row['가격']),requiredLevel=integer(row['장착렙']),jobKinds=[k for k,f in enumerate(flags)if integer(row[f])==1])
  cases.append(dict(item=item,level=level,kind=kind,expected=native.describe(item_record(row),level,kind,owned=bool(i%2))))
 # Boundary predicate: the original requires flag exactly1 and signed-short level.
 for flag in [0,1,2,255]:
  for level in [-32768,-1,0,1,32767]:
   record=bytearray(660);struct.pack_into('<Q',record,40,2**63-1);record[24]=flag
   item=dict(id=-1,name='',description='',price=2**63-1,requiredLevel=0,jobKinds=[0]if flag==1 else[])
   cases.append(dict(item=item,level=level,kind=0,expected=native.describe(record,level,0)))
 font=Font((ROOT/'Assets/_Original/SourceData/Native/dark14.mxf.bytes').read_bytes());wrap=[]
 texts=['A'*n for n in [0,1,54,55,56,57,100,120]]+['가'*n for n in [27,28,29,55]]+['가 A 漢ㄱ '*40,' '*120,'a_b %d '*25]
 texts +=[part for row in cases[::47]for part in row['expected']]
 for text in texts:
  remaining=text;lines=[]
  while font.measure(remaining)>388:
   font.uc.mem_write(0x0d065000,bytes(4096));font.uc.mem_write(0x0d067000,bytes(4096));font.uc.mem_write(font.text,remaining.encode('cp949')+b'\0');font.run(0x43fdf0,[font.text,0x0d065000,0x0d067000,388],font.obj)
   read=lambda address:bytes(font.uc.mem_read(address,4096)).split(b'\0')[0].decode('cp949');head,remaining=read(0x0d065000),read(0x0d067000);assert head;lines.append(head)
  lines.append(remaining);wrap.append(dict(text=text,expected='\n'.join(lines)))
 selection=[];selector=ShopSelectionNative()
 for right,index,offset,origin in itertools.product([False,True],[-1,0,11,12,15,27,28,40],[0,3,12],[(0,0),(35,50)]):
  x,y=origin;selection.append(dict(right=right,index=index,offset=offset,x=x,y=y,expected=selector.outline(right,index,offset,x,y)))
 for index in [0,13]:selection.append(dict(right=False,index=index,offset=0,x=0,y=0,expected=selector.outline(False,index,empty=True)))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(items=cases,wrap=wrap,selection=selection),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ShopDetailsChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'ShopDetailsChecks.cs'))
 for name in ['NativeShopDetails.cs','NativeTextMetrics.cs']:args+='\n"'+str(ROOT/'Assets/_Original/Runtime/UI'/name)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1900:])
 result=json.loads(p.stdout);paths=[Path(__file__),OUT/'native_shop_details_emulator.py',OUT/'native_shop_selection_emulator.py',OUT/'native_mercenary_window_emulator.py',OUT/'ShopDetailsChecks.cs',OUT/'build_native_shop_details.py',OUT/'native-shop-details-art.json',OUT/'native_font_emulator.py',ROOT/'Assets/_Original/Runtime/UI/NativeShopDetails.cs',ROOT/'Assets/_Original/Runtime/UI/NativeTextMetrics.cs',ROOT/'Assets/_Original/Runtime/UI/NativeItemPress.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Facilities.cs',ROOT/'Assets/_Original/Resources/OriginalShopDetails.json',ROOT/'Assets/_Original/Data/Tables/items.csv']
 evidence=ROOT/'분석/Ghidra/Saver_Late/ShopDetails/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];paths.append(path)
 result.update(nativeCodeExecuted=True,nativeItems=len(items),nativeDescriptionCases=len(cases),nativeStoredParagraphCases=len(wrap),nativeSelectionCases=len(selection),logs=str(WORK),scope='Actual44B1C0 complete description through captured MFC/table/list boundaries and43FDF0 stored-line splitter. All1212 existing converted original item rows projected into native records, flag==1 and signed level boundaries. Runtime text/predicate/wrapping/resource parity; no stock/price approval/server economy changes. 44AA70 rectangles plus actual MFC CDC+2C SelectStockObject execute with GDI rectangle/objects as boundaries, confirming NULL_BRUSH5 and before-sprite33px outline. Full native loader, palette/GDI raster/DirectDraw/Unity pixels, OS pointer/drag not executed.',sources={str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest()for path in paths});(OUT/'native-shop-details-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
