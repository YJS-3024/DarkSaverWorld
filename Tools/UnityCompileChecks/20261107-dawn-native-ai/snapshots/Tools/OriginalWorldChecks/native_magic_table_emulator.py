"""Link real relocated setunit GetMagicTbl into native callers.

The320-byte newmagicrecord rows are supplied from repository CSV in the exact
column destinations of original1ED0 sscanf. File/heap/loader/pattern payloads are
boundaries; the lookup/cache/legacy projection execute actual x86.
"""
import csv,hashlib,struct
from pathlib import Path
from native_ai_emulator import pefile,UC_X86_REG_EIP,UC_X86_REG_ESP
ROOT=Path(__file__).resolve().parents[2]
class SetunitMagicTable:
    BASE=0x30000000;ROWS=BASE+0x2dbe0;LEGACY=BASE+0x5388;GET=BASE+0x2610;GET2=BASE+0x25d0
    def __init__(self,host):
        self.host=host;self.uc=host.uc
        raw=Path('/workspace/.tools/ds-ghidra/installed/saver/setunit.dll').read_bytes();self.sha256=hashlib.sha256(raw).hexdigest();assert self.sha256=='71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca'
        pe=pefile.PE(data=raw);pe.relocate_image(self.BASE);self.uc.mem_map(self.BASE,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(self.BASE,pe.get_memory_mapped_image())
        self.uc.mem_write(self.ROWS,bytes(1000*320))
        self.rows=list(csv.DictReader((ROOT/'Assets/_Original/Data/Tables/Magictbl.csv').open(encoding='utf-8-sig')))
        columns=['속성','거리','범위','MP','시간','팀','유닛','EA','EB','EC','TICK','LVPT','패턴','iconx','icony']
        for i,row in enumerate(self.rows):
            record=self.ROWS+i*320;self.write(record,int(row['번호']));name=row['이름'].encode('cp949');assert len(name)<256;self.uc.mem_write(record+4,name+b'\0')
            for j,column in enumerate(columns):self.write(record+0x104+j*4,int(row[column]))
        # Actual file loader is outside this bounded table lookup test. It would
        # set the loaded flag after its archive/stdio work; provide that flag.
        self.write(self.BASE+0x269850,1)
    def write(self,at,value):self.uc.mem_write(at,struct.pack('<I',value&0xffffffff))
    def redirect(self):self.uc.reg_write(UC_X86_REG_EIP,self.GET)
    def call(self,id,new=False):
        sp=self.host.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<II',self.host.STOP,id&0xffffffff));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(self.GET2 if new else self.GET,self.host.STOP+1,timeout=1000000,count=100000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.host.STOP
        from native_ai_emulator import UC_X86_REG_EAX
        pointer=self.uc.reg_read(UC_X86_REG_EAX)
        return dict(pointer=pointer,legacy=list(struct.unpack('<6i',self.uc.mem_read(self.LEGACY,24))),attribute=struct.unpack('<i',self.uc.mem_read(self.LEGACY+0x170,4))[0],cacheKey=struct.unpack('<i',self.uc.mem_read(self.BASE+0x5020,4))[0])

from check_native_magic_packets import NativeMagicClientOutgoing,NativeMagicOutgoing
from native_ai_emulator import NativeAi
class LinkedGameMagic(NativeMagicClientOutgoing):
    def __init__(self):super().__init__();self.nativeTable=SetunitMagicTable(self)
    def dispatch(self,uc,address,size,data):
        if 'GetMagicTbl@@'in self.hooks.get(address,''):self.nativeTable.redirect();return
        super().dispatch(uc,address,size,data)
class LinkedUserMagic(NativeMagicOutgoing):
    def __init__(self,case):super().__init__(case);self.nativeTable=SetunitMagicTable(self)
    def _dispatch(self,uc,address,size,data):
        if 'GetMagicTbl@@'in self.hooks.get(address,''):self.nativeTable.redirect();return
        super()._dispatch(uc,address,size,data)
    def close(self):pass # Keep the lookup cache across the explicitly controlled sequence.
    def dispose(self):NativeAi.close(self)
