"""Ensure current callback regressions reject three pinned pre-fix implementations.
Run the three focused checks first: their successful current managed fixtures and
response files are prerequisites. Nothing under Assets, real saves or Git index
is modified. A failed old implementation is a deliberate negative control.
"""
import hashlib,json,subprocess,os
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-callback-session-controls');BASE='88587912'
ROWS=[('chat','/tmp/ds-native-chat-composer-checks','ChatComposerChecks','LocalServerManager.Chat.cs','native-chat-composer-check.json','connection changes cancel remaining chat callbacks'),('private','/tmp/ds-native-private-chat-checks','PrivateChatChecks','LocalServerManager.PrivateChat.cs','native-private-chat-check.json','closed/private or expired-session request'),('space','/tmp/ds-native-world-text-checks','WorldTextChecks','LocalServerManager.WorldText.cs','native-world-text-check.json','logout/reconnect stops stale space-move')]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True);sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet',DOTNET_DbgEnableMiniDump='0');sources=[Path(__file__)]
 for kind,folder,exe,source,report,label in ROWS:
  p=OUT/report;r=json.loads(p.read_text());assert r.get('mismatches',0)==0
  for name,digest in r['sources'].items():assert sha(ROOT/name)==digest,'Run focused current check first: '+report
  sources.extend([p,OUT/(exe+'.cs'),ROOT/'Assets/_Original/Runtime/Entry'/source])
 sources.append(ROOT/'Assets/_Original/Runtime/Entry/LocalServerManager.ClientSettings.cs')
 def run(row):
  kind,folder,exeName,source,report,label=row;old=Path(folder);dest=WORK/kind;dest.mkdir(exist_ok=True);saved=dest/source
  saved.write_bytes(subprocess.check_output(['git','show',BASE+':Assets/_Original/Runtime/Entry/'+source],cwd=ROOT));exe=dest/(exeName+'.dll');rsp=(old/'checks.rsp').read_text().replace(str(old/(exeName+'.dll')),str(exe)).replace(str(ROOT/'Assets/_Original/Runtime/Entry'/source),str(saved));(dest/'checks.rsp').write_text(rsp);exe.with_suffix('.runtimeconfig.json').write_bytes((old/(exeName+'.runtimeconfig.json')).read_bytes())
  p=subprocess.run([str(sdk/'dotnet'),str(csc),'@'+str(dest/'checks.rsp')],cwd=ROOT,env=env,capture_output=True,text=True);(dest/'compile.log').write_text(p.stdout+p.stderr);assert p.returncode==0,p.stdout[-1200:]
  p=subprocess.run([str(sdk/'dotnet'),str(exe),str(ROOT),str(old/'cases.json')],cwd=ROOT,env=env,capture_output=True,text=True);(dest/'run.log').write_text(p.stdout+p.stderr);assert p.returncode!=0 and label in p.stderr,p.stdout+p.stderr
  failure=next(line.strip()for line in p.stderr.splitlines()if 'Exception:'in line);return dict(control=kind,base=BASE,source=source,baseSourceSha256=sha(saved),compiled=True,rejectedByCurrentRegression=True,failure=failure)
 with ThreadPoolExecutor(max_workers=3)as pool:results=list(pool.map(run,ROWS))
 r=dict(controls=len(results),mismatches=0,unityPlay=False,nativeCodeExecuted=False,cases=results,logs=str(WORK),scope='Three current focused managed fixtures reject the pinned pre-fix callback bodies, each replaced separately. This proves regression sensitivity, not original native behavior, real network timing or Unity EventSystem/Play.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in sources});(OUT/'callback-session-regressions.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
