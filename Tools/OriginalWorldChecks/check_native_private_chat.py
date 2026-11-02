"""Private-chat decoded routing, native reply bytes and authenticated client boundaries."""
import hashlib,json,os,random,subprocess
from pathlib import Path
from native_private_chat_emulator import PrivateChatNative
from native_private_render_emulator import PrivateRenderNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-private-chat-checks')
def main():
 WORK.mkdir(exist_ok=True);native=PrivateChatNative();routes=[];filters=[];replies=[];renders=[];creates=[];renderer=PrivateRenderNative();rng=random.Random(422500)
 names=['Hero        ','hero        ','영웅        ','abcdefghijkl','            ']
 for sender in names:
  for existing in [[],[sender],[name for name in names[:3]if name!=sender]]:
   for body in ['> hello','< 안녕','']:
    for suppression in [0,1,-1]:
     text=sender+body;routes.append(dict(text=text,senders=existing,suppression=suppression,expected=native.receive(text,existing,suppression)))
 for terms in [[],['bad'],['bad','나쁜'],['b','**']]:
  for text in ['','bad','bad bad','/to bad bad','/unknown bad','/메모 bad bad','나쁜 bad','badX','BAD',' < bad']:
   filters.append(dict(text=text,terms=terms,expected=native.private_filter(text,terms)))
 for sender in names:
  for text in ['','hello','bad','/unknown bad','나쁜 bad','x'*60]:
   for terms in [[],['bad','나쁜']]:
    for whisper in [False,True]:
     processed=native.private_filter(text,terms);replies.append(dict(sender=sender,text=text,terms=terms,whisper=whisper,processed=processed,expected=native.reply(sender,processed,whisper)))
 for sequence in range(45):
  rows=[rng.choice(['>hello','<안녕','continued','','가나다','plain'])for _ in range(rng.randrange(1,25))]
  for first in sorted(set([0,1,2,3,max(0,len(rows)-6),len(rows)-1])):
   for depth in [8,16]:renders.append(dict(rows=rows,first=first,depth=depth,expected=renderer.render(rows,first,depth,residue=0)))
 residue=[dict(marker=value,draws=renderer.render(['plain','continued'],0,16,residue=value))for value in [0,ord('>'),ord('<')]]
 for count in [0,1,3,17,20]:
  for room in [0,1000,2500,65535]:creates.append(dict(count=count,room=room,expected=native.create('New         ','>hello',room,count)))
 fixture=WORK/'cases.json';fixture.write_text(json.dumps(dict(routes=routes,filters=filters,replies=replies,renders=renders,creates=creates),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'PrivateChatChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'PrivateChatChecks.cs'))
 for path in ['Runtime/UI/NativeCodePage949.cs','Runtime/UI/NativeWorldChatLog.cs','Runtime/UI/NativeWorldChatFilter.cs','Runtime/UI/NativeWorldChatComposer.cs','Runtime/UI/NativeWorldTextPackets.cs','Runtime/UI/NativePrivateChat.cs','Runtime/UI/NativeMemoInbox.cs','Runtime/UI/OriginalClientSettings.cs','Runtime/Entry/LocalServerManager.Memos.cs','Runtime/Entry/LocalServerManager.ClientSettings.cs','Runtime/Entry/LocalServerManager.Chat.cs','Runtime/Entry/LocalServerManager.WorldText.cs','Runtime/Entry/LocalServerManager.PrivateChat.cs']:args+='\n"'+str(ROOT/'Assets/_Original'/path)+'"\n'
 args+='\n"'+str(OUT/'ClientWindowManagerHost.cs')+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(fixture)])]:
  result=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(result.stdout+result.stderr)
  if result.returncode:raise RuntimeError((result.stdout+result.stderr)[-1800:])
 report=json.loads(result.stdout);report.update(nativeCodeExecuted=True,nativeRoutingCases=len(routes),nativeFilterCases=len(filters),nativeReplyCases=len(replies),nativeRenderCases=len(renders),nativeCreateCases=len(creates),nativeTextDrawCalls=sum(len(row['expected'])for row in renders),uninitializedMarkerInputs=residue,nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',scope='Actual476230 private routing,477520 new-window gate/array registration/cascade,4537A0/453930 reply/order,425680 mode2 and422500 lookback/row-clip/color/shadow call ordering. MFC/Edit/printf/List/allocator and final surface/font calls isolated; window constructor/init boundary supplies an available UI. Renderer marker stack input explicitly0; when no marker exists in prior2rows, source leaves it uninitialized. Extra0/>/< residue cases demonstrate that difference; managed adapter deliberately chooses cyan for that undefined case. Title16bit literal and EM_LIMITTEXT(60) are static findings. No raw delivery,8bit palette/IME/physical input, UI creation failure equality, complete framebuffer or Unity Play.',logs=str(WORK),sources={str(path.relative_to(ROOT)):hashlib.sha256(path.read_bytes()).hexdigest()for path in [OUT/'check_native_private_chat.py',OUT/'native_private_chat_emulator.py',OUT/'native_private_render_emulator.py',OUT/'native_chat_composer_emulator.py',OUT/'PrivateChatChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativePrivateChat.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWorldChatLog.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWorldChatFilter.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.PrivateChat.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.PrivateChat.cs']});(OUT/'native-private-chat-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','uninitializedMarkerInputs']}))
if __name__=='__main__':main()
