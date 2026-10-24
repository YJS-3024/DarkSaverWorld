"""Native AttackChar setup, conditional overlays, counter lifetimes and sprite rows."""
import hashlib,json,struct,subprocess
from unicorn.x86_const import *
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write

def setup():
 h=Harness('userunit.dll');base=0x30010000;h.put(0x1010a3a0,base);return h,base

def fragment(h,start,end,regs):
 h.u.reg_write(UC_X86_REG_ESP,0x20010000)
 for reg,value in regs.items():h.u.reg_write(reg,value&0xffffffff)
 h.u.emu_start(start,end,count=100000);assert h.u.reg_read(UC_X86_REG_EIP)==end

def main():
 rows=[];cases=[]
 for dx in range(-5,6):
  for dy in range(-5,6):
   for extra in (-1,0,1,32767):
    h,b=setup();actor=b;target=b+736
    h.put(actor+0x70,10+dx,10+dy);h.put(target+0x70,10,10)
    for u in (actor,target):h.put(u+0x100,0x140);h.put(u+0x148,9);h.put(u+0x158,55);h.put(u+0x20,777)
    h.put(actor+0x294,99);h.u.mem_write(0x30001000,struct.pack('<hhh',0,1,extra));h.run(0x1000cee0,(0x30001000,))
    near=abs(dx)+abs(dy)<3;ox,oy=(dx*16,dy*16) if near else (0,0)
    assert h.get(target+0x284)==ox&0xffffffff and h.get(target+0x288)==oy&0xffffffff
    assert h.get(actor+0x284)==(-ox)&0xffffffff and h.get(actor+0x288)==(-oy)&0xffffffff
    assert h.get(target+0x280)==0 and h.get(actor+0x280)==0xffffffff
    assert h.get(target+0x294)==extra&0xffffffff and h.get(actor+0x294)==99
    for u in (actor,target):assert h.get(u+0x100)==0x144 and h.get(u+0x158)==0 and h.get(u+0x20)==777
    expected=1 if ox<0 else 2 if ox>0 else 3 if oy<0 else 4 if oy>0 else 9
    assert h.get(target+0x148)==expected
    cases.append(dict(dx=dx,dy=dy,extra=extra,target_offset_x=ox,target_offset_y=oy,target_facing=expected))
    rows.append(dict(case=f'attack_{dx}_{dy}_{extra}',result='two participants flagged; distance/facing/offset; target extra copied; HP preserved',match=True))
 # Execute exact draw block, replacing SetRect/BltFast only.
 for counter in (0,1,7,15,16,48):
  for extra in (-1,0,1,32767):
   for offset in ((0,0),(16,0),(0,-16)):
    for partner in (-1,0):
     h,b=setup();h.put(b+0x158,counter);h.put(b+0x280,partner&0xffffffff,*[v&0xffffffff for v in offset]);h.put(b+0x294,extra&0xffffffff)
     h.put(0x1010a3b4,111);h.put(0x1010a3b8,222);draws=[]
     def rect():
      sp=h.u.reg_read(UC_X86_REG_ESP);p,l,t,r,bt=[h.get(sp+4+i*4) for i in range(5)];h.put(p,l,t,r,bt);h.ret(1,20)
     def draw():
      sp=h.u.reg_read(UC_X86_REG_ESP);surface=h.get(sp+12);p=h.get(sp+16);draws.append((surface,tuple(struct.unpack('<4i',h.u.mem_read(p,16)))));h.ret(1,20)
     h.stubs={0x300f0000:rect,0x1001176e:draw}
     fragment(h,0x1000ffc1,0x100100d4,{UC_X86_REG_EAX:b,UC_X86_REG_ESI:0,UC_X86_REG_EBP:0x300f0000,UC_X86_REG_EBX:100,UC_X86_REG_EDI:200})
     allowed=offset!=(0,0) or partner==-1;col=(counter//2)%4
     expected=[(222,(col*32,32,col*32+32,64))] if allowed else []
     if allowed and counter<=15 and extra!=0:expected.append((111,(250+col*32,240,282+col*32,272)))
     assert draws==expected,(counter,extra,offset,partner,draws,expected)
     assert h.get(b+0x294)==extra&0xffffffff
     rows.append(dict(case=f'overlay_{counter}_{extra}_{offset}_{partner}',result='base/additional draw eligibility and exact source rect; extra retained',match=True))
 for mode in (0,1):
  for preview in (0,2):
   for counter in (0,15,47,48,49):
    h,b=setup();h.put(b+0x100,0x144);h.put(b+0x158,counter);h.put(b+0x2b0,preview);h.put(0x1010a3d8,mode)
    fragment(h,0x100105d8,0x10010675,{UC_X86_REG_ESI:0,UC_X86_REG_EAX:b,UC_X86_REG_EBX:0})
    count=counter+1;cleared=(mode==0 and preview!=2) or (mode!=0 and count>48)
    expected=0 if cleared else count%48 if mode==0 else count
    assert h.get(b+0x158)==expected and h.get(b+0x100)==(0x140 if cleared else 0x144)
    rows.append(dict(case=f'attack_update_{mode}_{preview}_{counter}',result='world/preview/combat counter and cleanup boundary matched',match=True))
 for counter in (0,24,25,26,27):
  h,b=setup();h.put(b+0x100,0x144);h.put(b+0x15c,counter,99);h.put(b+0x2ac,1)
  h.put(0x20010000,0,0,0,0,0x300ff000);h.u.reg_write(UC_X86_REG_ESP,0x20010000)
  for reg,val in ((UC_X86_REG_ESI,0),(UC_X86_REG_EAX,b),(UC_X86_REG_EBX,0)):h.u.reg_write(reg,val)
  h.u.emu_start(0x10010675,0x300ff000,count=1000);assert h.u.reg_read(UC_X86_REG_EIP)==0x300ff000
  cleared=counter>25
  assert h.get(b+0x15c)==(0 if cleared else counter+1) and h.get(b+0x100)==(0x104 if cleared else 0x144)
  assert h.get(b+0x160)==(0 if cleared else 99) and h.get(b+0x2ac)==(0xffffffff if cleared else 1)
  rows.append(dict(case='hp_update_'+str(counter),result='26 is retained until next update; cleanup resets shared delta/type',match=True))
 for display in (100,700,750,799,800):
  for counter in (0,1,17,18,19):
   h,b=setup();h.put(b,1,1);h.put(b+0x68,display);h.put(b+0x70,4,5);h.put(b+0x100,0x80);h.put(b+0x164,counter);h.put(0x100e32a0+4*(200*4+5),0)
   sounds=[]
   def play():sounds.append('play');h.ret()
   def stop():sounds.append('stop');h.ret()
   h.put(0x10012170,0x300f0000);h.put(0x10012174,0x300f0010);h.stubs={0x300f0000:play,0x300f0010:stop}
   h.run(0x10010100,(0,));immediate=700<=display<800;finished=immediate or counter+1>18
   assert h.get(b)==1 and h.get(b+4)==(0 if finished else 1)
   assert h.get(b+0x100)==(0 if finished else 0x80)
   assert h.get(b+0x164)==(counter if immediate else counter+1)
   assert h.get(0x100e32a0+4*(200*4+5))==(0xffffffff if finished else 0)
   assert sounds==([] if immediate else (['play'] if counter==0 else [])+(['stop'] if finished else []))
   rows.append(dict(case=f'death_{display}_{counter}',result='display700..799 immediate disable; others 19th counter removal; active field preserved',match=True))
 for hpflag in (0,0x40):
  h,b=setup();h.put(b,1,1);h.put(b+0x68,100);h.put(b+0x100,0x80|hpflag);h.put(b+0x164,1)
  h.run(0x100101f0,(0,));assert h.get(b+0x164)==(1 if hpflag else 2)
  rows.append(dict(case='death_hp_gate_'+str(hpflag),result='death update held while HP display flag40 is active',match=True))
 write('전투_공격연결_원본명령대조.csv',rows);write('전투_공격거리_방향.csv',cases)
 asm=[]
 for a,b in [(0x1000cee0,0x1000d0c7),(0x1000ffc1,0x100100d4),(0x100105d8,0x100106d7),(0x10010100,0x10010228)]:asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'userunit.dll')],text=True))
 (OUT/'전투_공격연결_근거.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,scope='native AttackChar writes, overlay draw block and attack/HP counter update instructions; SetRect/BltFast modeled; no Windows or damage calculation',extra_semantics='signed short preserved; all nonzero values enable optional overlay if prior draw gate passes; no named critical-hit interpretation established',binary_sha256=hashlib.sha256((ROOT/'userunit.dll').read_bytes()).hexdigest())
 (OUT/'전투_공격연결검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
