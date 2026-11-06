"""Reproduce F3's deferred null-texture initialization using production method bodies.

Component creation, RectTransform projection and Unity's destroyed-object equality
are managed boundaries. This does not run an actual Unity scene or Canvas.
"""
import hashlib,json,os,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-picture-initialization-checks')

def block(source,marker):
 start=source.index(marker);brace=source.index('{',start);depth=1;end=brace+1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]

def factory(source,name,personal=None,baseline=False):
 methods=[]
 for result,method in [('RectTransform','Group'),('void','Place'),('RawImage','Picture')]:
  match=re.search(r'private\s+static\s+'+result+r'\s+'+method+r'\(',source);assert match,method
  code=block(source,match[0])
  if baseline and method=='Picture':
   changed=re.sub(r'texture\s*!=\s*null\s*&&\s*','',code);assert changed!=code,'counterexample must remove precisely the optional-texture guard';code=changed
  methods.append(code)
 text='using UnityEngine;using UnityEngine.UI;using DarkSaver.Original.Entry;\ninternal static class '+name+'{\n'+'\n'.join(methods)+'\npublic static RawImage Create(RectTransform parent,string name,Rect rect,Texture texture,Rect? source)=>Picture(parent,name,rect,texture,source);\npublic static RectTransform CreateGroup(RectTransform parent,string name,Rect rect)=>Group(parent,name,rect);\n'
 if personal is not None:
  initialization=next(line.strip()for line in personal.splitlines()if 'p.Hired=Picture('in line)
  refresh=block(personal,'if(p.Hired!=null)')
  hiding='p.Root.gameObject.SetActive(false);';assert personal.index(hiding)<personal.index('personalWindows.Add(id,p)')
  text+='public static HiredWindow BuildHired(RectTransform parent){var p=new HiredWindow{Full=parent,Root=parent};'+hiding+initialization+'return p;}\n'
  text+='public static void PaintHired(HiredWindow p,HiredProfile profile,Texture icon){Texture CommanderIcon(int job)=>icon;'+refresh+'}\n'
 return text+'}\n'

def visibility(world,tutorial,waiting,entry,protocol,personal,chat,baseline):
 enum=block(protocol,'public enum EntryStage')
 text='using System;using UnityEngine;using UnityEngine.UI;using DarkSaver.Original.Entry;\nnamespace DarkSaver.Original.Entry{'+enum+'}\ninternal static class StartupVisibilityHost{\n'
 text+='static RectTransform Group(RectTransform parent,string name,Rect rect)=>WorldPictureHost.CreateGroup(parent,name,rect);\npublic static GameObject Construct(int stage,RectTransform screen,Action<GameObject> fill){switch(stage){\n'
 specs=[(world,'private void Build()','canvasObject',True),(tutorial,'private void Build()','canvasObject',True),(waiting,'private void BuildWaitingLesson()','waitingCanvas',True),(entry,'private void BuildLogin()','login',False),(entry,'private void BuildServers()','servers',False),(entry,'private void BuildCreation()','creation',False)]
 for index,(source,method,name,canvas)in enumerate(specs):
  body=block(source,method);m=re.search(r'\b'+name+r'\s*=\s*(?:new GameObject|Group)\([^;]+;',body);assert m,(name,method)
  hide=name+('.SetActive(false);'if canvas else'.gameObject.SetActive(false);');end=body.index(hide)+len(hide);prefix=body[m.start():end]
  assert not body[m.end():body.index(hide)].strip(),'hide before child construction, not at the end'
  if baseline:prefix=prefix.replace(hide,'')
  value=name if canvas else name+'.gameObject';text+='case '+str(index)+':{'+('GameObject 'if canvas else'RectTransform ')+name+';'+prefix+'fill?.Invoke('+value+');return '+value+';}\n'
 text+='default:throw new ArgumentOutOfRangeException(nameof(stage));}}\npublic static void Publish(int stage,GameObject value,Action paint){switch(stage){\n'
 for index,(source,method,name,paint)in enumerate([(world,'private void Begin(EntryCharacter value)','canvasObject','Paint'),(tutorial,'private void BeginBattleLesson()','canvasObject','Paint'),(waiting,'private void BeginWaitingLesson()','waitingCanvas','PaintWaitingLesson')]):
  body=block(source,method);call=paint+'();';at=body.rindex(call);show=name+'.SetActive(true);';assert show in body[at:],'publish follows successful first paint'
  if index<2:assert name+'.SetActive(false);'in body[:at],'cached canvases must also hide before preparing a new phase'
  text+='case '+str(index)+':{GameObject '+name+'=value;Action '+paint+'=paint;'+call+show+'return;}\n'
 text+='default:throw new ArgumentOutOfRangeException(nameof(stage));}}\n'
 body=block(entry,'private void ShowStage()');stage_lines=[]
 for name in ['login','servers','creation']:
  stage_lines.append(re.search(r'\b'+name+r'\.gameObject\.SetActive\([^;]+;',body)[0])
 text+='public static void ApplyEntryStage(RectTransform login,RectTransform servers,RectTransform creation,bool useLoginAndServerSelection,EntryStage Stage){'+''.join(stage_lines)+'}\n'
 personal_paint=block(personal,'private void PaintPersonalWindows()');statement='p.Root.gameObject.SetActive(visible);';assert statement in personal_paint
 text+='public static void ApplyPersonalVisibility(HiredWindow p,bool visible){'+statement+'}\n}\n'
 assert personal.index('p.Root.gameObject.SetActive(false);')<personal.index('personalWindows.Add(id,p)')
 chat_body=block(chat,'private void BuildWorldChat()');assert chat_body.index('chatPanel.gameObject.SetActive(false);')<chat_body.index('chatPanel.gameObject.AddComponent')
 return text

