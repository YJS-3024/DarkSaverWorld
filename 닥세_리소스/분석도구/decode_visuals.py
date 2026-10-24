"""Static GRS/MRC/UI/RBM reconstruction. Does not execute original code or rules."""
from pathlib import Path
import struct,json,csv,collections,hashlib,io
from PIL import Image,ImageDraw,ImageChops

class Reader:
 def __init__(self,b): self.b=b;self.o=0
 def take(self,n):
  assert n>=0 and self.o+n<=len(self.b),(self.o,n,len(self.b))
  out=self.b[self.o:self.o+n];self.o+=n;return out
 def u32(self):return struct.unpack('<I',self.take(4))[0]
 def i32(self):return struct.unpack('<i',self.take(4))[0]
 def u8(self):return self.take(1)[0]

def read_grs(p):
 r=Reader(p.read_bytes());w,h,n=r.u32(),r.u32(),r.u32();groups=[];sprites=[]
 def bitmap():
  off=r.o;iw,ih,size=r.u32(),r.u32(),r.u32();ignored=r.take(2);data=r.take(size)
  assert size in (iw*ih,iw*ih+2),(p,off,iw,ih,size)
  return dict(offset=off,width=iw,height=ih,ignored_prefix=ignored.hex(),bytes=size,data=data)
 for _ in range(n):
  tag,cols,rows=r.u32(),r.u32(),r.u32();pal=r.take(1024);tiles=[]
  for _ in range(cols*rows):
   a,b,frames=r.u8(),r.u8(),r.u8();tiles.append(dict(a=a,b=b,frames=list(r.take(frames))))
  images=[bitmap() for _ in range(r.u8())]
  groups.append(dict(tag=tag,cols=cols,rows=rows,palette=pal,tiles=tiles,images=images))
 for _ in range(r.u32()):
  sw,sh=r.u32(),r.u32();pal=r.take(1024);a,b,n=r.u8(),r.u8(),r.u8();images=[bitmap() for _ in range(n)]
  sprites.append(dict(width=sw,height=sh,palette=pal,a=a,b=b,images=images))
 assert r.o==len(r.b),p
 return dict(width=w,height=h,groups=groups,sprites=sprites)

def grs_image(pal,im,transparent=False):
 w,h=im['width'],im['height'];b=im['data'];pixels=b''.join(b[y*w:(y+1)*w] for y in range(h-1,-1,-1))
 out=Image.frombytes('P',(w,h),pixels);out.putpalette([c for i in range(0,1024,4) for c in pal[i:i+3]])
 rgba=out.convert('RGBA')
 if transparent:
  # DManage.dll MakeSurfaceFromCDIBitmap calls SetColorKey(RGB(0,0,0)).
  r,g,b=rgba.convert('RGB').split()
  mask=ImageChops.lighter(ImageChops.lighter(r,g),b).point(lambda v:255 if v else 0)
  rgba.putalpha(mask)
 return rgba

def read_mrc(p):
 r=Reader(p.read_bytes());ids=[r.u32() for _ in range(r.u32())];w,h,n=r.u32(),r.u32(),r.u32();c=w*h
 a,b,raw=r.take(c),r.take(c),r.take(c*5);tiles=[raw[i:i+5] for i in range(0,len(raw),5)];sprites=[]
 for _ in range(n):
  v=r.take(16);x,y=struct.unpack_from('<2i',v,4)
  sprites.append(dict(resource=v[0],x=x,y=y,index=v[12],frame=v[13],padding=v[1:4].hex()+v[14:16].hex()))
 assert r.o==len(r.b)
 return dict(ids=ids,width=w,height=h,a=a,b=b,tiles=tiles,sprites=sprites)

def read_ui_library(p):
 r=Reader(p.read_bytes());n=r.u32();items={};fields={'.lce':5,'.lnm':7,'.ltn':6}[p.suffix.lower()]
 for _ in range(n):
  v=[r.i32() for _ in range(fields)];items[v[0]]=dict(id=v[0],x=v[1],y=v[2],width=v[3],height=v[4],extra=v[5:])
 bmp_bytes=r.u32();off=r.o;im=Image.open(io.BytesIO(r.take(bmp_bytes)));im.load();assert r.o==len(r.b)
 for v in items.values():assert v['x']>=0 and v['y']>=0 and v['x']+v['width']<=im.width and v['y']+v['height']<=im.height,(p,v)
 return items,im.convert('RGBA'),off

