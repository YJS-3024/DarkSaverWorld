"""Bounded x86 execution of original UserUnit AI with deterministic map/clock/I/O hooks.

This executes calculation/control code in Unicorn, not a Windows game or Unity Play.
The original DLL is read-only; host calls, allocation and network sends are forbidden.
"""
import hashlib,json,struct,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT.parent/'.tools/ds-native-emulation-deps'),str(ROOT.parent/'.tools/ds-ghidra-python'),str(ROOT/'Tools/CombatApChecks/python-deps')]
import pefile
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_HOOK_MEM_INVALID
from unicorn.x86_const import UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP,UC_X86_REG_ECX

class NativeAi:
    BASE=0x10000000;STACK=0x0f000000;STOP=0x0e000000;STUB=0x0d000000;DATA=0x0c000000;RUNTIME=0x0c100000
    def __init__(self,case):
        inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
        row=next(f for p in inventory['payloads']for f in p['files']if f.get('sha256')=='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b')
        self.path=Path(row['extractedFile']);raw=self.path.read_bytes();assert hashlib.sha256(raw).hexdigest()==row['sha256']
        pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0,0x1000);self.uc.mem_map(self.BASE,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(self.BASE,pe.get_memory_mapped_image())
        for address,size in [(self.STACK,0x100000),(self.STOP,0x1000),(self.STUB,0x100000),(self.DATA,0x500000)]:self.uc.mem_map(address,size)
        self.uc.mem_write(self.STOP,b'\xf4');self.case=case;self.units={u['id']:u for u in case['units']};self.packets=[];self.random_calls=0
        self.exports={e.name.decode().split('@@')[0][1:]:self.BASE+e.address for e in pe.DIRECTORY_ENTRY_EXPORT.symbols if e.name}
        self.handles=[];self.hooks={};self._hook_add(UC_HOOK_CODE,self._dispatch,begin=self.STOP,end=self.STOP)
        for n,imp in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
            name=imp.name.decode()if imp.name else '#'+str(imp.ordinal);address=self.STUB+n*16
            self.uc.mem_write(imp.address,struct.pack('<I',address));self.hooks[address]=name;self.uc.mem_write(address,b'\xc3')
        self._hook_add(UC_HOOK_CODE,self._dispatch,begin=self.STUB,end=self.STUB+0xfffff)
        for address,name in [(self.exports['GetAttrTable3'],'attribute'),(self.exports['mainmapgetchar'],'occupant'),(self.exports['mainmapsetchar'],'set-occupant'),(self.exports['GetTeamID'],'team'),(0x10004ff0,'send-move'),(0x10005240,'send-attack'),(0x100051e0,'send-rest'),(0x1001189c,'empty-string-create'),(0x100118a2,'empty-string-destroy'),(0x100118fc,'free-null-path-chain'),(0x1000f810,'append-direction'),(self.exports['AIStart'],'ai-start')]:
            self.hooks[address]=name;self._hook_add(UC_HOOK_CODE,self._dispatch,begin=address,end=address)
        self.write(0x1010b3a0,self.RUNTIME);self.write(0x100e4298,case['width']);self.write(0x100e429c,case['height']);self.write(0x1010b3d8,1)
        self.occupied={}
        for unit in case['units']:
            base=self.RUNTIME+unit['id']*0x2e0
            fields={0:int(unit.get('nativeActive',True)),4:int(unit.get('nativeEnabled',True)),0x10:unit['id'],0x1c:unit.get('movementType',0),0x20:unit.get('hp',100),0x24:unit.get('maxHp',100),0x30:unit.get('attackRange',1),0x3c:unit.get('movementBudget',6),0x4c:unit.get('commandRange',3),0x5c:unit.get('maxAp',20),0x60:unit.get('ap',20),0x64:unit.get('team',0 if unit['id']<400 else 8),0x70:unit['x'],0x74:unit['y'],0x100:unit.get('nativeState',0),0x108:unit.get('troopOrder',2),0x1fc:case.get('hint',0)if unit['id']==case.get('member',1)else 0,0x298:0}
            for offset,value in fields.items():self.write(base+offset,value)
            self.occupied[(unit['x'],unit['y'])]=unit['id']
        self.callbacks=0
        self._hook_add(UC_HOOK_MEM_INVALID,self._invalid)
    def _hook_add(self,*args,**kwargs):
        self.handles.append(self.uc.hook_add(*args,**kwargs))
    def close(self):
        for handle in self.handles:self.uc.hook_del(handle)
        self.handles.clear()
    def _invalid(self,uc,access,address,size,value,data):
        self.invalid_access="Native unmapped access "+hex(address)+" PC="+hex(uc.reg_read(UC_X86_REG_EIP));return False
    def write(self,address,value):self.uc.mem_write(address,struct.pack('<I',value&0xffffffff))
    def read(self,address):return struct.unpack('<I',self.uc.mem_read(address,4))[0]
    def signed(self,value):return value if value<0x80000000 else value-0x100000000
    def args(self,n):return [self.signed(self.read(self.uc.reg_read(UC_X86_REG_ESP)+4+i*4))for i in range(n)]
    def ret(self,value=0,cleanup=0):
        sp=self.uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);self.uc.reg_write(UC_X86_REG_EAX,value&0xffffffff);self.uc.reg_write(UC_X86_REG_ESP,sp+4+cleanup);self.uc.reg_write(UC_X86_REG_EIP,pc)
    def _dispatch(self,uc,address,size,data):
        if address==self.STOP:uc.emu_stop();return
        name=self.hooks.get(address);self.callbacks+=1
        if name=='attribute':
            x,y=self.args(2);self.ret(self.case.get('attributes',[0]*(self.case['width']*self.case['height']))[y*self.case['width']+x]if 0<=x<self.case['width']and 0<=y<self.case['height']else -1)
        elif name and 'GetAttrTable2@@'in name:
            kind,attribute=self.args(2);ptr=self.DATA+0x10000+(kind*32+max(attribute,0))*16
            costs=self.case.get('terrain');cost=costs[attribute*30+kind*3+2]if costs is not None and 0<=attribute<27 else 1
            self.write(ptr+8,cost);self.ret(ptr)
        elif name=='occupant':x,y=self.args(2);self.ret(self.occupied.get((x,y),-1))
        elif name=='set-occupant':x,y,unit=self.args(3);self.occupied[(x,y)]=unit;self.ret()
        elif name=='team':unit=self.args(1)[0];self.ret(self.units[unit].get('team',0 if unit<400 else 8))
        elif name=='empty-string-create':
            target=uc.reg_read(UC_X86_REG_ECX);self.write(target,self.DATA+0x8000);self.ret(target)
        elif name=='empty-string-destroy':
            target=uc.reg_read(UC_X86_REG_ECX)
            if self.read(target)!=self.DATA+0x8000:raise RuntimeError('Unexpected nonempty native path string')
            self.ret()
        elif name=='free-null-path-chain':
            if uc.reg_read(UC_X86_REG_ECX)!=0:raise RuntimeError('Unexpected nonempty native path chain')
            self.ret()
        elif name=='append-direction':self.ret() # Presentation queue append; not read by this bounded backtracker.
        elif name=='ai-start':self.packets.append(dict(kind='ai-start',unit=self.args(1)[0]));self.ret()
        elif name=='rand':
            sequence=self.case.get('random',[0]);value=sequence[self.random_calls%len(sequence)];self.random_calls+=1;self.ret(value)
        elif name in ['clock','_clock']:self.ret(int(self.case.get('clock',0)))
        elif name=='send-move':
            ptr=self.args(1)[0];values=struct.unpack('<11h',self.uc.mem_read(ptr,22));self.packets.append(dict(kind='move',source=values[0],x=values[1],y=values[2],nativePoints=[list(values[i:i+2])for i in range(1,11,2)]));self.ret()
        elif name=='send-attack':
            ptr=self.args(1)[0];source,target,a,b=struct.unpack('<hhhh',self.uc.mem_read(ptr,8));self.packets.append(dict(kind='attack',source=source,target=target,sourceTerrain=a,targetTerrain=b));self.ret()
        elif name=='send-rest':self.packets.append(dict(kind='rest',source=self.args(1)[0]));self.ret()
        else:raise RuntimeError('Unimplemented native input boundary '+str(name)+' at '+hex(address))
    def run_tick(self,menu=None):
        member=self.case.get('member',1);sp=self.STACK+0xff000
        args=[]if menu is None else [(member&65535)|((menu[0]&65535)<<16),menu[1]]
        self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*args));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(self.exports['IncTick'if menu is None else'SetTickMenu'],self.STOP+1,timeout=5000000,count=5000000)
        if self.uc.reg_read(UC_X86_REG_EIP)!=self.STOP:raise RuntimeError('native tick instruction bound')
        result=dict(units=[dict(id=u['id'],ap=self.read(self.RUNTIME+u['id']*0x2e0+0x60),state=self.read(self.RUNTIME+u['id']*0x2e0+0x100),commandFlags=self.read(self.RUNTIME+u['id']*0x2e0+0x10c))for u in self.case['units']],packets=self.packets)
        self.close();return result
    def run(self):
        member=self.case.get('member',1);sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<III',self.STOP,member,0));self.uc.reg_write(UC_X86_REG_ESP,sp)
        try:self.uc.emu_start(0x100041f0,self.STOP+1,timeout=5000000,count=5000000)
        except Exception as error:raise RuntimeError(str(error)+" PC="+hex(self.uc.reg_read(UC_X86_REG_EIP))+" "+getattr(self,"invalid_access",""))from error
        if self.uc.reg_read(UC_X86_REG_EIP)!=self.STOP:raise RuntimeError('native instruction/time bound reached at '+hex(self.uc.reg_read(UC_X86_REG_EIP)))
        base=self.RUNTIME+member*0x2e0
        result=dict(packets=self.packets,state=self.read(base+0x100),hint=self.signed(self.read(base+0x1fc)),ap=self.read(base+0x60),randomCalls=self.random_calls)
        self.close();return result

if __name__=='__main__':
    case=dict(width=12,height=12,member=1,units=[dict(id=0,x=5,y=5,troopOrder=2),dict(id=1,x=4,y=5,nativeState=0x10),dict(id=400,x=4,y=4)])
    print(json.dumps(NativeAi(case).run()))
