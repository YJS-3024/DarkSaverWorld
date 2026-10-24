"""Static code evidence and display/input metadata. Never executes game code."""
from pathlib import Path
import csv,json,hashlib,subprocess,struct
from PIL import Image,ImageDraw,ImageChops
from decode_visuals import read_ui_library,read_lsc,read_grs

# Evidence addresses are scoped to this local executable, not a general ABI.
EVIDENCE=[
 ('DManage.dll',0x10008410,0x100084a0,'GRS surface creation calls SetColorKey(0)'),
 ('DManage.dll',0x10006300,0x10006346,'Converts COLORREF to native pixel and sets source color key'),
 ('DManage.dll',0x10001dd3,0x10001e87,'16-bit image/mask blend; skip zero source; multiply then shift 8'),
 ('Saver.exe',0x403870,0x4038e6,'Button constructor: rect=(x,y,x+width,y+height)'),
 ('Saver.exe',0x403970,0x4039c6,'State 0 gives hit rect; states 1/2/3 select source segments'),
 ('Saver.exe',0x403ab0,0x403bf6,'Pointer-in-rect via PtInRect; state 4 excluded from hover'),
 ('Saver.exe',0x404000,0x40412f,'LTN surface strip uses 3 widths for extra=0 and 2 for nonzero'),
 ('Saver.exe',0x4341e2,0x43425f,'Final scene list creates animated-image objects via 402EF0'),
 ('Saver.exe',0x402ef0,0x402f72,'Animated image ctor copies dimensions, frame count and delay from LNM'),
 ('Saver.exe',0x4030b0,0x4030f6,'LNM animation advances one frame on elapsed > delay, wraps at frame count'),
 ('userunit.dll',0x1000ac70,0x1000ac8a,'Frame sequence from GetTickCount: 0,1,2,1'),
 ('userunit.dll',0x1000b9ea,0x1000ba54,'General idle facing row and 333ms frame period'),
 ('userunit.dll',0x1000bd9e,0x1000bdd2,'Movement 125ms period; special ID range uses modulo 3'),
 ('userunit.dll',0x1000bdf8,0x1000bf57,'Movement direction numbers -> row offset and pixel displacement'),
 ('GameCtl.dll',0x10001b20,0x10001b60,'Effect frame advance when elapsed > delay, advances once per draw'),
]

def blend16(dst,src,alpha,masks=(0xf800,0x07e0,0x001f)):
 if src==0:return dst
 return sum((((dst&m)+(((src&m)-(dst&m))*alpha>>8))&m) for m in masks)

