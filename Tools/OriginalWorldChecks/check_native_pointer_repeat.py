"""Compare the production held-pointer clock against the exact Saver input pump.
Window, DirectInput and scene draw are captured boundaries, never Unity Play.
"""
import hashlib,json,os,random,subprocess,sys
from pathlib import Path
from native_pointer_repeat_emulator import NativePointerRepeat
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-native-pointer-repeat-checks')
def signed(v):return ((v+2**31)%2**32)-2**31
def main():
 WORK.mkdir(exist_ok=True)
 p=subprocess.run([sys.executable,str(OUT/'check_map_pointer.py')],cwd=ROOT,capture_output=True,text=True)
 (WORK/'pointer-regression.log').write_text(p.stdout+p.stderr)
 if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-1800:])
 cases=[dict(start=start,times=[signed(start+d)for d in deltas])for start in [0,1000,2**31-20,-50,-1]for deltas in [
  [0,1,29,30,31,31,32,60,61,62,1000,1000,1030,1031],
  [31,30,0,-1,32,61,62],
  [2**31-1,2**31,2**31+30,2**31+31],
 ]]
 rng=random.Random(4119)
 for _ in range(24):
  start=signed(rng.getrandbits(32));now=start;times=[]
  for _ in range(24):now=signed(now+rng.choice([-100,-31,-1,0,1,29,30,31,32,1000]));times.append(now)
  cases.append(dict(start=start,times=times))
 path=WORK/'cases.json';path.write_text(json.dumps(cases))
 sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk'
 p=subprocess.run([str(sdk/'dotnet'),str(Path('/tmp/ds-map-pointer-checks/MapPointerChecks.dll')),str(path)],cwd=ROOT,capture_output=True,text=True)
 if p.returncode:raise RuntimeError(p.stdout+p.stderr)
 actual=json.loads(p.stdout);samples=0
 native=NativePointerRepeat()
 for mode in [0,1,1280]:
  native.mode(mode)
  for case,expected in zip(cases,actual):
   observed=native.trace(case['start'],case['times'])
   assert observed==expected,(mode,case,expected,observed)
   samples+=len(observed)
 sources=[Path(__file__),OUT/'native_pointer_repeat_emulator.py',OUT/'MapPointerChecks.cs',ROOT/'Assets/_Original/Runtime/Maps/NativeMapPointer.cs']
 result=dict(cases=len(cases)*3,clockSamples=samples,modes=[0,1,1280],mismatches=0,unityPlay=False,originalSha256=native.sha,
  scope='Actual Saver4119B0 signed32 clock threshold/repeat counter with DirectInput/scene draw/held callback captured; production NativePointerRepeatClock compiled unchanged. Boundary/wrap/backward/delayed/repeated polls, no catch-up; no Windows input, Unity dispatch, wall-time measurement or exact frame cadence claim.',
  sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},logs=str(WORK))
 (OUT/'native-pointer-repeat-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
if __name__=='__main__':main()
