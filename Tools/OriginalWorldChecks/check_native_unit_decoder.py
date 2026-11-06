"""Compare50/51 decoded integers and slots against actual GameCtl/UserUnit x86."""
import hashlib,json,random,struct,subprocess,sys
from pathlib import Path
from native_ai_emulator import NativeAi,UC_X86_REG_ESP,UC_X86_REG_EIP
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-unit-decoder-checks')
sys.path.insert(0,str(ROOT/'Tools/ScenarioPlacementChecks'))
from audit_initial_roster_sources import packet_probe,PE

def vectors():
    rng=random.Random(5051);cases=[]
    names=[b'',b'A','검사'.encode('cp949'),b'x'*255]
    for kind in (0x50,0x51):
        for i in range(40):
            unit=(0,1,400,999)[i%4];job=(100,200,401,767)[i%4];x,y=(5,19,0,199)[i%4],(7,0,199,198)[i%4]
            words=[rng.randint(-32768,32767)for _ in range(16 if kind==0x50 else 45)]
            words[0]=unit;words[2]=job;words[14 if kind==0x50 else 20]=x;words[15 if kind==0x50 else 21]=y
            if kind==0x51:
                words[22:30]=[101,301,-1,701,0,-1,-1,-1] if i%2 else [0,101,301,701,201,401,501,601]
                words[30:38]=[10101,0,-1,21101,-1,-1,-1,-1] if i%2 else [-1,10101,0,21101,-1,-1,-1,-1]
            values=[(0,1,0x7fffffff,0x80000000,0xffffffff)[i%5],rng.getrandbits(32)]
            if kind==0x51:values+=[rng.getrandbits(32),rng.getrandbits(32)]
            label,owner=names[i%4],names[(i+1)%4]
            body=struct.pack('<'+'h'*len(words),*words)+struct.pack('<'+'I'*len(values),*values)+bytes([len(label)])+label
            if kind==0x51:body+=bytes([len(owner)])+owner
            cases.append(dict(packet=list(bytes([0x5c,kind])+body),unit=unit,job=job,x=x,y=y,label=label.hex().upper(),name=owner.hex().upper()if kind==0x51 else''))
    return cases

def status_results():
    native=NativeAi(dict(width=12,height=12,units=[dict(id=0,x=5,y=5)]));results=[]
    values=[0,*[1<<i for i in range(32)],0xffff,0xffffffff,0x5555,0xaaaa,0x8123,0x98760001]
    for value in values:
        native.write(native.RUNTIME+0x104,0xffffffff)
        sp=native.STACK+0xff000;native.uc.mem_write(sp,struct.pack('<III',native.STOP,0,value));native.uc.reg_write(UC_X86_REG_ESP,sp)
        native.uc.emu_start(0x1000c950,native.STOP+1,timeout=5000000,count=10000)
        assert native.uc.reg_read(UC_X86_REG_EIP)==native.STOP
        results.append((value,native.read(native.RUNTIME+0x104)))
    native.close();return results

def status_mutations(table):
    count=0
    for unit in [0,1,2999]:
        other=2 if unit==1 else 1
        native=NativeAi(dict(width=12,height=12,units=[dict(id=unit,x=5,y=5),dict(id=other,x=6,y=5)]))
        pointer=native.RUNTIME+unit*0x2e0;observer=native.RUNTIME+other*0x2e0
        protected=bytes(native.uc.mem_read(observer,0x2e0))
        for active in [0,1]:
            for enabled in [0,1]:
                for flags in [0,1,2,0x81,0xffff,0x80000000,0xffff0001,0xffffffff]:
                    before=bytearray(bytes(range(256))*3)[:0x2e0];struct.pack_into('<II',before,0,active,enabled);struct.pack_into('<I',before,0x104,0xabcdef01)
                    native.uc.mem_write(pointer,bytes(before));sp=native.STACK+0xff000
                    native.uc.mem_write(sp,struct.pack('<III',native.STOP,unit,flags));native.uc.reg_write(UC_X86_REG_ESP,sp)
                    native.uc.emu_start(0x1000c950,native.STOP+1,timeout=5000000,count=10000)
                    assert native.uc.reg_read(UC_X86_REG_EIP)==native.STOP
                    expected=bytearray(before)
                    if active:struct.pack_into('<I',expected,0x104,table[flags&65535])
                    assert bytes(native.uc.mem_read(pointer,0x2e0))==bytes(expected),(unit,active,enabled,hex(flags))
                    assert bytes(native.uc.mem_read(observer,0x2e0))==protected
                    count+=1
        native.close()
    return count

