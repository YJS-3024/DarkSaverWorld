"""Actual GameCtl13D60 EVT loader; extracted file/CString storage are boundaries."""
import re
import struct
from native_event_step_emulator import NativeEventStep
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_ECX


class NativeEventLoader(NativeEventStep):
    def hook(self,uc,address,size,data):
        name = self.hooks.get(address)
        if name == 'brief-empty' and hasattr(self,'nil'):
            obj = uc.reg_read(UC_X86_REG_ECX)
            self.write(obj,self.nil)
            self.ret(obj)
        elif address == 0x10014864: # CString + CString, used in the archive path.
            out,left,right = self.args(3)
            self.write(out,self.string(self.text(self.read(left))+self.text(self.read(right))))
            self.ret(out,12)
        elif name == 'atoi':
            match = re.match(rb'\s*([+-]?\d+)',self.text(self.args(1)[0]))
            self.ret(int(match[1]) if match else 0)
        else:
            super().hook(uc,address,size,data)

    def load(self,source,key='01',archive=True,extracted=True,opened=True):
        self.setup(['END'])
        self.nil = self.string(b'')
        # DLL global CString constructors are not executed by this oracle.
        # Supply their empty storage, including record0 for stray END lines;
        # preserve active flags so the actual loader must clear them itself.
        empty_slots=struct.pack('<I',self.nil)*399
        for number in range(100):
            self.uc.mem_write(0x1006c130+number*0x64c,empty_slots)
        self.lines = source.splitlines()
        self.row = 0
        self.paths = []
        self.extracted = extracted
        self.opened = opened
        self.write(0x1001a8b0,0x0d060000 if archive else 0)
        self.write(0x1001a8b4,self.string(b'root\\'))
        self.write(0x1006c0e8,self.string(key.encode('ascii')))
        stack = 0x0f0ff000
        self.uc.mem_write(stack,struct.pack('<I',self.stop))
        self.uc.reg_write(UC_X86_REG_ESP,stack)
        self.uc.emu_start(0x10013d60,self.stop+1,timeout=10000000,count=10000000)
        assert self.uc.reg_read(UC_X86_REG_EIP) == self.stop
        events = {}
        for number in range(100):
            record = 0x1006c120+number*0x64c
            if not self.read(record):
                continue
            body = []
            for slot in range(399):
                pointer = self.read(record+16+slot*4)
                if not pointer:
                    break
                command = self.text(pointer).decode('cp949')
                # Empty padded strings terminate interpretation too. END fills
                # the remaining cells; only its first occurrence is observable.
                if not command:
                    break
                body.append(command)
                if command == 'END':
                    break
            events[number] = body
        return events
