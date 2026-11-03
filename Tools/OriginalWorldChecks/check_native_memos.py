"""Native memo decode/delete and production local-manager ownership/session integration."""
import hashlib,json,os,random,struct,subprocess,sys
from pathlib import Path
from native_memo_emulator import MemoNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-memo-checks')
def main():
 WORK.mkdir(exist_ok=True);native=MemoNative();rng=random.Random(43);records=[]
 for i in range(80):
  sender=['','A','용사','모험가','다크세이버','abcdefghijkl'][i%6].encode('cp949');date=['','26/11/02','1999-12-31','D'*31][i%4].encode('cp949');body=['','원본 메모','hello world','B'*99,'가나다라'*10+'abc'][i%5].encode('cp949')
  packet=bytes([1,i])+sender.ljust(12,b'\0')+struct.pack('<H',len(date))+date+struct.pack('<H',len(body))+body
  result=native.record(packet);assert result['cursor']==len(packet);records.append(dict(packet=packet.hex(),expected=result))
 deletes=[dict(count=n,page=page,row=row,expected=native.delete(n,page,row))for n in range(26)for page in [0,1]for row in range(10)]
 sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'));from native_pe import PE
 pe=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');literal=pe.string(0x490fb8).encode('cp949');assert literal==b'/\xb8\xde\xb8\xf0\xba\xb8\xb1\xe2';assert bytes.fromhex('c7460401000000')in pe.read(0x473b74,0x20)
 cases=dict(records=records,deletes=deletes,viewPacket=(bytes([0x7a,5,9])+literal).hex());(WORK/'cases.json').write_text(json.dumps(cases,ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'MemoChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'MemoChecks.cs'))
 for p in [ROOT/'Assets/_Original/Runtime/UI/NativeCodePage949.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoInbox.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoTransfer.cs',ROOT/'Assets/_Original/Runtime/UI/OriginalClientSettings.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Memos.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.ClientSettings.cs',OUT.resolve()/'ClientWindowManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1900:])
 r=json.loads(p.stdout);r.update(nativeRecordCases=len(records),nativeDeleteCases=len(deletes),nativeSha256=pe.sha256,nativeCodeExecuted=True,unityPlay=False,mismatches=0,logs=str(WORK),scope='Native43C690 fields/cursor and4673A0 byte packet/index removal. MFC heap/CString/array storage and final send isolated; no host network. Production memo/settings RPCs with real account services and isolated Mono lifetime host. Decompressed memo payload only;7E body decode is tested by native-memo-transfer-check.json; this report exercises inner records/delete and legacy payload/settings paths. Server mailbox persistence excluded.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_memos.py',OUT/'native_memo_emulator.py',OUT/'MemoChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeCodePage949.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoInbox.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoTransfer.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Memos.cs']});(OUT/'native-memo-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
