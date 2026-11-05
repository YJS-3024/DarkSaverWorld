"""Original31 receiver and result kill-list draw progression; no inferred deaths."""
import struct
from native_result_emulator import NativeResult
from native_ai_emulator import UC_X86_REG_ECX

class NativeResultKills(NativeResult):
    def __init__(self,pictures=None):
        super().__init__();self.pictures=pictures or {};self.labels=[];self.draws=[];self.confirming=False
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if self.confirming and address==0x100147bc:
            this=uc.reg_read(UC_X86_REG_ECX);self.write(this,self.string(self.text(self.args(1)[0])));self.ret(this,4)
        elif self.confirming and address==0x1000fab0:
            self.confirmActions.append(['script-boundary',self.text(self.args(1)[0]).decode('cp949')]);self.ret(0,4)
        elif self.confirming and address==0x1000e750:self.confirmActions.append(['cleanup-boundary']);self.ret()
        elif self.confirming and name=='PostMessageA':self.confirmActions.append(['post']+self.args(4));self.ret(1,16)
        elif name and 'GetCharAbility@@'in name:
            job=self.args(1)[0];p=0x0d010000+(job%1000)*16;self.write(p+4,self.pictures.get(job,job));self.ret(p)
        elif name and 'GetCharSurface@@'in name:
            picture=self.args(1)[0];self.ret(0 if picture==0xffffffff else 0x0d100000+picture)
        elif name and 'GetFxSurface@@'in name:self.ret(0x0d070300)
        elif name and 'BltFast@CDDSurface@@'in name and self.args(5)[2]==0x0d070300:
            x,y,surface,p,flags=self.args(5);l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16))
            self.draws.append(dict(kind='effects',id=0,x=x,y=y,sourceX=l,sourceY=t,width=r-l,height=b-t));self.ret(1,20)
        else:super().hook(uc,address,size,data)
    def open(self):
        self.strings=0x0d080000
        for p,v in [(0x1001a8dc,1),(0x1001c434,1),(0x1001c430,0),(0x1001c450,0x0d000020),
                    (0x1006ad4c,0),(0x1001c4fc,0),(0x1001c438,0),(0x1001a1dc,0),(0x1006a890,0)]:self.write(p,v)
    def receive_jobs(self,jobs):
        packet=b'\x5c\x31'+struct.pack('<h',len(jobs))+struct.pack('<'+'h'*len(jobs),*jobs)
        self.uc.mem_write(0x0d040000,packet);self.run(0x1000eb10,[0x0d040000])
        count=struct.unpack('<i',self.uc.mem_read(0x1006ad4c,4))[0]
        return list(struct.unpack('<'+'i'*max(0,count),self.uc.mem_read(0x1006ad50,max(0,count)*4)))
    def frame(self):
        self.strings=0x0d080000;self.labels=[];self.draws=[];self.formats=[];self.bodies=[];self.blits=[]
        self.run(0x10007350,[0,0])
        return dict(remaining=self.read(0x1006ad4c),page=self.read(0x1001c438),counter=self.read(0x1001c4fc),
                    icons=list(self.draws),texts=list(self.labels))
    def confirm(self,outcome,count,tutorial,visible,control=0):
        self.confirming=True;self.confirmActions=[];self.trace=[];self.existing=[]
        self.write(0x1006c118,tutorial);self.write(0x1001c434,visible);self.write(0x1006a740,outcome);self.write(0x1006ad4c,count)
        try:self.run(0x10004a80,[8,control])
        finally:self.confirming=False
        return dict(callbacks=list(self.trace),actions=list(self.confirmActions))
