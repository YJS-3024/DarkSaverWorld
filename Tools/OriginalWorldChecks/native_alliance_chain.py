"""Actual GameCtl B2 -> by-value UserUnit SetAllyInfo with explicit stale locals.

Caller return PC and all 92 argument bytes cross separate original DLL VMs.
The uninitialised tail is controlled test input, never recovered server data.
"""
import struct
from native_scenario_end_emulator import NativeScenarioEnd
from check_native_alliance_metadata import Teams
from native_ai_emulator import UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP
class AllianceUser(Teams):
    def receive_by_value(self,return_pc,payload):
        assert len(payload)==92
        stack=self.STACK+0xff000
        self.uc.mem_write(stack-256,bytes([0xa5])*256)
        self.uc.mem_write(stack,struct.pack('<I',return_pc)+payload)
        self.uc.reg_write(UC_X86_REG_ESP,stack)
        def stop(uc,at,size,data):uc.emu_stop()
        handle=self.uc.hook_add(UC_HOOK_CODE,stop,begin=return_pc,end=return_pc)
        try:self.uc.emu_start(self.exports['SetAllyInfo'],return_pc+1,timeout=1000000,count=100000)
        finally:self.uc.hook_del(handle)
        assert self.uc.reg_read(UC_X86_REG_EIP)==return_pc
        assert self.uc.reg_read(UC_X86_REG_ESP)==stack+4
        return dict(localTeam=self.read(0x1003d540),lastLoopTeam=self.read(0x1003d548),relations=[self.read(0x1003d54c+i*4)for i in range(21)])
class AllianceGame(NativeScenarioEnd):
    def __init__(self,user):self.user=user;self.calls=[];self.tail=(20,0);super().__init__()
    def hook(self,uc,at,size,data):
        if at==0x1000d4c0 and self.tail is not None:
            # Local86 bytes start at entryESP-58. Receiver writes count/rows,
            # leaving subsequent pairs at this explicitly supplied pattern.
            stack=uc.reg_read(UC_X86_REG_ESP)
            uc.mem_write(stack-0x58,b'\x99\x99'+struct.pack('<hh',*self.tail)*21+b'\xa5\xa5')
        name=self.hooks.get(at,'')
        if 'SetAllyInfo@@'in name:
            stack=uc.reg_read(UC_X86_REG_ESP);pc=self.read(stack)
            assert pc==0x1000d540,'Preserve the real caller return bytes for count21 extra iteration'
            payload=bytes(uc.mem_read(stack+4,92))
            result=self.user.receive_by_value(pc,payload)
            self.calls.append(dict(returnPc=pc,argumentsHex=payload.hex(),result=result));self.ret();return
        super().hook(uc,at,size,data)
    def receive(self,rows,team,tail=(20,0),count=None):
        self.calls=[];self.tail=tail;count=len(rows)if count is None else count
        packet=b'\x5c\xb2'+struct.pack('<h',count)+b''.join(struct.pack('<hh',*r)for r in rows)
        self.write(0x1006a738,team);self.uc.mem_write(0x0d040000,packet)
        self.run(0x1000eb10,[0x0d040000]);assert len(self.calls)==1
        return self.calls[0]
