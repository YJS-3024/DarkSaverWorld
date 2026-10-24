"""Static skill-to-renderer links and pattern-99 replay metadata. No DLL execution."""
from pathlib import Path
import csv,json,struct,hashlib,re,subprocess,collections
from PIL import Image,ImageDraw
BASE=Path(__file__).resolve().parents[1];ROOT=BASE/'설치본_추출/Saver_Files/Saver';ARC=BASE/'설치본_추출/ARC_해제/gameres';OUT=BASE/'분석결과'
class PE:
 def __init__(self,path):
  self.b=path.read_bytes();p=struct.unpack_from('<I',self.b,60)[0];n=struct.unpack_from('<H',self.b,p+6)[0];s=struct.unpack_from('<H',self.b,p+20)[0];self.base=struct.unpack_from('<I',self.b,p+52)[0];self.sections=[struct.unpack_from('<4I',self.b,p+24+s+i*40+8) for i in range(n)]
 def data(self,va,n):
  r=va-self.base
  for vs,v,rs,o in self.sections:
   if v<=r<v+rs:return self.b[o+r-v:o+r-v+n]
  raise ValueError(hex(va))
 def u32(self,va):return struct.unpack('<I',self.data(va,4))[0]
 def string(self,va):return self.data(va,256).split(b'\0')[0].decode('ascii')
def write(name,rows):
 with (OUT/name).open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
def table(name):
 return [json.loads(r['tokens']) for r in csv.DictReader((OUT/f'표_{name}.csv').open(encoding='utf-8-sig'))]
