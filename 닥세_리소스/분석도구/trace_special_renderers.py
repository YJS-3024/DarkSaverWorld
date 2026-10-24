"""Isolated x86 instruction emulation of renderer functions with mocked external APIs.
Requires unicorn==2.1.4. Never invokes PE entrypoints, Windows or a game server.
Output is a draw-command experiment under declared synthetic inputs, not gameplay capture.
"""
import sys,struct,json,csv,collections,hashlib
from pathlib import Path
from analyze_special_skills import PE,ROOT,BASE,OUT,ARC,table,write
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UcError
from unicorn.x86_const import *
class Trace:
 def __init__(self):
  self.pe=PE(ROOT/'userunit.dll');self.u=Uc(UC_ARCH_X86,UC_MODE_32);u=self.u
  for a,n in [(0,0x1000),(0x10000000,0x200000),(0x20000000,0x200000),(0x30000000,0x300000),(0x40000000,0x100000),(0x50000000,0x100000)]:u.mem_map(a,n)
  b=self.pe.b;p=struct.unpack_from('<I',b,60)[0];sz=struct.unpack_from('<H',b,p+20)[0];n=struct.unpack_from('<H',b,p+6)[0]
  for i in range(n):
   vs,va,rs,off=struct.unpack_from('<4I',b,p+24+sz+i*40+8);u.mem_write(0x10000000+va,b[off:off+rs])
  self.patterns={int(t[0]):t for t in table('MagicPtn')};self.skills={int(t[0]):t for t in table('Magictbl')};self.body_mode=False;self.hooks={};self.events=[];self.steps=0
  # Every external import is redirected; unknown imports fail closed.
  for a in range(0x10012000,0x1001217c,4):
   stub=0x50000000+(a-0x10012000)*4;self.w(a,stub);u.mem_write(stub,b'\xc3');self.hooks[stub]=('import',a)
  # x87 conversion uses synthetic helper instructions with truncation control word.
  u.mem_write(0x50002020,bytes.fromhex('d93d10200050 66a110200050 660d000c 66a312200050 d92d12200050 df3d00200050 d92d10200050 a100200050 8b1504200050 c3'))
  u.hook_add(UC_HOOK_CODE,self.hook)
 def w(self,a,x):self.u.mem_write(a,struct.pack('<I',x&0xffffffff))
 def r(self,a):return struct.unpack('<I',self.u.mem_read(a,4))[0]
 def ints(self,a,n):return struct.unpack('<'+'i'*n,self.u.mem_read(a,4*n))
 def ret(self,val=0,pop=0):
  u=self.u;sp=u.reg_read(UC_X86_REG_ESP);ip=self.r(sp);u.reg_write(UC_X86_REG_EAX,val&0xffffffff);u.reg_write(UC_X86_REG_ESP,sp+4+pop);u.reg_write(UC_X86_REG_EIP,ip)
 def hook(self,u,a,size,_):
  self.steps+=1
  sp=u.reg_read(UC_X86_REG_ESP);args=lambda n:self.ints(sp+4,n)
  if a==0x5000f000:u.emu_stop();return
  if a==0x1000b5d0 and not self.body_mode:self.events.append(dict(op='body_common',args=args(3)));self.ret();return
  if a==0x1000f8e0:self.ret(1);return
  if a==0x1000f920:
   dst,pos,flag=args(3);x,y=self.ints(pos,2);self.w(dst,x*32);self.w(dst+4,y*32);self.ret(dst);return
  if a not in self.hooks:return
  _,iat=self.hooks[a]
  if iat==0x1001215c:
   skill_id=args(1)[0];self.bind_record(skill_id);self.ret(0x40000000);return
  if iat==0x10012144:
   ptr,l,t,r,b=args(5);u.mem_write(ptr,struct.pack('<4i',l,t,r,b));self.ret(1,20);return
  if iat==0x10012138:
   dst,p1,p2=args(3);x=self.ints(p1,4);y=self.ints(p2,4);rect=(max(x[0],y[0]),max(x[1],y[1]),min(x[2],y[2]),min(x[3],y[3]));ok=rect[0]<rect[2] and rect[1]<rect[3]
   u.mem_write(dst,struct.pack('<4i',*(rect if ok else (0,0,0,0))));self.ret(int(ok),12);return
  if iat==0x1001213c:
   ptr,dx,dy=args(3);l,t,r,b=self.ints(ptr,4);u.mem_write(ptr,struct.pack('<4i',l+dx,t+dy,r+dx,b+dy));self.ret(1,12);return
  if iat==0x10012140:
   u.mem_write(args(1)[0],b'\0'*16);self.ret(1,4);return
  if iat==0x10012020:
   self.events.append(dict(op='surface_fill',color=args(1)[0]));self.ret(1,4);return
  if iat==0x10012018:
   x,y,surf,rect,flags=args(5);self.events.append(dict(op='BltFast',surface=hex(surf&0xffffffff),dst=[x,y],src=list(self.ints(rect,4)),flags=flags));self.ret(1,20);return
  if iat==0x1001201c:
   dst,surf,src,flags,fx=args(5);self.events.append(dict(op='Blt',surface=hex(surf&0xffffffff),dst=list(self.ints(dst,4)) if dst else None,src=list(self.ints(src,4)) if src else None,flags=flags));self.ret(1,20);return
  if iat in [0x10012170,0x10012174]:self.events.append(dict(op='play_sound' if iat==0x10012170 else 'stop_sound',id=args(1)[0]));self.ret();return
  if iat==0x1001211c:self.ret(7);return
  if iat==0x10012064:
   x,y,z=args(3);num=x*y;sign=-1 if (num<0)!=(z<0) else 1;value=sign*((abs(num)+abs(z)//2)//abs(z)) if z else -1;self.ret(value,12);return
  if iat==0x10012068:self.ret(100000);return
  if iat==0x1001210c:u.reg_write(UC_X86_REG_EIP,0x50002020);return
  raise RuntimeError(f'unmocked import {hex(iat)} at return {hex(self.r(sp))}')
 def bind_record(self,skill_id):
  t=self.skills[skill_id];p=self.patterns[int(t[14])]
  self.w(0x40000000,skill_id)
  for off,col in {4:5,8:3,12:4,16:7,20:8,24:9,28:10,0x16c:11,0x170:2,0x174:13}.items():self.w(0x40000000+off,int(t[col]))
  # Radius is deliberately overridden for renderer geometry experiments.
  self.w(0x4000000c,3)
  for off,i in {0x28:2,0x2c:3,0x30:4,0x34:5,0x38:6,0x3c:7,0x58:8,0x40:9,0x44:10,0x48:11,0x4c:12,0x50:13,0x54:14,0x5c:15,0x60:16,0x64:17,0x68:18}.items():self.w(0x40000000+off,int(p[i]))
 def run(self,pattern,handler,side,counter,xy=(8,6),direction=-1):
  u=self.u;u.mem_write(0x30000000,b'\0'*0x1000);u.mem_write(0x40000000,b'\0'*0x1000)
  self.w(0x1010a3d4,0);self.w(0x100df3f8,0);self.w(0x100df3fc,0);self.w(0x100df400,640);self.w(0x100df404,480);self.w(0x1010a3a0,0x30000000);self.w(0x1010a3a8,0x60001000)
  for a,ptr in [(0x1010a3b4,0x60000001),(0x1010a3b8,0x60000002),(0x1010a3bc,0x60000003),(0x1010a3c0,0x60000004)]:self.w(a,ptr)
  self.w(0x100e3298,20);self.w(0x100e329c,15);self.w(0x10015108,20);self.w(0x1001510c,15);self.w(0x100e3290,0);self.w(0x100e328c,0)
  skill_id=next((int(t[0]) for t in self.skills.values() if t[14]==pattern[0]),90000+int(pattern[0]))
  if skill_id not in self.skills:
   t=[str(skill_id),'synthetic_unreferenced_pattern']+['0']*15;t[3]='3';t[4]='3';t[14]=pattern[0];self.skills[skill_id]=t
  for off,val in [(0,1),(4,1),(0x68,100),(0x70,xy[0]),(0x74,xy[1]),(0x100,0x100),(0x148,4),(0x174,skill_id),(0x1c4,counter),(0x1c8,3),(0x1cc,3),(0x1d0,direction),(0x1d4,6),(0x1dc,skill_id+100000),(0x1e0,counter),(0x1e8,0)]:self.w(0x30000000+off,val)
  self.bind_record(skill_id)
  for address,value in getattr(self,'input_overrides',{}).items():self.w(address,value)
  if self.body_mode:
   display_id,flags,facing=self.body_case
   for off,val in [(0x68,display_id),(0x100,flags),(0x148,facing)]:self.w(0x30000000+off,val)
   self.w(0x1010a3d8,0);self.w(0x10015110,0);self.w(0x100df40c+display_id*16,0x60003000)
  u.reg_write(UC_X86_REG_FPCW,0x37f);u.reg_write(UC_X86_REG_FPSW,0);u.reg_write(UC_X86_REG_FPTAG,0xffff)
  sp=0x201ff000;u.mem_write(sp,struct.pack('<4I',0x5000f000,*([12,9,counter] if side=='area' else ([0,8,6] if self.body_mode else [0,0,0]))));u.reg_write(UC_X86_REG_ESP,sp);u.reg_write(UC_X86_REG_EBP,0);u.reg_write(UC_X86_REG_EFLAGS,2);self.events=[];self.steps=0
  try:
   u.emu_start(handler,0,count=150000)
   if u.reg_read(UC_X86_REG_EIP)!=0x5000f000:raise RuntimeError('instruction limit')
   return dict(ok=True,events=self.events,steps=self.steps,next_actor=self.r(0x300001e0),next_target=self.r(0x300001c4),attack_counter=self.r(0x30000158),hp_counter=self.r(0x3000015c),movement_gate=self.r(0x1017fa10))
  except (UcError,RuntimeError) as e:return dict(ok=False,error=str(e),ip=hex(u.reg_read(UC_X86_REG_EIP)),events=self.events,steps=self.steps)
def main():
 tr=Trace();patterns={int(t[0]):t for t in table('MagicPtn')};links=list(csv.DictReader((OUT/'특수기_패턴_표시함수.csv').open(encoding='utf-8-sig')));rows=[]
 for link in links:
  pid=int(link['pattern_id']);kind=int(link['render_kind'])
  if kind==99:continue # CString-based path has separate static verification.
  for side in ['actor','target']:
   h=int(link[side+'_handler_va'],16)
   for count in range(51):
    result=tr.run(patterns[pid],h,side,count)
    rows.append(dict(pattern_id=pid,render_kind=kind,side=side,counter=count,handler_va=hex(h),**result))
 # Cover directional variants of the beam actor functions and area pass.
 for link in links:
  kind=int(link['render_kind']);pid=int(link['pattern_id'])
  if kind not in [6,16,19,3,4,13]:continue
  side='area' if kind in [3,4,13] else 'actor'
  h={3:0x1000a1b0,4:0x1000a3b0,13:0x1000a650}.get(kind,int(link['actor_handler_va'],16))
  for direction in [-1,-2,-3,-4]:
   for count in [0,1,4,5,9,10,15,30]:
    result=tr.run(patterns[pid],h,side,count,direction=direction)
    rows.append(dict(pattern_id=pid,render_kind=kind,side=side,counter=count,direction=direction,handler_va=hex(h),**result))
 (OUT/'특수기_격리명령추적.json').write_text(json.dumps(dict(method='Unicorn 2.1.4 isolated instruction emulation; mocked Windows, renderer, table, RNG and sound APIs; no original entrypoint or server',synthetic_inputs=dict(target_tile=[8,6],origin_tile=[3,3],radius=3,direction=-1,rand=7),runs=rows),ensure_ascii=False,separators=(',',':')))
 # Independent checks against previously statically decoded centered and beam rules.
 for r in rows:
  if r['side']!='target' or r['render_kind'] not in [5,7,9,22] or not r['ok']:continue
  p=patterns[r['pattern_id']];n=int(p[8]);limit=n*(2 if r['render_kind']==22 else 1);limit=8 if limit<3 else limit;c=r['counter']
  draws=[e for e in r['events'] if e['op']=='BltFast']
  assert len(draws)==int(c<limit),(r['pattern_id'],c,draws)
  if draws:
   k=c%n;x,y,w,h,dx,dy=map(int,p[2:8]);assert draws[0]['src']==[x+k*dx,y+k*dy,x+k*dx+w,y+k*dy+h]
   assert draws[0]['dst']==[256-int((w-32)/2),192-int((h-32)/2)]
 for r in rows:
  if r['render_kind']==6 and r['side']=='actor' and r['counter']<15:
   d=[e for e in r['events'] if e['op']=='BltFast'];assert len(d)==2+17*(r['counter']//5+1)
  if r['render_kind']==16 and r['side']=='actor' and r['counter']<15:
   d=[e for e in r['events'] if e['op']=='BltFast'];assert len(d)==2*min(r['counter']+1,4)+2*min(r['counter'],3)
 failed=[r for r in rows if not r['ok']]
 groups=collections.defaultdict(list)
 for r in rows:groups[(r['render_kind'],r['side'],r['handler_va'])].append(r)
 overview=[]
 for (kind,side,h),rs in sorted(groups.items()):
  draws=[e for r in rs for e in r['events'] if e['op'] in ['Blt','BltFast']]
  overview.append(dict(render_kind=kind,side=side,handler_va=h,runs=len(rs),passed=sum(r['ok'] for r in rs),draw_commands=len(draws),surfaces=','.join(sorted(set(e['surface'] for e in draws))),sound_ids=','.join(map(str,sorted(set(e['id'] for r in rs for e in r['events'] if e['op']=='play_sound')))),body_call_observed=any(e['op']=='body_common' for r in rs for e in r['events']),scope='synthetic sampled input; not full Windows or all-branch proof'))
 write('특수기_격리명령추적_경로.csv',overview)
 summary=dict(runs=len(rows),passed=len(rows)-len(failed),paths=len(overview),failure_types=[dict(error=k[0],ip=k[1],count=v) for k,v in collections.Counter((r.get('error'),r.get('ip')) for r in failed).items()],method='isolated original x86 renderer instructions, external APIs mocked',source_sha256=hashlib.sha256((ROOT/'userunit.dll').read_bytes()).hexdigest())
 (OUT/'특수기_격리명령추적_요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,indent=2))
if __name__=='__main__':main()
