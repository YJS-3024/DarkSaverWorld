"""Original auxiliary battle receivers; source analysis, not new runtime routes."""
import hashlib,json,struct
from pathlib import Path
from native_scenario_end_emulator import NativeScenarioEnd
from native_ai_emulator import UC_X86_REG_ECX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class Auxiliary(NativeScenarioEnd):
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and ('setmonsterai@@'in name or'AIStart@@'in name):
            value=self.args(1)[0];self.calls.append(['setmonsterai'if'setmonsterai@@'in name else'AIStart',value-2**32 if value>=2**31 else value]);self.ret()
        elif name and ('??2@'in name or name=='#823'):self.ret(0x0d060000)
        elif name and ('??3@'in name or name=='#825'):self.ret()
        elif name=='#859':
            this=uc.reg_read(UC_X86_REG_ECX);self.write(this,self.string(bytes([self.args(1)[0]&255])));self.ret(this,4)
        else:super().hook(uc,address,size,data)
    def receive(self,packet):
        self.events=[];self.calls=[];self.confirmActions=[];self.trace=[];self.existing=[];self.confirming=True
        self.uc.mem_write(0x0d040000,packet);self.run(0x1000eb10,[0x0d040000]);return dict(calls=self.calls,events=self.events)
def main():
    n=Auxiliary();n.open();n.write(0x1006a708,n.OBJECT);n.uc.mem_write(n.OBJECT,bytes(512));ai=[];scalars=[];tables=[];notices=[];suffixes=[]
    for unit in [-32768,-2,-1,0,1,4,400,2999,32767]:
        r=n.receive(b'\x5c\x0c'+struct.pack('<h',unit));assert r['calls']==[['setmonsterai',unit],['AIStart',unit]];ai.append(dict(unit=unit,native=r))
    for value in [-32768,-1,0,1,20,32767]:
        n.write(0x1001a4b8,0xa5a5a5a5);n.receive(b'\x5c\x40'+struct.pack('<h',value));actual=struct.unpack('<i',n.uc.mem_read(0x1001a4b8,4))[0];assert actual==value;scalars.append(dict(opcode='5C40',value=value,native=actual))
    for value in [0,1,2,127,128,255]:
        n.write(0x1006c084,0xa5a5a5a5);n.receive(bytes([0x5c,0x5c,value]));actual=n.read(0x1006c084);assert actual==value;scalars.append(dict(opcode='5C5C',value=value,native=actual))
    samples=[[],[(0,401,1)],[(4,405,0),(10,411,1),(49,415,2)],[(0,401,1),(50,405,0),(100,411,1)],[(i,401+i%30,i%4)for i in range(100)],[(2999,799,255)]]
    for rows in samples:
        original=[(1000+i,0x7f00+i,0x8f00+i)for i in range(100)];expected=list(original)
        raw=b''.join(struct.pack('<iii',*r)for r in original);n.uc.mem_write(0x1006a89c,raw)
        before=bytes(n.uc.mem_read(0x1006a89c-16,16));after=bytes(n.uc.mem_read(0x1006ad4c,16))
        packet=b'\x5c\x30'+struct.pack('<h',len(rows))+b''.join(struct.pack('<hhB',*r)for r in rows);n.receive(packet)
        if rows:
            expected=[(job,0,flag)for job,active,flag in original]
            for unit,job,flag in rows:expected[unit%10+((unit//10)%5)*10]=(job,1,0 if flag==1 else 1)
        actual=[struct.unpack('<iii',n.uc.mem_read(0x1006a89c+i*12,12))for i in range(100)]
        assert actual==expected and bytes(n.uc.mem_read(0x1006a89c-16,16))==before and bytes(n.uc.mem_read(0x1006ad4c,16))==after
        tables.append(dict(rows=rows,active=[i for i,r in enumerate(actual)if r[1]==1]))
    for name in ['테스트','다른이','', 'A\0B']:
        for ignored in [0,1,255]:
            n.open();n.write(0x1006a73c,n.string('테스트'.encode('cp949')));n.uc.mem_write(0x0d060000,bytes([0xcc])*256);raw=name.encode('cp949')
            r=n.receive(bytes([0x5c,0x20,ignored,len(raw)])+raw);expected_name=name.split('\0')[0]
            if expected_name=='테스트':assert r['events']==[]
            else:assert len(r['events'])==1 and r['events'][0][:2]==['dialogue',[40,130,561,65]]and r['events'][0][2]==expected_name+'님이 승리하셨습니다.'
            notices.append(dict(name=name,ignored=ignored,native=r))
    for value in [0,1,2,26,27,31,32,127,128,160,255]:
        n.open();n.write(0x1006bd0c,n.string(b'old'));n.receive(bytes([0x5c,0x66,value]));actual=n.text(n.read(0x1006bd0c));expected=b''if value==0 or(value+0x60)&255==0 else bytes([(value+0x60)&255]);assert actual==expected
        suffixes.append(dict(value=value,resultHex=actual.hex()))
    from native_retreat_confirmation_emulator import controls
    retreat=controls()
    sources=[Path(__file__),OUT/'native_retreat_confirmation_emulator.py',OUT/'native_battle_options_emulator.py',*[p for p in (ROOT/'분석/Ghidra/Saver_Late/RetreatConfirmation').iterdir()if p.suffix in ['.c','.log']],*[OUT/name for name in ['native_scenario_end_emulator.py','native_result_kills_emulator.py','native_result_emulator.py','native_battle_briefing_emulator.py','native_battle_status_emulator.py','native_battle_options_emulator.py']]]
    result=dict(nativeAiReceiptCases=len(ai),nativeScalarCases=len(scalars),nativeDeploymentCases=len(tables),nativeNamedNoticeCases=len(notices),nativeSuffixCases=len(suffixes),nativeRetreatConfirmationCases=len(retreat),nativeRetreatConfirmationSha256=hashlib.sha256(json.dumps(retreat,separators=(',',':')).encode()).hexdigest(),suffixCStringReadOnly=True,mismatches=0,unityPlay=False,runtimeAuxiliaryRoutesConnected=False,originalGameCtlSha256=n.sha256,
        scope='Actual EB10 dispatch and0C/20/30/40/5C/66 receiver bodies; AI imports, bounded allocation, MFC CString and notice construction are captured.0C only verifies call arguments/order,30 table mutation has no X/Y,40 scalar meaning is not inferred,20 notices do not remove units,66 stores a byte suffix. DlgButtonAction28/E780/F040/5C flag: capture request5C61 before close28; exactly1 and strict-numeric/default-1 <999 open8 at110,10. Suffix output here is C-string bytes only; actual CString length including embeddedNUL is independently verified by native-scenario-suffix-check. No new production routes, server rules, actual UI/audio or whole-client completeness claim.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},ai=ai,scalars=scalars,deployments=tables,notices=notices,suffixes=suffixes)
    (OUT/'native-auxiliary-packets-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','ai','scalars','deployments','notices','suffixes']},ensure_ascii=False))
if __name__=='__main__':main()
