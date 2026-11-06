"""Original candidate parser and DCL data versus current cache/filter notification path."""
import hashlib,json,os,struct,subprocess,tempfile,zlib
from pathlib import Path
from native_candidate_emulator import CandidateNative,CandidateEnvelopeNative,CandidateWindowNative
from native_memo_transfer_emulator import DclWriter,TransferNative,SHA
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-candidate-checks')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True);parser=CandidateNative();dcl=TransferNative();receiver=CandidateEnvelopeNative();original=ROOT/'Assets/_Original/SourceData/Native/candid.dat.bytes';text=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/candid.txt.txt';raw=text.read_bytes().decode('utf-8-sig').encode('cp949');parsers=[dict(payload=raw.hex(),expected=parser.parse(raw))]
 for terms in [[],['bad'],[''],['가','나쁜말'],['bad','\r\nnext'],['x y','\tinside'],['99999x']]:
  for words in [[],['TWME1'],['first','가나다','A'*255]]:
   payload=b'V0.06\r\n'+b','.join(t.encode('cp949')for t in terms)+(b','if terms else b'')+b'99999,\r\n'+str(len(words)).encode()+b'\r\n'+b'\r\n'.join(v.encode('cp949')for v in words)+b'\r\n';parsers.append(dict(payload=payload.hex(),expected=parser.parse(payload)))
 for count in ['-1','-2147483648','+0','0junk']:
  payload=b'1234567bad,99999,\r\n'+count.encode()+b'\r\n';parsers.append(dict(payload=payload.hex(),expected=parser.parse(payload)))
 unsigned=[]
 for size in [32768,40000,65535]:
  prefix=b'V0.06\r\n';tail=b',99999,\r\n0\r\n';count=size-len(prefix)-len(tail);writer=DclWriter(1,6);writer.literal(prefix+b'A');remaining=count-1
  while remaining>1:
   n=min(remaining,518);writer.match(n,1);remaining-=n
  if remaining:writer.literal(b'A')
  writer.literal(tail);compressed,plain=writer.finish();assert len(plain)==size;expected=dcl.decode(compressed,size);assert expected['result']==0 and expected['overflow']==0 and expected['payload']==plain.hex();body=struct.pack('<HHI',len(compressed),size,(~zlib.crc32(plain))&0xffffffff)+compressed;unsigned.append(dict(body=body.hex(),native=expected))
 protocol=[]
 for blob in [original.read_bytes(),None,bytes.fromhex(unsigned[0]['body'])]:
  if blob is None:
   w=DclWriter();w.literal(b'V0.06\r\nbad,99999,\r\n1\r\nUser\r\n');packed,plain=w.finish();blob=struct.pack('<HHI',len(packed),len(plain),(~zlib.crc32(plain))&0xffffffff)+packed
  for opcode in [0x12,0x13]:
   packet=bytes([0x11,opcode])+(blob if opcode==0x13 else b'');protocol.append(dict(packet=packet.hex(),cache=blob.hex(),expected=receiver.receive(packet,blob)))
 for fault in ['crc','short-output','long-output','mode','dictionary']:
  blob=bytearray(original.read_bytes())
  if fault=='crc':blob[4]^=1
  elif fault=='short-output':struct.pack_into('<H',blob,2,1172)
  elif fault=='long-output':struct.pack_into('<H',blob,2,1174)
  elif fault=='mode':blob[8]=2
  else:blob[9]=7
  for opcode in [0x12,0x13]:
   packet=bytes([0x11,opcode])+(bytes(blob) if opcode==0x13 else b'');result=receiver.receive(packet,bytes(blob));assert not result['accepted'] and not result['written'];protocol.append(dict(packet=packet.hex(),cache=bytes(blob).hex(),expected=result))
 windowChecks=[];window=CandidateWindowNative()
 for classes in [[],['TWME1'],['twme1'],['TWME1','TWME1'],['Class','다크세이버'],['A'*254],['A'*255]]:
  for name in ['', 'TWME1','twme1','TWME1extra','Class','다크세이버','A'*254,'A'*255]:
   captured=name.encode('cp949')[:254].decode('cp949');windowChecks.append(dict(classes=classes,captured=captured,expected=window.inspect(classes,name)))
 invalid=[b'',b'1234567',b'1234567bad',b'1234567bad,99999,',b'1234567bad,99999,\r\n',b'1234567bad,99999,\r\n+',b'1234567bad,99999,\r\n2147483648',b'1234567bad,99999,\r\n2\r\none\r\n',b'1234567bad,99999,\r\n1\r\n'+b'A'*256,b'1234567\x81,99999,\r\n0\r\n',b'1234567bad,99999,\r\n1\r\n\x81']
 (WORK/'cases.json').write_text(json.dumps(dict(parsers=parsers,protocol=protocol,windowChecks=windowChecks,unsigned=unsigned,invalid=[b.hex()for b in invalid],original=original.read_bytes().hex()),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'CandidateChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'CandidateChecks.cs'))+'\n-main:CandidateChecks\n'
 paths=[OUT/'check_native_candidate_data.py',OUT/'native_candidate_emulator.py',OUT/'native_chat_composer_emulator.py',OUT/'native_memo_transfer_emulator.py',OUT/'CandidateChecks.cs',OUT/'MemoChecks.cs',original,text]
 for p in [ROOT/'Assets/_Original/Runtime/UI'/n for n in ['NativeCodePage949.cs','NativeMemoInbox.cs','NativeMemoTransfer.cs','OriginalClientSettings.cs','NativeCandidateData.cs','NativeCandidateCacheFile.cs','NativeWorldChatFilter.cs','NativeWorldChatComposer.cs','NativeWorldTextPackets.cs']]+[ROOT/'Assets/_Original/Runtime/Entry'/n for n in ['LocalServerManager.Memos.cs','LocalServerManager.ClientSettings.cs','LocalServerManager.Chat.cs','LocalServerManager.WorldText.cs','LocalServerManager.Candidates.cs']]+[OUT.resolve()/'ClientWindowManagerHost.cs',OUT.resolve()/'MemoChecks.cs']:
  args+='\n"'+str(p)+'"\n';paths.append(p)
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 with tempfile.TemporaryDirectory(prefix='cache-',dir=WORK)as directory:
  for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json'),directory])]:
   p=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
   if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2200:])
 result=json.loads(p.stdout);evidence=ROOT/'분석/Ghidra/Saver_Late/ChatTermsCache/evidence.json';paths.append(evidence)
 for row in json.loads(evidence.read_text())['files']:
  path=ROOT/row['asset'];assert sha(path)==row['sha256'];paths.append(path)
 art=OUT/'native-candidate-cache-art.json';data=json.loads(art.read_text());assert sha(ROOT/data['resource'])==data['sha256'];paths.extend([art,ROOT/data['resource'],Path(str(ROOT/data['resource'])+'.meta'),ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs']);paths.extend(ROOT/n for n in data['sources'])
 result.update(nativeParserCases=len(parsers),nativeReceiverCases=len(protocol),nativeWindowClassCases=len(windowChecks),nativeUnsignedDclCases=len(unsigned),nativeSha256=SHA,nativeCodeExecuted=True,unityPlay=False,logs=str(WORK),scope='Actual415BA0 logical11 subtype12 cache-read/13 CRC/decode/parser/cache-write gates with in-memory CFile boundaries. Actual4161E0 parser executes comma/sentinel/scanf pointer increments with MFC CString/array storage and CRT scan primitives isolated. Actual478930/CRC decode unsigned32768..65535 cache data. Production approved11/12 and11/13 world-client metadata, duplicate lists/byte CRLF, filtering versus original+188 window-class checks, ownership/session/replay/reentrant persistence, isolated Linux atomic cache file operations; actual416430 class-name matching/posts captured with isolated Win32 primitives; no OS enumeration or original CFile failure/Windows parser-heap overflow, pre-login handshake, OS network or Unity Play claim.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in paths});(OUT/'native-candidate-data-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
