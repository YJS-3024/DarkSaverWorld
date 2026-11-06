"""Native66/FAB0 filename/command trace and production suffix service integration."""
import hashlib,itertools,json,subprocess,sys
from pathlib import Path
from native_scenario_script_loader_emulator import NativeScenarioScriptLoader
from check_native_battle_responses import contract_sources
from build_native_outro_variants import variant_paths
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-scenario-suffix')
def main():
    WORK.mkdir(exist_ok=True)
    # The filename/service fixture needs the real scenario compiler host. A
    # fresh runner work-dir must prepare it before consuming this report.
    old=Path('/tmp/ds-scenario-progress-checks')
    if not(old/'checks.rsp').is_file()or not(old/'ScenarioProgressChecks.runtimeconfig.json').is_file():
        prerequisite=subprocess.run([sys.executable,str(OUT/'check_scenario_progress.py')],cwd=ROOT,text=True,capture_output=True)
        (WORK/'scenario-prerequisite.log').write_text(prerequisite.stdout+prerequisite.stderr)
        assert prerequisite.returncode==0,(prerequisite.stdout+prerequisite.stderr)[-2500:]
    import struct
    from native_mfc_format_emulator import MfcFormat,UC_X86_REG_ECX,UC_X86_REG_ESP,UC_X86_REG_EIP
    mfc=MfcFormat();obj=0x0c020000;p=0x0c020100;sp=0x0f0ff000
    for value in range(256):
        mfc.uc.mem_write(p-12,struct.pack('<iii',1,3,8)+b'old'+bytes(8));mfc.uc.mem_write(obj,struct.pack('<I',p));mfc.uc.mem_write(sp,struct.pack('<II',mfc.stop,value));mfc.uc.reg_write(UC_X86_REG_ECX,obj);mfc.uc.reg_write(UC_X86_REG_ESP,sp)
        mfc.uc.emu_start(0x6c143b76,mfc.stop+1,timeout=1000000,count=100000);assert mfc.uc.reg_read(UC_X86_REG_EIP)==mfc.stop
        assert struct.unpack('<i',mfc.uc.mem_read(p-8,4))[0]==1 and bytes(mfc.uc.mem_read(p,2))==bytes([value,0])
    old=Path('/tmp/ds-scenario-progress-checks');exe=WORK/'ScenarioSuffix.dll';sdk=magic.UNITY/'DotNetSdk'
    args=(old/'checks.rsp').read_text().replace(str(old/'ScenarioProgressChecks.dll'),str(exe))+'\n"'+str(OUT.resolve()/'NativeScenarioSuffixChecks.cs')+'"\n/main:ScenarioSuffixEntry\n'
    for p in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(p)not in args:args+='\n"'+str(p)+'"\n'
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'ScenarioProgressChecks.runtimeconfig.json').read_bytes())
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('service',[str(sdk/'dotnet'),str(exe),str(ROOT)]),('decoder',[str(sdk/'dotnet'),str(exe),str(ROOT),'decode'])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
        if phase=='service':service=json.loads(r.stdout)
    managed=json.loads(r.stdout);n=NativeScenarioScriptLoader();suffixes=[];filenames=[];loads=[];controls=[]
    for value,expected in enumerate(managed):
        actual=n.suffix(value);assert list(actual)==expected,(value,actual,expected);suffixes.append(actual)
    for extension,suffix,tutorial in itertools.product(['deo','DEE','dee','DeO','evt'],[b'',b'a',b'b',b'z',b'\0'],[False,True]):
        value=n.script('ALL\nEND',key='02',ext=extension,suffix=suffix,tutorial=tutorial)
        expected=['root\\Map\\tutorial.arc','tutorial.deo']if tutorial else['root\\Map\\02set.arc','02'+(suffix.decode()if extension.lower()=='dee'else'')+'.'+extension]
        expected=[v.split('\0')[0]for v in expected]
        assert value['paths']==expected,(extension,suffix,tutorial,value);assert value['commands']==['ALL','END']and value['mode']==2 and value['active']==1
        # Tutorial always loads tutorial.deo, but the record's+8 flag still
        # reflects requested extension DEE. Preserve the distinction.
        assert value['outro']==int(extension.lower()=='dee')
        filenames.append(dict(extension=extension,suffix=suffix.hex(),tutorial=tutorial,paths=value['paths']))
    for p in variant_paths():
        source=p.read_text();key=p.name[:2];suffix=p.name[2:3].encode();value=n.script(source,key=key,suffix=suffix)
        assert value['commands']==source.splitlines()[:999],p
        loads.append(dict(path=p.relative_to(ROOT).as_posix(),rows=len(value['commands']),selected=value['paths'][1]))
    for name,source,kwargs in [('empty','',{}),('999-limit','END\n'*1001,{}),('missing-archive','END',dict(archive=False)),('missing-extract','END',dict(extracted=False)),('open-failure','END',dict(opened=False))]:
        value=n.script(source,**kwargs);controls.append(dict(name=name,result=value))
        if name=='999-limit':assert len(value['commands'])==999
        elif name=='empty':assert value['mode']==2 and value['commands']==[]
        else:assert value['mode']==1 and not value['commands']
    asset=ROOT/'Assets/_Original/Data/Campaign/NativeCampaignArt.asset';text=asset.read_text()
    for p in variant_paths():
        import re
        guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1];assert 'guid: '+guid in text
    sources=contract_sources(args,__file__)
    paths=[OUT/'check_scenario_progress.py',OUT/'native_scenario_script_loader_emulator.py',OUT/'native_event_loader_emulator.py',OUT/'native_mfc_format_emulator.py',OUT/'native_event_step_emulator.py',OUT/'build_native_outro_variants.py',ROOT/'Assets/_Original/Runtime/Campaign/NativeCampaignArt.cs',asset,*variant_paths()]
    for p in paths:sources[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    report=dict(nativeSuffixBytes=256,nativeMfcCharAssignments=256,mfcLibraries=mfc.sources,filenameCases=len(filenames),originalVariantFiles=len(loads),loaderControls=len(controls),service=service,mismatches=0,unityPlay=False,originalGameCtlSha256=n.sha256,sources=sources,filenames=filenames,variantLoads=loads,controls=controls,logs=str(WORK),scope='Actual EB10/E060 byte assignment and FAB0 filename/load body. Archive/file/CString/allocation/first-clear decision/unit-clear/status are explicit boundaries. Original six variant command arrays compared; managed common66 ingress/ending/file-resume checked separately. No server branch-selection rule, original frame timing or Unity Play claim.')
    (OUT/'native-scenario-suffix-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','filenames','variantLoads','controls']}))
if __name__=='__main__':main()
