"""Link RBM archives to the actual COMBINE_CLASS scene command, without executing scripts."""
from analyze_special_skills import PE,ROOT,OUT,BASE,write
import csv,json,hashlib,subprocess,re

def main():
 pe=PE(ROOT/'GameCtl.dll');assert pe.string(0x1001a578)=='COMBINE_CLASS';assert pe.string(0x1001a090)=='gameres\\effect1.arc';assert pe.string(0x1001a0a4)=='gameres\\effect2.arc'
 cases=[];characters=[]
 for folder in sorted((BASE/'설치본_추출/ARC_해제/MAP').iterdir()):
  for p in folder.glob('*.DEE.utf8.txt'):
   lines=p.read_text().splitlines()
   for i,line in enumerate(lines):
    if not line.startswith('COMBINE_CLASS '):continue
    t=list(map(int,line.split()[1:]));assert len(t)==5;n,x,y,steps,delay=t
    cases.append(dict(archive=folder.name,file=p.name,command_line=i+1,count=n,target_x=x,target_y=y,move_steps=steps,step_delay_ms=delay,phase1_mask='effect1.arc',phase2_mask='effect2.arc',color='COLORREF 0x00ffffff',color_count=1,mask_delay_ms=300,hold_threshold_ms=3000,fade_alpha_step_per_call=8))
    for j in range(n):
     cid,cx,cy=map(int,lines[i+1+j].split());characters.append(dict(archive=folder.name,id=cid,x=cx,y=cy,source_rect='32,32,64,64',position_formula='start + trunc((target-start)*counter/steps)',overlay_center_offset='16,16'))
 assert len(cases)==3
 write('RBM_실제호출_장면.csv',cases);write('RBM_합성연출_캐릭터.csv',characters)
 states=[dict(state='moving',asset='effect1.arc',color='white',timing='elapsed > script delay, one step per draw; no catch-up',transition='step exceeds script steps; replace alpha object with effect2',scope='COMBINE_CLASS visual effect only'),dict(state='hold',asset='effect2.arc',color='white',timing='elapsed >= 3000ms',transition='state 2, reset falling counter and fade alpha',scope='COMBINE_CLASS visual effect only'),dict(state='fall_and_fade',asset='effect2.arc',color='white',timing='one falling/fade step per call',transition='alpha+8 >255; state3 done',scope='y offset=6*old counter, alpha 0..248; ToTheColor output not native-pixel verified')]
 write('RBM_합성연출_단계.csv',states)
 ranges=[(0x1001392b,0x10013ad5,'COMBINE_CLASS parser, five header integers, character rows and factory'),(0x10002070,0x10002143,'Factory argument transfer: target XY, move steps, delay and character surfaces'),(0x10002150,0x10002240,'Effect1 constructor: white array count1; default movement values'),(0x100022b0,0x10002325,'Movement clock and transition'),(0x10002330,0x1000245a,'32x32 character patch and linearly interpolated mask centers'),(0x10002460,0x100025bb,'Stage changes and Effect2 creation'),(0x100025c0,0x10002679,'Three-second hold, falling Y, white fade per call'),(0x10001d30,0x10001dc8,'Mask/color index clock and color-mask AlphaBlit')]
 ev=[];asm=[]
 for a,b,n in ranges:
  ev.append(dict(file='GameCtl.dll',start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256((ROOT/'GameCtl.dll').read_bytes()).hexdigest(),observation=n));asm.append(n+'\n'+subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'GameCtl.dll')],text=True))
 write('RBM_실제호출_코드근거.csv',ev);(OUT/'RBM_실제호출_코드발췌.asm.txt').write_text('\n'.join(asm))
 summary=dict(scene_commands=len(cases),character_rows=len(characters),archives=['effect1.arc','effect2.arc'],external_color='0x00ffffff',external_color_count=1,alpha_mask_delay_ms=300,meaning='COMBINE_CLASS scripted composition visual, not a demonstrated combat-skill mask binding',native_pixel_validation=False,original_execution=False)
 (OUT/'RBM_실제용도_요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
