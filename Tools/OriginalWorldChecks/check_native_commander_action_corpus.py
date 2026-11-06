"""Original commander selection -> target query -> target build -> emitted packet.
Production codecs consume the captured constructor inputs, not decoded expected bytes.
"""
import csv,hashlib,json,subprocess
from pathlib import Path
from native_commander_action_corpus import NativeCommanderActionCorpus
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-commander-action-corpus')
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
    WORK.mkdir(exist_ok=True);table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv'
    rows=[{key:int(r[column])for key,column in [('id','번호'),('mp','MP'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('attribute','속성'),('pattern','패턴')]}for r in csv.DictReader(table.open(encoding='utf-8-sig'))]
    assert len(rows)==440;cases=[];kinds={};hp0=0;far=0
    control_names=['hp29','hp30','inactive','moving','no-ready','no-row','rest-hurt','rest-full','zero-rest0','zero-rest1','invalid-magic','no-target']
    matrix=[(row,variant)for row in rows for variant in ['ready','status80']+(['mp-short']if row['mp']>0 else[])] +[(rows[0],name)for name in control_names]
    for row,variant in matrix:
            native=NativeCommanderActionCorpus(row,variant);result=native.run()
            empty=variant in ['inactive','moving','no-ready','no-row','rest-full','no-target']
            assert len(result['inputs'])==len(result['packets'])==(0 if empty else 1),(row,variant,result)
            expected_state=17 if variant=='moving'else 0 if variant=='no-ready'or not empty else 16
            assert result['ap']==(20 if empty else 0) and result['state']==expected_state and result['order']==(4 if variant=='no-row'else 2),(row,variant,result)
            if variant in ['hp29','rest-hurt','zero-rest0']:assert result['inputs'][0]['kind']=='rest'
            if variant=='invalid-magic':assert result['inputs'][0]['kind']=='attack'
            for i in result['inputs']:
                kinds[i['kind']]=kinds.get(i['kind'],0)+1
                units={u['id']:u for u in native.case['units']}
                if i['kind']=='magic':
                    assert all(units[t].get('nativeActive',True)and units[t].get('nativeEnabled',True)for t in i['targets'])
                    hp0+=sum(units[t].get('hp',100)==0 for t in i['targets'])
                elif i['kind']=='attack':
                    a,b=units[i['source']],units[i['target']];far+=abs(a['x']-b['x'])+abs(a['y']-b['y'])>1
            cases.append(dict(metadata=row,variant=variant,**result))
    input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'CommanderPacketCorpus.dll';harness=OUT/'NativeCommanderPacketCorpus.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for p in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(input),str(output)])]:
        p=subprocess.run(command,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr);assert p.returncode==0,(p.stdout+p.stderr)[-2500:]
    managed=json.loads(output.read_text());assert len(managed)==len(cases)
    differences=[dict(index=i,metadata=c['metadata'],variant=c['variant'],native=c['packets'],managed=m)for i,(c,m)in enumerate(zip(cases,managed))if c['packets']!=m]
    (WORK/'differences.json').write_text(json.dumps(differences,indent=2)+'\n')
    from check_native_battle_responses import contract_sources
    sources=contract_sources(args,__file__)
    for p in [table,OUT/'native_commander_action_corpus.py',OUT/'check_native_magic_target_catalog.py',OUT/'check_native_magic_packets.py',OUT/'native_ai_emulator.py',ROOT/'분석/Ghidra/Saver_Late/MonsterCommanderAi/100042e0.c']:
        sources[p.relative_to(ROOT).as_posix()]=sha(p)
    result=dict(tableRows=len(rows),nativeActionCases=len(cases),controlCases=len(control_names),packetKinds=kinds,hp0MagicTargetOccurrences=hp0,outsideBasicRangeAttackRequests=far,mismatches=len(differences),unityPlay=False,runtimeCommanderAiConnected=False,
        originalUserUnitSha256='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',sources=sources,casesSha256=sha(input),logs=str(WORK),
        scope='Actual042E0 executes real03860/03A40/03C00, CheckMagicTarget, original attack/rest/magic builders and compares complete emitted application bytes with current production codecs using separately captured builder inputs. All440 metadata rows at ready/status80 and valid insufficient MP, plus12 full-body HP/flags/missing-rule/rest/zero-weight/invalid-magic/no-target controls. Exact map/received teams/rand/metadata and bounded allocation/free/final send supplied; no target query/target-build/send-builder replacement. All runtime unit bytes checked: only actor AP/100/108 may change. No server acceptance/range/damage/heal/cost/reward formula, complete0C scheduling or Unity/Windows playback claim.')
    (OUT/'native-commander-action-corpus-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'},ensure_ascii=False));assert not differences,differences[:3]
if __name__=='__main__':main()
