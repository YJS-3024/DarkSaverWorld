"""Native7E handler/DCL/CRC differential and real local-manager session integration."""
import hashlib,json,os,struct,subprocess,zlib
from pathlib import Path
from native_memo_transfer_emulator import DclWriter,TransferNative,EnvelopeNative,envelope,tables,SHA
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-memo-transfer-checks')
def main():
 WORK.mkdir(exist_ok=True);native=TransferNative();outer=EnvelopeNative();valid=[];gates=[]
 memo=bytes([1,7])+'용사'.encode('cp949').ljust(12,b'\0')+struct.pack('<H',8)+b'26/11/03'+struct.pack('<H',4)+'메모'.encode('cp949')
 def add(writer,gate=False):
  compressed,plain=writer.finish();assert 0<len(plain)<=32767 and len(compressed)<=8192
  expected=native.decode(compressed,len(plain));assert expected['result']==0 and expected['overflow']==0 and expected['payload']==plain.hex() and expected['crc']==(~zlib.crc32(plain)&0xffffffff)
  body=envelope(compressed,plain);valid.append(dict(body=body.hex(),plain=plain.hex(),native=expected))
  if gate:gates.append(dict(body=body.hex(),expected=outer.body(body)))
  return body
 for coded in [0,1]:
  for dictionary in [4,5,6]:
   w=DclWriter(coded,dictionary);w.literal(memo);add(w,True)
   w=DclWriter(coded,dictionary);w.literal(bytes(range(256)));w.match(2,4);w.match(258,256);w.match(518,1);add(w)
   w=DclWriter(coded,dictionary);w.literal(b'A');remaining=32766
   while remaining>1:
    n=min(remaining,518);w.match(n,1);remaining-=n
   if remaining:w.literal(b'A')
   add(w,True)
   w=DclWriter(coded,dictionary)
   # All dictionary distance symbols, including the final native output-block boundary.
   prefix=bytes(range(256))*(1<<(dictionary-2))
   w.literal(prefix);w.match(3,len(prefix));add(w,True)
   w=DclWriter(coded,dictionary);w.literal(prefix)
   for symbol in range(64):w.match(3,(symbol<<dictionary)+1)
   for base,extra in zip(w.t['base'],w.t['extra']):
    for size in sorted({base+2,min(518,base+(1<<extra)+1)}):w.match(size,1)
   add(w,True)
 # Declared compressed padding is ignored after native EOS; body framing remains exact.
 w=DclWriter();w.literal(memo);comp,plain=w.finish();padded=comp+bytes(8192-len(comp));expected=native.decode(padded,len(plain));assert expected['result']==0 and expected['payload']==plain.hex()
 body=envelope(padded,plain);valid.append(dict(body=body.hex(),plain=plain.hex(),native=expected));gates.append(dict(body=body.hex(),expected=outer.body(body)))
 sample=bytes.fromhex(valid[0]['body']);compressed=sample[8:]
 for mutation in ['crc','short-output','long-output','mode','small-dictionary','large-dictionary','truncated-stream']:
  body=bytearray(sample)
  if mutation=='crc':body[4]^=1
  elif mutation=='short-output':struct.pack_into('<h',body,2,len(memo)-1)
  elif mutation=='long-output':struct.pack_into('<h',body,2,len(memo)+1)
  elif mutation=='mode':body[8]=2
  elif mutation=='small-dictionary':body[9]=3
  elif mutation=='large-dictionary':body[9]=7
  else:body=bytearray(envelope(compressed[:3],memo))
  result=outer.body(body);assert not result['accepted'];gates.append(dict(body=body.hex(),expected=result))
 candid=ROOT/'Assets/_Original/SourceData/Native/candid.dat.bytes';candidText=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/candid.txt.txt';original=candid.read_bytes();plain=candidText.read_bytes().decode('utf-8-sig').encode('cp949');compressedCount,decodedCount,checksum=struct.unpack('<HHI',original[:8]);assert len(original)==8+compressedCount and len(plain)==decodedCount
 expected=native.decode(original[8:],decodedCount);assert expected['result']==0 and expected['overflow']==0 and expected['payload']==plain.hex() and expected['crc']==checksum
 valid.append(dict(body=original.hex(),plain=plain.hex(),native=expected)) # Actual original compressed bytes; not a memo batch or an invented server notice.
 bad=DclWriter();bad.literal(b'\x02\0');badcomp,badplain=bad.finish()
 cases=dict(valid=valid,outer=gates,badMemo=envelope(badcomp,badplain).hex());(WORK/'cases.json').write_text(json.dumps(cases));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'MemoTransferChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'MemoTransferChecks.cs'))+'\n-main:MemoTransferChecks\n'
 sources=[candid,candidText,OUT/'check_native_memo_transfer.py',OUT/'native_memo_transfer_emulator.py',OUT/'MemoTransferChecks.cs',OUT/'MemoChecks.cs',OUT/'ClientWindowManagerHost.cs']
 for p in [ROOT/'Assets/_Original/Runtime/UI'/name for name in ['NativeCodePage949.cs','NativeMemoInbox.cs','NativeMemoTransfer.cs','OriginalClientSettings.cs']]+[ROOT/'Assets/_Original/Runtime/Entry'/name for name in ['LocalServerManager.Memos.cs','LocalServerManager.ClientSettings.cs']]+[OUT.resolve()/'ClientWindowManagerHost.cs',OUT.resolve()/'MemoChecks.cs']:
  args+='\n"'+str(p)+'"\n';sources.append(p)
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2300:])
 evidence=ROOT/'분석/Ghidra/Saver_Late/MemoTransfer/evidence.json';static=json.loads(evidence.read_text());sources.append(evidence)
 for row in static['files']:
  q=ROOT/row['asset'];assert hashlib.sha256(q.read_bytes()).hexdigest()==row['sha256'];sources.append(q)
 assert json.loads((evidence.parent/'dcl-tables.json').read_text())==tables()
 r=json.loads(p.stdout);r.update(actualOriginalCompressedFixtures=1,nativeDclCases=len(valid),nativeBodyCases=len(gates),nativeSha256=SHA,nativeCodeExecuted=True,mismatches=0,unityPlay=False,logs=str(WORK),scope='Actual original Candid.Dat compressed fixture also matches preserved CP949 source; it is decoder input only, never a memo batch or notification. Actual478930 plus43B2F0/43B390/477E10 callbacks; actual427D30 decode/count/CRC/overflow gates. Heap allocation and inner memo construction/storage isolated; inner record parsing covered separately. Complete7E body after opcode demultiplexing; no TCP framing, multipart assembly, server mailbox persistence or Unity Play. Managed unsafe signed sizes/trailing bytes rejected more strictly than native. Real account/local-manager session/replay/logout and atomic inbox tests use isolated stores.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in sources});(OUT/'native-memo-transfer-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
