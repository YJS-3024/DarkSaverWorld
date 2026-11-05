"""Original resetcharacter prefix followed by real feedback/death updates and draws."""
import struct
from native_feedback_renderer import Renderer
from native_ai_emulator import UC_HOOK_CODE,UC_X86_REG_ESP,UC_X86_REG_EIP

class NativeUnitReset(Renderer):
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and ('S_PlaySound' in name or 'StopSound' in name):
            self.ret();return  # Audio boundary only; no host playback.
        super()._dispatch(uc,address,size,data)
    def prepare(self):
        c=self.case;actor=c['unit'];base=self.RUNTIME+actor*0x2e0
        for unit in c['units']:
            at=self.RUNTIME+unit['id']*0x2e0
            for offset,key in [(0x18,'job'),(0x68,'job'),(0x28,'mp'),(0x2c,'maxMp')]:self.write(at+offset,unit[key])
            self.write(at+0x148,{0:3,1:4,2:2,3:1}[unit['direction']]);self.write(0x100e040c+unit['job']*16,5)
        operation=c['operation']
        if operation=='attack':
            self.uc.mem_write(self.DATA+0x6000,struct.pack('<hhh',c['target'],c['source'],1))
            self.invoke(self.exports['AttackChar'],[self.DATA+0x6000])
        elif operation in ('hp','mp','miss','hp-death'):
            if operation=='miss':self.invoke(self.exports['SetFail'],[actor])
            else:self.invoke(self.exports['SetHpDisplay' if operation!='mp' else 'SetMpDisplay'],[(actor&65535)|((c['value']&65535)<<16)])
        if operation in ('death','hp-death'):self.invoke(self.exports['dmKillChar2'],[actor])
        for _ in range(c['beforeTicks']):self.invoke(0x10010210,[actor])
        if c.get('directionBeforeReset')is not None:self.write(base+0x148,{0:3,1:4,2:2,3:1}[c['directionBeforeReset']])
        return base
    def reset(self):
        c=self.case;actor=c['unit'];old=next(u for u in c['units']if u['id']==actor);f=[0]*44
        for index,value in [(0,actor),(1,c['newJob']),(2,c['newJob']),(3,3),(6,0),(7,200),(8,c['newHp']),(9,65),(10,3),
                            (11,60),(12,20),(13,6),(16,50),(18,old['x']),(19,old['y']),(22,20),(23,7),(24,old.get('team',0 if actor<400 else 8)),(43,1)]:f[index]=value
        f[25:41]=[-1]*16
        self.uc.mem_write(self.DATA+0x8000,struct.pack('<44I',*[value&0xffffffff for value in f]))
        stop=0x1000b43f;handle=self.uc.hook_add(UC_HOOK_CODE,lambda uc,a,s,d:uc.emu_stop(),begin=stop,end=stop)
        try:
            sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<II',self.STOP,self.DATA+0x8000));self.uc.reg_write(UC_X86_REG_ESP,sp)
            self.uc.emu_start(0x1000b0a0,stop+1,timeout=5000000,count=30000)
            assert self.uc.reg_read(UC_X86_REG_EIP)==stop
        finally:self.uc.hook_del(handle)
        self.write(0x100e040c+c['newJob']*16,5);self.invoke(0x1000c950,[actor,0])
    def trace(self):
        c=self.case;base=self.prepare();self.reset();trace=[]
        for step in range(c['ticks']+1):
            flags=self.read(base+0x100);self.frames=[];self.invoke(0x1000fcd0,[c['unit']])
            trace.append(dict(numeric=None if not flags&64 else dict(counter=self.read(base+0x15c),delta=self.signed(self.read(base+0x160)),kind=self.signed(self.read(base+0x2ac)),hp=self.signed(self.read(base+0x20)),maxHp=self.signed(self.read(base+0x24))),
                attack=None if not flags&4 else dict(counter=self.read(base+0x158),partner=self.signed(self.read(base+0x280)),offsetX=self.signed(self.read(base+0x284)),offsetY=self.signed(self.read(base+0x288)),flag=self.signed(self.read(base+0x294)),direction={3:0,4:1,2:2,1:3}[self.read(base+0x148)]),
                death=None if not flags&128 else dict(counter=self.read(base+0x164),held=bool(flags&64)),frames=list(self.frames)))
            if step<c['ticks']:self.invoke(0x10010210,[c['unit']])
        self.close();return trace
