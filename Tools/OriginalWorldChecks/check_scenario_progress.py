"""Source-backed scenario script/event/return checks; explicit authority fixtures, not Unity Play."""
import hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent
WORK=Path('/tmp/ds-scenario-progress-checks');SDK=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk')
def main():
 WORK.mkdir(exist_ok=True)
 old=Path('/tmp/ds-original-campaign-checks');exe=WORK/'ScenarioProgressChecks.dll'
 args=(old/'checks.rsp').read_text().replace(str(old/'CampaignChecks.dll'),str(exe)).replace(str(OUT/'CampaignChecks.cs'),str(OUT/'ScenarioProgressChecks.cs'))
 args+='\n'+str(OUT/'ScenarioManagerHost.cs')+'\n'+str(ROOT/'Assets/_Original/Runtime/Campaign/LocalServerManager.Campaign.cs')+'\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args)
 env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
 for name,cmd in [('compile',[str(SDK/'dotnet'),str(next(SDK.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(SDK/'dotnet'),str(exe),str(ROOT)])]:
  if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'CampaignChecks.runtimeconfig.json').read_bytes())
  r=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
  if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
 result=json.loads(r.stdout)
 from PIL import Image
 asset_checks=0
 for name in ['scenario-effect-assets.json','scenario-music-assets.json']:
  evidence=json.loads((OUT/name).read_text())
  for path,digest in evidence['sources'].items():
   if path.startswith('external:'):continue
   assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path;asset_checks+=1
  for row in evidence['outputs']:
   path=ROOT/row['asset'];assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'];asset_checks+=1
   if path.suffix=='.png':
    im=Image.open(path);assert im.size==(row['width'],row['height']);assert hashlib.sha256(im.getchannel('A').tobytes()).hexdigest()==row['rawAlphaSha256'];asset_checks+=1
   else:
    if 'importerSha256' in row:
     importer=Path(str(path)+'.meta').read_text();assert hashlib.sha256(Path(str(path)+'.meta').read_bytes()).hexdigest()==row['importerSha256'];assert '    loadType: 2\n' in importer and '  preloadAudioData: 0\n' in importer and '  loadInBackground: 1\n' in importer;asset_checks+=1
    info=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','format=duration','-of','json',str(path)],text=True));assert abs(float(info['format']['duration'])-row['nominalSeconds'])<.05;asset_checks+=1
 music_assets=json.loads((OUT/'scenario-music-assets.json').read_text())
 result.update(assetChecks=asset_checks,combineMasks=8,literalMidiClips=14,totalMidiClips=len(music_assets['outputs']),mapMusicRows=music_assets.get('declaredMapRows',0),missingNativeMidi=music_assets['missingNativePaths'])
 expected={'GameCtl.dll':'f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0','DManage.dll':'08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3'}
 inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
 native={name:next(f['extractedFile']for p in inventory['payloads']for f in p['files']if f.get('sha256')==digest)for name,digest in expected.items()}
 result.update(nativeSha256={name:hashlib.sha256(Path(path).read_bytes()).hexdigest()for name,path in native.items()},
  logs=str(WORK),unityPlay=False,
  scope='Source script/event parser, original MRC mutation, isolated managed entry/approved clear/outro/result/return, save/reconnect and refusal. Not 97 combat clears; no fabricated HP/AP, enemy roster or rewards.')
 result['testCodeSha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_scenario_progress.py',OUT/'ScenarioProgressChecks.cs',OUT/'ScenarioManagerHost.cs']}
 result['codeSha256']={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for pattern in ['Campaign/*.cs','World/WorldModel.cs','World/WorldService.cs','Maps/NativeMapDecoder.cs']for p in (ROOT/'Assets/_Original/Runtime').glob(pattern)}
 (OUT/'scenario-progress-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['codeSha256','nativeSha256','testCodeSha256']},ensure_ascii=False))
if __name__=='__main__':main()
