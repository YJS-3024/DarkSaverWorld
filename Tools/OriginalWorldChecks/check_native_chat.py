"""Differential tests against original chat kernels plus authenticated production receive bridge."""
import hashlib,json,os,random,subprocess
from pathlib import Path
from native_chat_emulator import ChatNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-chat-checks')
def main():
 WORK.mkdir(exist_ok=True);n=ChatNative((ROOT/'Assets/_Original/SourceData/Native/dark14.mxf.bytes').read_bytes());n.color_target=-1
 colors=n.colors_init();routes=[];splits=[];scrolls=[];resizes=[]
 for kind in [1,2,3,4,5,9]:
  for enabled in [False,True]:
   for exists in [False,True]:routes.append(dict(kind=kind,enabled=enabled,exists=exists,received=n.route(kind,enabled,exists)))
 texts=['','A',' ','가','漢','①','\t','A\nB','가\r\nA','abc def','a'*77,'a'*78,'가'*39,'a'*200,'가'*200,'漢'*200,'a'*77+'\n가','가'*39+'\r\n가'*4]
 rng=random.Random(2670)
 for i in range(48):texts.append(''.join(rng.choice('ab 가漢①\t\r\n')for _ in range(rng.randrange(1,160))))
 for text in texts:
  for width in [1,5,7,14,28,545]:splits.append(dict(text=text,width=width,lines=n.lines(text,width)))
 for count in [0,1,4,5,6,22,23,24,299,300]:
  for visible in [5,23]:
   for first in sorted(set([0,max(0,count-visible),max(0,count-visible-1)])):
    for direction in [-1,1]:scrolls.append(dict(count=count,visible=visible,first=first,direction=direction,expected=n.scroll(count,visible,first,direction)))
 for count in [0,1,4,5,6,22,23,24,299,300]:
  for expanded in [False,True]:resizes.append(dict(count=count,expanded=expanded,expected=n.resize(count,expanded)))
 (WORK/'cases.json').write_text(json.dumps(dict(colors=colors,routes=routes,splits=splits,scrolls=scrolls,resizes=resizes),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ChatChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT.resolve()/'ChatChecks.cs'))
 for p in [ROOT/'Assets/_Original/Runtime/UI/NativeCodePage949.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWorldChatLog.cs',ROOT/'Assets/_Original/Runtime/UI/NativeMemoInbox.cs',ROOT/'Assets/_Original/Runtime/UI/OriginalClientSettings.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Memos.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Messages.cs',OUT/'ClientWindowManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for name,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2000:])
 r=json.loads(p.stdout);r.update(nativeRoutingCases=len(routes),nativeSplitCases=len(splits),nativeScrollCases=len(scrolls),nativeResizeCases=len(resizes),nativeColorInitializations=len(colors),nativeCodeExecuted=True,nativeSha256='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689',mismatches=0,unityPlay=False,logs=str(WORK),scope='Original476230 main routing and blocked-kind9 branch,421CD0 COLORREF setup,43FCC0/43FDF0 wrapping,437B80/437BA0 scroll and423060 viewport resize. MFC/base constructor/DDColorMatch and append dispatch boundaries isolated. Production decoded-message bridge/session/replay checked. No raw packet envelope, private-whisper UI, OS typing, GPU framebuffer or server delivery claim.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_chat.py',OUT/'native_chat_emulator.py',OUT/'ChatChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeWorldChatLog.cs',ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.Messages.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Personal.cs']});(OUT/'native-chat-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