def main():
    WORK.mkdir(exist_ok=True);cases=vectors();input=WORK/'cases.json';output=WORK/'managed.json';flags=WORK/'flags.bin';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'NativeUnitDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'NativeUnitDifferential.cs'))
    for source in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output),str(flags)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2500:])
    base=Path('/workspace/.tools/ds-ghidra/installed/saver');ctl,units=PE(base/'GameCtl.dll'),PE(base/'UserUnit.dll')
    assert ctl.sha256=='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0'
    assert units.sha256=='74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b'
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[]
    for case,current in zip(cases,managed):
        packet=bytes(case['packet']);native=packet_probe(ctl,units,packet[1],case['unit'],case['job'],case['x'],case['y'],body=packet[2:],details=True)
        actual={hex(i*4):value for i,value in enumerate(current['fields'])if i not in(41,42)}
        if actual!=native['unitInfoDwords'] or current['magic']!=native['magic'] or current['skills']!=native['skills'] or native['bodyConsumed']!=len(packet)-2 or not current['roundtrip'] or current['className']!=case['label'] or current['name']!=case['name']:
            mismatches.append(dict(case=case,native=native,managed=current))
    reset_cases=0
    for kind in (0x50,0x51):
        case=next(c for c in cases if c['packet'][1]==kind);packet=bytes(case['packet'])
        for active in (0,1):
            for enabled in (0,1):
                for state in (0,0x10,0x80):
                    native=packet_probe(ctl,units,kind,case['unit'],case['job'],case['x'],case['y'],body=packet[2:],details=True,runtime_seed={0:active,4:enabled,0x100:state,0x108:2})
                    assert native['resetFlags']==dict(active=1,enabled=1,state=state if enabled else 0x30,order=2 if enabled else 1),native['resetFlags']
                    reset_cases+=1
    effect_cases=0
    for kind in (0x50,0x51):
        case=next(c for c in cases if c['packet'][1]==kind);packet=bytes(case['packet'])
        seed={offset:0x10203040+offset for offset in range(0x14c,0x2e0,4)}
        native=packet_probe(ctl,units,kind,case['unit'],case['job'],case['x'],case['y'],body=packet[2:],details=True,runtime_seed=seed)
        assert len(native['clearedEffectFields'])==101
        assert all(value==0 for offset,value in native['clearedEffectFields'].items()if int(offset,16)<0x2dc)
        assert native['clearedEffectFields']['0x2dc']==seed[0x2dc]
        effect_cases+=1
    status=status_results();table=struct.unpack('<65536I',flags.read_bytes())
    for value,actual in status:assert table[value&65535]==actual,(hex(value),hex(actual),hex(table[value&65535]))
    bits=dict(status)
    for value,actual in enumerate(table):
        expected=0
        for bit in range(16):
            if value&(1<<bit):expected|=bits[1<<bit]
        assert actual==expected,(value,actual,expected)
    full_status_cases=status_mutations(table)
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    paths=[Path(__file__),OUT/'NativeUnitDifferential.cs',OUT/'native_ai_emulator.py',ROOT/'Tools/ScenarioPlacementChecks/audit_initial_roster_sources.py',ROOT/'Assets/_Original/Runtime/Combat/NativeUnitSnapshots.cs',ROOT/'Assets/_Original/Runtime/Combat/BattleTicks.cs',ROOT/'분석/InstallerVersions/installer_inventory.json']
    result=dict(cases=len(cases),integerFieldComparisons=len(cases)*42,nativeResetFlagCases=reset_cases,nativeEffectResetCases=effect_cases,effectResetDwordComparisons=effect_cases*100,effectTailPreservationCases=effect_cases,statusNativeCases=len(status),statusCombinations=len(table),nativeStatusWholeRecordCases=full_status_cases,mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,originalGameCtlSha256=ctl.sha256,originalUserUnitSha256=units.sha256,casesSha256=hashlib.sha256(input.read_bytes()).hexdigest(),sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},scope='Actual late GameCtl dispatch50/51 and UserUnit reset prefix; compare42 integer fields, first-minus-one spell/skill lists and consumed body extent. Reset prefix also executes prior active/enabled/state/order combinations and clears100 DWORDs14C..2D8 while retaining2DC. Original MagicFlagC950 independently executes each incoming bit plus mixed/high-bit values and compares full736-byte target/observer records across active/enabled states and three IDs; managed all65536 low-bit combinations compared to native bit basis. CString allocation/assignment modeled; string wire bytes checked only for lossless codec roundtrip. Malformed guards are adapter protections. No Windows/Unity Play or inferred server roster/growth rules.',logs=str(WORK))
    (OUT/'native-unit-decoder-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    assert not mismatches
if __name__=='__main__':main()
