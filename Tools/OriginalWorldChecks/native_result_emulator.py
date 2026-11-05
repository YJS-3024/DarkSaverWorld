"""Original5C54 destination table and result-window draw; MFC/graphics are boundaries."""
import struct
from native_battle_status_emulator import BattleStatusNative
class NativeResult(BattleStatusNative):
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if address==0x0d000020:
            surface,x,y,p,color,font=self.args(6);self.labels.append(dict(x=x if x<2**31 else x-2**32,y=y if y<2**31 else y-2**32,text=self.text(p).decode('cp949'),color=color));self.ret()
        elif name and 'GetCharAbility@@'in name:
            job=self.args(1)[0];p=0x0d010000+(job%1000)*16;self.write(p+4,job);self.ret(p)
        elif name and 'GetCharSurface@@'in name:self.ret(0x0d100000+self.args(1)[0])
        elif name and ('GetItemAttr@@'in name or'GetItemTable@@'in name):
            item=self.args(1)[0];p=0x0d020000+(item%1000)*16;self.write(p+4,item);self.ret(p)
        elif name and ('GetItemSurface@@'in name or'GetItemImage@@'in name):self.ret(0x0d070200)
        elif name and 'BltFast@CDDSurface@@'in name:
            x,y,surface,p,flags=self.args(5);l,t,r,b=struct.unpack('<4i',uc.mem_read(p,16));kind='marker'if surface==0x0d070100 else'item'if surface==0x0d070200 else'body'
            self.draws.append(dict(kind=kind,id=surface-0x0d100000 if kind=='body'else 0,x=x if x<2**31 else x-2**32,y=y if y<2**31 else y-2**32,sourceX=l,sourceY=t,width=r-l,height=b-t));self.ret(1,20)
        else:super().hook(uc,address,size,data)
    def receive(self,packet):
        self.strings=0x0d080000;self.uc.mem_write(0x0d040000,bytes(packet));self.run(0x1000eb10,[0x0d040000])
        count=struct.unpack('<h',self.uc.mem_read(0x1006a74c,2))[0];rows=[]
        for i in range(count):
            row=bytes(self.uc.mem_read(0x1006a750+i*32,32));rows.append(dict(words=list(struct.unpack('<14h',row[:28])),experience=struct.unpack('<I',row[28:])[0]))
        return dict(outcome=struct.unpack('<h',self.uc.mem_read(0x1006a740,2))[0],eventTotal=struct.unpack('<h',self.uc.mem_read(0x1006a742,2))[0],eventCompleted=struct.unpack('<h',self.uc.mem_read(0x1006a744,2))[0],spoils=self.read(0x1006a748),rows=rows)
    def draw(self):
        self.strings=0x0d080000;self.labels=[];self.draws=[];self.formats=[];self.bodies=[];self.blits=[]
        for p,v in [(0x1001a8dc,1),(0x1001c434,1),(0x1001c430,1),(0x1001c450,0x0d000020),(0x1001c480,0x0d070100),(0x1006ad4c,0),(0x1001c4fc,0),(0x1001c438,0),(0x1001a1dc,0)]:self.write(p,v)
        self.run(0x10007350,[0,0]);return dict(icons=self.draws,texts=[r for r in self.labels if r['y']==274 or r['x']==205])