def main():
 pe=PE(ROOT/'userunit.dll');ptns=table('MagicPtn');skills=table('Magictbl');assert all(len(t)==19 for t in ptns) and all(len(t)==17 for t in skills)
 evidence=[];asm=[]
 ranges=[('setunit.dll',0x10001fbb,0x1000203a,'Magictbl scanf layout: ID, name, 15 integers'),('setunit.dll',0x10002128,0x100021f2,'MagicPtn scanf layout and direct ID-indexed storage'),('setunit.dll',0x10002661,0x1000280c,'GetMagicTbl copies selected pattern into legacy runtime record'),('setunit.dll',0x10002eb0,0x10002ec2,'GetPatternTable direct record stride 328'),('userunit.dll',0x1000cbb0,0x1000cd7e,'Magic event seeds actor and target slots, origin and target position'),('userunit.dll',0x10010707,0x100107d9,'Actor-side dispatch'),('userunit.dll',0x100107d9,0x10010951,'Target-side dispatch and area deduplication'),('userunit.dll',0x10010992,0x10010a7a,'Area effect second pass'),('userunit.dll',0x10005600,0x10005920,'Legacy projectile and centered target effect'),('userunit.dll',0x100096c0,0x10009a20,'Centered target effects and doubled slow sequence'),('userunit.dll',0x1000a970,0x1000ac38,'Pattern 99 timing, loops, coordinates, FX3'),('userunit.dll',0x1000e7d3,0x1000e857,'FX3 archive to display surface binding')]
 for fn,a,b,n in ranges:
  p=ROOT/fn;code=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True)
  evidence.append(dict(file=fn,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),observation=n));asm.append(n+'\n'+code)
 write('특수기_코드근거.csv',evidence);(OUT/'특수기_코드발췌.asm.txt').write_text('\n'.join(asm))
 # Decode machine jump-table bytes as data, never as x86 instructions.
 def dispatch(kind,actor=False):
  low,high,lut,jumps,default=(2,99,0x10010aac,0x10010a7c,0x100107d0) if actor else (1,99,0x10010b4c,0x10010b10,0x10010948)
  branch=pe.u32(jumps+4*pe.data(lut+kind-low,1)[0]) if low<=kind<=high else default
  code=pe.data(branch,180)
  # A handler call follows each decoded case. Grouped branches need explicit split.
  if branch==0x10010838:return branch,{4:0x100064a0,23:0x10009510}.get(kind,0x10009370)
  for m in re.finditer(b'\xe8(.{4})',code,re.S):
   dest=branch+m.start()+5+struct.unpack('<i',m.group(1))[0]
   if 0x10005000<=dest<0x1000b000:return branch,dest
  raise ValueError(hex(branch))
 patterns=[];byid={}
 for t in ptns:
  pid=int(t[0]);kind=int(t[16]);ab,ah=dispatch(kind,True);tb,th=dispatch(kind)
  row=dict(pattern_id=pid,label=t[1],render_kind=kind,actor_handler_va=hex(ah),target_handler_va=hex(th),actor_case_va=hex(ab),target_case_va=hex(tb),fx1_xy=','.join(t[2:4]),fx1_size=','.join(t[4:6]),fx1_step=','.join(t[6:8]),fx1_frames=int(t[8]),fx2_xy=','.join(t[9:11]),fx2_size=','.join(t[11:13]),fx2_step=','.join(t[13:15]),fx2_frames=int(t[15]),sound1=int(t[17]),sound2=int(t[18]),scope='code-verified dispatch; individual handler internals partial')
  patterns.append(row);byid[pid]=row
 write('특수기_패턴_표시함수.csv',patterns)
 linked=[]
 for t in skills:
  pid=int(t[14]);p=byid.get(pid);assert p is not None,(t[0],pid)
  linked.append(dict(skill_id=int(t[0]),name=t[1],entry_category='emotion' if p['render_kind']==99 else 'magic-table entry; may include skills, items and internal effects',pattern_id=pid,pattern_label=p['label'],render_kind=p['render_kind'],actor_handler_va=p['actor_handler_va'],target_handler_va=p['target_handler_va'],sound1=p['sound1'],sound2=p['sound2'],timing='pattern string in 250ms units' if p['render_kind']==99 else 'handler-specific; not MP/time/TICK columns',status='table and dispatch linked; full choreography only where separately documented'))
 write('특수기_전체_연출대응.csv',linked)
 # Complete source-rectangle sequences for the simple centered target handlers.
 centered=[];bounds=[]
 for t in ptns:
  kind=int(t[16])
  if kind not in [5,7,9,22]:continue
  pid=int(t[0]);x,y,w,h,dx,dy=map(int,t[2:8]);n=int(t[8]);limit=n*(2 if kind==22 else 1)
  if limit<3:limit=8
  atlas='etc2.arc/Fx2.bmp' if kind==22 else 'etc.arc/Fx.bmp';image=Image.open(ARC/atlas)
  for counter in range(limit):
   frame=counter%n;left=x+frame*dx;top=y+frame*dy;inside=0<=left<left+w<=image.width and 0<=top<top+h<=image.height
   row=dict(pattern_id=pid,render_kind=kind,counter=counter,frame=frame,left=left,top=top,right=left+w,bottom=top+h,atlas=atlas,in_atlas=inside,position='(tile-camera)*32-trunc((size-32)/2)',timing='one counter advance per handler invocation; no fixed millisecond claim')
   centered.append(row)
   if not inside:bounds.append(row)
 write('특수기_중심효과_프레임.csv',centered)
 for pid in [16,24,50]:
  rs=[r for r in centered if r['pattern_id']==pid]
  if not rs:continue
  canvas=Image.new('RGB',(len(rs)*100,130),'#555555');draw=ImageDraw.Draw(canvas)
  for i,r in enumerate(rs):
   im=Image.open(ARC/r['atlas']).convert('RGB');patch=im.crop(tuple(r[k] for k in ['left','top','right','bottom']));patch.thumbnail((96,96),Image.Resampling.NEAREST);canvas.paste(patch,(i*100,24));draw.text((i*100+3,5),f"step {r['counter']}",fill='white')
  canvas.save(OUT/f'특수기_패턴{pid}_중심효과.png')
 if bounds:write('특수기_중심효과_범위예외.csv',bounds)
 # Pattern 99 is a distinct atlas path; its second position pair is a draw offset.
 assert pe.string(0x1001518c)=='gameres\\etc3.arc' and pe.string(0x10015184)=='fx3.bmp'
 sheet=Image.open(ARC/'etc3.arc/fx3.bmp').convert('RGB');new=[]
 for t in ptns:
  if int(t[16])!=99:continue
  pid=int(t[0]);n=int(t[8]);repeat=int(t[1][1]);delays=t[1][3:];assert len(delays)==n and delays.isdigit()
  limit=n*repeat if n*repeat>=3 else 8
  x,y,w,h,dx,dy=map(int,t[2:8]);ox,oy=map(int,t[9:11]);imgs=[];dur=[]
  for k in range(n):
   rect=(x+k*dx,y+k*dy,x+k*dx+w,y+k*dy+h);assert 0<=rect[0]<rect[2]<=sheet.width and 0<=rect[1]<rect[3]<=sheet.height
   delay=int(delays[k])*250
   new.append(dict(pattern_id=pid,frame=k,left=rect[0],top=rect[1],right=rect[2],bottom=rect[3],delay_ms=delay,repeat_digit=repeat,stop_counter=limit,draw_x_offset=ox,draw_y_offset=oy,atlas='gameres/etc3.arc/fx3.bmp',delay_rule='floor(elapsed_ms/250) >= digit; one advance per call'))
   imgs.append(sheet.crop(rect).resize((192,192),Image.Resampling.NEAREST));dur.append(delay)
  if pid in [101,105,115]:imgs[0].save(OUT/f'특수기_패턴{pid}_fx3.gif',save_all=True,append_images=imgs[1:],duration=dur,loop=0)
 write('특수기_패턴99_프레임시간.csv',new)
 # Validate the compiler's signed division-by-250 expansion on elapsed positive samples.
 for elapsed in range(100000):assert ((elapsed*0x10624dd3)>>36)==elapsed//250
 counts=collections.Counter(r['render_kind'] for r in linked)
 summary=dict(centered_effect_patterns=len(set(r['pattern_id'] for r in centered)),centered_effect_frames=len(centered),centered_effect_bounds_exceptions=len(bounds),patterns=len(patterns),skills=len(linked),missing_pattern_refs=0,skill_counts_by_render_kind=dict(counts),pattern99_patterns=len(set(r['pattern_id'] for r in new)),pattern99_frames=len(new),pattern99_emotion_entries=counts[99],evidence_ranges=len(evidence),original_binary_execution=False,rbm_to_skill_binding='not established; current verified renderers use FX atlases')
 (OUT/'특수기_분석요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
