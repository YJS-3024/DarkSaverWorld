"""Actual GameCtl5C60 team-name decoder and status-header callback; no pixels."""
import struct
from native_battle_status_emulator import BattleStatusNative

class NativeBattleTeams(BattleStatusNative):
    def __init__(self):
        self.capture_labels=False;self.unit_team=0;self.labels=[]
        super().__init__()
    def write(self,address,value):
        if self.capture_labels and address==0x1001c450:value=0x0d000020
        if self.capture_labels and address==self.record+100:value=self.unit_team
        super().write(address,value)
    def hook(self,uc,address,size,data):
        if address==0x0d000020 and self.capture_labels:
            args=self.args(6);self.labels.append(self.text(args[3]));self.ret()
        else:super().hook(uc,address,size,data)
    def receive(self,packet,local_team=-99):
        self.strings=0x0d080000;self.write(0x1006a738,local_team);self.write(0x1006a73c,self.string(b'previous'))
        self.uc.mem_write(0x0d040000,bytes(packet));self.run(0x1000eb10,[0x0d040000])
        count=struct.unpack('<h',self.uc.mem_read(0x1006bd10,2))[0]
        rows=[]
        for index in range(count):
            pointer=0x1006bd12+index*42;team=struct.unpack('<h',self.uc.mem_read(pointer,2))[0]
            rows.append(dict(team=team,name=self.text(pointer+2).hex().upper()))
        return dict(count=count,rows=rows,localName=self.text(self.read(0x1006a73c)).hex().upper())
    def header(self,team):
        self.capture_labels=True;self.unit_team=team;self.labels=[]
        try:
            self.draw(1,17,200,3,45,7,20,123,1000)
            assert len(self.labels)==3
            return self.labels[0].hex().upper()
        finally:self.capture_labels=False
