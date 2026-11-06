"""Actual1ED0 argument preparation+original CRT sscanf over all original lines.
Archive/stdio and outer file loop remain boundaries. No host numeric parser writes
record fields: original machine code computes all destinations and parses values.
"""
import csv,hashlib,json,struct
from pathlib import Path
from native_mfc_format_emulator import MfcFormat
from native_ai_emulator import pefile,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP
from unicorn.x86_const import UC_X86_REG_ESI
from native_magic_table_emulator import SetunitMagicTable
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class NativeMagicTableParser(MfcFormat):
    START=0x10001fe6;AFTER=0x1000203c;SCRATCH=0x0c050000;INPUT=0x0c009000;STACK=0x0f0ff000
    def __init__(self):
        super().__init__();raw=Path('/workspace/.tools/ds-ghidra/installed/saver/setunit.dll').read_bytes()
        self.setunitSha=hashlib.sha256(raw).hexdigest();assert self.setunitSha=='71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca'
        p=pefile.PE(data=raw);self.uc.mem_map(0x10000000,(p.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(0x10000000,p.get_memory_mapped_image());self.uc.mem_write(0x1000408c,struct.pack('<I',self.exports['sscanf']))
        self.manual=SetunitMagicTable(self)
    def parse(self,line):
        self.stop=self.AFTER;assert len(line)<2048
        self.uc.mem_write(self.INPUT,line+b'\0');self.uc.mem_write(self.SCRATCH-16,b'\xa5'*16+bytes(320)+b'\xa5'*16)
        self.uc.reg_write(UC_X86_REG_EAX,self.INPUT);self.uc.reg_write(UC_X86_REG_ESI,self.SCRATCH+0x138);self.uc.reg_write(UC_X86_REG_ESP,self.STACK)
        self.uc.emu_start(self.START,self.AFTER+1,timeout=1000000,count=1000000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.AFTER and self.uc.reg_read(UC_X86_REG_ESP)==self.STACK
        assert self.uc.reg_read(UC_X86_REG_EAX)==17
        assert bytes(self.uc.mem_read(self.SCRATCH-16,16))==b'\xa5'*16 and bytes(self.uc.mem_read(self.SCRATCH+320,16))==b'\xa5'*16
        return bytes(self.uc.mem_read(self.SCRATCH,320))
    def lookup(self,id):
        self.stop=0x0c0ff000;self.uc.mem_write(0x10269850,struct.pack('<I',1));self.uc.mem_write(self.STACK,struct.pack('<II',self.stop,id));self.uc.reg_write(UC_X86_REG_ESP,self.STACK)
        self.uc.emu_start(0x10002610,self.stop+1,timeout=1000000,count=100000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.stop and self.uc.reg_read(UC_X86_REG_ESP)==self.STACK+4
        return list(struct.unpack('<6i',self.uc.mem_read(0x10005388,24)))+[struct.unpack('<i',self.uc.mem_read(0x100054f8,4))[0]]
def main():
    original=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/magictbl.atr.txt';csvfile=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv'
    lines=original.read_text().splitlines();assert lines[0].startswith(';번호 ')
    lines=lines[1:];rows=list(csv.DictReader(csvfile.open(encoding='utf-8-sig')));assert len(lines)==len(rows)==440
    n=NativeMagicTableParser();records=[];columns=['속성','거리','범위','MP','시간','팀','유닛','EA','EB','EC','TICK','LVPT','패턴','iconx','icony']
    for i,(line,row)in enumerate(zip(lines,rows)):
        record=n.parse(line.encode('cp949'))
        assert struct.unpack('<i',record[:4])[0]==int(row['번호'])and record[4:260].split(b'\0')[0]==row['이름'].encode('cp949'),i
        assert list(struct.unpack('<15i',record[260:]))==[int(row[c])for c in columns],i
        # Independently validate the byte layout supplied by all linked-table
        # oracles against the native caller/CRT result, not another host mapper.
        assert record==bytes(n.uc.mem_read(n.manual.ROWS+i*320,320)),('Manual lookup input layout differs from original parser',i)
        n.uc.mem_write(0x1002dbe0+i*320,record);records.append(hashlib.sha256(record).hexdigest())
    for row in rows:
        assert n.lookup(int(row['번호']))==[int(row[c])for c in ['번호','MP','거리','범위','팀','유닛','속성']],row['번호']
    paths=[Path(__file__),original,csvfile,OUT/'native_mfc_format_emulator.py',OUT/'native_magic_table_emulator.py',OUT/'native_ai_emulator.py',ROOT/'분석/Ghidra/Saver_Late/MagicTableProjection/10001ed0.c',ROOT/'분석/Ghidra/Saver_Late/MagicTableProjection/10002610.c']
    report=dict(originalLines=len(lines),nativeConversions=len(lines)*17,nativeLegacyProjections=len(rows),byteIdenticalOracleInputRows=len(rows),recordBytes=320,canaryControls=len(rows)*2,mismatches=0,unityPlay=False,originalSetunitSha256=n.setunitSha,originalRuntimeHashes=n.sources,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},recordSha256=records,callerCodeSha256=hashlib.sha256(bytes(n.uc.mem_read(n.START,n.AFTER-n.START))).hexdigest(),callerRange='10001FE6..1000203B',
      scope='Original1ED0 caller argument construction and original MSVCRT sscanf run on all440 decoded original magictbl.atr lines in supplied C-locale image state. Original format string and destination computations parse all17 columns; source names/numeric CSV and320-byte inputs of other table oracles match exactly. Parsed bytes copied to original row array and actual GetMagicTbl projects7 relevant fields for all440 rows. Scratch stack/input/destination, zero padding, record handoff, TLS/locks/heap are controlled; archive/CStdioFile/outer file loop, pattern payload and DLL/OS initialization are not executed. MFC image mapped by reused host but no MFC formatting claim here. No Windows/Unity/server result.')
    (OUT/'native-magic-table-parser-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','recordSha256']},ensure_ascii=False))
if __name__=='__main__':main()
