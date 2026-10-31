"""Run production tutorial/session/account code, compile Unity-facing code and audit source links.
These are managed/API/source checks, never a substitute for Unity Play/physical input.
"""
import csv,hashlib,json,re,struct,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original'
DATA=Path('/workspace/.tools/unity/6000.5.7f1/Editor/Data');WORK=Path('/tmp/ds-original-tutorial-checks')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True)
 entry=subprocess.run([sys.executable,str(ROOT/'Tools/OriginalEntryChecks/check_entry.py')],cwd=ROOT,text=True,capture_output=True)
 (WORK/'entry-regression.log').write_text(entry.stdout+entry.stderr)
 if entry.returncode:raise RuntimeError((entry.stdout+entry.stderr)[-2000:])
 sdk=DATA/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
 core=[BASE/'Runtime/Entry'/n for n in ['EntryProtocol.cs','EntryAccountService.cs','EntryClient.cs','LocalAccountFileStore.cs']]
 core += [BASE/'Runtime/Tutorial'/n for n in ['TutorialScript.cs','TutorialSession.cs','TutorialService.cs','WaitingLesson.cs']]
 core += [BASE/'Runtime/World'/n for n in ['WorldModel.cs','WorldService.cs']] + [BASE/'Runtime/Campaign'/n for n in ['CampaignModel.cs','CampaignService.cs','ScenarioGatewaySettings.cs','ScenarioGatewayService.cs']]
 refs=['-r:'+str(p) for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
 exe=WORK/'TutorialChecks.dll';args=['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+list(map(str,core))+[str(Path(__file__).with_name(n)) for n in ['TutorialChecks.cs','WaitingLessonChecks.cs']]
 rsp=WORK/'tutorial.rsp';rsp.write_text('\n'.join('"'+str(a)+'"' for a in args)+'\n')
 compile=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,text=True,capture_output=True)
 (WORK/'compile.log').write_text(compile.stdout+compile.stderr)
 if compile.returncode:raise RuntimeError((compile.stdout+compile.stderr)[-2500:])
 version=next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name
 exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 run=subprocess.run([str(dotnet),str(exe)],cwd=ROOT,text=True,capture_output=True)
 (WORK/'run.log').write_text(run.stdout+run.stderr)
 if run.returncode:raise RuntimeError((run.stdout+run.stderr)[-2500:])
 result=json.loads(run.stdout)
 evidence=json.loads((ROOT/'Tools/OriginalTutorialChecks/tutorial-assets.json').read_text())
 for row in evidence['outputs']:assert sha(ROOT/row['asset'])==row['sha256'],row['asset']
 for path,digest in evidence['sources'].items():assert sha(ROOT/path)==digest,path
 # Raw archive/CSV/MRC fixtures remain separately audited by check_foundation.py.
 sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
 from decode_visuals import read_mrc
 m=read_mrc(BASE/'SourceData/Native/map/301.mrc.bytes');definition=json.loads((BASE/'Data/Tutorial/Tutorial301.json').read_text())
 assert definition['map']['attributes']==[t[3]&0x7f for t in m['tiles']]
 assert len(definition['decorations'])==len(m['sprites'])==31
 for actual,source in zip(definition['decorations'],m['sprites']):assert (actual['x'],actual['y'])==(source['x']*32,source['y']*32)
 rows={int(r['직업']):r for r in csv.DictReader((BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig'))}
 for a in definition['map']['abilities']:
  r=rows[a['job']];assert a['name']==r['이름']
  for key,native in dict(hp='체력수',maxHp='체력한',mp='마력수',maxMp='마력한',attack='공력수',defense='방력수').items():assert a[key]==int(r[native])
 # Checked directly against native executable bytes, independently of the interpreter.
 inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
 late=next(p for p in inventory['payloads'] if 'saver.exe' in p['containers'])
 waiting_source=ROOT/'분석/Saver_Late/waiting_tutorial_resources.json'
 waiting_before=waiting_source.read_bytes()
 original_exe=next(r for r in late['files'] if r['file']=='saver.exe')
 waiting_audit=subprocess.run([sys.executable,str(ROOT/'Tools/OriginalTutorialChecks/extract_waiting_tutorial.py'),original_exe['extractedFile']],cwd=ROOT,text=True,capture_output=True)
 if waiting_audit.returncode:raise RuntimeError(waiting_audit.stdout+waiting_audit.stderr)
 assert waiting_source.read_bytes()==waiting_before,'Waiting native resource audit changed; regenerate art before verification'
 waiting_evidence=json.loads(waiting_before)
 fixtures={
  'userunit.dll':{0x1000ea34:'b801000000',0x1000ea46:'c78416bc00000065000000',0x1000ea64:'c78416e000000075270000',0x1000ee7b:'b963000000',0x1000eeba:'c746340c000000',0x1000eed7:'c746580a000000',0x1000b221:'89540634',0x1000b22e:'894c1638',0x1000b290:'8b5758',0x1000b293:'8954065c',0x1000ec99:'8b4804'},
  'gamectl.dll':{0x10008a17:'c7460828000000',0x10008a20:'c7460c32000000',0x10008a29:'c7460c2c010000',0x10008a3c:'c7461031020000',0x10008a43:'c7461468000000',0x10005615:'b820000000',0x100056c3:'8d8b8f000000',0x1000571b:'8d8b9c000000',0x100057c7:'8d8ba9000000',0x10005880:'8d8bb6000000',0x10005449:'8d84807c010000',0x10005432:'8d9a48010000',0x1000647a:'83c3f9',0x100064b3:'83c329',0x10006601:'8d4541'}}
 for filename,addresses in fixtures.items():
  row=next(r for r in late['files'] if r['file'].lower()==filename);raw=Path(row['extractedFile']).read_bytes()
  assert hashlib.sha256(raw).hexdigest()==row['sha256']
  pe=struct.unpack_from('<I',raw,60)[0];count=struct.unpack_from('<H',raw,pe+6)[0];sections=pe+24+struct.unpack_from('<H',raw,pe+20)[0]
  for address,value in addresses.items():
   for i in range(count):
    _,rva,length,offset=struct.unpack_from('<IIII',raw,sections+i*40+8)
    if rva<=address-0x10000000<rva+length:
     position=offset+address-0x10000000-rva;assert raw[position:position+len(bytes.fromhex(value))]==bytes.fromhex(value),(filename,hex(address));break
   else:raise AssertionError((filename,hex(address)))
 assert definition['magicTravelX']==336 and definition['magicTravelFrames']==3 and definition['magicImpactX']==0 and definition['magicImpactFrames']==5
 assert (definition['magicSound1'],definition['magicSound2'])==(0,-1)
 assert not any(w['key']==7 for w in definition['windows'])
 root=(BASE/'Runtime/Tutorial/TutorialSceneRoot.cs').read_text()
 assert 'typeof(Canvas)' in root and 'canvas.renderMode = RenderMode.ScreenSpaceOverlay' in root
 assert 'actor.X * 32 - 32' in root and 'menuLayer = Group' in root and root.index('menuLayer = Group')>root.index('windowsLayer = Group')
 assert 'new Rect(40, 50, 562, 104)' in root
 assert 'windowsLayer.SetAsLastSibling()' in root
 assert 'actor.ShowsAttackEffect' in root and 'actor.AttackFxX - actor.X' in root
 assert 'dialogueLayer.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.Message))' in root
 assert 'canvas.sortingOrder = 200' in root and 'typeof(UIDocument)' not in root
 # Native control dimensions/artwork: retain original pixels rather than flat-color substitutes.
 from PIL import Image
 with Image.open(BASE/'Art/Tutorial/Board.png') as board:
  assert board.size[0]>=435 and board.size[1]>=415
  assert board.getpixel((15,385))[:3]==(255,247,0)  # native HP, not solid red
  assert board.getpixel((15,397))[:3]==(107,247,49) # native MP, not solid blue
 hud=(BASE/'Runtime/Tutorial/TutorialSceneRoot.Windows.cs').read_text()
 assert 'new Rect(32, 32, 32, 32)' in hud and 'new Rect(65, 34, 48, 9)' in hud
 assert 'new Rect(380 + digit * 5, 342, 5, 7)' in hud
 assert 'Color.red' not in hud.split('if (key == 3)')[0]
 with Image.open(BASE/'Art/Tutorial/Actor201.png') as actor, Image.open(BASE/'Art/Archives/gameres/char0.arc/200r.bmp.png') as source:
  assert actor.convert('RGB').tobytes()==source.convert('RGB').tobytes() and actor.size==source.size
 assert (BASE/'Data/Tutorial/Tutorial301.json').read_bytes().find(b'\r')<0
 waiting=json.loads((BASE/'Data/Tutorial/Waiting1500.json').read_bytes())
 raw_waiting=read_mrc(BASE/'SourceData/Native/map/1500.mrc.bytes')
 assert waiting['map']['attributes']==[0 if t[3]==255 else t[3]&127 for t in raw_waiting['tiles']]
 assert [line['text'] for line in waiting['lines']]==[r['text'] for r in waiting_evidence['resources']]
 assert [line['id'] for line in waiting['lines'] if line['skipped']]==[61236,61237,61238]
 assert (BASE/'Data/Tutorial/Waiting1500.json').read_bytes().find(b'\r')<0
 manifest=json.loads((ROOT/'Tools/OriginalEntryChecks/entry-check.json').read_text())
 result.update(entryRegressionAssertions=manifest['assertions'],runtimeApiErrors=manifest['runtimeApiErrors'],editorApiErrors=manifest['editorApiErrors'],playerApiErrors=manifest['playerApiErrors'],warnings=manifest['warnings'],sceneScriptsResolved=4,
  nativeInstructionFixtures=sum(map(len,fixtures.values())),sourceLinkedArtOutputs=len(evidence['outputs']),sourceHashesVerified=len(evidence['sources']),
  waitingNativeInstructionFixtures=len(waiting_evidence['nativeInstructionChecks'])+1,waitingResources=36,waitingShownResources=33,
  scope='Production interpreter/account/disk managed tests plus Unity Runtime/Editor/Player API compilation and source/reference audit. No Unity import, rendered Play, OS input or listening.',logs=str(WORK))
 Path(__file__).with_name('tutorial-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
