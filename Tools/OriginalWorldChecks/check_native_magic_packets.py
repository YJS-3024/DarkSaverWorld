"""Execute original UserUnit magic builder and GameCtl magic decoder/echo condition."""
import csv,hashlib,json,struct,subprocess
from pathlib import Path
from check_native_move_hints import NativeMove
from check_native_battle_decoder import NativeDecoder
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP,UC_HOOK_CODE
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-magic-packet-checks')
class NativeMagicOutgoing(NativeMove):
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='bounded-new':self.ret(self.DATA+0x70000);return
        if name=='send-bytes':
            ptr,count=self.args(2);assert count==16+2*len(self.case['targets']);self.sent.append(bytes(uc.mem_read(ptr,count)).hex());self.ret();return
        if name and 'GetMagicTbl@@'in name:self.ret(self.DATA+0x8000);return
        super()._dispatch(uc,address,size,data)
    def run_cast(self):
        c=self.case;row=c['metadata'];ptr=self.DATA+0x6000;targets=self.DATA+0x6100
        for at,key in [(0,'id'),(8,'distance'),(12,'range'),(16,'team'),(20,'unit'),(0x170,'pattern')]:self.write(self.DATA+0x8000+at,row[key])
        if c['targets']:self.uc.mem_write(targets,struct.pack('<'+'h'*len(c['targets']),*c['targets']))
        self.uc.mem_write(ptr,struct.pack('<hhhhIB3xI',row['id'],c['source'],c['x'],c['y'],0,len(c['targets']),targets))
        self.invoke(0x10005090,[ptr,c['mode']-1]);assert len(self.sent)==1;result=self.sent[0];self.close();return result
class NativeMagicIncoming(NativeDecoder):
    def __init__(self):
        super().__init__();self.byte_reads=0
        self.handles.append(self.uc.hook_add(UC_HOOK_CODE,self.dispatch,begin=0x1000c560,end=0x1000c560))
    def dispatch(self,uc,address,size,data):
        if address==0x1000c560:self.byte_reads+=1;return
        name=self.hooks.get(address)
        if name and ('??2@'in name or 'operator_new'in name or name=='#823'):value=self.DATA+0x8000
        elif name and ('??3@'in name or 'operator_delete'in name or name=='#825'):value=0
        elif name and 'GetUnitInfoP@@'in name:self.uc.mem_write(self.DATA+0x9000+100,struct.pack('<i',self.source_team));value=self.DATA+0x9000
        elif name and 'Magic@@'in name:
            a,b,ignored,count,targets=self.args(5);count&=255
            self.calls.append(dict(magic=self.signed(a&65535),source=self.signed(a>>16),x=self.signed(b&65535),y=self.signed(b>>16),targets=list(struct.unpack('<'+'h'*count,uc.mem_read(targets,count*2)))));value=0
        else:return super().dispatch(uc,address,size,data)
        sp=uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);uc.reg_write(UC_X86_REG_EAX,value);uc.reg_write(UC_X86_REG_ESP,sp+4);uc.reg_write(UC_X86_REG_EIP,pc)
    def run_magic(self,c):
        self.calls=[];self.reads=0;self.byte_reads=0;self.source_team=c['sourceTeam'];self.uc.mem_write(0x1006a738,struct.pack('<i',c['localTeam']))
        self.uc.mem_write(self.DATA,bytes(c['packet']));sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<II',self.STOP,self.DATA));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(0x1000eb10,self.STOP+1,timeout=5000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and len(self.calls)<=1
        return dict(call=bool(self.calls),**(self.calls[0]if self.calls else dict(magic=0,source=0,x=0,y=0,targets=[])),wordReads=self.reads,byteReads=self.byte_reads)
