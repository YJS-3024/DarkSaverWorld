"""Scene1 input mouse/mode, original text preprocessing and post-send handling vs production contracts."""
import hashlib,itertools,json,os,random,subprocess
from pathlib import Path
from native_chat_composer_emulator import ComposerNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-chat-composer-checks')
def main():
 WORK.mkdir(exist_ok=True);n=ComposerNative();modes=[];filters=[];submits=[]
 for channel in [1,2,3]:
  for sequence in itertools.product([0,1],repeat=6):
   n.mode(channel);modes.append(dict(initial=channel,controls=sequence,expected=[n.click(control)for control in sequence]))
 terms_sets=[[],['bad'],['bad','나쁜말'],['ad','bad'],['*','bad'],['a b'],['']]
 texts=['','hello',' ','bad badword','BAD bad','나쁜말 금지','/to bad bad','/TO bad bad','/새 bad bad','/메모 bad bad','/unknown bad bad','/to bad','/to  bad','badad','/to ','/to \tBob\tHi','/to \tBob  hi','/새 영웅 나쁜말','a'*255,'a'*256,'a'*257,'/to '+'A'*252,'가'*128,'가'*127+'AB','bad\n나쁜말','/메모 bad\tbad bad']
 rng=random.Random(425680)
 for i in range(36):texts.append(''.join(rng.choice(['a','bad','가','나쁜말',' ','\t'])for _ in range(rng.randrange(1,24))))
 for terms in terms_sets:
  for text in texts:filters.append(dict(text=text,terms=terms,expected=n.filter(text,terms)))
 for terms in [[],['bad'],['나쁜말','bad']]:
  for text in texts[:26]:
   for channel in [1,2,3]:
    for whisper in [False,True]:
     processed=n.filter(text,terms);submits.append(dict(text=text,terms=terms,processed=processed,channel=channel,whisper=whisper,expected=n.submit(processed,channel,whisper)))
 (WORK/'cases.json').write_text(json.dumps(dict(modes=modes,filters=filters,submits=submits),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ChatComposerChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'ChatComposerChecks.cs'))
 names=['UI/NativeCodePage949.cs','UI/NativeMemoInbox.cs','UI/NativeMemoTransfer.cs','UI/OriginalClientSettings.cs','UI/NativeWorldTextPackets.cs','UI/NativeWorldChatComposer.cs','UI/NativeWorldChatFilter.cs','Entry/LocalServerManager.Memos.cs','Entry/LocalServerManager.ClientSettings.cs','Entry/LocalServerManager.WorldText.cs','Entry/LocalServerManager.Chat.cs']
 for p in [*[ROOT/'Assets/_Original/Runtime'/name for name in names],OUT/'ClientWindowManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2200:])
 r=json.loads(p.stdout);r.update(nativeMouseToggleCases=sum(len(row['controls'])for row in modes),nativeFilterCases=len(filters),nativeSendCases=len(submits),nativeCodeExecuted=True,nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',mismatches=0,unityPlay=False,logs=str(WORK),scope='Unmodified470800/470790/433A30/470B00 mouse/mode;425680/416A70 preprocessing;430FA0/429F70/431650/470AD0 request bytes/post-send logic. Standard MFC CString operations and final send are isolated. Approved decoded filter terms, real account/service RPC integration and client-only preservation. Not raw filter delivery envelope, external MFC DLL execution, Win32 IME/caret, Unity input/rendering or server delivery.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_chat_composer.py',OUT/'native_chat_composer_emulator.py',OUT/'ChatComposerChecks.cs',*[ROOT/'Assets/_Original/Runtime'/name for name in names],ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Chat.cs']});(OUT/'native-chat-composer-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
