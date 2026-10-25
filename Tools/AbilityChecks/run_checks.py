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
ref=max((home/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=version)/'ref/net10.0'
compiler=max((home/'sdk').iterdir(),key=version)/'Roslyn/bincore/csc.dll'
with tempfile.TemporaryDirectory(prefix='ds-abilities-') as directory:
    work=Path(directory);records=[]
    for row in csv.DictReader((ROOT/'닥세_리소스/분석결과/표_Magictbl.csv').open(encoding='utf-8-sig')):
        tokens=json.loads(row['tokens']);records.append(dict(name=tokens[1],values=[int(tokens[0])]+list(map(int,tokens[2:]))))
    fixture=work/'catalog.json';fixture.write_text(json.dumps(records,ensure_ascii=False),encoding='utf-8')
    jobs={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/jobs.csv').open(encoding='utf-8-sig'))}
    class_rows=[r for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/experience_progression.csv').open(encoding='utf-8-sig')) if 100<=int(r['jobId'])<=169 and r['level']=='1']
    class_fixture=[]
    for row in class_rows:
        job=jobs[int(row['jobId'])];ids=[]
        for field in ('learnedMagic','usableSkills'):
            ids.extend(int(s) for s in row[field].split(',') if s.strip() and int(s)>0)
        class_fixture.append(dict(id=int(row['jobId']),name=row['jobName'],hp=int(job['체력수']),mp=max(0,int(job['마력수'])),attack=int(job['공력수']),defense=int(job['방력수']),magicAttack=max(0,int(job['마공수'])),magicDefense=max(0,int(job['마방수'])),abilities=ids))
    (work/'classes.json').write_text(json.dumps(class_fixture,ensure_ascii=False),encoding='utf-8')
    target=work/'AbilityChecks.dll'
    command=[str(dotnet),str(compiler),'-nologo','-target:exe','-langversion:latest','-out:'+str(target),'-r:'+str(runtime)]
    command+=['-r:'+str(p) for p in list(ref.glob('*.dll'))+list(unity.glob('*.dll'))]
    command+=[str(ROOT/'Tools/AbilityChecks/Program.cs'),str(ROOT/'Tools/AbilityChecks/BalanceChecks.cs')]
    # A Windows command line cannot hold all Unity/reference assembly paths.
    response=work/'compile.rsp'
    response.write_text('\n'.join('"'+arg+'"' for arg in command[2:]),encoding='utf-8')
    compiled=subprocess.run(command[:2]+['@'+str(response)],cwd=ROOT)
    if compiled.returncode:raise SystemExit(compiled.returncode)
    shutil.copy(runtime,work/runtime.name)
    for assembly in unity.glob('*.dll'):shutil.copy(assembly,work/assembly.name)
    (work/'AbilityChecks.runtimeconfig.json').write_text(json.dumps(dict(runtimeOptions=dict(tfm='net10.0',framework=dict(name='Microsoft.NETCore.App',version='10.0.0')))),encoding='utf-8')
    env=dict(os.environ,DOTNET_CLI_HOME=str(Path(tempfile.gettempdir())/'ds-dotnet-cli'))
    tested=subprocess.run([str(dotnet),str(target),str(fixture),str(ROOT/'Tools/MapChecks/all_abilities_check.json'),str(work/'classes.json'),str(ROOT/'Tools/MapChecks/ability_balance_check.json')],cwd=ROOT,env=env)
    if tested.returncode==0:
        report=ROOT/'Tools/MapChecks/all_abilities_check.json';data=json.loads(report.read_text(encoding='utf-8'));data['sourceCatalogSha256']=hashlib.sha256((ROOT/'닥세_리소스/분석결과/표_Magictbl.csv').read_bytes()).hexdigest();data['runtimeAssemblySha256']=hashlib.sha256(runtime.read_bytes()).hexdigest();report.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    raise SystemExit(tested.returncode)