def main():
 WORK.mkdir(exist_ok=True);runtime=ROOT/'Assets/_Original/Runtime'
 world=runtime/'World/WorldSceneRoot.cs';tutorial=runtime/'Tutorial/TutorialSceneRoot.cs';personal=runtime/'World/WorldSceneRoot.Personal.cs';layout=runtime/'Entry/NativeEntryArt.cs';host=OUT/'PictureInitializationChecks.cs'
 waiting=runtime/'Tutorial/TutorialSceneRoot.Waiting.cs';entry=runtime/'Entry/EntrySceneRoot.cs';protocol=runtime/'Entry/EntryProtocol.cs';chat=runtime/'World/WorldSceneRoot.Chat.cs'
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';compiler=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))];env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet');results={}
 for variant in ['baseline','current']:
  baseline=variant=='baseline';folder=WORK/variant;folder.mkdir(exist_ok=True)
  a=folder/'WorldPictureHost.cs';a.write_text(factory(world.read_text(),'WorldPictureHost',personal.read_text(),baseline))
  b=folder/'TutorialPictureHost.cs';b.write_text(factory(tutorial.read_text(),'TutorialPictureHost',baseline=baseline))
  c=folder/'StartupVisibilityHost.cs';c.write_text(visibility(world.read_text(),tutorial.read_text(),waiting.read_text(),entry.read_text(),protocol.read_text(),personal.read_text(),chat.read_text(),baseline))
  exe=folder/'PictureInitializationChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe),*refs,*map(str,[host,layout,a,b,c])]
  if baseline:args.append('-define:BASELINE')
  rsp=folder/'checks.rsp';rsp.write_text('\n'.join('"'+arg+'"'for arg in args)+'\n');exe.with_suffix('.runtimeconfig.json').write_bytes(Path('/tmp/ds-original-entry-checks/EntryChecks.runtimeconfig.json').read_bytes())
  for phase,cmd in [('compile',[str(sdk/'dotnet'),str(compiler),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
   p=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True);(folder/(phase+'.log')).write_text(p.stdout+p.stderr)
   if p.returncode:raise RuntimeError(variant+' '+phase+': '+(p.stdout+p.stderr)[-2000:])
  results[variant]=json.loads(p.stdout)
 assert results['baseline']['reproducedNullExceptions']==2 and results['current']['reproducedNullExceptions']==0
 assert results['baseline']['partialUiLeaks']==6 and results['current']['partialUiLeaks']==0
 paths=[Path(__file__),host,world,tutorial,personal,layout,waiting,entry,protocol,chat];report=dict(results=results,mismatches=0,unityPlay=False,scope='Actual production method/statement extraction with managed component, texture and Unity-null boundaries. Baseline removes optional-texture and initial visibility guards to reproduce two null-UV failures and six partially visible parent stages. Current F3 creation/refresh, cropped images, construction failure/first-paint publication and real entry stage visibility statements execute. Personal/chat ordering is audited. No complete scene Build, actual Unity import/Canvas/EventSystem or rendered Play claim.',logs=str(WORK),sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
 (OUT/'picture-initialization-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))

if __name__=='__main__':main()
