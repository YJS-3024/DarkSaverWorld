"""Original61 receipt, message acknowledgement and result confirm boundaries.
Pure analysis: the production scenario flow is not wired to61 by this emulator.
"""
from native_result_kills_emulator import NativeResultKills
from native_ai_emulator import UC_X86_REG_ECX
class NativeScenarioEnd(NativeResultKills):
    OBJECT=0x0d020000
    def __init__(self):super().__init__();self.events=[]
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name in ['SendMessageA','PostMessageA']:
            self.events.append([name,self.args(4)]);self.ret(0,16)
        elif name and 'setgamestatus@@'in name:
            self.events.append(['game-status',self.args(1)[0]]);self.ret()
        elif address==0x10008790:
            args=self.args(5);this=uc.reg_read(UC_X86_REG_ECX)
            self.events.append(['dialogue',args[:4],self.text(args[4]).decode('cp949')])
            self.write(this,1);self.write(this+4,1);self.ret(0,20)
        elif address==0x10008d20:
            # This clears linked visual nodes (08D20->091E0), leaving the tail sentinel; it does not drain the0D event FIFO.
            self.events.append(['visual-list-clear-boundary']);self.ret()
        else:super().hook(uc,address,size,data)
    def begin(self,flag,outcome,tutorial,winner='테스트'):
        previous_strings=getattr(self,'strings',0x0d080000)
        self.open()
        if winner is None:self.strings=previous_strings
        self.events=[];self.confirming=True;self.confirmActions=[];self.trace=[];self.existing=[]
        self.write(0x1006a708,self.OBJECT);self.uc.mem_write(self.OBJECT,bytes(512));self.write(0x1006a740,outcome)
        self.write(0x1006c118,tutorial)
        if winner is not None:self.write(0x1006a73c,self.string(winner.encode('cp949')))
        self.write(0x1001c4cc,0x13579)
        self.uc.mem_write(0x0d040000,bytes([0x5c,0x61,flag]));self.run(0x1000eb10,[0x0d040000]);return self.snapshot()
    def snapshot(self):
        return dict(events=list(self.events),actions=list(self.confirmActions),callbacks=list(self.trace),
            resultFlag=self.read(0x1001c434),noticeMode=self.read(self.OBJECT+0x18),noticeOutcome=self.read(0x1001c4cc),
            active=self.read(self.OBJECT),waiting=self.read(self.OBJECT+4),firstLine=self.read(self.OBJECT+0x1b0))
    def acknowledge(self,tutorial,mode,position=0,total=6):
        self.events=[];self.confirmActions=[];self.trace=[];self.write(0x1006c118,tutorial)
        for offset,value in [(0,1),(4,1),(0x18,1),(0x1ac,mode),(0x1b0,position),(0x1b4,total)]:self.write(self.OBJECT+offset,value)
        self.uc.reg_write(UC_X86_REG_ECX,self.OBJECT);self.run(0x10008ca0,[]);return self.snapshot()

    def receive_teams(self,packet,local_team):
        self.strings=0x0d080000;self.write(0x1006a738,local_team);self.write(0x1006a73c,self.string(b'previous'))
        self.uc.mem_write(0x0d040000,bytes(packet));self.run(0x1000eb10,[0x0d040000])
        return self.text(self.read(0x1006a73c)).decode('cp949')
