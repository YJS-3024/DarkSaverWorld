"""Compile actual editor save queue methods; preserve assertions and Unity boundary limits."""
import hashlib,json,os,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-editor-save-queue')
sys.path.insert(0,str(ROOT/'Tools/OriginalWorldChecks'))
from check_picture_initialization import block

def main():
    WORK.mkdir(exist_ok=True)
    source=ROOT/'Assets/_Original/Editor/Scenario/ScenarioEditorWindow.cs'
    text=source.read_text();methods='\n'.join(block(text,'private void '+name+'(')for name in ['QueueEditorSave','DispatchQueuedSave'])
    host=WORK/'ProductionQueue.cs';host.write_text('using System;using UnityEngine;using UnityEditor;\ninternal sealed partial class QueueHost{\n'+methods+'\n}\n')
    sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'SaveQueueChecks.dll'
    args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]+[str(host),str(OUT/'SaveQueueChecks.cs')]
    rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args)+'\n')
    version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name
    exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
    for phase,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe)])]:
        proc=subprocess.run(cmd,cwd=ROOT,env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet'),capture_output=True,text=True);(WORK/(phase+'.log')).write_text(proc.stdout+proc.stderr)
        if proc.returncode:raise RuntimeError((proc.stdout+proc.stderr)[-2000:])
    result=json.loads(proc.stdout)
    sources=[Path(__file__),OUT/'SaveQueueChecks.cs',source,ROOT/'Assets/_Original/Editor/Scenario/ScenarioEditorWindow.GUI.cs',ROOT/'Assets/_Original/Editor/Scenario/ScenarioEditorWindow.Gateways.cs',ROOT/'Tools/OriginalWorldChecks/check_picture_initialization.py']
    result.update(mismatches=0,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},logs=str(WORK))
    (OUT/'save-queue-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
