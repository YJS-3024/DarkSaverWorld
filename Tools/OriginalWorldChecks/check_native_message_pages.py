"""Actual GameCtl pipe-line parser, three-row drawing, page and timeout branches.
CString storage, graphics and host callbacks are bounded substitutes; no pixel/timing claim.
"""
import hashlib,json,struct,subprocess,sys
from pathlib import Path
from native_scenario_end_emulator import NativeScenarioEnd
from native_ai_emulator import UC_X86_REG_ECX,UC_X86_REG_EAX
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
class NativeMessage(NativeScenarioEnd):
    def hook(self,uc,address,size,data):
        name=self.hooks.get(address)
        if getattr(self,'captureDispatch',False) and address in [0x100089f0,0x10008790]:
            args=self.args(3 if address==0x100089f0 else 5)
            self.dispatched.append(dict(kind='portrait'if address==0x100089f0 else'plain',arguments=args[:-1],text=self.text(args[-1]).decode('cp949')))
            self.ret(0,len(args)*4)
        elif getattr(self,'captureDispatch',False) and address==0x100147bc:
            this=uc.reg_read(UC_X86_REG_ECX);self.write(this,self.string(self.text(self.args(1)[0])));self.ret(this,4)
        elif name=='#2764':self.ret(self.text(self.read(uc.reg_read(UC_X86_REG_ECX))).find(self.text(self.args(1)[0])),4)
        elif name=='atoi':self.ret(int(self.text(self.args(1)[0])or b'0'))
        elif name and 'GetFace@@'in name:self.faces.append(self.args(4));self.ret()
        elif address==0x0d000020:
            surface,x,y,p,color,flags=self.args(6)
            self.labels.append(dict(x=x,y=y,text=self.text(p).decode('cp949'),color=color,flags=flags));self.ret()
        else:super().hook(uc,address,size,data)
    def prepare(self,text,mode=1,first=0,tutorial=0,start=0,now=0):
        self.open();self.events=[];self.confirmActions=[];self.trace=[];self.existing=[];self.confirming=False
        self.uc.mem_write(self.OBJECT,bytes(512));self.write(0x1006a708,self.OBJECT);self.write(0x1006c118,tutorial)
        self.uc.reg_write(UC_X86_REG_ECX,self.OBJECT)
        self.run(0x10008840,[self.string(text.encode('cp949'))]);count=self.uc.reg_read(UC_X86_REG_EAX)
        rows=[self.text(self.read(self.OBJECT+0x1c+i*4)).decode('cp949')for i in range(count)]
        for offset,value in [(0,1),(4,1),(8,40),(12,50),(16,561),(20,104),(0x18,1),(0x1ac,mode),(0x1b0,first),(0x1b4,count),(0x1bc,start)]:self.write(self.OBJECT+offset,value)
        self.clock=now;self.labels=[];self.draws=[];self.formats=[];self.bodies=[];self.blits=[]
        self.uc.reg_write(UC_X86_REG_ECX,self.OBJECT);self.run(0x10008ad0,[])
        return dict(rows=rows,labels=self.labels,draws=self.draws,state=self.snapshot())
    def next_page(self):
        self.uc.reg_write(UC_X86_REG_ECX,self.OBJECT);self.run(0x10008ca0,[]);return self.snapshot()
    def dispatch(self,mode,face):
        self.open();self.captureDispatch=True;self.dispatched=[];self.formats=[]
        self.write(0x1006a708,self.OBJECT)
        try:self.run(0x100109e0,[self.string(f'{mode} {face} 0 0 literal'.encode('cp949'))]);return list(self.dispatched)
        finally:self.captureDispatch=False
    def construct(self,location,text,tutorial):
        self.open();self.captureDispatch=False;self.faces=[];self.trace=[];self.existing=[];self.events=[];self.confirmActions=[]
        self.uc.mem_write(self.OBJECT,bytes(512));self.write(0x1006a708,self.OBJECT);self.write(0x1006c118,tutorial);self.write(0x1001c484,0x0d070400)
        self.uc.reg_write(UC_X86_REG_ECX,self.OBJECT);self.run(0x100089f0,[location,125,self.string(text.encode('cp949'))])
        return dict(state=self.snapshot(),faces=self.faces,rect=[self.read(self.OBJECT+i)for i in [8,12,16,20]],rows=self.read(self.OBJECT+0x1b4))
    def update_dialogue(self):
        self.labels=[];self.draws=[];self.formats=[];self.bodies=[];self.blits=[]
        # Exact Update callsite: active flag test ->08AD0, ending before script work.
        self.uc.emu_start(0x10003baf,0x10003bbf,timeout=1000000,count=100000)
        return dict(texts=self.labels,blits=len(self.draws))
def lines(value):
    rows=value.split('|')
    if rows[-1]=='':rows.pop()
    return [row.strip(' \t\r\n\v\f')for row in rows]
