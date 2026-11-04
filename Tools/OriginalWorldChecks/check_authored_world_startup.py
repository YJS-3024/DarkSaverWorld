"""Reproduce old presentation-readiness gate and exercise real saved gateway startup."""
import hashlib,json,os,subprocess
from pathlib import Path
from check_picture_initialization import block
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-authored-world-startup')
def main():
 WORK.mkdir(exist_ok=True);sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';old=Path('/tmp/ds-npc-gateway-checks')
 manager=ROOT/'Assets/_Original/Runtime/World/LocalServerManager.World.cs';art=ROOT/'Assets/_Original/Runtime/Campaign/NativeCampaignArt.cs';ui=ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs'
 line=next(l.strip()for l in manager.read_text().splitlines()if l.strip().startswith('public WorldReply Request_BeginWorld('));merge=block(art.read_text(),'public WorldDefinition Merge(')
 begin=block(ui.read_text(),'private void Begin(');assert 'campaign?.IsConfigured'not in begin and 'definition=campaign.Merge(definition);'in begin,'UI still drops configured gateways based on presentation readiness'
 results={}
 for variant in ['baseline','current']:
  request=line if variant=='current'else line.replace('campaignArt != null ?','campaignArt?.IsConfigured == true ?')
  host=WORK/('Host-'+variant+'.cs');host.write_text('using System;using System.Linq;using DarkSaver.Original.World;using DarkSaver.Original.Campaign;\nnamespace DarkSaver.Original.Entry { public sealed partial class LocalServerManager { '+request+' }}\nnamespace DarkSaver.Original.Campaign {public sealed partial class NativeCampaignArt { '+merge+' }}\n')
  exe=WORK/(variant+'.dll');args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe))+'\n-main:AuthoredWorldStartupChecks\n'
  args+='\n"'+str(host)+'"\n"'+str(OUT/'AuthoredWorldStartupChecks.cs')+'"\n'
  if variant=='baseline':args+='-define:BASELINE\n'
  rsp=WORK/(variant+'.rsp');rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
  for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
   proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(variant+'-'+phase+'.log')).write_text(proc.stdout+proc.stderr)
   if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-2400:])
  results[variant]=json.loads(proc.stdout)
 sources=[Path(__file__),OUT/'AuthoredWorldStartupChecks.cs',manager,art,ui,OUT/'check_picture_initialization.py',ROOT/'Assets/_Original/Data/Campaign/UserGateways.json',ROOT/'Assets/_Original/Data/Campaign/CampaignCatalog.json',ROOT/'Assets/_Original/Data/World/KaosiaWorld.json']
 report=dict(results=results,mismatches=0,unityPlay=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},logs=str(WORK))
 (OUT/'authored-world-startup-check.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(results))
if __name__=='__main__':main()