def read_lsc(p):
 r=Reader(p.read_bytes());w,h=r.i32(),r.i32();region=[r.i32() for _ in range(4)];decor=[]
 for _ in range(r.u32()):
  id,n=r.u32(),r.u32()
  for _ in range(n):decor.append(dict(id=id,x=r.i32(),y=r.i32(),repeat_x=r.i32(),repeat_y=r.i32()))
 buttons=[]
 for kind in ['button','animation']:
  for _ in range(r.u32()):buttons.append(dict(kind=kind,id=r.u32(),x=r.i32(),y=r.i32()))
 assert r.o==len(r.b)
 return dict(width=w,height=h,region=region,decor=decor,buttons=buttons)

def main():
 base=Path(__file__).resolve().parents[1];root=base/'설치본_추출/Saver_Files/Saver';decoded=base/'설치본_추출/ARC_해제';out=base/'분석결과';out.mkdir(exist_ok=True)
 def write(name,rows):
  if rows:
   with (out/name).open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
 grs={};grsmeta=[];pixelmatches=[];imagecount=0;existing={}
 for d in (base/'map').iterdir():
  if not d.is_dir():continue
  index=collections.defaultdict(list)
  for p in d.glob('*.bmp'):
   with Image.open(p) as im:index[(im.size,hashlib.sha256(im.convert('RGB').tobytes()).hexdigest())].append(p.relative_to(base).as_posix())
  existing[d.name.lower()]=index
 for p in sorted((root/'MAP').glob('*.grs')):
  data=read_grs(p);grs[int(p.stem[4:])]=data
  grsmeta.append(dict(file=p.name,width=data['width'],height=data['height'],tile_groups=len(data['groups']),sprite_groups=len(data['sprites']),tile_cells=sum(len(x['tiles']) for x in data['groups']),image_frames=sum(len(x['images']) for x in data['groups']+data['sprites']),exact_eof=True))
  for kind,groups in [('tile',data['groups']),('sprite',data['sprites'])]:
   for gi,g in enumerate(groups):
    for fi,im in enumerate(g['images']):
     imagecount+=1
     rgb=grs_image(g['palette'],im).convert('RGB');key=(rgb.size,hashlib.sha256(rgb.tobytes()).hexdigest())
     for old in existing.get(p.stem.lower(),{}).get(key,[]):pixelmatches.append(dict(grs=p.name,kind=kind,group=gi,frame=fi,source_offset=im['offset'],existing=old))
 write('GRS_구조검증.csv',grsmeta);write('GRS_기존BMP_픽셀일치.csv',pixelmatches)
 # Validate every distinct MRC reference; count exceptions without silently substituting assets.
 maps=[];seen=set();issues=[];placements=[]
 for p in sorted((root/'MAP').iterdir()):
  if p.suffix.lower() not in ('.mrc','.mrtc'):continue
  digest=hashlib.sha256(p.read_bytes()).hexdigest()
  if digest in seen:continue
  seen.add(digest);d=read_mrc(p);bad=0
  for i,(rid,group) in enumerate(zip(d['a'],d['b'])):
   try:assert grs[rid]['groups'][group]['images']
   except (KeyError,IndexError,AssertionError):bad+=1;issues.append(dict(map=p.name,kind='background',index=i,raw=f'{rid},{group}'))
  for i,v in enumerate(d['tiles']):
   if v[1]==255:continue
   try:
    g=grs[v[0]]['groups'][v[1]];frames=g['tiles'][v[2]]['frames'];assert frames and all(f<len(g['images']) for f in frames)
   except (KeyError,IndexError,AssertionError):bad+=1;issues.append(dict(map=p.name,kind='overlay',index=i,raw=v.hex()))
  for i,v in enumerate(d['sprites']):
   try:assert v['frame']<len(grs[v['resource']]['sprites'][v['index']]['images'])
   except (KeyError,IndexError,AssertionError):bad+=1;issues.append(dict(map=p.name,kind='sprite',index=i,raw=json.dumps(v)))
   if p.name=='01.mrc':placements.append(dict(map=p.name,record=i,**v))
  maps.append(dict(file=p.name,width=d['width'],height=d['height'],reference_issues=bad))
 write('맵_리소스참조검증.csv',maps);write('맵_리소스참조예외.csv',issues);write('맵01_스프라이트배치.csv',placements)
 def render_map(name):
  d=read_mrc(root/'MAP'/name);w,h=d['width'],d['height'];canvas=Image.new('RGBA',(w*32,h*32),(0,0,0,255))
  for i,(rid,group) in enumerate(zip(d['a'],d['b'])):
   g=grs[rid]['groups'][group];canvas.alpha_composite(grs_image(g['palette'],g['images'][0]),(i%w*32,i//w*32))
   v=d['tiles'][i]
   if v[1]!=255:
    g=grs[v[0]]['groups'][v[1]];f=g['tiles'][v[2]]['frames'][0];canvas.alpha_composite(grs_image(g['palette'],g['images'][f],True),(i%w*32,i//w*32))
  for v in d['sprites']:
   s=grs[v['resource']]['sprites'][v['index']];canvas.alpha_composite(grs_image(s['palette'],s['images'][v['frame']],True),(v['x']*32,v['y']*32))
  canvas.convert('RGB').save(out/f"원본맵_{Path(name).stem}_정적복원.png")
  return name
 renders=[]
 for n in ['01.mrc','02.mrc','1000.mrc']:
  if next((x['reference_issues'] for x in maps if x['file']==n),0)==0:renders.append(render_map(n))
 # UI libraries and scene-local placement.
 libs={};rects=[];uimeta=[]
 for folder,stem in [('Glib','GameLib'),('Wlib','waitlib')]:
  bylower={p.name.lower():p for p in (root/folder).iterdir()}
  for ext in ['.lce','.lnm','.ltn']:
   p=bylower[(stem+ext).lower()];items,im,bmpoff=read_ui_library(p);libs[(folder,ext)]=(items,im)
   for v in items.values():rects.append(dict(library=p.relative_to(root).as_posix(),id=v['id'],x=v['x'],y=v['y'],width=v['width'],height=v['height'],extra=json.dumps(v['extra']),bmp_offset=bmpoff))
 uiplacements=[];decorrows=[];scene_data={}
 for folder in ['Glib','Wlib']:
  for p in sorted((root/folder).glob('*.lsc')):
   d=read_lsc(p);scene_data[(folder,p.name)]=d;missing=0
   for v in d['decor']:
    missing+=v['id'] not in libs[(folder,'.lce')][0];decorrows.append(dict(scene=p.relative_to(root).as_posix(),**v))
   for v in d['buttons']:
    part=libs[(folder,'.lnm' if v['kind']=='animation' else '.ltn')][0].get(v['id']);missing+=part is None
    uiplacements.append(dict(scene=p.relative_to(root).as_posix(),**v,source_width=part['width'] if part else '',source_height=part['height'] if part else '',right_candidate=v['x']+part['width'] if part else '',bottom_candidate=v['y']+part['height'] if part else '',runtime_hit_test_verified=False))
   uimeta.append(dict(scene=p.relative_to(root).as_posix(),width=d['width'],height=d['height'],declared_region=json.dumps(d['region']),decor_placements=len(d['decor']),buttons=sum(v['kind']=='button' for v in d['buttons']),animations=sum(v['kind']=='animation' for v in d['buttons']),missing_ids=missing,exact_eof=True))
 write('UI_아틀라스좌표.csv',rects);write('UI_장면구조.csv',uimeta);write('UI_버튼배치.csv',[v for v in uiplacements if v['kind']=='button']);write('UI_애니메이션배치.csv',[dict(scene=v['scene'],id=v['id'],x=v['x'],y=v['y'],width=v['source_width'],height=v['source_height'],frames=libs[(v['scene'].split('/')[0],'.lnm')][0][v['id']]['extra'][0],delay_ms=libs[(v['scene'].split('/')[0],'.lnm')][0][v['id']]['extra'][1]) for v in uiplacements if v['kind']=='animation']);write('UI_테두리배치.csv',decorrows)
 panels=[]
 for folder,name in [('Glib','gscene0.lsc'),('Glib','gscene17.lsc'),('Wlib','scene1.lsc'),('Wlib','scene40.lsc')]:
  d=scene_data[(folder,name)];canvas=Image.new('RGBA',(d['width'],d['height']),(0,0,0,255))
  parts,atlas=libs[(folder,'.lce')]
  for v in d['decor']:
   a=parts[v['id']];im=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']))
   for y in range(v['repeat_y']):
    for x in range(v['repeat_x']):canvas.alpha_composite(im,(v['x']+x*a['width'],v['y']+y*a['height']))
  parts,atlas=libs[(folder,'.ltn')]
  for v in d['buttons']:
   parts,atlas=libs[(folder,'.lnm' if v['kind']=='animation' else '.ltn')]
   a=parts[v['id']];im=atlas.crop((a['x'],a['y'],a['x']+a['width'],a['y']+a['height']));canvas.alpha_composite(im,(v['x'],v['y']))
  canvas.convert('RGB').save(out/f'UI_{folder}_{Path(name).stem}_정적배치.png')
 # Mask buffers: retain stored row order until orientation/blending is verified.
 masks=[];ms=Image.new('RGB',(600,400),'#777777');dr=ImageDraw.Draw(ms)
 for i,p in enumerate(sorted((decoded/'gameres').glob('effect*.arc/*.rbm'))):
  b=p.read_bytes();mode,w,h,stride=struct.unpack_from('<4I',b);assert len(b)==16+stride*h and stride>=w
  raw=b''.join(b[16+y*stride:16+y*stride+w] for y in range(h));im=Image.frombytes('L',(w,h),raw)
  masks.append(dict(archive=p.parent.name,file=p.name,mode_raw=mode,width=w,height=h,stride=stride,bytes=len(b),min_value=min(raw),max_value=max(raw)))
  im.thumbnail((140,170));x=i%4*150;y=i//4*200;ms.paste(im,(x,y+22));dr.text((x+2,y+4),p.parent.name[:7]+'/'+p.stem,fill='white')
 ms.save(out/'RBM_마스크_원시값.png');write('RBM_구조.csv',masks)
 # Visual row evidence for two representative sprites; not a universal animation manifest.
 sheet=Image.new('RGB',(600,450),'#777777');dr=ImageDraw.Draw(sheet);rowlabels=['UP','DOWN','RIGHT','LEFT','FRONT ATTACK?','BACK ATTACK?'];frame_rows=[]
 for k,(arc,name) in enumerate([('char0.arc','100r.bmp'),('char1.arc','301R.bmp')]):
  im=Image.open(decoded/'gameres'/arc/name).convert('RGB');dr.text((k*300+100,5),name,fill='white')
  for y in range(im.height//32):
   dr.text((k*300+2,35+y*64),str(y)+' '+rowlabels[y],fill='white')
   crop=im.crop((0,y*32,96,(y+1)*32)).resize((192,64),Image.Resampling.NEAREST);sheet.paste(crop,(k*300+100,25+y*64))
   frame_rows.append(dict(archive=arc,file=name,row=y,cell_width=32,cell_height=32,columns=3,visual_interpretation=rowlabels[y],evidence='visual sample only'))
 sheet.save(out/'캐릭터_표본_행해석.png');write('캐릭터_표본_행해석.csv',frame_rows)
 summary=dict(grs_files=len(grsmeta),grs_image_frames=imagecount,grs_tile_groups=sum(x['tile_groups'] for x in grsmeta),grs_sprite_groups=sum(x['sprite_groups'] for x in grsmeta),matching_grs_frames=len(set((x['grs'],x['kind'],x['group'],x['frame']) for x in pixelmatches)),pixel_match_pairs=len(pixelmatches),unique_maps=len(maps),reference_issues=len(issues),rendered_maps=renders,ui_scenes=len(uimeta),ui_atlas_records=len(rects),ui_buttons=sum(x['kind']=='button' for x in uiplacements),ui_animation_placements=sum(x['kind']=='animation' for x in uiplacements),ui_missing_ids=sum(x['missing_ids'] for x in uimeta),rbm_masks=len(masks))
 (out/'시각복원_요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
