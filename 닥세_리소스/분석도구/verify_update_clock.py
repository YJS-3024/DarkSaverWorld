"""Verify that the 80ms movement gate does not gate all combat display counters."""
from trace_special_renderers import Trace
from analyze_special_skills import table,OUT,write
import json,subprocess

def main():
 t=Trace();p=table('MagicPtn')[0];rows=[]
 for age in [0,79,80,81,500,501]:
  for counter in [0,1,10]:
   t.input_overrides={0x1017fa14:100000-age,0x30000100:0x44,0x30000158:counter,0x3000015c:counter,0x30000160:-10,0x1010a3d8:1}
   r=t.run(p,0x10010ec0,'update',counter)
   assert r['ok'],r
   assert r['movement_gate']==int(age>=80)
   assert r['attack_counter']==counter+1 and r['hp_counter']==counter+1
   rows.append(dict(age_ms=age,initial_counter=counter,movement_gate=r['movement_gate'],attack_after=r['attack_counter'],hp_after=r['hp_counter'],same_current_tick=100000,method='isolated original instructions; GetTickCount fixed'))
 write('전투_80ms게이트_검증.csv',rows)
 parts=[]
 for name,a,b in [('userunit.dll',0x10010ec0,0x10010fbb),('userunit.dll',0x100110a6,0x100110b9),('Saver.exe',0x42e710,0x42e741),('Saver.exe',0x42e7d0,0x42e7ec)]:
  from analyze_special_skills import ROOT
  parts.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/name)],text=True))
 (OUT/'전투_갱신단계_코드발췌.asm.txt').write_text('\n'.join(parts));print('18개 시간 경계 검증 통과: 공격/HP 표시는 80ms 미만에서도 증가')
if __name__=='__main__':main()
