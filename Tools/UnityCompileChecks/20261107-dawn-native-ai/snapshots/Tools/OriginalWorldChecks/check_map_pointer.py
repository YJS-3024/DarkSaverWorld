"""Execute actual map pointer callbacks through isolated Unity boundaries."""
import hashlib,json,os,subprocess
from pathlib import Path
from check_picture_initialization import block
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-map-pointer-checks')
def main():
 WORK.mkdir(exist_ok=True);source=(OUT/'PictureInitializationChecks.cs').read_text();stubs=WORK/'UnityStubs.cs';stubs.write_text('using System;using System.Collections.Generic;\n'+block(source,'namespace UnityEngine\n'))
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
 actual=ROOT/'Assets/_Original/Runtime/Maps/NativeMapPointer.cs';host=OUT/'MapPointerChecks.cs';exe=WORK/'MapPointerChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe),*refs,str(stubs),str(host),str(actual)]
 rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args));exe.with_suffix('.runtimeconfig.json').write_bytes(Path('/tmp/ds-original-entry-checks/EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 r=json.loads(p.stdout);r.update(mismatches=0,sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [Path(__file__),host,actual,OUT/'PictureInitializationChecks.cs']},logs=str(WORK));(OUT/'map-pointer-check.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
