"""Static client analysis; never executes extracted game files. Python 3 + Pillow."""
from pathlib import Path
import struct

def lzh_decode(b,size):
 N=4096;F=60;NC=314;T=627;R=626
 freq=[0]*(T+1);prnt=[0]*(T+NC);son=[0]*T
 for i in range(NC):freq[i]=1;son[i]=i+T;prnt[i+T]=i
 i=0
 for j in range(NC,R+1):freq[j]=freq[i]+freq[i+1];son[j]=i;prnt[i]=prnt[i+1]=j;i+=2
 freq[T]=65535;prnt[R]=0
 bp=0
 def bits(n):
  nonlocal bp
  v=0
  for _ in range(n):
   if bp>=len(b)*8:raise ValueError('EOF')
   v=(v<<1)|((b[bp>>3]>>(7-(bp&7)))&1);bp+=1
  return v
 def update(c):
  if freq[R]>=32768:
   j=0
   for i in range(T):
    if son[i]>=T:freq[j]=(freq[i]+1)//2;son[j]=son[i];j+=1
   i=0
   for j in range(NC,T):
    f=freq[i]+freq[i+1];k=j-1
    while k>=0 and f<freq[k]:k-=1
    k+=1
    freq[k+1:j+1]=freq[k:j];son[k+1:j+1]=son[k:j]
    freq[k]=f;son[k]=i;i+=2
   for i in range(T):
    k=son[i];prnt[k]=i
    if k<T:prnt[k+1]=i
  c=prnt[c+T]
  while True:
   freq[c]+=1;k=freq[c];l=c+1
   if k>freq[l]:
    while k>freq[l+1]:l+=1
    freq[c]=freq[l];freq[l]=k
    i=son[c];prnt[i]=l
    if i<T:prnt[i+1]=l
    j=son[l];son[l]=i;prnt[j]=c
    if j<T:prnt[j+1]=c
    son[c]=j;c=l
   c=prnt[c]
   if c==0:break
 # canonical position code
 lengths=[3]+[4]*3+[5]*8+[6]*12+[7]*24+[8]*16
 table={};code=0;prev=0
 for s,ln in enumerate(lengths):code<<=ln-prev;table[(ln,code)]=s;code+=1;prev=ln
 ring=bytearray(b' '*N);pos=N-F;out=bytearray()
 while len(out)<size:
  c=son[R]
  while c<T:c=son[c+bits(1)]
  c-=T;update(c)
  if c<256:out.append(c);ring[pos]=c;pos=(pos+1)&4095
  else:
   v=0;high=None
   for ln in range(1,9):
    v=(v<<1)|bits(1)
    if (ln,v) in table:high=table[(ln,v)];break
   if high is None:raise ValueError('position')
   off=(high<<6)|bits(6);start=(pos-off-1)&4095
   for k in range(c-255+2):
    v=ring[(start+k)&4095];out.append(v);ring[pos]=v;pos=(pos+1)&4095
 return bytes(out[:size])

def lzss_decode(b,size):
 ring=bytearray(4096);pos=4078;out=bytearray();i=0
 while i<len(b):
  flags=b[i];i+=1
  for bit in range(8):
   if i>=len(b):return bytes(out[:size])
   if flags&(1<<bit):
    x=b[i];i+=1;out.append(x);ring[pos]=x;pos=(pos+1)&4095
   else:
    if i+2>len(b):return bytes(out[:size])
    a,c=b[i:i+2];i+=2;off=a|((c&240)<<4);ln=(c&15)+3
    for k in range(ln):
     x=ring[(off+k)&4095];out.append(x);ring[pos]=x;pos=(pos+1)&4095
 return bytes(out[:size])