class NativeMagicClientOutgoing(NativeMagicIncoming):
    def __init__(self):
        super().__init__();self.hooks[0x1000e2c0]='send-cast';self.handles.append(self.uc.hook_add(UC_HOOK_CODE,self.dispatch,begin=0x1000e2c0,end=0x1000e2c0));self.sent=[]
    def dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and 'GetMagicTbl@@'in name:value=self.DATA+0xa000
        elif name=='send-cast':
            ptr,count=self.args(2);self.sent.append(bytes(uc.mem_read(ptr,count)).hex());value=0
        else:return super().dispatch(uc,address,size,data)
        sp=uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);uc.reg_write(UC_X86_REG_EAX,value);uc.reg_write(UC_X86_REG_ESP,sp+4);uc.reg_write(UC_X86_REG_EIP,pc)
    def run_cast(self,c):
        self.sent=[];row=c['metadata'];ptr=self.DATA+0x6000;targets=self.DATA+0x6100
        for at,key in [(0,'id'),(8,'distance'),(12,'range'),(16,'team'),(20,'unit'),(0x170,'pattern')]:self.uc.mem_write(self.DATA+0xa000+at,struct.pack('<i',row[key]))
        if c['targets']:self.uc.mem_write(targets,struct.pack('<'+'h'*len(c['targets']),*c['targets']))
        if 'item'in c:
            self.uc.mem_write(ptr,struct.pack('<HHhhhhIB3xI',c['item'],c['token'],row['id'],c['source'],c['x'],c['y'],0,len(c['targets']),targets));entry=0x1000e610;args=[ptr]
        else:
            self.uc.mem_write(ptr,struct.pack('<hhhhIB3xI',row['id'],c['source'],c['x'],c['y'],0,len(c['targets']),targets));entry=0x1000e4c0;args=[ptr,c['mode']-1]
        self.uc.mem_write(0x1001a8ec,struct.pack('<i',0x400));sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<'+'I'*(len(args)+1),self.STOP,*args));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(entry,self.STOP+1,timeout=5000000,count=100000);assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and len(self.sent)==1;return self.sent[0]
