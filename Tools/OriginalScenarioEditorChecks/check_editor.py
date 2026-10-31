"""Check the new editor's actual source joins, file writes and native map pixels.
Does not run Unity EditorWindow/OS input or enable runtime monster spawning.
"""
import hashlib
import csv
import json
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/_Original'
WORK=Path('/tmp/ds-original-scenario-editor-checks')
UNITY=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data')

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    WORK.mkdir(exist_ok=True)
    original=json.loads((BASE/'SourceIndex.json').read_text())
    snapshot={r['asset']:sha(ROOT/r['asset'])for r in original['sources']}
    # Reuse the project's real runtime/editor/player API compiler.
    api=subprocess.run([sys.executable,str(ROOT/'Tools/OriginalEntryChecks/check_entry.py')],cwd=ROOT,capture_output=True,text=True)
    (WORK/'api.log').write_text(api.stdout+api.stderr)
    if api.returncode:raise RuntimeError((api.stdout+api.stderr)[-1600:])
    sdk=UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
    sources=[BASE/'Editor/Scenario'/name for name in ['ScenarioLayoutModel.cs']]+[BASE/'Runtime/Maps/NativeMapDecoder.cs',Path(__file__).with_name('EditorChecks.cs')]
    refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
    exe=WORK/'EditorChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+list(map(str,sources))
    rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+a+'"'for a in args)+'\n')
    build=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,capture_output=True,text=True);(WORK/'compile.log').write_text(build.stdout+build.stderr)
    if build.returncode:raise RuntimeError((build.stdout+build.stderr)[-1600:])
    version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name
    exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
    run=subprocess.run([str(dotnet),str(exe),str(ROOT),str(WORK)],cwd=ROOT,capture_output=True,text=True);(WORK/'run.log').write_text(run.stdout+run.stderr)
    if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-1800:])
    result=json.loads(run.stdout)
    table={int(r['직업']):r for r in csv.DictReader((BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig'))}
    pictures={int(p.name.split('r')[0])for p in (BASE/'Art/Archives/gameres').glob('**/*r.bmp.png')if p.name.split('r')[0].isdigit()}
    literal=json.loads((BASE/'Data/Placements/scenario_monster_placements.json').read_text())
    original_jobs={u['job']for s in literal['scenarios']for u in s['units']}
    assert all(int(table[j]['직그림'])in pictures for j in original_jobs)
    sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
    from decode_visuals import read_mrc,read_grs,grs_image
    from PIL import Image
    actor_frames=0
    for picture in {int(table[j]['직그림'])for j in original_jobs}:
        raw=next((BASE/'Art/Archives/gameres').glob('**/'+str(picture)+'r.bmp.png'))
        with Image.open(raw)as original_sheet,Image.open(BASE/'Art/Campaign'/('Picture'+str(picture)+'.png'))as prepared_sheet:
            assert prepared_sheet.size==original_sheet.size,picture
            w,h=original_sheet.width//3,original_sheet.height//4;box=(0,h,w,2*h)
            source_pixels=list(original_sheet.convert('RGB').crop(box).getdata())
            actual=list(prepared_sheet.convert('RGBA').crop(box).getdata())
            assert actual==[(*p,0 if p==(0,0,0)else 255)for p in source_pixels],picture
            assert any(p[3]>0 for p in actual),'Blank native front frame '+str(picture)
            actor_frames+=1
    result['nativeActorFrontFramesCompared']=actor_frames
    cache={};pixel_checks=[]
    for key in ['01','02','26','101','201','314','1010']:
        path=BASE/'SourceData/Native/map';m=read_mrc(path/(key+'.mrc.bytes'))
        for rid in m['ids']:
            if rid not in cache:cache[rid]=read_grs(path/f'mdsr{rid}.grs.bytes')
        image=Image.new('RGBA',(m['width']*32,m['height']*32),(0,0,0,255))
        for i,(rid,group,detail)in enumerate(zip(m['a'],m['b'],m['tiles'])):
            pos=(i%m['width']*32,i//m['width']*32);g=cache[rid]['groups'][group]
            image.alpha_composite(grs_image(g['palette'],g['images'][0]),pos)
            if detail[1]!=255:
                g=cache[detail[0]]['groups'][detail[1]];frame=g['tiles'][detail[2]]['frames'][0]
                image.alpha_composite(grs_image(g['palette'],g['images'][frame],True),pos)
        for s in m['sprites']:
            g=cache[s['resource']]['sprites'][s['index']]
            image.alpha_composite(grs_image(g['palette'],g['images'][s['frame']],True),(s['x']*32,s['y']*32))
        actual=(WORK/(key+'.rgba')).read_bytes();assert actual==image.tobytes(),key
        image.save(WORK/(key+'-native-preview.png'));pixel_checks.append(key)
    assert all(sha(ROOT/p)==digest for p,digest in snapshot.items())
    api_result=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
    result.update(nativePixelComparisons=pixel_checks,originalSourceFilesUnchanged=len(snapshot),originalMonsterJobPicturesResolved=len(original_jobs),
        catalogPicturesMissing=[int(r['직업'])for r in table.values()if int(r['직그림'])not in pictures],
        runtimeApiErrors=api_result['runtimeApiErrors'],editorApiErrors=api_result['editorApiErrors'],playerApiErrors=api_result['playerApiErrors'],warnings=api_result['warnings'],entryRegression=api_result['assertions'],
        editorSources={p.relative_to(ROOT).as_posix():sha(p)for p in (BASE/'Editor/Scenario').glob('*.cs')},
        scope='New editor API compilation; production source/parser/render/file-store contracts; native static pixel comparison. No Unity window, OS input, import or runtime spawn validation.',logs=str(WORK))
    Path(__file__).with_name('editor-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items()if k not in ['rendered','editorSources']},ensure_ascii=False))

if __name__=='__main__':main()
