"""Compare common body renderer output across combat display flags, with preloaded mock surfaces."""
from trace_special_renderers import Trace
from analyze_special_skills import OUT,table,write
import json

def main():
 tr=Trace();tr.body_mode=True;p=table('MagicPtn')[0];rows=[]
 for id in [100,345,445,700,750,999]:
  for facing in range(1,9):
   baseline=None
   for flags in [0,4,0x40,0x100,0x104,0x140]:
    tr.body_case=(id,flags,facing);r=tr.run(p,0x1000b5d0,'body',0)
    if not r['ok']:raise RuntimeError((id,flags,facing,r))
    draws=[e for e in r['events'] if e.get('surface')=='0x60003000'];assert len(draws)==(id!=999)
    rect=draws[0]['src'] if draws else []
    if baseline is None:baseline=rect
    assert rect==baseline,(id,flags,facing)
    rows.append(dict(display_id=id,facing=facing,flags=hex(flags),body_source_rect=json.dumps(rect),same_as_no_combat_flag=rect==baseline,scope='common idle body renderer, preloaded surface, fixed GetTickCount 100000; movement/other callers excluded'))
 write('전투_본체표시플래그_대조.csv',rows)
 (OUT/'전투_본체표시_대조요약.json').write_text(json.dumps(dict(cases=len(rows),all_match_baseline=True,display_ids=[100,345,445,700,750,999],mode='isolated x86 emulation with mocked preloaded surfaces; not Windows capture'),ensure_ascii=False,indent=2));print('본체 표시',len(rows),'조건 대조 통과')
if __name__=='__main__':main()
