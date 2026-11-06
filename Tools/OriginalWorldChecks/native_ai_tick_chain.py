"""Original tick/menu receipt forwards into persistent original AI queue VM."""
from native_ai_frame_chain import NativeAiQueueGame
class NativeAiTickGame(NativeAiQueueGame):
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address,'')
        if 'IncTick@@'in name:
            self.imports.append(['IncTick']);self.user.call(self.user.exports['IncTick']);self.ret();return
        if 'SetTickMenu@@'in name:
            arguments=self.args(2);self.imports.append(['SetTickMenu',*arguments]);self.user.call(self.user.exports['SetTickMenu'],arguments);self.ret();return
        super().hook(uc,address,size,data)
    def packet(self,packet):
        self.trace=[];self.existing=[];self.uc.mem_write(0x0d040000,packet);self.run(0x1000eb10,[0x0d040000])
