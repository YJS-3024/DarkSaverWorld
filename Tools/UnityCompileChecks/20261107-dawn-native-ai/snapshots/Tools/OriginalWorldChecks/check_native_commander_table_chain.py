"""Original commander body/targets/wire with real setunit legacy table projection."""
import csv,hashlib,json
from pathlib import Path
from native_commander_action_corpus import NativeCommanderActionCorpus
from native_magic_table_emulator import SetunitMagicTable
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class LinkedCommander(NativeCommanderActionCorpus):
    def __init__(self,*args):super().__init__(*args);self.nativeTable=SetunitMagicTable(self)
    def _dispatch(self,uc,address,size,data):
        if 'GetMagicTbl@@'in self.hooks.get(address,''):self.nativeTable.redirect();return
        super()._dispatch(uc,address,size,data)
def main():
    table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';rows=[{k:int(r[c])for k,c in [('id','번호'),('mp','MP'),('attribute','속성'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('pattern','패턴')]}for r in csv.DictReader(table.open(encoding='utf-8-sig'))]
    controls=['hp29','hp30','inactive','moving','no-ready','no-row','rest-hurt','rest-full','zero-rest0','zero-rest1','invalid-magic','no-target']
    matrix=[(row,variant)for row in rows for variant in ['ready','status80']+(['mp-short']if row['mp']>0 else[])]+[(rows[0],name)for name in controls]
    cases=[];kinds={}
    for row,variant in matrix:
        plain=NativeCommanderActionCorpus(row,variant).run();linked=LinkedCommander(row,variant).run()
        assert linked==plain,(row,variant,plain,linked)
        for request in linked['inputs']:kinds[request['kind']]=kinds.get(request['kind'],0)+1
        cases.append(dict(metadata=row,variant=variant,result=linked))
    # Audit raw AI references, preserving original typos rather than fixing them.
    import re
    ai=ROOT/'Assets/_Original/Data/Campaign/NativeAiRules.json';ids={r['id']for r in rows};missing=[]
    for entry in json.loads(ai.read_text())['entries']:
        for line,text in enumerate(entry['text'].splitlines(),1):
            for raw in re.findall(r'MAGIC2?_(\d+)',text):
                if int(raw)not in ids:missing.append(dict(map=entry['key'],line=line,magic=int(raw)))
    assert missing==[dict(map='81',line=3,magic=80601),dict(map='81',line=15,magic=90)],missing
    paths=[Path(__file__),table,ai,*[OUT/name for name in ['native_magic_table_emulator.py','native_commander_action_corpus.py','check_native_magic_packets.py','check_native_magic_target_catalog.py','check_native_move_hints.py','check_native_outbound_packets.py','native_ai_emulator.py']]]
    report=dict(nativeCommanderCases=len(cases),nativeBodyExecutions=len(cases)*2,packetKinds=kinds,missingAiSpellReferences=missing,mismatches=0,unityPlay=False,runtime0CConnected=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},casesSha256=hashlib.sha256(json.dumps(cases,separators=(',',':')).encode()).hexdigest(),
      scope='Actual UserUnit042E0/targets/CheckMagicTarget/wire with real relocated setunit GetMagicTbl, compared with explicit corrected legacy-field boundary for all440 rows and readiness/status80/MP-short plus12 controls. Compare full packet/constructor inputs/AP/100/108/random/byte mutations. Original CSV320-byte rows and loaded flag supplied; file loader/pattern payload are outside scope. AI81 missing80601/90 audited without replacement or operational effect claim. No production commander scheduling/server rules/Windows/Unity Play.')
    (OUT/'native-commander-table-chain-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
