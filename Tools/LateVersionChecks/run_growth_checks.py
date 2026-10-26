"""Execute unchanged production growth/profile sources in an explicit managed host."""
import json,os,subprocess,sys,tempfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];dotnet=Path(sys.argv[1]).resolve();home=dotnet.parent
version=lambda p:tuple(map(int,p.name.split('.')))
refpack=max((home/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=version);major=refpack.name.split('.')[0];tfm='net'+major+'.0';refs=refpack/'ref'/tfm
compiler=max((home/'sdk').iterdir(),key=version)/'Roslyn/bincore/csc.dll'
with tempfile.TemporaryDirectory(prefix='ds-late-growth-') as folder:
 work=Path(folder)
 for path,name in [('Assets/_Game/Runtime/Combat/ExperienceProgression.asset','growth'),('Assets/_Game/Runtime/Magic/MagicCatalog.asset','magic')]:
  data=next(o for o in UnityPy.load(str(ROOT/path)).objects if o.type.name=='MonoBehaviour').read_typetree();(work/(name+'.json')).write_text(json.dumps(data['entries'] if name=='growth' else data,ensure_ascii=False))
 sources=['GameConstants.cs','Jobs/NativePlayerJobs.cs','Combat/ExperienceProgressionTable.cs','Magic/MagicCatalog.cs','Core/NativeCharacterProfile.cs','Core/NativeCharacterProfile.Abilities.cs','Core/NativeCharacterProfile.Levels.cs','Core/NativeCharacterProfile.Equipment.cs','Core/NativeInventory.cs','Core/NativeTutorialProgress.cs']
 target=work/'GrowthChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-out:'+str(target)]+['-r:'+str(p) for p in refs.glob('*.dll')]+[str(ROOT/'Assets/_Game/Runtime'/p) for p in sources]+[str(ROOT/'Tools/LateVersionChecks'/p) for p in ['GrowthHost.cs','GrowthChecks.cs']]
 rsp=work/'compile.rsp';rsp.write_text('\n'.join('"'+a+'"' for a in args));env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-ui-dotnet')
 subprocess.run([str(dotnet),str(compiler),'@'+str(rsp)],check=True,env=env)
 (work/'GrowthChecks.runtimeconfig.json').write_text(json.dumps(dict(runtimeOptions=dict(tfm=tfm,framework=dict(name='Microsoft.NETCore.App',version=major+'.0.0')))))
 subprocess.run([str(dotnet),str(target),str(work/'growth.json'),str(work/'magic.json'),str(ROOT/'Tools/LateVersionChecks/growth_check.json')],check=True,env=env,cwd=ROOT)
