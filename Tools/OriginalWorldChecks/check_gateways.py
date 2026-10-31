"""NPC gate authoring/storage/runtime contracts; never write real account/settings files."""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';WORK=Path('/tmp/ds-npc-gateway-checks');UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')
def main():
 WORK.mkdir(exist_ok=True)
 r=subprocess.run([sys.executable,str(Path(__file__).with_name('check_campaign.py'))],cwd=ROOT,text=True,capture_output=True);(WORK/'regression.log').write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-1800:])
 sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
 sources=[BASE/'Runtime/Entry'/p for p in ['EntryProtocol.cs','EntryAccountService.cs','EntryClient.cs','LocalAccountFileStore.cs']]
 sources += [BASE/'Runtime/Tutorial'/p for p in ['TutorialScript.cs','TutorialSession.cs','TutorialService.cs','WaitingLesson.cs']]+[BASE/'Runtime/World'/p for p in ['WorldModel.cs','WorldService.cs']]
 sources += [BASE/'Runtime/Campaign'/p for p in ['CampaignModel.cs','CampaignService.cs','ScenarioGatewaySettings.cs','ScenarioGatewayService.cs']]+[Path(__file__).with_name('GatewayChecks.cs')]
 refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
 exe=WORK/'GatewayChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+list(map(str,sources));rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args)+'\n')
 r=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True);(WORK/'compile.log').write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-1800:])
 version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name;exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 r=subprocess.run([str(dotnet),str(exe),str(ROOT)],cwd=ROOT,text=True,capture_output=True);(WORK/'run.log').write_text(r.stdout+r.stderr)
 if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-1800:])
 result=json.loads(r.stdout);evidence=json.loads(Path(__file__).with_name('gateway-assets.json').read_text())
 for p,digest in evidence['sources'].items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==digest,p
 for item in evidence['outputs']:assert hashlib.sha256((ROOT/item['asset']).read_bytes()).hexdigest()==item['sha256'],item['asset']
 campaign=json.loads(Path(__file__).with_name('campaign-check.json').read_text());result.update(runtimeApiErrors=campaign['runtimeApiErrors'],editorApiErrors=campaign['editorApiErrors'],playerApiErrors=campaign['playerApiErrors'],warnings=campaign['warnings'],campaignRegression=campaign['assertions'],logs=str(WORK))
 Path(__file__).with_name('gateway-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
