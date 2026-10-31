"""Compile the changed editor with Unity APIs, then check actual production troop logic."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Tools/ScenarioPlacementChecks'


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--unity-data',type=Path,default=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data'))
    parser.add_argument('--work',type=Path,default=Path('/tmp/ds-editor-troop-check'))
    args=parser.parse_args();args.work.mkdir(parents=True,exist_ok=True)
    sdk=args.unity_data/'DotNetSdk';dotnet=sdk/'dotnet'
    csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    netrefs=next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0'))
    runtime=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name
    ui=next((args.unity_data/'Resources/PackageManager/ProjectTemplates/libcache').glob('*/ScriptAssemblies/UnityEngine.UI.dll'))
    helper=ROOT/'Assets/_Game/Editor/ScenarioPlacementTroops.cs'
    window=ROOT/'Assets/_Game/Editor/ScenarioPlacementWindow.cs'
    sources=sorted((ROOT/'Assets/_Game/Runtime').rglob('*.cs'))+[window,helper]
    paths=sorted((ROOT/'Assets/_Game/Resources/Scenarios').glob('*/Content.json'))
    snapshot={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}

    def compile(name,options):
        response=args.work/(name+'.rsp');response.write_text('\n'.join('"'+str(o)+'"' for o in options)+'\n')
        proc=subprocess.run([str(dotnet),str(csc),'@'+str(response)],cwd=ROOT,text=True,capture_output=True)
        (args.work/(name+'.log')).write_text(proc.stdout+proc.stderr)
        if proc.returncode:raise RuntimeError('Compile failed: '+str(args.work/(name+'.log')))
        return proc.stdout.count('warning CS')

    api=args.work/'EditorTroopApi.dll'
    warnings=compile('api',['-nologo','-target:library','-langversion:latest','-nostdlib+',
                            '-define:UNITY_EDITOR','-out:'+str(api),
                            '-r:'+str(args.unity_data/'NetStandard/ref/2.1.0/netstandard.dll')]
                     +['-r:'+str(p) for p in sorted((args.unity_data/'Managed/UnityEngine').glob('*.dll'))]
                     +['-r:'+str(ui)]+sources)
    exe=args.work/'EditorTroopChecks.dll'
    compile('checks',['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe),'-r:'+str(api)]
            +['-r:'+str(p) for p in sorted(netrefs.glob('*.dll'))]+[helper,OUT/'EditorTroopChecks.cs'])
    (args.work/'EditorTroopChecks.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':runtime}}}))
    proc=subprocess.run([str(dotnet),str(exe),str(ROOT)],cwd=args.work,text=True,capture_output=True)
    (args.work/'run.log').write_text(proc.stdout+proc.stderr)
    if proc.returncode:raise RuntimeError('Managed checks failed: '+str(args.work/'run.log'))
    summary=json.loads(proc.stdout)
    assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in snapshot.items())
    summary.update(apiCompileErrors=0,existingApiWarnings=warnings,compiledRuntimeSources=len(sources)-2,
                   contentFilesUnchanged=len(snapshot),unityGuiExecuted=False,
                   sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [helper,window]},
                   logs=str(args.work))
    (OUT/'editor-troop-check.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(summary,ensure_ascii=False))


if __name__=='__main__':
    main()
