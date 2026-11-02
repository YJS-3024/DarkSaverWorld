"""Native self-information16 signed numeric fields and original DrawText coordinates."""
import hashlib,json,os,random,struct,subprocess,sys
from pathlib import Path
from native_self_info_emulator import SelfInfoNative
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-self-info-checks')
def main():
 WORK.mkdir(exist_ok=True);n=SelfInfoNative();cases=[];rng=random.Random(57510)
 for i in range(80):
  data=bytearray(rng.randbytes(732))
  if i<5:
   for offset in range(4,732,2):struct.pack_into('<h',data,offset,[-32768,-1,0,1,32767][i])
  x,y=[(0,0),(35,23),(-80,90),(500,-20)][i%4];cases.append(dict(record=bytes(data).hex(),x=x,y=y,expected=n.values(bytes(data),x,y)))
 # Capture the separate software-font header's literal format and operand order; not semantic level inference.
 sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'));from native_pe import PE
 pe=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');pe.expect(0x457991,'0fbf4d080fbf550a5152');pe.expect(0x457a6d,'8b4d108b550c5152');assert pe.string(0x490a88)=='%d(%d)'and pe.string(0x490a7c)=='%lu/%lu'
 (WORK/'cases.json').write_text(json.dumps(dict(cases=cases,genders=[pe.string(a)for a in [0x490a6c,0x490a74,0x490a78]]),ensure_ascii=False));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'SelfInfoChecks.dll';sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 args=(old/'contracts.rsp').read_text().replace(str(old/'EntryChecks.dll'),str(exe)).replace(str(ROOT/'Tools/OriginalEntryChecks/EntryChecks.cs'),str(OUT/'SelfInfoChecks.cs'))+'\n"'+str(ROOT/'Assets/_Original/Runtime/UI/NativeSelfInformation.cs')+'"\n';rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes());env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(WORK/'cases.json')])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 r=json.loads(p.stdout);r.update(nativeRecordCases=len(cases),nativeNumericDrawCalls=sum(len(row['expected'])for row in cases),nativeCodeExecuted=True,nativeSha256=pe.sha256,mismatches=0,unityPlay=False,logs=str(WORK),scope='Original575F0 numeric basic block457F0E..458302 with initialized register/stack state;16signed field loads, rectangles and order. Standard MFC formatting and GDI drawing captured boundaries. Separate header operand/format instruction checks. Production exact memory-record projection, known totals, unknown preservation and deep-copy/DataContract checks. Not raw network decoder, level-pair semantic inference, complete GDI/Unity window or OS font equality.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_native_self_info.py',OUT/'native_self_info_emulator.py',OUT/'native_chat_composer_emulator.py',OUT/'SelfInfoChecks.cs',ROOT/'Assets/_Original/Runtime/UI/NativeSelfInformation.cs',ROOT/'Assets/_Original/Runtime/Growth/CharacterGrowth.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.Personal.cs']});(OUT/'native-self-info-check.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
