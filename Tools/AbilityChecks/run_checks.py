import re
"""Execute actual compiled production combat classes without Unity scene/native calls.
Requires a fresh API compilation DLL, .NET SDK, Unity Managed assemblies.
Usage: python run_checks.py /tmp/ds-dotnet/dotnet /tmp/ds-scenario01-runtime.dll
"""
from pathlib import Path
import csv,json,os,shutil,subprocess,sys,tempfile,hashlib
ROOT=Path(__file__).resolve().parents[2]
dotnet=Path(sys.argv[1]).resolve();runtime=Path(sys.argv[2]).resolve();home=dotnet.parent
unity=Path(sys.argv[3]).resolve() if len(sys.argv)>3 else ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/Managed/UnityEngine'
if runtime.stat().st_mtime<max(p.stat().st_mtime for p in (ROOT/'Assets/_Game/Runtime').rglob('*.cs')):raise SystemExit('Recompile the runtime DLL after the latest runtime source change.')
version=lambda p:tuple(map(int,p.name.split('.')))
ref_pack=max((home/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=version)
major=ref_pack.name.split('.')[0];tfm='net'+major+'.0';ref=ref_pack/'ref'/tfm
compiler=max((home/'sdk').iterdir(),key=version)/'Roslyn/bincore/csc.dll'
with tempfile.TemporaryDirectory(prefix='ds-abilities-') as directory:
    work=Path(directory);records=[]
    for tokens in csv.reader((ROOT/'분석/Saver_Late/unity_catalogs/Magictbl.csv').open(encoding='utf-8-sig')):
        if not tokens[0].isdigit():continue
        records.append(dict(name=tokens[1],values=[int(tokens[0])]+list(map(int,tokens[2:]))))
    fixture=work/'catalog.json';fixture.write_text(json.dumps(records,ensure_ascii=False),encoding='utf-8')
    jobs={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/jobs.csv').open(encoding='utf-8-sig'))}
    class_rows=[r for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/experience_progression.csv').open(encoding='utf-8-sig')) if 100<=int(r['jobId'])<=172 and r['level']=='1']
    class_fixture=[]
    for row in class_rows:
        job=jobs[int(row['jobId'])];ids=[]
        for field in ('learnedMagic','usableSkills'):
            ids.extend(int(s) for s in row[field].split(',') if s.strip() and int(s)>0)
        class_fixture.append(dict(id=int(row['jobId']),name=row['jobName'],family=int(job['병종']),hp=int(job['체력수']),mp=max(0,int(job['마력수'])),attack=int(job['공력수']),defense=int(job['방력수']),magicAttack=max(0,int(job['마공수'])),magicDefense=max(0,int(job['마방수'])),abilities=ids,learnedMagic=[int(v) for v in row['learnedMagic'].split(',') if v.strip() and int(v)>0],usableSkills=[int(v) for v in row['usableSkills'].split(',') if v.strip() and int(v)>0]))
    (work/'classes.json').write_text(json.dumps(class_fixture,ensure_ascii=False),encoding='utf-8')
    source_grants=[]
    for folder in sorted((ROOT/'분석/Saver_Late/decoded/map').glob('*set.arc')):
        key=folder.name[:-7]
        if not key.isdigit():continue
        granted=set()
        for path in folder.iterdir():
            if path.suffix.lower() not in ('.evt','.dee'):continue
            for line in path.read_bytes().splitlines():
                match=re.match(rb'^\s*GETGENERAL\s+(\d+)\b',line)
                if match:granted.add(int(match[1]))
        source_grants.append(dict(scenario=key,jobs=sorted(granted)))
    clear_report=json.loads((ROOT/'분석/Saver_Late/clear_rewards.json').read_text(encoding='utf-8'))
    clear_items=sorted({r['id'] for scene in clear_report['scenarios'] for r in scene['rewards'] if r['kind']=='item'})
    (work/'scenario_grants.json').write_text(json.dumps(dict(scenarios=source_grants,clearItemIds=clear_items)),encoding='utf-8')
    target=work/'AbilityChecks.dll'
    command=[str(dotnet),str(compiler),'-nologo','-target:exe','-langversion:latest','-out:'+str(target),'-r:'+str(runtime)]
    command+=['-r:'+str(p) for p in list(ref.glob('*.dll'))+list(unity.glob('*.dll'))]
    command+=[str(ROOT/'Tools/AbilityChecks/Program.cs'),str(ROOT/'Tools/AbilityChecks/BalanceChecks.cs'),str(ROOT/'Tools/AbilityChecks/ScenarioRewardChecks.cs')]
    # A Windows command line cannot hold all Unity/reference assembly paths.
    response=work/'compile.rsp'
    response.write_text('\n'.join('"'+arg+'"' for arg in command[2:]),encoding='utf-8')
    compiled=subprocess.run(command[:2]+['@'+str(response)],cwd=ROOT)
    if compiled.returncode:raise SystemExit(compiled.returncode)
    shutil.copy(runtime,work/runtime.name)
    for assembly in unity.glob('*.dll'):shutil.copy(assembly,work/assembly.name)
    (work/'AbilityChecks.runtimeconfig.json').write_text(json.dumps(dict(runtimeOptions=dict(tfm=tfm,framework=dict(name='Microsoft.NETCore.App',version=major+'.0.0')))),encoding='utf-8')
    env=dict(os.environ,DOTNET_CLI_HOME=str(Path(tempfile.gettempdir())/'ds-dotnet-cli'))
    tested=subprocess.run([str(dotnet),str(target),str(fixture),str(ROOT/'Tools/MapChecks/all_abilities_check.json'),str(work/'classes.json'),str(ROOT/'Tools/MapChecks/ability_balance_check.json'),str(work/'scenario_grants.json')],cwd=ROOT,env=env)
    if tested.returncode==0:
        report=ROOT/'Tools/MapChecks/all_abilities_check.json';data=json.loads(report.read_text(encoding='utf-8'));data['sourceCatalogSha256']=hashlib.sha256((ROOT/'분석/Saver_Late/unity_catalogs/Magictbl.csv').read_bytes()).hexdigest();data['runtimeAssemblySha256']=hashlib.sha256(runtime.read_bytes()).hexdigest();report.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    raise SystemExit(tested.returncode)
