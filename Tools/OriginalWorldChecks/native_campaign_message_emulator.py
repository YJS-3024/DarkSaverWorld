"""Actual GameCtl109E0/10810 aliases with actual supplied MFC/CRT Format.
Dialogue window entrypoints, CString helpers and read-only roster are boundaries.
"""
import re
from native_event_step_emulator import NativeEventStep
from native_ai_emulator import UC_X86_REG_ECX
from native_mfc_format_emulator import MfcFormat
class NativeCampaignMessage(NativeEventStep):
    def __init__(self):
        super().__init__();self.hooks[0x10014840]='alias-find';self.hooks[0x100147e6]='alias-mid';self.formatter=MfcFormat();self.formats=[];self.outputs=[]
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address,'');this=uc.reg_read(UC_X86_REG_ECX)
        if name=='atoi':
            match=re.match(rb'\s*([+-]?\d+)',self.text(self.args(1)[0]));self.ret(int(match[1])if match else 0)
        elif name=='alias-find':self.ret(self.text(self.read(this)).find(self.text(self.args(1)[0])),4)
        elif name=='alias-mid':
            out,start,count=self.args(3);self.write(out,self.string(self.text(self.read(this))[start:start+count]));self.ret(out,12)
        elif name=='status-format':
            args=self.args(8);out,fmt=args[:2];pattern=self.text(fmt);values=[];i=2
            for token in re.findall(rb'%lu|%s|%S|%d|%%',pattern):
                if token==b'%%':continue
                v=args[i];i+=1;values.append(self.text(v)if token in [b'%s',b'%S']else v)
            result=self.formatter.format(pattern,values);self.formats.append(dict(pattern=pattern.decode('cp949'),result=result.decode('cp949')));self.write(out,self.string(result));self.ret()
        elif address==0x10008790:
            args=self.args(5);self.outputs.append(dict(mode=0,face=0,rect=args[:4],text=self.text(args[4]).decode('cp949')));self.ret(0,20)
        elif address==0x100089f0:
            mode,face,text=self.args(3);self.outputs.append(dict(mode=mode,face=face,text=self.text(text).decode('cp949')));self.ret(0,12)
        else:super().hook(uc,address,size,data)
    def message(self,source,names,jobs,own_name,subject=0):
        self.setup(['END'],subject=subject);self.formats=[];self.outputs=[];self.write(0x1006a73c,self.string(own_name.encode('cp949')))
        for i in range(10):
            self.uc.mem_write(0x1001aa8e+i*412,jobs[i].to_bytes(2,'little'));self.uc.mem_write(0x1001ab24+i*412,names[i].encode('cp949')+b'\0')
        self.run(0x100109e0,[self.string(source.encode('cp949'))]);return list(self.outputs)