def main():
    native=NativeMessage();cases=[];source=[];pages=0;dispatches=[];constructors=[]
    for mode in [0,1,2,3]:
        for face in [100,125,815]:
            r=native.dispatch(mode,face)
            expected=[dict(kind='plain',arguments=[40,130,561,65],text='literal')]if mode==0 else [dict(kind='portrait',arguments=[mode,face],text='literal')]if mode in [1,2]else[]
            assert r==expected,(mode,face,r)
            dispatches.append(dict(mode=mode,face=face,native=r))
    for location in [1,2]:
        for tutorial in [0,1]:
            for text in ['', 'one|two|three','one|two|three|four']:
                r=native.construct(location,text,tutorial);total=len(lines(text))
                assert r['rect']==[40,50 if location==1 else 300,561,104]and r['faces']==[[0x0d070400,125,0,0]]and r['rows']==total
                assert r['state']['active']==1 and r['state']['waiting']==(0 if tutorial and total<=3 else 1)and r['state']['firstLine']==(3 if tutorial and total>3 else 0)
                drawing=native.update_dialogue();assert drawing['blits']>0 and len(drawing['texts'])==3
                if text=='':assert all(row['text']==''for row in drawing['texts'])
                r['update']=drawing
                constructors.append(dict(location=location,tutorial=tutorial,text=text,native=r))
    examples=['','a','a|','a||','a| ',' a | b | c | d ','one||three|four|five|six|seven','\t한글 \t| 둘 | 셋 | 넷']
    for p in (ROOT/'Assets/_Original/SourceData/Decoded').rglob('*.txt'):
        for line,text in enumerate(p.read_text().splitlines(),1):
            if text.strip().startswith('MESSAGE '):
                parts=text.strip().split(None,5)
                if len(parts)==6 and len(lines(parts[5]))>3:source.append((p,line,parts[5]))
    assert len(source)==57
    for text in examples+[row[2]for row in source]:
        expected=lines(text)
        for first in range(0,max(1,len(expected)),3):
            r=native.prepare(text,first=first);assert r['rows']==expected,(text,expected,r['rows'])
            assert r['labels']==[dict(x=150,y=73+i*20,text=expected[first+i]if first+i<len(expected)else'',color=0xffffff,flags=0)for i in range(3)]
            frame=[d for d in r['draws']if d['sourceX']in [0,40]and d['sourceY']==300]
            assert {(d['x'],d['y'])for d in frame}=={(40,50),(582,50)},frame
            state=native.next_page();last=first+3>=len(expected)
            assert state['active']==(0 if last else 1)and state['firstLine']==first+3
            assert state['callbacks']==([[0x4ca,8,0,0],[0x4d0,8,110,10]]if last else[])
            pages+=1
        cases.append(dict(text=text,rows=len(expected),pages=max(1,(len(expected)+2)//3)))
    timeouts=[]
    for tutorial in [0,1]:
        for text in ['notice','']:
            for start in [0,2**31-5000]:
                for delta in [9999,10000,10001]:
                    r=native.prepare(text,mode=0,tutorial=tutorial,start=start,now=(start+delta)&0xffffffff)
                    expired=bool(text)and delta>10000
                    assert r['state']['active']==(0 if expired and not tutorial else 1)
                    assert r['state']['waiting']==(0 if expired and not tutorial else 1)
                    timeouts.append(dict(tutorial=tutorial,empty=not text,start=start,delta=delta,active=r['state']['active'],waiting=r['state']['waiting']))
    paths=[Path(__file__),*[OUT/name for name in ['native_scenario_end_emulator.py','native_battle_briefing_emulator.py','native_battle_options_emulator.py','native_battle_status_emulator.py','native_result_emulator.py','native_result_kills_emulator.py','CampaignChecks.cs']],ROOT/'Assets/_Original/Runtime/Campaign/CampaignModel.cs',ROOT/'Assets/_Original/Runtime/Campaign/WorldSceneRoot.Campaign.cs',ROOT/'Assets/_Original/Runtime/Tutorial/TutorialSceneRoot.cs']
    result=dict(nativeMessages=len(cases),nativePages=pages,sourceMessagesWithMoreThanThreeRows=len(source),nativeTimeoutCases=len(timeouts),nativeMessageDispatchCases=len(dispatches),nativeMessageConstructorCases=len(constructors),mismatches=0,unityPlay=False,
        originalSha256=native.sha256,scope='Original08840 pipe split/trim,08AD0 three-line text operands and actual08E10 rounded frame edges,08CA0 page/close and strict10000ms plain-notice timeout. Host text/blit/CString/callbacks captured. Production managed pagination is tested separately; no font pixels, physical input or measured playback speed claim.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},messages=cases,timeouts=timeouts,dispatches=dispatches,constructors=constructors,
        sourceMessages=[dict(path=p.relative_to(ROOT).as_posix(),line=line,sha256=hashlib.sha256(p.read_bytes()).hexdigest())for p,line,text in source])
    (OUT/'native-message-pages-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','messages','timeouts','sourceMessages','dispatches','constructors']},ensure_ascii=False))
if __name__=='__main__':main()
