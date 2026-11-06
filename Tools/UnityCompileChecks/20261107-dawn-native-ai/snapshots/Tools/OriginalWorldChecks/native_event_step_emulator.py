"""Actual10F20 single-step execution, with explicit MFC/graphics/unit/transport boundaries."""
from native_scenario_end_emulator import NativeScenarioEnd
from native_ai_emulator import UC_X86_REG_ECX,UC_X86_REG_EAX
class NativeEventStep(NativeScenarioEnd):
    SCRIPT=0x0d030000
    def __init__(self):
        super().__init__();self.clock=1000;self.confirming=False;self.units=[];self.sent=[];self.events=[];self.trace=[];self.existing=[];self.profile=[];self.firstClear=True;self.mutations=[]
    def unit_address(self,unit):
        assert 0<=unit<32,unit
        return 0x0d0c0000+unit*0x300
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and ('??0CDDSurface@@'in name or '??1CDDSurface@@'in name):self.ret(uc.reg_read(UC_X86_REG_ECX))
        elif name and 'GetUnitInfoP@@'in name:
            unit=self.args(1)[0];self.units.append(unit);self.ret(self.unit_address(unit))
        elif address in [0x10010450,0x10010500]:
            self.mutations.append(['CHANGESPRITE'if address==0x10010450 else'CHANGETILE2',self.text(self.args(1)[0]).decode('cp949')]);self.ret(0,4)
        elif address==0x10010010:self.profile.append(['FIRSTCLEAR',self.firstClear]);self.ret(int(self.firstClear),4)
        elif address==0x10010250:self.profile.append(['SAVECLEAR']);self.ret(0,4)
        elif name=='#823' or name and name.startswith('??2@'):
            count=self.args(1)[0];assert 0<count<=2048
            self.ret(0x0d070000)
        elif name=='#825' or name and name.startswith('??3@'):self.ret()
        elif name=='atoi':self.ret(int(self.text(self.args(1)[0])or b'0'))
        elif address==0x1000e2c0:
            p,count=self.args(2);self.sent.append(list(uc.mem_read(p,count)));self.ret()
        elif address==0x10008d20:return # Execute the real linked-list clear function.
        else:super().hook(uc,address,size,data)
    def setup(self,lines,subject=0,mode=1,team=0,local_team=0,tutorial=0,moving=()):
        assert 0<len(lines)<399
        self.strings=0x0d080000;self.uc.mem_write(self.SCRIPT,bytes(2048));self.units=[];self.sent=[];self.events=[];self.trace=[];self.profile=[];self.mutations=[];self.clock=1000
        for unit in range(32):self.uc.mem_write(self.unit_address(unit),bytes(0x300))
        for unit in moving:self.write(self.unit_address(unit)+0x100,1)
        for offset,value in [(4,mode),(12,1),(0x14,subject),(0x18,team)]:self.write(self.SCRIPT+offset,value)
        for i,line in enumerate(lines+['']):self.write(self.SCRIPT+0x54+i*4,self.string(line.encode('cp949')))
        self.write(0x1006c0e8,self.string(b'26'));self.write(0x1006c118,tutorial);self.write(0x1006a738,local_team);self.write(0x1001c4d4,0);self.write(0x10093864,self.SCRIPT)
        self.write(0x100936d0,0);self.write(0x100936d4,0xffff)
        self.write(0x1006a708,0x0d020000);self.uc.mem_write(0x0d020000,bytes(2048))
    def snapshot(self):
        signed=lambda p:self.read(p)if self.read(p)<2**31 else self.read(p)-2**32
        return dict(mode=signed(self.SCRIPT+4),active=signed(self.SCRIPT+12),subject=signed(self.SCRIPT+0x14),team=signed(self.SCRIPT+0x18),
            waiting=signed(self.SCRIPT+0x20),waitValue=signed(self.SCRIPT+0x24),waitClock=signed(self.SCRIPT+0x28),line=signed(self.SCRIPT+0x34),
            execution=self.read(0x1001a50c),testResult=self.read(0x1001a508),profile=list(self.profile),mutations=list(self.mutations),unitReads=list(self.units),sent=list(self.sent),events=list(self.events),callbacks=list(self.trace))
    def step(self,clock=None):
        if clock is not None:self.clock=clock
        self.uc.reg_write(UC_X86_REG_ECX,self.SCRIPT);self.run(0x10010f20,[]);r=self.snapshot();r['returnValue']=self.uc.reg_read(UC_X86_REG_EAX);return r
