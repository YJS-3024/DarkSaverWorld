"""Compile source-backed map clients and exercise production managed campaign state."""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';WORK=Path('/tmp/ds-original-campaign-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')
def main():
 WORK.mkdir(exist_ok=True)
 regression=subprocess.run([sys.executable,str(Path(__file__).with_name('check_world.py'))],cwd=ROOT,capture_output=True,text=True);(WORK/'world-regression.log').write_text(regression.stdout+regression.stderr)
 if regression.returncode:raise RuntimeError((regression.stdout+regression.stderr)[-2000:])
 sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
 sources=[BASE/'Runtime/Entry'/p for p in ['EntryProtocol.cs','EntryAccountService.cs','EntryClient.cs','LocalAccountFileStore.cs']]
 sources += [BASE/'Runtime/Tutorial'/p for p in ['TutorialScript.cs','TutorialSession.cs','TutorialService.cs','WaitingLesson.cs']]
 sources += [BASE/'Runtime/World'/p for p in ['WorldModel.cs','WorldService.cs']]
 sources += [BASE/'Runtime/Campaign'/p for p in ['CampaignModel.cs','CampaignService.cs']]+[BASE/'Runtime/Maps/NativeMapDecoder.cs',Path(__file__).with_name('CampaignChecks.cs')]
 refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
 exe=WORK/'CampaignChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+list(map(str,sources));rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args)+'\n')
 run=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,capture_output=True,text=True);(WORK/'compile.log').write_text(run.stdout+run.stderr)
 if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-1800:])
 version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name;exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 run=subprocess.run([str(dotnet),str(exe),str(ROOT)],cwd=ROOT,capture_output=True,text=True);(WORK/'run.log').write_text(run.stdout+run.stderr)
 if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-1800:])
 result=json.loads(run.stdout);evidence=json.loads(Path(__file__).with_name('campaign-assets.json').read_text())
 for path,digest in evidence['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path
 for item in evidence['outputs']:assert hashlib.sha256((ROOT/item['asset']).read_bytes()).hexdigest()==item['sha256'],item['asset']
 assert not evidence['connections']
 old=json.loads(Path(__file__).with_name('world-check.json').read_text());result.update(runtimeApiErrors=old['runtimeApiErrors'],editorApiErrors=old['editorApiErrors'],playerApiErrors=old['playerApiErrors'],warnings=old['warnings'],worldRegression=old['assertions'],tutorialRegression=old['tutorialRegression'],entryRegression=old['entryRegression'],sourceHashes=len(evidence['sources']),derivedOutputs=len(evidence['outputs']),logs=str(WORK))
 Path(__file__).with_name('campaign-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='intro'},ensure_ascii=False))
if __name__=='__main__':main()
