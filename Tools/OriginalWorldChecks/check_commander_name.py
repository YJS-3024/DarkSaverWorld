"""Original F3 name request bytes and production manager/session ownership checks."""
import hashlib,json,os,subprocess
from pathlib import Path
from native_commander_name_emulator import CommanderNameNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-commander-name-checks')
def main():
 WORK.mkdir(exist_ok=True);n=CommanderNameNative();cases=[];keys=[]
 for record in [0,1,255,256,513,65534,65535]:
  for name in ['', ' ','새 이름','Mixed_이름','끝 공백 ','a'*12,'가'*30,'A'*196]:
   row=n.submit(record,name);assert row['hidden']==1 and row['focus']==[0x12345]and len(row['packets'])==1;cases.append(dict(record=record,name=name,packet=row['packets'][0]))
 for key in [0,13,27,65,112,255]:
  row=n.submit(513,'편집',key);assert len(row['packets'])==int(key==13)and row['hidden']==int(key in [13,27]);keys.append(dict(key=key,**row))
 (WORK/'cases.json').write_text(json.dumps(dict(packets=cases,keys=keys),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'CommanderNameChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'CommanderNameChecks.cs'))
 names=['UI/NativeCodePage949.cs','UI/NativeMemoInbox.cs','UI/NativeMemoTransfer.cs','UI/NativeCommanderName.cs','Entry/LocalServerManager.Memos.cs','Entry/LocalServerManager.CommanderName.cs']
 for p in [*[ROOT/'Assets/_Original/Runtime'/name for name in names],OUT/'ClientWindowManagerHost.cs']:args+='\n"'+str(p)+'"\n'
 rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2000:])
 r=json.loads(p.stdout)
 # Constructor4570B5..4570DC stores the exact edit/hit rectangle144,39..232,55.
 import sys
 sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'));from native_pe import PE
 native=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');assert native.sha256=='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
 rect=[native.u32(address+6)for address in [0x4570b5,0x4570c9,0x4570bf,0x4570d3]];assert rect==[144,39,232,55]
 ui=(ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.CommanderName.cs').read_text();assert ui.count('new Rect(144,39,88,16)')==2
 r.update(nativeEditRect=rect,nativeCodeExecuted=True,nativePacketCases=len(cases),nativeKeyCases=keys,mismatches=0,logs=str(WORK),nativeScope='Actual4587C0/458750 with isolated MFC CString, edit control, allocator200, final send and focus/hide. Native executed names fit the original200-byte allocation. Larger BYTE-wrap cases are managed/static copy semantics only; no original heap overflow execution.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_commander_name.py',OUT/'native_commander_name_emulator.py',OUT/'CommanderNameChecks.cs',OUT/'ClientWindowManagerHost.cs',*[ROOT/'Assets/_Original/Runtime'/name for name in names],ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.CommanderName.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Personal.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Utilities.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs']});(OUT/'commander-name-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k not in ['sources','nativeKeyCases']}))
if __name__=='__main__':main()