class NativeLevelTargets(NativeAi):
    """Run UserUnit CheckMagicTarget, including its actual area and team tests."""
    def __init__(self,case):
        super().__init__(case)
        self.hooks[0x10011896]='level-allocate';self._hook_add(UC_HOOK_CODE,self._dispatch,begin=0x10011896,end=0x10011896)
        row=case['metadata']
        for at,key in [(0,'id'),(8,'distance'),(12,'range'),(16,'team'),(20,'unit'),(0x170,'pattern')]:self.write(self.DATA+0x20000+at,row[key])
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name=='level-allocate':self.ret(self.DATA+0x30000)
        elif name and 'GetMagicTbl@@'in name:self.ret(self.DATA+0x20000)
        else:super()._dispatch(uc,address,size,data)
    def targets(self):
        c=self.case;actor=next(u for u in c['units']if u['id']==c['source']);ptr=self.DATA+0x1000;sp=self.STACK+0xff000
        self.uc.mem_write(sp,struct.pack('<6I',self.STOP,ptr,4001,c['source'],actor['x'],actor['y']));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(self.exports['CheckMagicTarget'],self.STOP+1,timeout=5000000,count=5000000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP
        ok=self.uc.reg_read(UC_X86_REG_EAX)!=0;count=self.uc.mem_read(ptr+12,1)[0]if ok else 0;target=self.read(ptr+16)if count else 0
        result=list(struct.unpack('<'+'h'*count,self.uc.mem_read(target,2*count)))if count else []
        self.close();return ok,result
class NativeLevelEffect(NativeMagicClientOutgoing):
    def dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name and 'GetUnitInfoP@@'in name:
            assert self.args(1)[0]==self.case['source'];actor=next(u for u in self.case['units']if u['id']==self.case['source'])
            self.uc.mem_write(self.DATA+0x9000+0x70,struct.pack('<ii',actor['x'],actor['y']));value=self.DATA+0x9000
        elif name and 'CheckMagicTarget@@'in name:
            ptr,magic,source,x,y=self.args(5);assert magic==4001 and source==self.case['source']
            ok,targets=self.target_result
            if ok:
                if targets:self.uc.mem_write(self.DATA+0x6100,struct.pack('<'+'h'*len(targets),*targets))
                self.uc.mem_write(ptr,struct.pack('<hhhhIB3xI',magic,source,x,y,0,len(targets),self.DATA+0x6100))
            value=int(ok)
        else:return super().dispatch(uc,address,size,data)
        sp=uc.reg_read(UC_X86_REG_ESP);pc=self.read(sp);uc.reg_write(UC_X86_REG_EAX,value);uc.reg_write(UC_X86_REG_ESP,sp+4);uc.reg_write(UC_X86_REG_EIP,pc)
    def run_level(self,c):
        self.case=c;self.sent=[];self.target_result=NativeLevelTargets(c).targets()if c['source']>=0 else (False,[])
        self.uc.mem_write(self.DATA,bytes(c['packet']));row=c['metadata']
        self.uc.mem_write(0x1001c490,struct.pack('<I',self.DATA+0xb000));self.uc.mem_write(self.DATA+0xb088,struct.pack('<ii',c['width'],c['height']))
        self.uc.mem_write(0x1001a0b8,struct.pack('<ii',20,15))
        for at,key in [(0,'id'),(8,'distance'),(12,'range'),(16,'team'),(20,'unit'),(0x170,'pattern')]:self.uc.mem_write(self.DATA+0xa000+at,struct.pack('<i',row[key]))
        self.uc.mem_write(0x1001a8ec,struct.pack('<i',c['mode']));sp=self.STACK+0xff000;self.uc.mem_write(sp,struct.pack('<II',self.STOP,self.DATA));self.uc.reg_write(UC_X86_REG_ESP,sp)
        self.uc.emu_start(0x1000eb10,self.STOP+1,timeout=5000000,count=100000)
        assert self.uc.reg_read(UC_X86_REG_EIP)==self.STOP and len(self.sent)<=1,(c['source'],c['mode'],c['packet'][1],hex(self.uc.reg_read(UC_X86_REG_EIP)),len(self.sent))
        return dict(unit=c['source'],packet=self.sent[0]if self.sent else None,focusLeft=self.read(0x1001c460)if c['source']>=0 else None,focusTop=self.read(0x1001c464)if c['source']>=0 else None)
def level_vectors():
    row=dict(id=4001,distance=0,range=8,team=34,unit=3,pattern=45);cases=[]
    for opcode in [0x64,0x65]:
        for mode in [0x200,0x400,0,0x800]:
            for source in [-32768,-1,0,400]:
                for variant in ['normal','boundary','inactive','disabled','dead-enabled','no-target']:
                    units=[dict(id=0,x=10,y=10,team=0),dict(id=1,x=9,y=10,team=0),dict(id=400,x=11,y=10,team=8),dict(id=401,x=10,y=11,team=8),dict(id=402,x=18,y=10,team=8),dict(id=403,x=17,y=12,team=8)]
                    if variant=='boundary':units[2].update(x=18,y=10);units[3].update(x=18,y=11);units[4].update(x=2,y=10)
                    if variant=='inactive':units[2]['nativeActive']=False
                    if variant=='disabled':units[2]['nativeEnabled']=False
                    if variant=='dead-enabled':units[2]['hp']=0
                    if variant=='no-target':
                        for u in units:u.update(team=0)
                    for u in units:
                        u.setdefault('nativeActive',True);u.setdefault('nativeEnabled',True)
                    actor=next((u for u in units if u['id']==source),None)
                    permitted=[u['id']for u in units if actor and u['team']!=actor['team']]
                    cases.append(dict(levelEffect=True,packet=[0x5c,opcode,*struct.pack('<h',source)],source=source,mode=mode,metadata=row,width=24,height=24,units=units,targets=permitted))
    return cases
def vectors():
    cases=[]
    with (ROOT/'Assets/_Original/Data/Tables/Magictbl.csv').open(encoding='utf-8-sig',newline='')as f:rows=list(csv.DictReader(f))
    for i,r in enumerate(rows):
        row={k:int(r[v])for k,v in dict(id='번호',distance='거리',range='범위',team='팀',unit='유닛',pattern='패턴').items()}
        for mode in [1,2]:cases.append(dict(metadata=row,source=[0,1,999,2999][i%4],x=-1 if row['unit']==8 else 3,y=-1 if row['unit']==8 else 4,mode=mode,targets=[0,1,400],width=12,height=12,units=[]))
    for count in [0,1,255]:
        for mode in [1,2]:
            c=json.loads(json.dumps(cases[0]));c['mode']=mode;c['targets']=list(range(count));cases.append(c)
    items=json.loads((ROOT/'Assets/_Original/Data/Combat/BattleItems.json').read_text())['entries']
    for item in items:
        original=next(c for c in cases if c['metadata']['id']==item['magic'])
        for token in [0,255,32768,65535]:
            c=json.loads(json.dumps(original));c.update(item=item['item'],token=token);cases.append(c)
    for magic_id in [-1,101,21101]:
        for source in [-1,0,400]:
            for count in [0,1,3,255]:
                for team in [0,8]:
                    for x,y in [(-1,4),(-32768,32767),(32767,-32768)]:cases.append(dict(packet=[0x5c,0x89,*struct.pack('<hhhhB',magic_id,source,x,y,count),*struct.pack('<'+'h'*count,*range(count))],localTeam=0,sourceTeam=team))
    return cases+level_vectors()
def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeMagicDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeMagicDifferential.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);incoming=NativeMagicIncoming();client=NativeMagicClientOutgoing();level=NativeLevelEffect();mismatches=[];comparisons=0
    for case,current in zip(cases,managed):
        results=[level.run_level(case)]if case.get('levelEffect')else [incoming.run_magic(case)]if 'packet'in case else [client.run_cast(case)]if 'item'in case else [NativeMagicOutgoing(case).run_cast(),client.run_cast(case)]
        for native in results:
            comparisons+=1
            if native!=current:mismatches.append(dict(input=case,native=native,managed=current))
    incoming.close();client.close();level.close();(WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeMagicPackets.cs','Assets/_Original/Runtime/Combat/NativeViewport.cs','Assets/_Original/Data/Tables/Magictbl.csv','Tools/OriginalWorldChecks/NativeMagicDifferential.cs','Tools/OriginalWorldChecks/check_native_magic_packets.py','Tools/OriginalWorldChecks/check_native_battle_decoder.py','Tools/OriginalWorldChecks/check_native_move_hints.py','Tools/OriginalWorldChecks/check_native_outbound_packets.py','Tools/OriginalWorldChecks/native_ai_emulator.py','분석/InstallerVersions/installer_inventory.json']
    result=dict(cases=len(cases),nativeComparisons=comparisons,outgoing=sum('packet'not in c for c in cases),incoming=sum('packet'in c and not c.get('levelEffect')for c in cases),levelEffectCases=sum(bool(c.get('levelEffect'))for c in cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual UserUnit10005090 and GameCtl1000E4C0/1000E610 magic builders; GameCtl DA10 decoder/echo suppression. Level-effect cases execute EB10 -> DF10 -> 3D10 integer viewport centering -> original UserUnit CheckMagicTarget (area/team/active/enabled/order), then original E4C0 mode-gated builder. Imported table/allocator/send boundaries are isolated; UserUnit target result is handed unchanged to the GameCtl import boundary. No XP threshold formula, health/cost prediction, Unity Play or socket framing.',logs=str(WORK))
    (OUT/'native-magic-packet-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