def main():
 base=Path(__file__).resolve().parents[1];root=base/'설치본_추출/Saver_Files/Saver';out=base/'분석결과'
 def write(n,rows):
  with (out/n).open('w',encoding='utf-8-sig',newline='') as f:
   w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
 evidence=[];assembly=[]
 for dll,start,end,meaning in EVIDENCE:
  p=root/dll;s=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={start}',f'--stop-address={end}',str(p)],text=True)
  evidence.append(dict(file=dll,start_va=hex(start),end_va_exclusive=hex(end),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),observation=meaning,method='static disassembly, no original code execution'))
  assembly.append(meaning+'\n'+s)
 write('표시입력_코드근거.csv',evidence);(out/'표시입력_코드발췌.asm.txt').write_text('\n'.join(assembly))
 libs={};states=[];modecounts={};preview=Image.new('RGB',(900,330),'#777777');dr=ImageDraw.Draw(preview)
 for folder,stem in [('Glib','GameLib'),('Wlib','waitlib')]:
  p=next(p for p in (root/folder).iterdir() if p.name.lower()==(stem+'.ltn').lower());items,atlas,_=read_ui_library(p);libs[folder]=items
  for v in items.values():
   extra=v['extra'][0];n=2 if extra else 3
   for segment in range(n):
    x=v['x']+segment*v['width'];y=v['y'];assert x+v['width']<=atlas.width and y+v['height']<=atlas.height
    states.append(dict(library=p.relative_to(root).as_posix(),id=v['id'],extra=extra,segment=segment,x=x,y=y,width=v['width'],height=v['height'],meaning=['base','pointer_inside','code_3'][segment] if extra==0 else ['latched_state_1','latched_state_2'][segment]))
  if folder=='Glib':
   for row,id in enumerate([19,37,38]):
    v=items[id];dr.text((5,row*100+8),f'ID {id}',fill='white')
    for col in range(3):
     x=v['x']+col*v['width'];y=v['y'];im=atlas.crop((x,y,x+v['width'],y+v['height'])).convert('RGB');im=im.resize((v['width']*3,v['height']*3),Image.Resampling.NEAREST)
     preview.paste(im,(90+col*270,row*100+25));dr.text((90+col*270,row*100+7),['base','pointer inside','code 3'][col],fill='white')
 preview.save(out/'UI_버튼상태_표본.png');write('UI_버튼상태_아틀라스.csv',states)
 hits=[];animations=[]
 for folder in ['Glib','Wlib']:
  for p in sorted((root/folder).glob('*.lsc')):
   scene=read_lsc(p)
   for v in scene['buttons']:
    if v['kind']=='animation':
     animations.append(dict(scene=p.relative_to(root).as_posix(),id=v['id'],x=v['x'],y=v['y']))
     continue
    r=libs[folder][v['id']];left,top=v['x'],v['y'];right,bottom=left+r['width'],top+r['height']
    hits.append(dict(scene=p.relative_to(root).as_posix(),kind=v['kind'],id=v['id'],left=left,top=top,right_exclusive=right,bottom_exclusive=bottom,hit_rule='left <= x < right and top <= y < bottom',normal_button_code_verified=v['kind']=='button',coordinates='scene local; caller screen transform not covered'))
 write('UI_입력사각형.csv',hits)
 # Direction decoding from the common movement draw branch.
 dirs=[(1,'LEFT',3,-8,0),(2,'RIGHT',2,8,0),(3,'UP',0,0,-8),(4,'DOWN',1,0,8),(5,'UP_LEFT',3,-5,-5),(6,'DOWN_LEFT',3,-5,5),(7,'UP_RIGHT',2,5,-5),(8,'DOWN_RIGHT',2,5,5)]
 write('캐릭터_방향코드.csv',[dict(code=c,direction=n,row32=row,x_multiplier=dx,y_multiplier=dy,scope='common movement draw path; special sprites require separate handling') for c,n,row,dx,dy in dirs])
 write('캐릭터_표시경로.csv',[dict(path='idle',period_ms=333,sequence='0,1,2,1',source='userunit.dll RVA B9EA-BA54',scope='common path, plus separate 64px branch'),dict(path='move',period_ms=125,sequence='0,1,2,1',source='userunit.dll RVA BD9E-BDD2',scope='common path; IDs 980<=id<999 use 0,1,2')])
 # Reconstructed timing comparison, not an original-game recording.
 sheet=base/'설치본_추출/ARC_해제/gameres/char0.arc/100r.bmp';im=Image.open(sheet).convert('RGB');frames=[]
 for tick in range(16):
  t=tick*125;canvas=Image.new('RGB',(360,310),'#777777');d=ImageDraw.Draw(canvas)
  for ci,period in enumerate([333,125]):
   col=[0,1,2,1][(t//period)%4];d.text((ci*180+10,6),f'{period}ms step',fill='white')
   for row in range(4):
    patch=im.crop((col*32,row*32,(col+1)*32,(row+1)*32)).resize((64,64),Image.Resampling.NEAREST);canvas.paste(patch,(ci*180+60,row*70+26));d.text((ci*180+3,row*70+28),['UP','DOWN','RIGHT','LEFT'][row],fill='white')
  frames.append(canvas)
 frames[0].save(out/'캐릭터_대기이동_시간비교.gif',save_all=True,append_images=frames[1:],duration=125,loop=0)
 # Final LSC list is animated imagery, resolved through LNM instead of LTN.
 animlib=next(p for p in (root/'Wlib').iterdir() if p.name.lower()=='waitlib.lnm')
 ai,aa,_=read_ui_library(animlib);v=ai[1];af=[]
 for k in range(v['extra'][0]):
  x=v['x']+k*v['width'];im=aa.crop((x,v['y'],x+v['width'],v['y']+v['height'])).convert('RGB').resize((192,192),Image.Resampling.NEAREST)
  panel=Image.new('RGB',(230,220),'#777777');panel.paste(im,(19,24));ImageDraw.Draw(panel).text((8,6),f'LNM ID 1 / frame {k}',fill='white');af.append(panel)
 af[0].save(out/'UI_대기화면_14프레임.gif',save_all=True,append_images=af[1:],duration=v['extra'][1],loop=0)
 pads=[]
 for name in ['345R.bmp','445R.bmp']:
  im=Image.open(base/'설치본_추출/ARC_해제/gameres/char2.arc'/name).convert('RGB');right=im.crop((96,0,98,128)).tobytes();assert not any(right)
  pads.append(dict(file=name,width=im.width,height=im.height,common_frame_width=32,usable_sheet_width=96,extra_right_columns=2,extra_rgb_all_black=True))
 write('캐릭터_98픽셀_여백검증.csv',pads)
 # Quantify the old palette-index assumption vs verified RGB key on all frames.
 diffs=[]
 for p in sorted((root/'MAP').glob('*.grs')):
  grs=read_grs(p)
  for kind,gs in [('tile',grs['groups']),('sprite',grs['sprites'])]:
   for gi,g in enumerate(gs):
    blacks={i for i in range(256) if g['palette'][i*4:i*4+3]==b'\0\0\0'}
    for fi,b in enumerate(g['images']):
     d=b['data'][:b['width']*b['height']];changed=sum((v==255)!=(v in blacks) for v in d)
     if changed:diffs.append(dict(grs=p.name,kind=kind,group=gi,frame=fi,changed_alpha_pixels=changed,black_palette_indices=','.join(map(str,sorted(blacks)))))
 if diffs:write('RGB색상키_수정영향.csv',diffs)
 # Endpoint observations for the reconstructed 16-bit image-mask kernel.
 values=[]
 for dst,src in [(0,0xffff),(0xffff,0),(0x001f,0xf800),(0xf800,0x07e0)]:
  for alpha in [0,1,128,255]:values.append(dict(dst=hex(dst),src=hex(src),alpha=alpha,result_rgb565=hex(blend16(dst,src,alpha)),method='Python reproduction of static kernel; not original DLL execution'))
 write('RBM_합성수식_표본.csv',values)
 assert blend16(0x1234,0,255)==0x1234
 assert blend16(0x1234,0xffff,0)==0x1234
 # Actual mask over a constant source/destination, to inspect raw blend direction.
 mask=base/'설치본_추출/ARC_해제/gameres/effect1.arc/mask1.rbm';b=mask.read_bytes();_,w,h,stride=struct.unpack_from('<4I',b);raw=b[16:];result=Image.new('RGB',(w,h));pix=result.load()
 for y in range(h):
  for x in range(w):
   val=blend16(0x0841,0xffe0,raw[y*stride+x]);pix[x,y]=(((val>>11)&31)*255//31,((val>>5)&63)*255//63,(val&31)*255//31)
 result.resize((336,336),Image.Resampling.NEAREST).save(out/'RBM_정적수식_합성예시.png')
 summary=dict(evidence_ranges=len(evidence),source_files=sorted(set(x['file'] for x in evidence)),ui_state_segments=len(states),normal_button_rects_verified=sum(x['normal_button_code_verified'] for x in hits),ui_animation_placements=len(animations),color_key_changed_frames=len(diffs),color_key_changed_alpha_pixels=sum(x['changed_alpha_pixels'] for x in diffs),normal_idle_ms=333,normal_move_ms=125,blend_denominator=256,original_binary_execution=False)
 (out/'표시입력_추가검증_요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
