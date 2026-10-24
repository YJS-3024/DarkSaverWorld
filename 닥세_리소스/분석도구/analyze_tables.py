"""Analyze decoded local definitions. Never evaluates event/AI rules or runs DLLs."""
from pathlib import Path
import struct,csv,json,re,collections,hashlib
from PIL import Image,ImageDraw

def main():
 base=Path(__file__).resolve().parents[1];root=base/'설치본_추출/Saver_Files/Saver';decoded=base/'설치본_추출/ARC_해제';out=base/'분석결과'
 def write(name,rows):
  if rows:
   with (out/name).open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
 # Export names are static metadata; no DLL entry point is executed.
 import subprocess,shutil
 if shutil.which('objdump'):
  exports=[]
  for p in sorted(root.glob('*.dll')):
   listing=subprocess.check_output(['objdump','-p',str(p)],text=True)
   block=listing.split('[Ordinal/Name Pointer] Table')[-1].split('PE File Base Relocations')[0]
   for name in re.findall(r'\[\s*\d+\]\s+\+base\[\s*\d+\]\s+[0-9a-f]+\s+(.+)',block):exports.append(dict(dll=p.name,export=name))
  write('DLL_공개함수.csv',exports)
 # Keep headers and original token sequence; flag ambiguity instead of shifting data.
 tableinfo=[];alltables={}
 for p in sorted((decoded/'gameres/set.arc').glob('*.atr')):
  text=p.read_bytes().decode('cp949');p.with_suffix('.utf8.txt').write_text(text)
  lines=text.splitlines();header=lines[0].lstrip(';').split();rows=[]
  for no,line in enumerate(lines,1):
   plain=line.split(';',1)[0].strip()
   if re.match(r'^-?\d+\s',plain):
    t=plain.split();rows.append(dict(line=no,id=t[0],field_count=len(t),tokens=json.dumps(t,ensure_ascii=False),raw=line))
  alltables[p.name]=[json.loads(r['tokens']) for r in rows]
  write('표_'+p.stem+'.csv',rows)
  tableinfo.append(dict(file=p.name,rows=len(rows),header=' | '.join(header),field_counts=json.dumps(dict(collections.Counter(r['field_count'] for r in rows))),duplicate_first_ids=len(rows)-len(set(r['id'] for r in rows))))
 write('정의표_개요.csv',tableinfo)
 variants=[]
 for a,b in [('itemtbl.atr','itemtbl1.atr'),('ability.atr','ability1.atr'),('TileAttr.atr','TileAttb.atr')]:
  aa={x[0]:x for x in alltables[a]};bb={x[0]:x for x in alltables[b]}
  for key in sorted(aa.keys()|bb.keys(),key=int):
   if aa.get(key)!=bb.get(key):variants.append(dict(table_a=a,table_b=b,id=key,tokens_a=json.dumps(aa.get(key),ensure_ascii=False),tokens_b=json.dumps(bb.get(key),ensure_ascii=False)))
 write('정의표_버전차이.csv',variants)
 # Name/image linkage is a direct column observation, not a verified runtime loader choice.
 units=[]
 for t in alltables['ability.atr']:
  units.append(dict(id=t[0],image_id=t[1],name=t[35] if len(t)>35 else '',description=' '.join(t[36:]),move=t[15],unit_type=t[22],raw_tokens=json.dumps(t,ensure_ascii=False)))
 write('캐릭터_이름_이미지.csv',units)
 items=[]
 for t in alltables['itemtbl.atr']:
  valid=len(t)>=43 and re.fullmatch(r'-?\d+',t[41]) is not None
  items.append(dict(id=t[0],image1=t[1],image2=t[2],slot_raw=t[3],equip_level_raw=t[4],name=t[42] if valid else '',description=' '.join(t[43:]) if valid else '',name_schema='fixed_prefix_candidate' if valid else 'ambiguous',raw_tokens=json.dumps(t,ensure_ascii=False)))
 write('아이템_이름_이미지.csv',items)
 # Map format from DManage.dll BB_Map::LoadMap (RVA A580).
 maps=[];dimensions=collections.Counter();samples={}
 for p in sorted((root/'MAP').iterdir()):
  if p.suffix.lower() not in ('.mrc','.mrtc'):continue
  b=p.read_bytes();n=struct.unpack_from('<I',b)[0];resource_ids=struct.unpack_from('<'+'I'*n,b,4);off=4+4*n;w,h,ns=struct.unpack_from('<3I',b,off);off+=12;cells=w*h
  assert len(b)==off+cells*7+ns*16,p
  a=b[off:off+cells];bb=b[off+cells:off+2*cells];tiles=b[off+2*cells:off+7*cells];sprites=[struct.unpack_from('<4i',b,off+7*cells+i*16) for i in range(ns)]
  maps.append(dict(file=p.name,width=w,height=h,resource_ids=','.join(map(str,resource_ids)),sprite_records=ns,grid_a_values=','.join(map(str,sorted(set(a)))),grid_b_values=','.join(map(str,sorted(set(bb)))),file_bytes=len(b),layout_verified=True));dimensions[(w,h)]+=1
  if p.name in ['01.mrc','1000.mrc']:
   samples[p.name]=dict(resource_ids=resource_ids,width=w,height=h,grid_a=list(a),grid_b=list(bb),tiles=[list(tiles[i:i+5]) for i in range(0,len(tiles),5)],sprites=sprites)
 write('맵_구조.csv',maps);(out/'맵_대표_데이터.json').write_text(json.dumps(samples,ensure_ascii=False))
 # Scenario captions and local scripts: collect commands, do not execute conditions.
 scenarios=[];scripts=[];commands=collections.Counter();examples={};audio_refs=[];script_texts={}
 for arc in sorted((decoded/'MAP').iterdir()):
  if not arc.is_dir():continue
  for p in sorted(arc.iterdir()):
   if p.suffix.lower() not in ['.evt','.dee','.deo','.srf','.pos','.set','.all']:continue
   text=p.read_bytes().decode('cp949',errors='replace');p.with_suffix(p.suffix+'.utf8.txt').write_text(text)
   if p.suffix.lower()=='.srf':
    ls=[s.strip() for s in text.splitlines() if s.strip()];scenarios.append(dict(archive=arc.name,file=p.name,title=ls[0] if ls else '',second_line_raw=ls[1] if len(ls)>1 else '',caption_status='placeholder' if not ls or ls[0].startswith(';') else 'text_present',text=text));continue
   scripts.append(dict(archive=arc.name,file=p.name,bytes=p.stat().st_size,lines=len(text.splitlines()),events=len(re.findall(r'^EVENT\b',text,re.M)),replacement_chars=text.count('\ufffd')))
   script_texts[(arc.name,p.name)]=text
   for line in text.splitlines():
    t=line.strip().split()
    if not t:continue
    if t[0].isdigit():t=t[1:]
    if not t:continue
    cmd=t[0]
    if re.fullmatch('[A-Z][A-Z0-9_]*',cmd):
     commands[cmd]+=1;examples.setdefault(cmd,line)
     if cmd=='PLAYMIDI':audio_refs.append(dict(archive=arc.name,file=p.name,command=line))
 write('지역_시나리오.csv',scenarios);write('연출_이벤트_파일.csv',scripts);write('연출_이벤트_명령.csv',[dict(command=k,count=v,example=examples[k]) for k,v in commands.most_common()]);write('연출_음악참조.csv',audio_refs)
 # Compare old recollection text with actual locally stored dialogue (no reward execution claim).
 duties=[]
 for p in sorted((decoded/'gameres/duty.arc').iterdir()):
  if p.suffix.lower()!='.txt' or p.name.endswith('.utf8.txt'):continue
  text=p.read_bytes().decode('cp949');m=re.match(r'duty(\d+)-(\d+)',p.name,re.I)
  duties.append(dict(quest=int(m[1]) if m else '',stage=int(m[2]) if m else '',file=p.name,npc_id_text=text.splitlines()[0],result_text=' | '.join(re.findall(r'\[결과\]([^\r\n]*)',text)),text=text))
 write('의뢰_대사단계.csv',duties)
 midi=(decoded/'gameres/set.arc/midiset.txt').read_bytes().decode('cp949');ml=midi.splitlines();mr=[]
 available={p.name.lower() for p in (root/'BGM').iterdir()}
 for ln,l in enumerate(ml[1:],2):
  t=l.split()
  if len(t)>=2 and t[0].isdigit():mr.append(dict(line=ln,map_id=t[0],midi=t[1],file_exists=t[1].lower() in available))
 write('맵_BGM_연결.csv',mr)
 # Symbolic layer preview: validates coordinates, not a reconstruction of original art/collision.
 d=samples['01.mrc'];w=d['width'];h=d['height'];scale=12
 panel=Image.new('RGB',(w*scale+24,h*scale+42),'white');dr=ImageDraw.Draw(panel);dr.text((12,7),'01.mrc grid A (raw values; not collision)',fill='black')
 for y in range(h):
  for x in range(w):
   v=d['grid_a'][y*w+x];color=(30,30,30) if v==0 else ((v*47)%180+40,(v*71)%180+40,(v*19)%180+40)
   dr.rectangle((12+x*scale,30+y*scale,11+(x+1)*scale,29+(y+1)*scale),fill=color)
 panel.save(out/'맵01_원시레이어.png')
 # UI bitmap contact sheet.
 es=list(csv.DictReader((out/'UI_내장_BMP.csv').open(encoding='utf-8-sig')));sheet=Image.new('RGB',(1000,380*((len(es)+1)//2)),'#eeeeee')
 for i,row in enumerate(es):
  p=decoded/'embedded'/Path(row['library']).name/f"{int(row['offset']):08x}.bmp";im=Image.open(p).convert('RGB');im.thumbnail((480,340));x=i%2*500;y=i//2*380
  ImageDraw.Draw(sheet).text((x+10,y+7),row['library']+' '+row['width']+'x'+row['height'],fill='black');sheet.paste(im,(x+10,y+30))
 sheet.save(out/'UI_라이브러리_미리보기.jpg')
 summary=dict(map_dimensions={f'{w}x{h}':n for (w,h),n in dimensions.items()},tables={x['file']:x['rows'] for x in tableinfo},variant_rows=len(variants),scenario_files=len(scenarios),scenario_captions=sum(x['caption_status']=='text_present' for x in scenarios),script_files=len(scripts),command_types=len(commands),quest_groups=len(set(x['quest'] for x in duties)),duty_files=len(duties),midi_declared=ml[0],midi_rows=len(mr),midi_duplicate_ids={k:n for k,n in collections.Counter(x['map_id'] for x in mr).items() if n>1},midi_missing_refs=sum(not x['file_exists'] for x in mr),items_ambiguous=sum(x['name_schema']=='ambiguous' for x in items))
 (out/'정의분석_요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
