"""Original DlgButtonAction28 and5C flag; network/window requests are captured."""
import struct
from native_battle_options_emulator import BattleOptionsNative
class NativeRetreatConfirmation(BattleOptionsNative):
    def hook(self,uc,address,size,data):
        if address==0x1000e2c0:
            pointer,length=self.args(2)
            self.trace.append(['send',bytes(uc.mem_read(pointer,length)).hex()]);self.ret();return
        if self.hooks.get(address)=='atoi':
            pointer=self.args(1)[0];raw=bytearray()
            while True:
                value=bytes(uc.mem_read(pointer,1));pointer+=1
                if value==b'\0':break
                raw.extend(value)
            # F040 has already enforced a whole nonempty ASCII digit string.
            assert raw and all(48<=c<=57 for c in raw)
            self.ret(int(raw));return
        super().hook(uc,address,size,data)
    def confirm(self,scenario,flag,control,tutorial=False):
        self.trace=[];self.existing=[];self.active=[False]*4;self.enabled=[False]*4
        self.write(0x1006c118,int(tutorial));raw=scenario.encode('ascii')+b'\0'
        self.uc.mem_write(0x0d050000,raw);self.write(0x1006c0e8,0x0d050000)
        self.uc.mem_write(0x0d060000,bytes([flag]));self.run(0x1000dfa0,[0x0d060000])
        assert self.read(0x1006c084)==flag
        self.run(0x10004a80,[28,control&0xffffffff]);return list(self.trace)
def controls():
    native=NativeRetreatConfirmation();cases=[]
    names=['','01','101','26','998','999','1000','01a','-1',' 01','000','26\0tail']
    for name in names:
        for flag in range(256):
            for button in [0,1]:
                actual=native.confirm(name,flag,button)
                expected=[['send','5c61'],[0x4cb,28,0,0]]if button==0 else[[0x4cb,28,0,0]]
                cstring=name.split('\0')[0]
                value=int(cstring)if cstring and all('0'<=c<='9'for c in cstring)else-1
                if button==0 and value<999 and flag==1:expected +=[[0x4ca,8,0,0],[0x4d0,8,110,10]]
                assert actual==expected,(name,flag,button,actual,expected)
                cases.append(dict(scenario=name,flag=flag,button=button,trace=actual))
    for tutorial in [False,True]:
        for control in [2,3,255,-1]:
            actual=native.confirm('01',1,control,tutorial);assert actual==[]
            cases.append(dict(scenario='01',flag=1,button=control,tutorial=tutorial,trace=actual))
    return cases
