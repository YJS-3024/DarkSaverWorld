"""FX-only contact sheets from isolated draw-command traces; skips empty rectangles."""
import json,collections
from PIL import Image,ImageDraw,ImageChops
from analyze_special_skills import OUT,ARC,write,ROOT
import subprocess,hashlib

def main():
 data=json.loads((OUT/'특수기_격리명령추적.json').read_text());rs=data['runs'];images={}
 for i,folder,name in [(1,'etc.arc','fx.bmp'),(2,'etc2.arc','fx2.bmp'),(3,'etc3.arc','fx3.bmp'),(4,'etc.arc','mcursor.bmp')]:
  p=next(p for p in (ARC/folder).iterdir() if p.name.lower()==name);images[hex(0x60000000+i)]=Image.open(p).convert('RGB')
 exceptions=[];bykey={}
 for r in rs:
  bykey.setdefault((r['render_kind'],r['side'],r['counter']),r)
  for e in r['events']:
   if e['op'] not in ['Blt','BltFast']:continue
   im=images.get(e['surface']);q=e['src']
   if im and q and not(0<=q[0]<q[2]<=im.width and 0<=q[1]<q[3]<=im.height):exceptions.append(dict(pattern_id=r['pattern_id'],kind=r['render_kind'],side=r['side'],counter=r['counter'],source_rect=json.dumps(q),atlas_size=str(im.size),classification='empty/inverted -> original BltFast wrapper empty check' if q[0]>=q[2] or q[1]>=q[3] else 'source boundary clipping required',scope='raw command before original DManage clipping wrapper'))
 if exceptions:write('특수기_원시그리기_범위분류.csv',exceptions)
 for kind,side in [(6,'actor'),(14,'actor'),(15,'actor'),(16,'actor'),(19,'actor'),(20,'actor'),(18,'actor'),(3,'area'),(4,'area'),(13,'area')]:
  panel=Image.new('RGB',(1280,260),'#555555');d=ImageDraw.Draw(panel)
  for col,count in enumerate([1,4,9,15]):
   r=bykey.get((kind,side,count))
   if not r:continue
   frame=Image.new('RGB',(640,480),'#384050')
   for e in r['events']:
    if e['op']=='surface_fill':frame.paste(e['color']&0xffffff,(0,0,640,480));continue
    if e['op'] not in ['Blt','BltFast'] or e['surface'] not in images:continue
    q=e['src']
    if not q or q[0]>=q[2] or q[1]>=q[3]:continue
    patch=images[e['surface']].crop(q)
    if e['op']=='Blt':
     dest=e['dst']
     if not dest or dest[2]<=dest[0] or dest[3]<=dest[1] or dest[2]-dest[0]>2048 or dest[3]-dest[1]>2048:continue
     patch=patch.resize((dest[2]-dest[0],dest[3]-dest[1]),Image.Resampling.NEAREST);x,y=dest[:2]
    else:x,y=e['dst']
    alpha=ImageChops.lighter(ImageChops.lighter(*patch.split()[:2]),patch.split()[2]).point(lambda v:255 if v else 0);frame.paste(patch,(x,y),alpha)
   panel.paste(frame.resize((320,240),Image.Resampling.NEAREST),(col*320,20));d.text((col*320+5,4),f'kind {kind} {side} counter {count}',fill='white')
  panel.save(OUT/f'특수기_종류{kind}_{side}_격리미리보기.png')
 # Static ranges for all legacy renderers and the low-level clipping guard.
 ev=[];parts=[]
 for name,start,end,note in [('userunit.dll',0x10005600,0x1000ac38,'Legacy specialized actor/target renderers, helpers, area pass and pattern99'),('DManage.dll',0x10005d10,0x10005e60,'BltFast wrapper: clipping and empty-rectangle early return')]:
  p=ROOT/name;parts.append(note+'\n'+subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={start}',f'--stop-address={end}',str(p)],text=True));ev.append(dict(file=name,start_va=hex(start),end_va_exclusive=hex(end),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),observation=note))
 write('특수기_전용함수_코드근거.csv',ev);(OUT/'특수기_전용함수_코드발췌.asm.txt').write_text('\n'.join(parts))
 summary=dict(raw_source_exceptions=len(exceptions),empty_rectangles=sum('empty' in r['classification'] for r in exceptions),source_boundary_clipping=sum('boundary' in r['classification'] for r in exceptions),preview_scope='FX only, synthetic inputs, no body; approximate RGB key/stretch, not native DirectDraw pixels')
 (OUT/'특수기_격리미리보기_요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(summary)
if __name__=='__main__':main()
