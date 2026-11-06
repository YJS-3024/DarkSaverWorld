"""Original AI81 typos with actual parser and setunit stale lookup cache."""
import csv,hashlib,json,struct
from pathlib import Path
from native_ai_rules_emulator import AiParserNative
from check_native_commander_table_chain import LinkedCommander
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def main():
    ai=ROOT/'Assets/_Original/Data/Campaign/NativeAiRules.json';source=next(e['text']for e in json.loads(ai.read_text())['entries']if e['key']=='81')
    parser=AiParserNative()
    try:table=parser.load(source,key='81')
    finally:parser.close()
    rows=[table[i:i+68]for i in range(0,len(table),68)];rules={r[0]:r for r in rows if r[0]in [420,540]};assert set(rules)=={420,540}
    csvfile=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';metadata={int(r['번호']):{k:int(r[c])for k,c in [('id','번호'),('mp','MP'),('attribute','속성'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('pattern','패턴')]}for r in csv.DictReader(csvfile.open(encoding='utf-8-sig'))}
    cases=[]
    for key,missing,roll in [(420,80601,51),(540,90,61)]:
        row=rules[key];assert row[12]==2 and row[13]==missing
        for prime in [101,806,906]:
            n=LinkedCommander(metadata[806]);n.nativeTable.call(prime)
            adjusted=row.copy();adjusted[0]=400 # Controlled unit-key remap only; no original spawn position is claimed.
            n.uc.mem_write(0x100d99a0,struct.pack('<68i',*adjusted));n.case['random']=[roll,0,2];n.write(n.RUNTIME+400*0x2e0+0x28,100)
            result=n.run();assert result['inputs']and result['inputs'][0]['kind']=='magic'and result['inputs'][0]['magic']==-1,(key,prime,result)
            packet=bytes.fromhex(result['packets'][0]);assert packet[:5]==b'\x5c\x89\x01\xff\xff'
            cases.append(dict(sourceMap='81',sourceUnit=key,sourceMagic=missing,controlledActor=400,primedValidMagic=prime,roll=roll,result=result))
    assert len({c['result']['packets'][0]for c in cases})>1,'Different prior valid metadata must expose different stale results'
    paths=[Path(__file__),ai,csvfile,*[OUT/name for name in ['native_ai_rules_emulator.py','native_magic_table_emulator.py','check_native_commander_table_chain.py','native_commander_action_corpus.py','check_native_magic_packets.py','check_native_magic_target_catalog.py','check_native_move_hints.py','check_native_outbound_packets.py','native_ai_emulator.py']]]
    report=dict(nativeCases=len(cases),originalParsedRules=len(rules),invalidMagicRequests=len(cases),mismatches=0,unityPlay=False,runtimeMissingMagicAliasAdded=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},cases=cases,
      scope='Actual UserUnit parser reads AI81; source420/540 action rows copied with only test actor key remapped to400. Actual commander/target/CheckMagicTarget/wire and real setunit getter run with explicit prior101/806/906 cache and roll51/61. Each missing spell emits legacy magic-1 with stale prior metadata; this is original invalid-request evidence, not an invented spell alias, actual server approval or map81 spawn/Play. Actor MP100, map/grid/team and file/allocator/send boundaries are supplied controls.')
    (OUT/'native-ai-missing-magic-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','cases']},ensure_ascii=False))
if __name__=='__main__':main()