def main():
 import csv,hashlib,json,collections,io,wave,re
 from PIL import Image,ImageDraw
 base=Path(__file__).resolve().parents[1]
 root=base/'설치본_추출/Saver_Files/Saver'
 out=base/'분석결과';out.mkdir(exist_ok=True)
 unpack=base/'설치본_추출/ARC_해제';unpack.mkdir(exist_ok=True)
 def csvout(name,rows):
  if rows:
   with (out/name).open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
 arcs=[];images=[];texts=[];sounds=[];summary={};errors=[]
 for p in sorted(root.rglob('*.arc')):
  b=p.read_bytes();assert b[0]==4 and b[1:5] in (b'0901',b'0720'),p
  off=5
  while True:
   assert off+36<=len(b),p
   rec=b[off:off+36];assert 0<rec[0]<=14
   name=rec[1:1+rec[0]].decode('cp949');off+=36
   if name=='HEADEND':break
   assert '/' not in name and '\\' not in name and name not in ('.','..')
   method=rec[15];size,packed,st,en=struct.unpack_from('<4I',rec,20)
   assert en==st+packed-1 and 0<=st<=en<len(b)
   data=b[st:en+1];checksum=sum(data)&65535
   assert checksum==struct.unpack_from('<H',rec,18)[0]
   row=dict(archive=p.relative_to(root).as_posix(),name=name,method=method,expanded_bytes=size,packed_bytes=packed,start=st,end=en,checksum16=checksum,status='pending',sha256='')
   try:
    d=(lzh_decode if method==0 else lzss_decode if method==1 else None)(data,size)
    assert len(d)==size,(len(d),size)
    dest=unpack/p.relative_to(root)/name;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(d)
    row.update(status='decoded_size_matched',sha256=hashlib.sha256(d).hexdigest())
    ext=Path(name).suffix.lower()
    if ext=='.bmp':
     im=Image.open(io.BytesIO(d));im.load()
     images.append(dict(archive=row['archive'],name=name,width=im.width,height=im.height,mode=im.mode,bpp=struct.unpack_from('<H',d,28)[0],pixel_sha256=hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest()))
    elif ext=='.wav':
     with wave.open(io.BytesIO(d)) as w:sounds.append(dict(name=name,channels=w.getnchannels(),rate=w.getframerate(),sample_bits=w.getsampwidth()*8,frames=w.getnframes(),seconds=round(w.getnframes()/w.getframerate(),4)))
    elif ext=='.txt':
     text=d.decode('cp949',errors='replace');(dest.with_suffix('.utf8.txt')).write_text(text)
     texts.append(dict(archive=row['archive'],name=name,bytes=len(d),replacement_chars=text.count('\ufffd'),text=text))
   except Exception as e:
    row['status']='failed: '+str(e);errors.append((str(p),name,str(e)))
   arcs.append(row)
 csvout('ARC_항목.csv',arcs);csvout('이미지_규격.csv',images);csvout('한글_텍스트.csv',texts);csvout('효과음_규격.csv',sounds)
 # Direct BMPs: inspect only image metadata, no alteration.
 direct=[]
 for p in sorted(root.rglob('*')):
  if p.suffix.lower()=='.bmp':
   with Image.open(p) as im:
    direct.append(dict(path=p.relative_to(root).as_posix(),width=im.width,height=im.height,mode=im.mode))
 csvout('개별_BMP_규격.csv',direct)
 # Structural observations; labels are offsets, not unproved semantics.
 maps=[]
 for p in sorted((root/'MAP').iterdir()):
  if p.suffix.lower() in ('.mrc','.mrtc'):
   b=p.read_bytes();v=struct.unpack_from('<6I',b)
   maps.append(dict(name=p.name,bytes=len(b),u32_00=v[0],u32_04=v[1],u32_08=v[2],u32_0c=v[3],u32_10=v[4],u32_14=v[5],sha256=hashlib.sha256(b).hexdigest()))
 csvout('맵_헤더.csv',maps)
 grs=[]
 for p in sorted((root/'MAP').glob('*.grs')):
  b=p.read_bytes();v=struct.unpack_from('<6I',b)
  grs.append(dict(name=p.name,bytes=len(b),u32_00=v[0],u32_04=v[1],u32_08=v[2],u32_0c=v[3],u32_10=v[4],u32_14=v[5]))
 csvout('타일_GRS_헤더.csv',grs)
 # MIDI event parser: tracks, program changes, tempos, notes, duration.
 midis=[]
 def vlq(b,i):
  v=0
  for _ in range(4):
   x=b[i];i+=1;v=(v<<7)|(x&127)
   if not x&128:return v,i
  raise ValueError('bad MIDI VLQ')
 for p in sorted((root/'BGM').iterdir()):
  if p.suffix.lower()!='.mid':continue
  b=p.read_bytes();assert b[:4]==b'MThd';hlen=struct.unpack_from('>I',b,4)[0];fmt,ntracks,division=struct.unpack_from('>3H',b,8);pos=8+hlen;tempos=[];programs=set();notes=0;endtick=0;tracknames=[]
  for track in range(ntracks):
   assert b[pos:pos+4]==b'MTrk';ln=struct.unpack_from('>I',b,pos+4)[0];t=b[pos+8:pos+8+ln];pos+=8+ln;i=0;tick=0;running=None
   while i<len(t):
    dt,i=vlq(t,i);tick+=dt;x=t[i]
    if x&128:i+=1
    else:
     assert running is not None;x=running
    if x==255:
     kind=t[i];i+=1;n,i=vlq(t,i);val=t[i:i+n];i+=n
     if kind==81:tempos.append((tick,int.from_bytes(val,'big')))
     if kind==3:tracknames.append(val.decode('cp949',errors='replace'))
     if kind==47:break
    elif x in (240,247):n,i=vlq(t,i);i+=n;running=None
    else:
     assert 128<=x<240;running=x;n=1 if x>>4 in (12,13) else 2;val=t[i:i+n];i+=n
     if x>>4==12:programs.add(val[0])
     if x>>4==9 and val[1]>0:notes+=1
   endtick=max(endtick,tick)
  duration=None
  if not division&32768 and fmt!=2:
   last=0;tempo=500000;duration=0.0
   for tick,newtempo in sorted(tempos)+[(endtick,tempo)]:
    duration+=(tick-last)*tempo/1000000/division;last=tick;tempo=newtempo
  midis.append(dict(name=p.name,format=fmt,tracks=ntracks,division=division,end_tick=endtick,duration_seconds=round(duration,3) if duration is not None else '',note_ons=notes,programs_0based=','.join(map(str,sorted(programs))),track_names=' | '.join(tracknames)))
 csvout('BGM_MIDI_분석.csv',midis)
 # Embedded BMPs in common UI libraries; validate file header and PIL decode.
 embedded=[]
 for folder in ('Glib','Wlib'):
  for p in sorted((root/folder).iterdir()):
   if p.suffix.lower() not in ('.lnm','.lce','.ltn'):continue
   b=p.read_bytes();i=0
   while True:
    st=b.find(b'BM',i)
    if st<0:break
    i=st+2
    if st+54>len(b):continue
    size=struct.unpack_from('<I',b,st+2)[0]
    if size<54 or st+size>len(b):continue
    try:
     im=Image.open(io.BytesIO(b[st:st+size]));im.load()
     if im.width>10000 or im.height>10000:continue
     dest=unpack/'embedded'/p.name/f'{st:08x}.bmp';dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(b[st:st+size])
     embedded.append(dict(library=p.relative_to(root).as_posix(),offset=st,bytes=size,width=im.width,height=im.height,mode=im.mode))
     i=st+size
    except Exception:pass
 csvout('UI_내장_BMP.csv',embedded)
 # Pixel comparison with existing project research resources.
 existing={}
 for folder in ['Job','Items','UI','Monster','Skill']:
  for p in (base/folder).rglob('*'):
   if p.suffix.lower() not in ('.bmp','.png'):continue
   try:
    with Image.open(p) as im:
     k=(im.size,hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest());existing.setdefault(k,[]).append(p.relative_to(base).as_posix())
   except Exception:pass
 matches=[]
 for row in images:
  for path in existing.get(((row['width'],row['height']),row['pixel_sha256']),[]):matches.append(dict(archive=row['archive'],name=row['name'],existing=path,pixel_match=True))
 csvout('기존자료_픽셀일치.csv',matches)
 # Compact previews; preserve original bytes in unpack folder.
 chosen=[('gameres/char1.arc','301R.bmp'),('gameres/item.arc','Item.bmp')]
 chosen += [(x['archive'],x['name']) for x in images if x['archive'] in ['gameres/etc.arc','gameres/etc2.arc','gameres/etc3.arc']]
 panels=[]
 for arc,name in chosen:
  p=unpack/arc/name
  if not p.exists():continue
  im=Image.open(p).convert('RGB');im.thumbnail((550,330),Image.Resampling.NEAREST)
  panel=Image.new('RGB',(570,365),'#dedede');ImageDraw.Draw(panel).text((10,8),arc+'/'+name,fill='black');panel.paste(im,(10,30));panels.append(panel)
 if panels:
  sheet=Image.new('RGB',(1140,365*((len(panels)+1)//2)),'white')
  for i,p in enumerate(panels):sheet.paste(p,((i%2)*570,(i//2)*365))
  sheet.save(out/'클라이언트_이미지_미리보기.jpg')
 summary=dict(archives=len(list(root.rglob('*.arc'))),entries=len(arcs),methods=dict(collections.Counter(x['method'] for x in arcs)),decoded=sum(x['status']=='decoded_size_matched' for x in arcs),errors=errors,images=len(images),texts=len(texts),wav=len(sounds),midis=len(midis),embedded_bmp=len(embedded),pixel_matches=len(matches),map_files=len(maps),grs_files=len(grs),image_dimensions=dict(collections.Counter(f"{x['width']}x{x['height']}" for x in images)))
 (out/'요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2))
 print(json.dumps(summary,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
