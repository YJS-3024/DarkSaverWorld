"""Enumerate exact late GameCtl5C dispatch destinations without executing handlers.
Routing presence is not handler completeness or a whole-client progress percentage.
"""
import hashlib,json,re,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/OriginalWorldChecks'))
from check_native_battle_decoder import NativeDecoder,UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP
HANDLERS={1:0xd640,3:0xd800,5:0xd760,6:0xd7b0,7:0xd830,0xc:0xd870,0xd:0xd8a0,0xe:0xd6a0,0xf:0xe290,0x13:0xdcb0,0x15:0xdcf0,0x20:0xd8f0,0x30:0xddc0,0x31:0xdec0,0x40:0xd610,0x50:0xc7d0,0x51:0xcb90,0x52:0xd400,0x53:0xd470,0x54:0xd280,0x5c:0xdfa0,0x60:0xd550,0x61:0xd150,0x64:0xdf10,0x65:0xdf10,0x66:0xe060,0x67:0xdb20,0x68:0xdfb0,0x82:0xd700,0x89:0xda10,0xb2:0xd4c0}
ALTERNATE={0x15:'Assets/_Original/Runtime/Campaign/CampaignService.cs:ReceiveCampaignEvents (legacy explicit local queue; raw15 uses ReceiveNativeImmediateEvents)'}
class Routes(NativeDecoder):
    def __init__(self):
        super().__init__();self.target=None;self.targets={0x10000000+v for v in HANDLERS.values()}
        self.handles.append(self.uc.hook_add(UC_HOOK_CODE,self.capture,begin=0x1000c000,end=0x1000eaff))
    def capture(self,uc,address,size,data):
        if address in self.targets:self.target=address;uc.emu_stop()
    def route(self,kind):
        self.target=None;self.calls=[];self.uc.mem_write(self.DATA,bytes([0x5c,kind])+bytes(30));sp=self.STACK+0xff000
        self.uc.mem_write(sp,struct.pack('<II',self.STOP,self.DATA));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(0x1000eb10,self.STOP+1,timeout=5000000,count=100000)
        if self.target is not None:return hex(self.target)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
        if self.calls:assert self.calls==[dict(kind='pulse',values=[])];return 'UserUnit.IncTick import'
        return None

def main():
    source=ROOT/'Assets/_Original/Runtime/Combat/NativeBattlePacketService.cs'
    declared={int(s,0)for s in re.findall(r'\btype\s*==\s*(0x[0-9a-fA-F]+|[0-9]+)',source.read_text())}
    n=Routes();rows=[];ignored=[]
    for code in range(256):
        target=n.route(code);expected='UserUnit.IncTick import'if code==4 else hex(0x10000000+HANDLERS[code])if code in HANDLERS else None
        assert target==expected,(hex(code),target,expected)
        if target is None:ignored.append(code);continue
        rows.append(dict(opcode=f'5C{code:02X}',destination=target,commonIngressDeclared=code in declared,alternateEntry=ALTERNATE.get(code)))
    n.close();assert len(rows)==32 and declared <= ({4}|set(HANDLERS))
    paths=[Path(__file__),source,ROOT/'Tools/OriginalWorldChecks/check_native_battle_decoder.py',ROOT/'분석/Ghidra/GameCtl/DispatchGamePacket.c']
    result=dict(originalGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',examinedSubtypes=256,originalDestinations=len(rows),declaredCommonRoutes=len(declared),mismatches=0,unityPlay=False,
        scope='Execute originalEB10 lookup only; stop at native handlers before body execution, IncTick captured at import. C# type== routing presence is a static inventory, not semantic correctness, all entrypoints, server support or completion percentage. Alternate event route explicitly identified; remaining routes need their own implementation/entrypoint evidence.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},routes=rows,defaultNoHandlerSubtypes=ignored)
    path=ROOT/'분석/Saver_Late/gamectl_packet_routes.json';path.write_bytes((json.dumps(result,ensure_ascii=False,indent=2)+'\n').encode());print(json.dumps({k:v for k,v in result.items()if k not in ['sources','routes','defaultNoHandlerSubtypes']},ensure_ascii=False))
if __name__=='__main__':main()
