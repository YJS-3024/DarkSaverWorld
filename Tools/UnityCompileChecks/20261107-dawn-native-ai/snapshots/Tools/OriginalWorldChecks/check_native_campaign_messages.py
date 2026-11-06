"""Source MESSAGE aliases and safe ANSI formats, compared to actual helper+MFC."""
import hashlib,json,subprocess
from pathlib import Path
from native_campaign_message_emulator import NativeCampaignMessage
from check_native_event_corpus import load_bodies
from check_native_battle_responses import contract_sources
from native_event_evidence import event_corpus_sources
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-campaign-messages')
def main():
    WORK.mkdir(exist_ok=True);old=Path('/tmp/ds-npc-gateway-checks');exe=WORK/'CampaignMessages.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeCampaignMessageDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args);sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet'
    baseline=contract_sources(args,__file__)|event_corpus_sources()
    for path in [OUT/'native_campaign_message_emulator.py',OUT/'native_mfc_format_emulator.py']:
        baseline[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    r=subprocess.run([str(dotnet),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True);(WORK/'compile.log').write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2000:]
    exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    bodies,_=load_bodies(sorted((ROOT/'Assets/_Original/SourceData/Decoded/map').glob('*/*.evt.txt')))
    commands=[c[len('MESSAGE '):]for b in bodies for c in b['body']if c.startswith('MESSAGE ')]
    distinct=list(dict.fromkeys(commands));names=['지휘관'+str(i)for i in range(10)];jobs=list(range(100,110));own='현재팀'
    controls=[f'{mode} COM{slot} COM{slot} COM{(slot+1)%10} %s|%s|100%%'for mode in [0,1,2]for slot in range(10)]
    controls+=['0 0 ME 0 %s','1 709 ID 1 %s','2 709 AME 1 %s','1 COM10 COM10 IDZ %s:%s','1 709 me 0 %s']
    # Native CString.Left/Mid count ANSI bytes; nonnumeric one-byte COM suffix uses atoi("")==0.
    controls += ['0 0 '+v+' 0 %s'for v in ['가ME','가ID','A가ME','ME가','ID가','AME','XID','가COM0']]
    controls += ['1 '+v+' '+v+' 0 %s'for v in ['COM','COMX','COM-1','COM가','COM0가','COM10']]
    data=[dict(command=c,ownName=own,names=names,jobs=jobs)for c in distinct+controls]
    data.extend(dict(command='0 0 ME 0 before %S after',ownName=value,names=names,jobs=jobs)for value in ['', 'A','AB','Hero','한','한글'])
    input=WORK/'input.json';output=WORK/'managed.json';input.write_text(json.dumps(data,ensure_ascii=False));r=subprocess.run([str(dotnet),str(exe),str(input),str(output)],cwd=ROOT,text=True,capture_output=True);(WORK/'run.log').write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2000:]
    managed=json.loads(output.read_text());n=NativeCampaignMessage();cases=[];wide=[]
    for c,m in zip(data,managed):
        native=n.message(c['command'],c['names'],c['jobs'],c['ownName'],subject=7);assert len(native)==1,(c,native)
        result=native[0]
        if '%S'in c['command']:
            assert m==dict(ok=True,text=result['text'],face=result['face']),(c,m,result)
            wide.append(dict(command=c['command'],ownName=c['ownName'],nativeInCLocale=result['text'],boundedManagedText=m['text']));continue
        assert m==dict(ok=True,text=result['text'],face=result['face']),(c,m,result)
        if result['mode']==0:assert result['rect']==[40,130,561,65]
        cases.append(dict(command=c['command'],native=result))
    for value in ['%d','%s%s%s','%','%x']:
        extra=dict(command='0 0 ME 0 '+value,ownName=own,names=names,jobs=jobs);input.write_text(json.dumps([extra],ensure_ascii=False));r=subprocess.run([str(dotnet),str(exe),str(input),str(output)],cwd=ROOT,text=True,capture_output=True);assert r.returncode==0 and json.loads(output.read_text())[0]['ok']==False
    sources=contract_sources(args,__file__)
    for path,digest in baseline.items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,('source-changed-during-check',path)
    sources.update(baseline)
    for path in [OUT/'native_campaign_message_emulator.py',OUT/'native_mfc_format_emulator.py',OUT/'native_event_step_emulator.py',OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_status_emulator.py',ROOT/'분석/InstallerVersions/installer_inventory.json']:
        sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    report=dict(sourceMessageOccurrences=len(commands),uniqueSourceMessages=len(distinct),safeAliasFormatComparisons=len(cases),boundedWideFormatComparisons=len(wide),malformedFormatRefusals=4,mismatches=0,unityPlay=False,
        nativeGameCtlSha256=n.sha256,additionalNativeLibraries=n.formatter.sources,
        scope='Actual GameCtl109E0/10810 parsing/alias calls and actual supplied MFC42/CRT ANSI Format in explicit C-locale image state. All unique source MESSAGE forms plus COM0..9/ME/ID/literal/two-argument/percent controls, mixed CP949/ASCII three-byte prefixes and native one-byte COM suffix atoi-to-zero controls. Roster, current team name and CString allocation/window creation are supplied; subject7 does not select the ME name. Safe %s/%% results and portrait job match production resolver. Source %S uses a wide pointer; production reproduces its bounded C-locale empty/single-ASCII conversion rather than normalize it to %s or read beyond an ANSI allocation. No Windows locale initialization, unsafe overread, original process/page rendering or Unity Play equivalence is claimed.',
        sources=sources,logs=str(WORK),safeCases=cases,wideCases=wide)
    (OUT/'native-campaign-message-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','safeCases','wideCases']},ensure_ascii=False))
if __name__=='__main__':main()
