"""Reproducible static combat-display evidence; never loads original DLL code."""
from pathlib import Path
import csv,hashlib,json,struct,subprocess
from PIL import Image
BASE=Path(__file__).resolve().parents[1]
ROOT=BASE/'설치본_추출/Saver_Files/Saver'
OUT=BASE/'분석결과'
RANGES=[('userunit.dll',0x1000cee0,0x1000d0c7,'AttackChar: shared flag 4, counters, relative offsets and facing'),('userunit.dll',0x1000ff17,0x100100fd,'Attack overlay source rectangles and optional extra effect'),('userunit.dll',0x100105d8,0x10010675,'Attack display counter increment, mode branches and cleanup'),('userunit.dll',0x1000cd80,0x1000cdec,'SetHpDisplay stores received HP and new-minus-old delta'),('userunit.dll',0x1000f950,0x1000facf,'HP change text, positive delta prefix and displayed HP interpolation'),('userunit.dll',0x10010675,0x100106d2,'HP display counter and flag cleanup'),('userunit.dll',0x1000e4e6,0x1000e75d,'Effect bitmap names and global surface bindings'),('GameCtl.dll',0x10001540,0x100018a0,'Constructors: external COLORREF array vs archive cover mode'),('GameCtl.dll',0x100018a0,0x10001a54,'INFO.TXT first two lines supply mask and cover counts'),('GameCtl.dll',0x10001b20,0x10001bbb,'Independent mask/color indices and two blend overloads')]
def write(name,rows):
 with (OUT/name).open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
def main():
 rows=[];asm=[]
 for file,start,end,meaning in RANGES:
  p=ROOT/file
  s=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={start}',f'--stop-address={end}',str(p)],text=True)
  rows.append(dict(file=file,start_va=hex(start),end_va_exclusive=hex(end),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),observation=meaning))
  asm.append(meaning+'\n'+s)
 write('전투표시_코드근거.csv',rows);(OUT/'전투표시_코드발췌.asm.txt').write_text('\n'.join(asm))
 frames=[]
 for t in range(16):
  col=(t//2)%4
  frames.append(dict(counter=t,fx2_left=col*32,fx2_top=32,fx2_right=(col+1)*32,fx2_bottom=64,fx_left=250+col*32,fx_top=240,fx_right=282+col*32,fx_bottom=272,mcursor_left=255+(t%2)*16,mcursor_top=0,mcursor_right=270+(t%2)*16,mcursor_bottom=15,scope='overlay rectangles; optional fx requires nonzero +294; not body attack frames'))
 write('전투표시_공격효과프레임.csv',frames)
 arc=BASE/'설치본_추출/ARC_해제/gameres'
 for name,path,rectkey in [('일반공격_fx2',arc/'etc2.arc/Fx2.bmp','fx2'),('추가타격_fx',arc/'etc.arc/Fx.bmp','fx')]:
  im=Image.open(path).convert('RGB');imgs=[]
  for v in frames[:8]:
   rect=tuple(v[rectkey+'_'+k] for k in ['left','top','right','bottom']);assert 0<=rect[0]<rect[2]<=im.width and 0<=rect[1]<rect[3]<=im.height
   imgs.append(im.crop(rect).resize((192,192),Image.Resampling.NEAREST))
  imgs[0].save(OUT/f'전투표시_{name}_순서.gif',save_all=True,append_images=imgs[1:],duration=80,loop=0)
 effects=[]
 for i in [1,2]:
  p=arc/f'effect{i}.arc';lines=(p/'info.txt').read_bytes().decode('cp949').splitlines();n,c=map(int,lines[:2]);masks=list(p.glob('*.rbm'));assert n==len(masks) and c==0
  effects.append(dict(archive=f'effect{i}.arc',mask_count=n,cover_count=c,verified_mask_files=len(masks),external_color_array_required_for_safe_current_draw_path=True,skill_binding='unresolved',delay_ms='caller dependent; constructor default 300'))
 write('전투표시_알파효과구성.csv',effects)
 summary=dict(evidence_ranges=len(rows),attack_overlay_counter_samples=len(frames),attack_sprite_frames=4,attack_ticks_per_sprite_frame=2,hp_delta='received_new_hp - previous_hp',hp_display_flag='0x40',attack_display_flag='0x4',alpha_archives=effects,original_binary_execution=False,server_damage_formula_analyzed=False)
 (OUT/'전투표시_분석요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
