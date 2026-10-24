"""Map sprite attribute mutation, tile animation and terrain table choice."""
import collections,hashlib,json,struct,subprocess
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_ECX,UC_X86_REG_EIP
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,PE,write
from decode_visuals import read_mrc,read_grs

def main():
 rows=[];changed=[];grs={int(p.stem[4:]):read_grs(p) for p in (ROOT/'MAP').glob('*.grs')};seen=set()
 for path in sorted((ROOT/'MAP').iterdir()):
  if path.suffix.lower() not in ('.mrc','.mrtc'):continue
  digest=hashlib.sha256(path.read_bytes()).hexdigest()
  if digest in seen:continue
  seen.add(digest);d=read_mrc(path);w,ht=d['width'],d['height'];raw=b''.join(d['tiles']);expected=bytearray(raw)
  h=Harness('DManage.dll');o=0x30000000;h.put(o+8,0x30001000);h.put(0x30001004,0x30002000);h.put(o+0x14,0x30020000,0x30080000);h.put(o+0x30,w,ht,len(d['sprites']));h.u.mem_write(0x30020000,raw)
  cursor=0x30090000
  for rid,g in grs.items():
   resource=cursor;cursor+=0x40;ptrs=cursor;cursor+=4*len(g['sprites'])
   h.put(0x30002000+rid*4,resource);h.put(resource+0x10,len(g['sprites']));h.put(resource+0x2c,ptrs)
   for i,s in enumerate(g['sprites']):
    rec=cursor;cursor+=16;h.put(ptrs+i*4,rec);h.put(rec+4,s['width'],s['height'])
  assert cursor<0x300e0000
  sprites=bytearray()
  for s in d['sprites']:
   record=bytearray(16);record[0]=s['resource'];struct.pack_into('<ii',record,4,s['x'],s['y']);record[12]=s['index'];record[13]=s['frame'];sprites+=record
   sprite=grs[s['resource']]['sprites'][s['index']];sw,sh=sprite['width']//32,sprite['height']//32
   right=min(s['x']+sw,w);bottom=min(s['y']+sh,ht);half=int(-0.5*((0 if sh==1 else sh)+1));top=s['y']-half
   for x in range(s['x'],right):
    for y in range(top,bottom):
     assert 0<=x<w and 0<=y<ht,(path.name,s)
     off=5*(y*w+x)+3;expected[off]=(expected[off]+128)&255
  if sprites:h.u.mem_write(0x30080000,bytes(sprites))
  # x87 conversion helper returns EAX truncated toward zero; preserve other arithmetic.
  h.u.mem_write(0x1000c0f2,bytes.fromhex('83ec08 d93c24 660d000c 6689442402 d96c2402 db5c2404 d92c24 8b442404 83c408 c3'))
  h.stubs={0x1000bed0:lambda:h.ret(),0x1000bebe:lambda:h.ret()}
  h.put(0x20010000,0x300ff000);h.u.reg_write(UC_X86_REG_ESP,0x20010000);h.u.reg_write(UC_X86_REG_ECX,o)
  h.u.emu_start(0x1000aa80,0x300ff000,count=10000000);assert h.u.reg_read(UC_X86_REG_EIP)==0x300ff000,path.name
  got=bytes(h.u.mem_read(0x30020000,len(raw)));assert got==expected,(path.name,[(i//5%w,i//5//w,raw[i],got[i],expected[i]) for i in range(3,len(raw),5) if got[i]!=expected[i]][:12])
  n=sum(got[i]!=raw[i] for i in range(3,len(raw),5));changed.append(dict(map=path.name,sprites=len(d['sprites']),changed_attribute_cells=n,sha256=digest))
  rows.append(dict(case='sprite_footprint_'+path.name,result='all attribute bytes match native footprint mutation',match=True))
 # Native tile-data frame selection, global byte counter and local row counter.
 for marker in (0,1,2,255):
  for phase in (0,1,7,255,256):
   h=Harness('DManage.dll');o=0x30000000;h.put(o+8,0x30001000);h.put(0x30001004,0x30002000);h.put(0x30002000,0x30003000)
   h.put(0x30003018,0x30004000);h.put(0x30004000,0x30005000);h.put(0x3000500c,0x30006000);h.put(0x30006000,0x30007000)
   h.u.mem_write(0x30007006,b'\x03');h.put(0x3000700c,0x30007100);h.u.mem_write(0x30007100,bytes([4,9,12]))
   h.put(o+0x14,0x30008000);h.put(o+0x20,0x30009000);h.put(0x30009000,0x3000a000);h.put(o+0x30,1,1)
   h.u.mem_write(0x30008000,bytes([0,0,0,22,marker]));h.u.mem_write(0x3000a000,b'\x07');h.put(0x3000b000,0,0)
   h.u.mem_write(o+4,bytes([phase&255]));h.run(0x1000a8b0,(0x3000b000,0x3000b004,0x3000b008),o)
   ptr=h.get(0x3000b008);got=bytes(h.u.mem_read(ptr,5));index=(phase&255) if marker>1 else 7
   assert got==bytes([0,0,0,22,[4,9,12][index%3]])
   assert bytes(h.u.mem_read(0x30008000,5))==bytes([0,0,0,22,marker])
   h.run(0x1000aa70,this=o);assert bytes(h.u.mem_read(o+4,1))==bytes([((phase&255)+1)&255])
   rows.append(dict(case=f'animation_{marker}_{phase}',result='frame table selection/global byte wrapping; raw tile preserved',match=True))
 # Dynamic tile mutation uses native bounds checking and copies all five byte fields.
 for x,y in ((1,1),(0,0),(-1,1),(4,1),(1,4)):
  for entry,args in ((0x10009070,(x,y,150)),(0x10009090,(x,y,2,3,4,5,6))):
   h=Harness('DManage.dll');o=0x30000000;m=0x30001000;h.put(o+0x4c,m);h.put(m+0x30,4,4);h.put(m+0x14,0x30002000);original=bytes([10,11,12,13,14])*16;h.u.mem_write(0x30002000,original)
   h.run(entry,tuple(v&0xffffffff for v in args),o);expected=bytearray(original)
   if 0<=x<4 and 0<=y<4:
    offset=5*(y*4+x)
    if entry==0x10009070:expected[offset+3]=150
    else:expected[offset:offset+5]=bytes([2,3,4,5,6])
   assert bytes(h.u.mem_read(0x30002000,80))==expected
   rows.append(dict(case=f'tile_mutation_{entry:x}_{x}_{y}',result='native in-bounds write / out-of-bounds no-op',match=True))
 for resource in (0,255):
  h=Harness('DManage.dll');o=0x30000000;m=0x30001000;h.put(o+0x4c,m);h.put(m+0x14,0x30002000,0x30003000);h.put(m+0x30,10,10,2);h.put(o+0x3c,0x30004000)
  original=bytes([1,2,3,4,5])*100;h.u.mem_write(0x30002000,original)
  for i in range(2):h.put(0x30003004+i*16,2,2)
  def sprite_dim():
   sp=h.u.reg_read(UC_X86_REG_ESP);p=h.get(sp+4);h.put(p,64,96);h.ret(p,8)
  h.stubs[0x1000a350]=sprite_dim;h.run(0x100090d0,(2,2,resource,7,8,150),o)
  assert bytes(h.u.mem_read(0x30003000,1))==bytes([resource])
  assert bytes(h.u.mem_read(0x3000300c,2))==bytes([7,8]) and bytes(h.u.mem_read(0x3000301c,2))==bytes(2)
  expected=bytearray(original)
  for x in range(2,4):
   for y in range(2,5):expected[5*(y*10+x)+3]=150
  assert bytes(h.u.mem_read(0x30002000,len(expected)))==expected
  rows.append(dict(case='sprite_change_'+str(resource),result='first coordinate match only; all visual fields replaced; full footprint attribute assigned; FF is written',match=True))
 # Timer and redraw trigger with strict 300ms threshold.
 for elapsed in (0,299,300,301,1000):
  for moved in (False,True):
   h=Harness('DManage.dll');o=0x30000000;m=0x30001000;h.put(o+0x34,1);h.put(o+0x4c,m);h.put(o+0x78,2,3);h.put(0x100131d0,1000);h.put(0x1000d07c,0x300f0000);h.stubs[0x300f0000]=lambda:h.ret(1000+elapsed);draws=[]
   def redraw():
    sp=h.u.reg_read(UC_X86_REG_ESP);draws.append([h.get(sp+4+i*4) for i in range(6)]);h.ret(clean=24)
   h.stubs[0x100092d0]=redraw;h.run(0x10009250,(4 if moved else 2,3,20,15),o)
   animated=elapsed>300;assert bytes(h.u.mem_read(m+4,1))==bytes([int(animated)])
   assert len(draws)==int(animated or moved)
   if draws:assert draws[0][1]==int(moved)
   rows.append(dict(case=f'prepare_timer_{elapsed}_{moved}',result='strict >300ms advances one frame; viewport change requests full redraw',match=True))
 # Sprite viewport intersection and cached visible-index order; Win32 rectangle APIs modeled.
 for camera in ((0,0),(2,3),(-1,-1)):
  for viewport in ((20,15),(2,2)):
   h=Harness('DManage.dll');o=0x30000000;m=0x30001000;positions=[(-2,0),(-1,0),(5,10),(5,1),(20,0),(19,14)]
   h.put(o+0x34,1);h.put(o+0x4c,m);h.put(m+0x38,len(positions));h.put(m+0x18,0x30002000);h.put(o+0x68,0x30003000);h.put(o+0x70,0xffffff9c,0xffffff9c)
   h.put(o+0x3c,0x30004000);h.put(o+0x20,0x30005000);h.put(0x30005000,0x30006000);h.put(0x30006004,0x30007000);h.put(0x30007000,0x30008000);h.put(0x30008004,0x30009000);h.put(0x30009000,1234)
   for i,(x,y) in enumerate(positions):
    a=0x30002000+16*i;h.put(a+4,x&0xffffffff,y&0xffffffff)
   def dim():
    sp=h.u.reg_read(UC_X86_REG_ESP);dest=h.get(sp+4);h.put(dest,64,64);h.ret(dest,clean=8)
   def rect(a):return list(struct.unpack('<4i',h.u.mem_read(a,16)))
   def offset():
    sp=h.u.reg_read(UC_X86_REG_ESP);p,dx,dy=[h.get(sp+i) for i in (4,8,12)];dx=dx if dx<2**31 else dx-2**32;dy=dy if dy<2**31 else dy-2**32
    r=rect(p);h.u.mem_write(p,struct.pack('<4i',r[0]+dx,r[1]+dy,r[2]+dx,r[3]+dy));h.ret(1,12)
   def intersect():
    sp=h.u.reg_read(UC_X86_REG_ESP);dest,a,b=[h.get(sp+i) for i in (4,8,12)];ra,rb=rect(a),rect(b);r=[max(ra[0],rb[0]),max(ra[1],rb[1]),min(ra[2],rb[2]),min(ra[3],rb[3])]
    good=r[2]>r[0] and r[3]>r[1];h.u.mem_write(dest,struct.pack('<4i',*(r if good else [0,0,0,0])));h.ret(int(good),12)
   def empty():
    r=rect(h.get(h.u.reg_read(UC_X86_REG_ESP)+4));h.ret(int(r[0]>=r[2] or r[1]>=r[3]),4)
   draws=[]
   def draw():
    sp=h.u.reg_read(UC_X86_REG_ESP);draws.append(tuple(struct.unpack('<ii',h.u.mem_read(sp+4,8))));h.ret(clean=16)
   h.put(0x1000d1f0,0x300f0000);h.put(0x1000d1ec,0x300f0010);h.put(0x1000d1e8,0x300f0020)
   h.stubs={0x1000a350:dim,0x300f0000:offset,0x300f0010:intersect,0x300f0020:empty,0x10005c00:draw}
   expected=[]
   for x,y in positions:
    sx,sy=32*(x-camera[0]),32*(y-camera[1])
    if sx+64>0 and sy+64>0 and sx<viewport[0]*32 and sy<viewport[1]*32:expected.append((sx,sy))
   args=(0,*[v&0xffffffff for v in camera],*viewport)
   h.run(0x10008940,args,o);assert draws==expected,(camera,viewport,draws,expected)
   draws.clear();h.run(0x10008940,args,o);assert draws==expected
   rows.append(dict(case=f'sprite_viewport_{camera}_{viewport}',result='native visible list and cached replay retain file order; exact edge contact excluded',match=True))
 # Table parser argument/store layout uses 30 sequential dwords, row stride180.
 table_dir=ROOT.parents[1]/'ARC_해제/gameres/set.arc'
 for filename in ('TileAttr.atr','TileAttb.atr'):
  lines=[line.split(';')[0].split() for line in (table_dir/filename).read_bytes().decode('cp949').splitlines()]
  lines=[v for v in lines if v and v[0].lstrip('-').isdigit()]
  for i,values in enumerate(lines):
   assert len(values)==31
   h=Harness('setunit.dll');sp=0x20010000;line=0x30001000;h.put(sp+0x10,line);h.put(line-8,1);h.u.mem_write(line,' '.join(values).encode()+bytes(1))
   h.u.reg_write(UC_X86_REG_ESP,sp);h.u.reg_write(UC_X86_REG_EAX,line)
   from unicorn.x86_const import UC_X86_REG_ESI,UC_X86_REG_EDI
   h.u.reg_write(UC_X86_REG_ESI,0x10265270+180*i);h.u.reg_write(UC_X86_REG_EDI,0x300f0000)
   pointers=[]
   def scan():
    callsp=h.u.reg_read(UC_X86_REG_ESP)
    for n,value in enumerate(values):
     ptr=h.get(callsp+12+4*n);pointers.append(ptr);h.put(ptr,int(value)&0xffffffff)
    h.ret(len(values))
   h.stubs[0x300f0000]=scan;h.u.emu_start(0x100012ea,0x10001384,count=10000)
   assert h.u.reg_read(UC_X86_REG_EIP)==0x10001384
   assert pointers[1:]==[0x10265200+180*i+4*j for j in range(30)]
   assert bytes(h.u.mem_read(0x10265200+180*i,120))==struct.pack('<30i',*map(int,values[1:]))
  rows.append(dict(case='terrain_store_'+filename,result=f'{len(lines)} original text rows -> native parser pointer order/180-byte row layout',match=True))
 p=PE(ROOT/'setunit.dll');names={hex(a):p.data(a,30).split(b'\0')[0].decode() for a in (0x10005098,0x10005294)}
 for mode in (0,1,2,3):
  h=Harness('setunit.dll');calls=[]
  def terrain():calls.append(h.get(h.u.reg_read(UC_X86_REG_ESP)+4));h.ret(1)
  h.stubs={0x100011a0:terrain,0x10001420:lambda:h.ret(1),0x10001a10:lambda:h.ret(1),0x10001ec0:lambda:h.ret(1)}
  h.run(0x10002540,(mode,));assert calls==[0x10005294]
  rows.append(dict(case='table_initial_'+str(mode),result='ReadSetFiles always reads tileattr.atr regardless item/ability mode',match=True))
 h=Harness('setunit.dll');calls=[]
 def terrain2():calls.append(h.get(h.u.reg_read(UC_X86_REG_ESP)+4));h.ret(1)
 h.stubs[0x100011a0]=terrain2;h.run(0x10001410);assert calls==[0x10005098]
 rows.append(dict(case='table_battle_override',result='ReadTileAttr2 loads tileattb.atr into same table',match=True))
 write('맵_스프라이트_속성변경.csv',changed);write('맵_런타임_추가_원본명령대조.csv',rows)
 asm=[]
 for name,a,b in [('DManage.dll',0x10008940,0x10008b58),('DManage.dll',0x10009070,0x100091d0),('DManage.dll',0x10009250,0x100092d0),('DManage.dll',0x1000aa70,0x1000aba3),('DManage.dll',0x1000a8b0,0x1000aa00),('setunit.dll',0x10002540,0x10002574),('setunit.dll',0x10001410,0x1000141e),('setunit.dll',0x100012ea,0x10001396),('GameCtl.dll',0x10007e60,0x10007e84)]:
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/name)],text=True))
 (OUT/'맵_런타임_추가_근거.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,maps=len(changed),maps_with_sprite_changes=sum(r['changed_attribute_cells']>0 for r in changed),changed_cells=sum(r['changed_attribute_cells'] for r in changed),terrain_filenames=names,scope='native sprite footprint/mutators/viewport cache, tile animation/frame lookup, table selector/parser pointer order; rectangle APIs/draw/CString/sscanf and x87 truncation helper modeled; resource dimensions from original GRS',limits='no Windows graphics/device or full application; historical frame interval not established',hashes={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in ('DManage.dll','setunit.dll','GameCtl.dll')})
 (OUT/'맵_런타임_추가검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
