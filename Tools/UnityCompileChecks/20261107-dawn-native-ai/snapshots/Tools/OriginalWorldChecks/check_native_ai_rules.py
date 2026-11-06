"""Actual112 AI file-loader outputs and parser controls against production raw tables."""
import hashlib,json,re,subprocess
from pathlib import Path
from native_ai_rules_emulator import AiParserNative
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-ai-rules')
def main():
    WORK.mkdir(exist_ok=True);cases=[];files=sorted((ROOT/'분석/Saver_Late/decoded/map').glob('*/*.ai'));assert len(files)==112
    for p in files:
        raw=p.read_bytes();assert all(c<128 for c in raw),p
        cases.append(dict(key=p.stem,text=raw.decode('ascii'),kind='file',seed=None))
    seed=[0x5a5a5a5a]*68
    for i,t in enumerate(['AREA(1,2,3)','AREA(1,2,3','AREA( 1,2,3)','AREA(1, 2, 3)','AREA(1,2,3)junk','SOILDER(4)','SOLDIER(4)','AREA(1,+2,3)','AREA','', '(1,2,3)','AREA(001,02,3)','SOILDER()']):cases.append(dict(key='move:'+str(i),text=t,kind='move',seed=seed))
    for i,t in enumerate(['ATTACK(30),MAGIC_104(40),REST(30)','MAGIC2_001(40),REST(60)','UNKNOWN(25)','42',' ATTACK(30)','ATTACK (30)','REST(-2),ATTACK(+10suffix)','REST(0),ATTACK(100)','ATTACK(10), REST(90)','','MAGIC_3_4(10)','REST(1),'*25]):cases.append(dict(key='actions:'+str(i),text=t,kind='actions',seed=seed))
    row='400 ODNORMAL AREA(1,2,3) ATTACK(100)'
    controls=['',row,row+'\n',row+'\n999\n410 ODATTACK AREA(3,4,5) REST(100)','999\n'+row,row+'\n\n410 ODATTACK AREA(3,4,5) REST(100)',row+'\n410',row+'\n410 ODATTACK',row+'\n410 ODATTACK SOILDER(0)',row+'\n410 ODATTACKL AREA(3,4,5) REST(100)','  '+row+'\t\r\n','400\tODNORMAL AREA(1,2,3) ATTACK(100)',';999\n'+row]
    for i,t in enumerate(controls):cases.append(dict(key='file-control:'+str(i),text=t,kind='file',seed=[0x12345678]*6800))
    for i in range(2):cases.append(dict(key='unavailable:'+str(i),text=None,kind='file',seed=[0x12345678]*6800))
    safety=[dict(key='too-many',text='\n'.join([row]*101),kind='safety',seed=None),dict(key='overflow',text='400 ODNORMAL AREA(999999999999999999,1,2) ATTACK(100)',kind='safety',seed=None)]
    catalog=ROOT/'Assets/_Original/Data/Campaign/NativeAiRules.json';data=json.loads(catalog.read_text());entries={e['key']:e for e in data['entries']};assert len(entries)==112
    for p in files:
        raw=p.read_bytes();assert entries[p.stem]==dict(key=p.stem,text=raw.decode('ascii'),sha256=hashlib.sha256(raw).hexdigest())
    metadata=Path(str(catalog)+'.meta');guid=re.search(r'^guid: ([a-f0-9]{32})$',metadata.read_text(),re.M)[1]
    asset=ROOT/'Assets/_Original/Data/Campaign/NativeCampaignArt.asset'
    assert re.findall(r'^  aiRules: \{fileID: 4900000, guid: ([a-f0-9]{32}), type: 3\}$',asset.read_text(),re.M)==[guid]
    inp=WORK/'cases.json';out=WORK/'managed.json';inp.write_text(json.dumps(cases+safety,ensure_ascii=False))
    sdk=magic.UNITY/'DotNetSdk';exe=WORK/'AiRules.dll';refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
    sources=[ROOT/'Assets/_Original/Runtime/Combat/NativeAiRules.cs',OUT.resolve()/'NativeAiRuleDifferential.cs']
    args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe),*refs,*map(str,sources)];rsp=WORK/'checks.rsp';rsp.write_text('\n'.join('"'+s+'"'for s in args))
    version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name;exe.with_suffix('.runtimeconfig.json').write_text(json.dumps(dict(runtimeOptions=dict(tfm='net8.0',framework=dict(name='Microsoft.NETCore.App',version=version)))))
    for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(out)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    managed=json.loads(out.read_text());assert len(managed)==len(cases)+len(safety);n=AiParserNative();differences=[];rows=0
    try:
        for c,m in zip(cases,managed):
            assert c['key']==m['key']and m['ok'],m
            if c['kind']=='move':native=n.move(c['text']);native+=c['seed'][len(native):]
            elif c['kind']=='actions':native=n.actions(c['text'])
            else:
                native=n.load(c['text']or'',key=c['key']if c['key'].isdigit()else'04',seed=c['seed'],opened=c['key']!='unavailable:0',extracted=c['key']!='unavailable:1')
                if c['kind']=='file'and c['key'].isdigit():rows+=sum(native[i*68]not in [-1,999]for i in range(100))
            bad=[i for i,(a,b)in enumerate(zip(native,m['output']))if a!=b]
            if bad:differences.append(dict(key=c['key'],fields=[dict(index=i,native=native[i],managed=m['output'][i])for i in bad[:12]]))
    finally:n.close()
    assert all(not r['ok']and r['output']is None for r in managed[len(cases):])
    (WORK/'differences.json').write_text(json.dumps(differences,ensure_ascii=False,indent=2)+'\n')
    paths=[Path(__file__),OUT/'native_ai_rules_emulator.py',OUT/'native_ai_emulator.py',OUT/'native_battle_briefing_emulator.py',*sources,*files,catalog,metadata,ROOT/'Assets/_Original/Data/Campaign/NativeCampaignArt.asset',ROOT/'Assets/_Original/Runtime/Campaign/NativeCampaignArt.cs',OUT/'build_native_ai_rules.py']
    report=dict(sourceFiles=len(files),matchedRuleRows=rows,parserControlCases=len(cases)-len(files),safeRefusals=len(safety),mismatches=len(differences),unityPlay=False,originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',originalSetunitSha256=n.setunit_sha,
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},logs=str(WORK),scope='Actual UserUnit02B40/02670/027D0/F4E0 and relocated original setunit GetStrTail/Head/Tails. CString/file/archive/format/atoi allocation are bounded hosts; compare all100x68 table fields including stale-token and unused-field retention. Positive numeric values stay in signed32; unsafe row overflow is refused rather than emulated. Not AI decisions, spawning, runtime0C, Windows/Unity Play or actual file/heap failure behavior.')
    (OUT/'native-ai-rules-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'},ensure_ascii=False));assert not differences,differences[:3]
if __name__=='__main__':main()
