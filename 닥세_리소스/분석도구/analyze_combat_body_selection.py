"""Native common body rendering, clock-based frame choice and surface callers."""
import hashlib,json,struct,subprocess
from unicorn.x86_const import UC_X86_REG_ESP
from analyze_packet_requests import Harness
from analyze_special_skills import ROOT,OUT,write

def main():
 rows=[];evidence=[]
 for display in (100,345,700,750,899,981,999):
  for facing in range(1,9):
   for time in (0,333,666,999):
    for terrain in (0,6):
     h=Harness('userunit.dll');b=0x30010000;h.put(0x1010a3a0,b);h.put(b+0x68,display);h.put(b+0x1c,2);h.put(b+0x148,facing);h.put(b+0x100,4)
     h.put(0x100e328c,0,0);h.put(0x10015108,20,15);h.put(0x1010a3d8,1);h.put(0x1010a3dc,0)
     h.put(0x100df40c+16*display,0x1111);h.put(0x1010a3c0,0x2222);h.put(0x10012068,0x300f0000)
     draws=[]
     def rect():
      sp=h.u.reg_read(UC_X86_REG_ESP);p,l,t,r,bt=[h.get(sp+4+i*4) for i in range(5)];h.put(p,l,t,r,bt);h.ret(1,20)
     def draw():
      sp=h.u.reg_read(UC_X86_REG_ESP);surface=h.get(sp+12);ptr=h.get(sp+16)
      if surface==0x1111:draws.append(tuple(struct.unpack('<4i',h.u.mem_read(ptr,16))))
      h.ret(1,20)
     h.stubs={0x10004f30:lambda:h.ret(terrain),0x1000ad00:lambda:h.ret(0),0x300f0000:lambda:h.ret(time),0x300f0010:rect,0x1001176e:draw}
     h.put(0x10012144,0x300f0010);h.run(0x1000b5d0,(0,8,6))
     if display==999:assert not draws
     else:
      assert len(draws)==1
      col=[0,1,2,1][time//333];row={1:3,2:2,3:0,4:1,5:3,6:3,7:2,8:2}[facing]
      size=64 if 700<=display<800 else 32;height=(48 if size==64 else 25) if terrain==6 and display!=899 else size
      assert draws[0]==(col*size,row*size,(col+1)*size,row*size+height),(display,facing,time,terrain,draws)
     rows.append(dict(display_id=display,facing=facing,time_ms=time,terrain=terrain,source_rect=json.dumps(draws[0] if draws else []),result='clock/facing/size/submerge crop matched; attack flag does not select extra body rows',match=True))
 write('전투_본체_시간지형_원본명령대조.csv',rows)
 # Preserve every directly named GetCharSurface caller, including local register-call references.
 sites=[('Saver.exe',0x412aa9),('Saver.exe',0x414057),('Saver.exe',0x41a99b),('Saver.exe',0x41ad5c),('Saver.exe',0x41c925),('Saver.exe',0x41dac5),('Saver.exe',0x4215b5),('Saver.exe',0x4230bf),('Saver.exe',0x423160),('GameCtl.dll',0x100026ac),('GameCtl.dll',0x100054ba),('GameCtl.dll',0x10006412),('GameCtl.dll',0x1000680e),('GameCtl.dll',0x100073d6),('GameCtl.dll',0x1000768a),('GameCtl.dll',0x10007811)]
 asm=[]
 for name,site in sites:
  evidence.append(dict(file=name,reference_va=hex(site),scope='direct import-reference inventory; register reference may serve several calls; not complete indirect call graph'))
  asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={site-64}',f'--stop-address={site+160}',str(ROOT/name)],text=True))
 for a,b in [(0x1000b5d0,0x1000bc8c),(0x1000ac40,0x1000acd1)]:asm.append(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'userunit.dll')],text=True))
 write('전투_캐릭터표면_외부참조.csv',evidence);(OUT/'전투_본체추가_근거.asm.txt').write_text('\n'.join(asm))
 result=dict(cases=len(rows),mismatches=0,scope='common body renderer original instructions; clock/tile/surface palette selector/rect/draw modeled; preloaded surfaces; not all callers or complete Windows render',frame='floor(GetTickCount/333)&3, index3 becomes1; 0,1,2,1',additional_rows='extra physical sheet rows are not selected by this normal body path; no unsupported attack/hit naming',external_surface_reference_sites=len(sites),binary_sha256=hashlib.sha256((ROOT/'userunit.dll').read_bytes()).hexdigest())
 (OUT/'전투_본체추가검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
