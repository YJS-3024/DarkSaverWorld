"""Verify sound-number filenames against soundll loader and local archive."""
from analyze_special_skills import PE,ROOT,OUT,ARC,table,write
import hashlib,json,subprocess,collections,wave

def main():
 p=PE(ROOT/'soundll.dll');assert p.string(0x10006034)=='0%d.wav' and p.string(0x1000603c)=='%d.wav' and p.string(0x10006044)=='gameres\\sound.arc'
 files={f.name.lower():f for f in (ARC/'sound.arc').iterdir() if f.suffix.lower()=='.wav'};refs=collections.Counter(int(t[i]) for t in table('MagicPtn') for i in [17,18] if int(t[i])>0);rows=[]
 for id in range(100):
  name=f'{id:02}.wav';f=files.get(name)
  row=dict(sound_id=id,loader_filename=name,asset_present=bool(f),pattern_references=refs[id],missing_behavior='null sound slot; play and stop skip null',sha256=hashlib.sha256(f.read_bytes()).hexdigest() if f else '')
  rows.append(row)
 write('효과음_ID_실제파일.csv',rows)
 links=[]
 for t in table('MagicPtn'):
  for col in [17,18]:
   id=int(t[col]);name=f'{id:02}.wav' if id>=0 else ''
   links.append(dict(pattern_id=int(t[0]),pattern_name=t[1],sound_column=col-16,sound_id=id,positive_id_guard_in_verified_handlers=id>0,asset_present=name in files if name else False,file=name,interpretation='missing asset -> null slot' if id>0 and name not in files else 'requires positive-ID call guard; ID 0 file exists'))
 write('특수기_효과음_파일대응.csv',links)
 ranges=[(0x100020a3,0x10002150,'LoadSound: slots 0..99, two-digit names for 0..9, null for missing archive entries'),(0x10002190,0x100021c5,'Play/stop checks indexed slot for null')];asm=[];ev=[]
 for a,b,n in ranges:
  asm.append(n+'\n'+subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'soundll.dll')],text=True));ev.append(dict(file='soundll.dll',start_va=hex(a),end_va_exclusive=hex(b),observation=n,sha256=hashlib.sha256((ROOT/'soundll.dll').read_bytes()).hexdigest()))
 write('효과음_코드근거.csv',ev);(OUT/'효과음_코드발췌.asm.txt').write_text('\n'.join(asm))
 summary=dict(loader_slots=100,actual_wav_files=len(files),positive_referenced_ids=len(refs),positive_missing_ids=[id for id in sorted(refs) if f'{id:02}.wav' not in files],id_zero_asset_exists='00.wav' in files,original_execution=False)
 (OUT/'효과음_연결요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
