"""Tutorial source/asset coverage inventory. Does not certify runtime playback.
Usage: --assets <unshield saver directory> --gamectl <late GameCtl.dll>
"""
import argparse,hashlib,importlib.util,json,struct
from collections import Counter
from pathlib import Path
from native_pe import PE
ROOT=Path(__file__).resolve().parents[2]
def module(name,path):
 spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def sha(b):return hashlib.sha256(b).hexdigest()
p=argparse.ArgumentParser();p.add_argument('--assets',type=Path,required=True);p.add_argument('--gamectl',type=Path,required=True);args=p.parse_args()
cat=module('catalog',ROOT/'닥세_리소스/분석도구/build_late_saver_catalog.py');dec=module('decoder',ROOT/'닥세_리소스/분석도구/analyze_client.py')
out=ROOT/'분석/Saver_Late';manifest={r['path'].lower():r for r in json.loads((out/'files.json').read_text())}
g=PE(args.gamectl);assert g.sha256==manifest['gamectl.dll']['sha256'];g.expect(0x1000fb03,'0f85e5020000');assert g.string(0x1001a524)=='Map\\tutorial.arc'
sequence=json.loads((out/'tutorial_command_sequence.json').read_text());commands=sequence['commands'];dispatch=json.loads((out/'tutorial_dispatch_audit.json').read_text());comparison={r['token']:r['address'] for r in dispatch['comparisons']}
bytype=[]
for name,n in sorted(Counter(c['command'] for c in commands).items()):
 bytype.append({'command':name,'count':n,'sourceLines':[c['sourceLine'] for c in commands if c['command']==name],'nativeComparison':comparison.get(name),'status':'dedicated comparison found; not all branch semantics certified' if name in comparison else 'no dedicated comparison in recovered interpreter'})
assert len(commands)==420 and len(bytype)==44
# All literal unit introductions and all dialogue speakers, without replacing COM0.
actors=[{'line':c['sourceLine'],'tokens':c['arguments'].split()} for c in commands if c['command']=='CHARPOS']
speakers=sorted({c['arguments'].split()[1] for c in commands if c['command']=='MESSAGE'})
needed={f'{i}r.bmp' for i in ['100','101','103','125','201','301','805']};assets=[]
for path in sorted(args.assets.rglob('*')):
 if not path.is_file():continue
 rel=path.relative_to(args.assets).as_posix().lower();raw=path.read_bytes()
 assert rel in manifest and sha(raw)==manifest[rel]['sha256'],rel
 if path.suffix.lower()!='.arc':
  if rel.startswith('map/2301'):continue
  assets.append({'source':rel,'sha256':sha(raw),'bytes':len(raw)});continue
 for name,method,size,packed in cat.arc_entries(path):
  wanted=rel.endswith('face.arc') and name.lower() in {f'{i}.bmp' for i in speakers} or name.lower() in needed or name.lower()=='fx.bmp' or rel.endswith('effect1.arc') or rel.endswith('effect2.arc')
  if not wanted:continue
  raw=(dec.lzh_decode if method==0 else dec.lzss_decode)(packed,size);assert len(raw)==size
  entry={'source':rel,'entry':name,'sha256':sha(raw),'bytes':size,'link':'candidate image by ID; graphics loader path must also be considered'}
  if raw[:2]==b'BM':entry['bmp']={'width':struct.unpack_from('<i',raw,18)[0],'height':struct.unpack_from('<i',raw,22)[0],'bits':struct.unpack_from('<H',raw,28)[0]}
  assets.append(entry)
for n in needed:assert any(r.get('entry','').lower()==n for r in assets),n
assert any(r.get('entry','').lower()=='125.bmp' and r['source'].endswith('face.arc') for r in assets)
windows=json.loads((ROOT/'Tools/MapChecks/original_tutorial_native_analysis.json').read_text())['windowBindings']
layouts=json.loads((out/'client_ui_layout_audit.json').read_text())['layouts']
# Retain actual source names; avoid assuming every screen uses a numeric scene filename.
ui=[r for r in layouts if r['source'].lower().startswith('glib/')]
report={'scope':'All 420 tutorial source commands classified by token and directly referenced source dependencies; not complete code-path semantics or execution',
 'gamectlSha256':g.sha256,'entry':{'gate':'0x1000FAFD checks tutorial flag; nonzero -> 0x1000FDEE','archive':'Map/tutorial.arc','entry':'tutorial.deo','emptyDataBranch':'0x1000FE64..1000FEA0 resets state and sends 0x590/0x4CD','recordLimit':'loader stores at most999 lines, starting at context+0x54'},
 'commandTypes':bytype,'actors':actors,'dialogueSpeakers':speakers,'dialogueCount':sum(c['command']=='MESSAGE' for c in commands),
 'map':next(r for r in json.loads((out/'maps.json').read_text()) if r['path']=='map/301.mrc'),
 'assets':assets,'windowBindingsHistoricalEvidence':windows,'battleLayouts':ui,
 'unresolved':['All menu/keyboard/pointer branch semantics and native child windows','Dynamic COM0 selection and every sprite loader transformation','Special map resource ID zero needs consumer semantics; resource existence alone is not proof of use','MIDI host notification loop and device playback measurement','Prior waiting-room guidance source; duplicate tutorial archive supplies no separate course']}
(out/'tutorial_dependency_audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
(ROOT/'Tools/OriginalClientChecks/evidence/late-tutorial-entry.asm.txt').write_text(g.disasm(0x1000fab0,0x10010003)+g.disasm(0x100123c2,0x1001241e))
print(json.dumps({'commands':len(commands),'commandTypes':len(bytype),'actorIntroductions':len(actors),'dialogueSpeakers':speakers,'assetRecords':len(assets),'battleLayouts':len(ui),'completeRuntimeAnalysis':False}))
