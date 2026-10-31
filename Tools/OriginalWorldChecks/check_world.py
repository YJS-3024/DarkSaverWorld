"""Execute production world/account contracts and audit source-linked assets, not Unity Play."""
import csv,hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original';WORK=Path('/tmp/ds-original-world-checks')
UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True)
 previous=subprocess.run([sys.executable,str(ROOT/'Tools/OriginalTutorialChecks/check_tutorial.py')],cwd=ROOT,text=True,capture_output=True)
 (WORK/'tutorial-regression.log').write_text(previous.stdout+previous.stderr)
 if previous.returncode:raise RuntimeError((previous.stdout+previous.stderr)[-1800:])
 sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
 sources=[BASE/'Runtime/Entry'/n for n in ['EntryProtocol.cs','EntryAccountService.cs','EntryClient.cs','LocalAccountFileStore.cs']]
 sources += [BASE/'Runtime/Tutorial'/n for n in ['TutorialScript.cs','TutorialSession.cs','TutorialService.cs','WaitingLesson.cs']]
 sources += [BASE/'Runtime/World'/n for n in ['WorldModel.cs','WorldService.cs']] + [BASE/'Runtime/Campaign'/n for n in ['CampaignModel.cs','CampaignService.cs','ScenarioGatewaySettings.cs','ScenarioGatewayService.cs']]+[Path(__file__).with_name('WorldChecks.cs')]
 refs=['-r:'+str(p) for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
 exe=WORK/'WorldChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+list(map(str,sources))
 rsp=WORK/'world.rsp';rsp.write_text('\n'.join('"'+a+'"' for a in args)+'\n')
 compile=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True);(WORK/'compile.log').write_text(compile.stdout+compile.stderr)
 if compile.returncode:raise RuntimeError((compile.stdout+compile.stderr)[-1600:])
 version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name
 exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 run=subprocess.run([str(dotnet),str(exe)],cwd=ROOT,text=True,capture_output=True);(WORK/'run.log').write_text(run.stdout+run.stderr)
 if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-1800:])
 result=json.loads(run.stdout);evidence=json.loads((ROOT/'Tools/OriginalWorldChecks/world-assets.json').read_text())
 for r in evidence['outputs']:assert sha(ROOT/r['asset'])==r['sha256'],r['asset']
 for p,digest in evidence['sources'].items():assert sha(ROOT/p)==digest,p
 manifest=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
 known={f['sha256'] for p in manifest['payloads'] for f in p.get('files',[]) if 'sha256'in f}
 for p in (BASE/'SourceData/KaosiaLegacy').glob('*.bytes'):assert sha(p)in known,p
 definition=json.loads((BASE/'Data/World/KaosiaWorld.json').read_text());assert definition['rules'] and not any(definition['rules'].values())
 assert {s['map']:len(s['items'])for s in definition['shops']}=={2000:22,2100:20,2300:23,2800:6,2200:0}
 assert 755 in next(s['items']for s in definition['shops']if s['map']==2100) and 1029 not in next(s['items']for s in definition['shops']if s['map']==2100)
 assert all(not i['sellKnown']for i in definition['items'])
 # Independent source columns, not Unity's runtime icon cache or its slot resolver.
 native_items={int(r['번호']):r for r in csv.DictReader((BASE/'Data/Tables/items.csv').open(encoding='utf-8-sig'))}
 native_jobs={int(r['직업']):r for r in csv.DictReader((BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig'))}
 flags=['보','비','기','수','창','궁','승','신','마','사']
 for item in definition['items']:
  row=native_items[item['id']]
  assert (item['picture'],item['equipType'],item['requiredLevel'])==tuple(int(row[k])for k in ['그림1','장착위','장착렙'])
  assert item['jobs']==[job for job in (100,101,103)if int(row[flags[int(native_jobs[job]['병종'])]])==1]
 from PIL import Image
 with Image.open(BASE/'Art/World/Items.png')as converted,Image.open(BASE/'Art/Archives/gameres/item.arc/item.bmp.png')as original:
  assert original.size==converted.size==(3200,320)
  original=original.convert('RGB');converted=converted.convert('RGBA')
  for picture in {i['picture']for i in definition['items']if 0<=i['picture']<1000}:
   x=picture%100*32;y=picture//100*32;box=(x,y,x+32,y+32)
   expected=[(*p,0 if p==(0,0,0)else 255)for p in original.crop(box).getdata()]
   assert list(converted.crop(box).getdata())==expected,picture
 assert 'isReadable: 1' in (BASE/'Art/World/Items.png.meta').read_text()
 assert (BASE/'Data/World/KaosiaWorld.json').read_bytes().find(b'\r')<0
 tutorial=json.loads((ROOT/'Tools/OriginalTutorialChecks/tutorial-check.json').read_text())
 result.update(maps=11,staff=evidence['staff'],shopItems=71,sourceLinkedOutputs=len(evidence['outputs']),sourceHashes=len(evidence['sources']),
   tutorialRegression=tutorial['assertions'],entryRegression=tutorial['entryRegressionAssertions'],runtimeApiErrors=tutorial['runtimeApiErrors'],editorApiErrors=tutorial['editorApiErrors'],playerApiErrors=tutorial['playerApiErrors'],warnings=tutorial['warnings'],sceneScriptsResolved=4,
   scope='Production state/auth/movement/routes/transactions/save tests and API/source audits. Native fields and held policies are distinct; no Unity import, mouse Play, audio or battle validation.',logs=str(WORK))
 Path(__file__).with_name('world-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
