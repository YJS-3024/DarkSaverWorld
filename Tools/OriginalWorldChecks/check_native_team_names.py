"""Original5C60 parsing and status-header selection versus actual managed codecs."""
import hashlib,json,struct,subprocess
from pathlib import Path
import check_magic_projectiles as magic
from native_battle_team_emulator import NativeBattleTeams
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-team-name-checks')
def vectors():
    names=[b'',b'A','검사'.encode('cp949'),b'x'*39,b'A\0B']
    cases=[]
    for count in (0,1,2,9,10,21):
        for variant in range(5):
            rows=[dict(team=i+1,name=names[(i+variant)%len(names)])for i in range(count)]
            if count>1 and variant==4:rows[1]['team']=rows[0]['team']
            if count and variant==3:rows[0]['team']=-2
            packet=b'\x5c\x60'+struct.pack('<h',count)+b''.join(struct.pack('<hB',r['team'],len(r['name']))+r['name']for r in rows)
            teams=list(dict.fromkeys([r['team']for r in rows]+[-32768]))
            cases.append(dict(packet=list(packet),teams=teams,localTeam=rows[-1]['team']if count else -99))
    return cases
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeTeamDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeTeamDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[];headers=0
    for case,current in zip(cases,managed):
        native=NativeBattleTeams();result=native.receive(case['packet'],case['localTeam'])
        actual=[native.header(team)for team in case['teams']];headers+=len(actual)
        if result['count']!=len(current['rows'])or result['rows']!=current['rows']or actual!=current['names']or not current['roundtrip']:
            mismatches.append(dict(case=case,native=result,headers=actual,managed=current))
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,ensure_ascii=False,indent=2))
    paths=[Path(__file__),OUT/'NativeTeamDifferential.cs',OUT/'native_battle_team_emulator.py',OUT/'native_battle_status_emulator.py',OUT/'native_battle_briefing_emulator.py',OUT/'native_battle_options_emulator.py',ROOT/'Assets/_Original/Runtime/Combat/NativeBattleTeamNames.cs']
    result=dict(cases=len(cases),nativeHeaderComparisons=headers,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,originalGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},scope='Actual GameCtl dispatch5C60/D550 variable team records and100054C0 name selection callback. Verify signed team IDs, empty/CP949/39-byte/embedded-NUL names, first duplicate and first9 search across0/1/2/9/10/21 records. CString/graphics/font callback are host boundaries; uninitialized stack tail and unsafe count/name writes are not reproduced. Parsed team local-name assignment executes but is not treated as permanent account rename. No Windows/Unity Play, pixels or actual server roster.',logs=str(WORK))
    (OUT/'native-team-name-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}));assert not mismatches
if __name__=='__main__':main()
