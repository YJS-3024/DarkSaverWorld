"""Actual supplied MFC CString Left/Mid byte-count decisions for message aliases.

CString allocation/copy and atomic reference counters are explicit boundaries;
the original Left/Mid count, clamping and copy-offset instructions execute.
"""
import hashlib
import json
import struct
from pathlib import Path
from native_mfc_format_emulator import MfcFormat,UC_X86_REG_ESP,UC_X86_REG_EIP,UC_X86_REG_ECX

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).parent


class Substrings(MfcFormat):
    def __init__(self):
        super().__init__()
        self.heap = 0x0c030000
        self.copies = []

    def string(self,out,raw):
        pointer = self.heap
        self.heap += ((len(raw)+43)//16)*16
        assert self.heap < 0x0c0c0000
        self.uc.mem_write(pointer,struct.pack('<iii',1,len(raw),len(raw))+raw+b'\0')
        self.uc.mem_write(out,struct.pack('<I',pointer+12))

    def value(self,obj):
        pointer = struct.unpack('<I',self.uc.mem_read(obj,4))[0]
        size = struct.unpack('<i',self.uc.mem_read(pointer-8,4))[0]
        assert 0 <= size < 2048
        return bytes(self.uc.mem_read(pointer,size))

    def hook(self,uc,address,size,data):
        name = self.hooks.get(address)
        if address == 0x6c0f198e: # Empty CString construction, no DLL startup.
            obj = uc.reg_read(UC_X86_REG_ECX)
            self.string(obj,b'')
            self.ret(obj)
        elif address == 0x6c14bbaa: # AllocCopy destination/count/byte-offset/extra.
            out,count,start,extra = self.args(4)
            assert extra == 0
            raw = self.value(uc.reg_read(UC_X86_REG_ECX))
            self.copies.append([count,start])
            self.string(out,raw[start:start+count])
            self.ret(0,16)
        elif name in ['InterlockedIncrement','InterlockedDecrement']:
            pointer = self.args(1)[0]
            value = struct.unpack('<i',uc.mem_read(pointer,4))[0]
            value += 1 if name.endswith('Increment') else -1
            uc.mem_write(pointer,struct.pack('<i',value))
            self.ret(value,4)
        else:
            super().hook(uc,address,size,data)

    def substring(self,raw,kind,*args):
        self.string(0x0c020000,raw)
        stack = 0x0f0ff000
        self.uc.mem_write(stack,struct.pack('<'+'I'*(2+len(args)),
                           self.stop,0x0c020010,*[v&0xffffffff for v in args]))
        self.uc.reg_write(UC_X86_REG_ESP,stack)
        self.uc.reg_write(UC_X86_REG_ECX,0x0c020000)
        self.copies = []
        # GameCtl's imported ordinal4129 Left and ordinal4278 Mid.
        self.uc.emu_start(0x6c144132 if kind=='left' else 0x6c14400c,
                         self.stop+1,timeout=1000000,count=100000)
        assert self.uc.reg_read(UC_X86_REG_EIP) == self.stop
        return self.value(0x0c020010),self.copies


def main():
    native = Substrings()
    cases = []
    for text in ['', 'COM','COMX','COM가','COM10','가ME','가ID','ME가','ID가','A가ME','가나다']:
        raw = text.encode('cp949')
        for count in [-1,0,1,2,3,4,20]:
            result,copies = native.substring(raw,'left',count)
            size = max(0,min(count,len(raw)))
            expected_copies = [] if count >= len(raw) else [[size,0]]
            # Negative count clamps to0 before comparing with an empty input.
            if not raw: expected_copies = []
            assert result == raw[:size] and copies == expected_copies,(text,count,result,copies)
            cases.append(dict(text=text,operation='Left',args=[count],
                              resultHex=result.hex(),copyArguments=copies))
        for start,count in [(3,1),(-1,1),(0,-1),(0,20),(20,1),(1,3)]:
            result,copies = native.substring(raw,'mid',start,count)
            offset = max(0,start)
            size = max(0,min(max(0,count),len(raw)-offset))
            expected_copies = [] if offset==0 and size==len(raw) else [[size,offset]]
            assert result == raw[offset:offset+size] and copies == expected_copies,(text,start,count,result,copies)
            cases.append(dict(text=text,operation='Mid',args=[start,count],
                              resultHex=result.hex(),copyArguments=copies))
    sources = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
               for p in [Path(__file__),OUT/'native_mfc_format_emulator.py',
                         OUT/'native_ai_emulator.py',ROOT/'분석/InstallerVersions/installer_inventory.json']}
    result = dict(nativeSubstringCases=len(cases),mismatches=0,unityPlay=False,
                  additionalNativeLibraries=native.sources,sources=sources,cases=cases,
                  scope='Actual exact supplied MFC42 Left4129/Mid4278 count/clamp/offset paths. '
                        'Mixed CP949/ASCII aliases prove three bytes and one suffix byte, '
                        'including split multibyte characters. AllocCopy/empty construction '
                        'and atomic refcounts are supplied boundaries; no DLL startup, '
                        'real allocator, OS locale initialization or rendered text claim.')
    (OUT/'native-message-substring-check.json').write_text(
        json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items()if k not in ['sources','cases']},ensure_ascii=False))


if __name__ == '__main__':
    main()
