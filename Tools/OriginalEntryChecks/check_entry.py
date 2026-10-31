"""Compile new entry code against Unity API and run real account/disk/client contracts."""
import hashlib
import json
import re
import struct
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT/'Assets/_Original'
DATA = Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')
WORK = Path('/tmp/ds-original-entry-checks')


def main():
    WORK.mkdir(exist_ok=True)
    runtime = sorted((BASE/'Runtime').rglob('*.cs')); editor = sorted((BASE/'Editor').rglob('*.cs'))
    sdk = DATA/'DotNetSdk'; dotnet = sdk/'dotnet'; csc = next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    ui = next((DATA/'Resources/PackageManager/ProjectTemplates/libcache').glob('*/ScriptAssemblies/UnityEngine.UI.dll'))
    common = ['-nologo','-target:library','-langversion:latest','-nostdlib+']
    refs = ['-r:'+str(DATA/'NetStandard/ref/2.1.0/netstandard.dll')]+['-r:'+str(p) for p in sorted((DATA/'Managed/UnityEngine').glob('*.dll'))]+['-r:'+str(ui)]
    def compile(name, options, sources):
        rsp = WORK/(name+'.rsp'); rsp.write_text('\n'.join('"'+str(a)+'"' for a in options+sources)+'\n')
        result = subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True)
        log = result.stdout+result.stderr; (WORK/(name+'.log')).write_text(log)
        if result.returncode: raise RuntimeError(name+' compilation failed; '+str(WORK/(name+'.log'))+'\n'+log[-2500:])
        return log.count('warning CS')
    runtime_api = WORK/'OriginalRuntime.dll'
    runtime_warnings = compile('runtime',common+refs+['-define:UNITY_EDITOR','-out:'+str(runtime_api)],runtime)
    editor_warnings = compile('editor',common+refs+['-r:'+str(runtime_api),'-out:'+str(WORK/'OriginalEditor.dll')],editor)
    player_refs = [r for r in refs if '/UnityEditor.' not in r]
    player_warnings = compile('player',common+player_refs+['-out:'+str(WORK/'OriginalPlayer.dll')],runtime)
    netrefs = ['-r:'+str(p) for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
    core = [BASE/'Runtime/Entry'/n for n in ['EntryProtocol.cs','EntryAccountService.cs','EntryClient.cs','LocalAccountFileStore.cs']]
    core += [BASE/'Runtime/World'/n for n in ['WorldModel.cs','WorldService.cs']] + [BASE/'Runtime/Campaign'/n for n in ['CampaignModel.cs','CampaignService.cs']]
    exe = WORK/'EntryChecks.dll'
    compile('contracts',['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+netrefs,core+sorted((BASE/'Runtime/Tutorial').glob('WaitingLesson.cs'))+sorted((BASE/'Runtime/Tutorial').glob('TutorialScript.cs'))+sorted((BASE/'Runtime/Tutorial').glob('TutorialSession.cs'))+sorted((BASE/'Runtime/Tutorial').glob('TutorialService.cs'))+[Path(__file__).with_name('EntryChecks.cs')])
    version = next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name
    exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
    result = subprocess.run([str(dotnet),str(exe)],cwd=ROOT,text=True,capture_output=True)
    (WORK/'contracts-run.log').write_text(result.stdout+result.stderr)
    if result.returncode: raise RuntimeError('Entry contracts failed: '+(result.stdout+result.stderr)[-2500:])
    checks = json.loads(result.stdout)
    guids = {}
    for p in (ROOT/'Assets').rglob('*.meta'):
        guid = re.search(r'^guid: ([a-f0-9]{32})$',p.read_text(),re.M)[1]
        assert guid not in guids,p
        guids[guid] = Path(str(p)[:-5])
    for p in [BASE/'Scenes/Start.unity']+list((BASE/'Data').rglob('*.asset')):
        for guid in re.findall(r'guid: ([a-f0-9]{32})',p.read_text()): assert guid in guids,(p,guid)
    scene = (BASE/'Scenes/Start.unity').read_text(); assert scene.count('MonoBehaviour:') == 4 and 'm_Name: Original Entry\n' in scene
    assert '  useLoginAndServerSelection: 0\n' in scene
    assert not (ROOT/'Assets/_Game').exists()
    art = json.loads((ROOT/'Tools/OriginalEntryChecks/entry-assets.json').read_text())
    for row in art['outputs']: assert hashlib.sha256((ROOT/row['asset']).read_bytes()).hexdigest() == row['sha256']
    # Direct PE instruction fixtures: these are source evidence, not a simulated game run.
    native = json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
    late = next(p for p in native['payloads'] if 'saver.exe' in p['containers']); original = next(r for r in late['files'] if r['file']=='saver.exe')
    raw = Path(original['extractedFile']).read_bytes()
    assert hashlib.sha256(raw).hexdigest() == art['staticEvidence']['executableSha256']
    pe = struct.unpack_from('<I',raw,60)[0]; sections = pe+24+struct.unpack_from('<H',raw,pe+20)[0]
    def at(va, size):
        for i in range(struct.unpack_from('<H',raw,pe+6)[0]):
            _,rva,length,offset = struct.unpack_from('<IIII',raw,sections+i*40+8)
            if rva <= va-0x400000 < rva+length:return raw[offset+va-0x400000-rva:][:size]
        raise ValueError(hex(va))
    fixtures = {0x40ef53:'b815010000',0x40ef58:'ba14010000',0x45be61:'bd64000000',0x45be9f:'bd65000000',0x45bebf:'bd67000000',0x4900b4:'302030203000',0x45bd1c:'c786e0000000fe000000',0x45bd30:'c786e4000000f1000000',0x45bd49:'68ffff4000'}
    for address,value in fixtures.items(): assert at(address,len(bytes.fromhex(value))) == bytes.fromhex(value),hex(address)
    root=(BASE/'Runtime/Entry/EntrySceneRoot.cs').read_text()
    layout=(BASE/'Runtime/Entry/NativeEntryArt.cs').read_text()
    assert 'AtlasButton(servers' not in root and 'Hit(servers, "Server connect"' in root
    assert 'new Vector2(121, 90)' in layout and art['geometry']['creationOrigin']==[121,90]
    assert 'RefreshSelection()' in root and 'selectionFrames[i].SetActive(i == selectedJob)' in root
    checks.update(runtimeSources=len(runtime),editorSources=len(editor),runtimeApiErrors=0,editorApiErrors=0,playerApiErrors=0,
                  warnings={'runtime':runtime_warnings,'editor':editor_warnings,'player':player_warnings},
                  nativeInstructionFixtures=len(fixtures),sceneScriptsResolved=4,entryArtworkOutputs=len(art['outputs']),
                  codeSha256={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in runtime+editor},
                  scope='New runtime/editor/player API compilation, actual account/disk/client managed tests and native instruction/asset-reference audit. Not Unity import, physical input or rendered Play.',logs=str(WORK))
    Path(__file__).with_name('entry-check.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2)+'\n');print(json.dumps(checks,ensure_ascii=False))


if __name__ == '__main__': main()
