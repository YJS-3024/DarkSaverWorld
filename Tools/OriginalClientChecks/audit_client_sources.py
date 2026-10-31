"""Read original resources and export non-network client evidence; never modify Assets.
Native files: unshield extraction of saver/data1.cab. Requires only Python stdlib.
"""
import argparse,csv,hashlib,json,re,struct
from collections import Counter
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'분석/Saver_Late';TABLES=OUT/'decoded/gameres/set.arc'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,data): (OUT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
def records(p):
 for n,line in enumerate(p.read_text().splitlines(),1):
  t=line.split(';')[0].split()
  if t and t[0].isdigit():yield n,t

def main(native):
 manifest={r['path'].lower():r for r in json.loads((OUT/'files.json').read_text())}
 binary={}
 for name in ['GameCtl.dll','UserUnit.dll','setunit.dll','DManage.dll']:
  p=native/name;expected=manifest[name.lower()]['sha256'];assert sha(p)==expected,name
  binary[name]={'sha256':expected,'data':p.read_bytes()}
 growth=list(records(TABLES/'levelabl.atr'));magic={int(t[0]):(n,t) for n,t in records(TABLES/'magictbl.atr')};sol={int(t[0]):(n,t) for n,t in records(TABLES/'solskill.atr')}
 old=json.loads((OUT/'source_quality.json').read_text());resolved=[];unresolved=[]
 for key,uses in old['missingGrowthAbilityIds'].items():
  i=int(key);record={'id':i,'growthKeys':uses,'occurrences':len(uses)}
  if i in sol:
   n,t=sol[i];record.update(table='solskill.atr',line=n,name=t[1],level=int(t[2]),troops=list(map(int,t[3].split(','))),count=int(t[4]));resolved.append(record)
  else:unresolved.append(record)
 patterns={int(t[0]) for n,t in records(TABLES/'magicptn.atr')}
 dangling=[{'id':i,'name':t[1],'line':n,'pattern':int(t[14])} for i,(n,t) in magic.items() if int(t[14])>0 and int(t[14]) not in patterns]
 anomalies={'scope':'Table cross-reference audit only; no invented skill or server rule.', 'sourceHashes':{p.name:sha(p) for p in [TABLES/'levelabl.atr',TABLES/'magictbl.atr',TABLES/'solskill.atr',TABLES/'magicptn.atr']},'resolvedAsHireSkills':resolved,'unresolvedInMagicAndHireTables':unresolved,'missingEffectPatterns':dangling,'shortGrowthRows':[{'line':n,'job':int(t[0]),'level':int(t[1]),'width':len(t),'tokens':t} for n,t in growth if len(t)!=18], 'duplicateGrowthRows':[{'line':n,'tokens':t} for n,t in growth if t[:2]==['163','35']], 'emptyTokenRows':old['emptyAbilityTokens'],'decodeErrors':json.loads((OUT/'decode_errors.json').read_text())}
 # Preserve the distinction between archive decoding errors and main-table parse success.
 for err in anomalies['decodeErrors']:
  token=err['entry'].encode();err['literalPresentInCheckedDlls']=[name for name,d in binary.items() if token in d['data']]
 save('client_table_audit.json',anomalies)
 tutorial=OUT/'decoded/map/tutorial.arc/tutorial.deo';early=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres/tutorial/TUTORIAL.DEO'
 text=tutorial.read_text();assert text.splitlines()==early.read_bytes().decode('cp949').splitlines()
 commands=[];dialog=0
 for line,raw in enumerate(text.splitlines(),1):
  t=raw.strip()
  if not t or t.startswith(';'):continue
  command=t.split()[0];dialog+=int(command=='MESSAGE');parts=t.split(None,5)
  commands.append({'sequence':len(commands)+1,'sourceLine':line,'dialogueIndex':dialog,'command':command,'arguments':t[len(command):].strip(),'raw':t})
 counts=Counter(r['command'] for r in commands)
 literals={}
 for c in sorted(counts):
  token=c.encode()+b'\0';offsets=[];b=binary['GameCtl.dll']['data'];start=0
  while True:
   i=b.find(token,start)
   if i<0:break
   offsets.append(i);start=i+1
  literals[c]={'count':counts[c],'gameCtlFileOffsets':offsets,'meaning':'String occurrence only; not proof of registered command dispatch.'}
 tutorialdata={'scope':'Complete original tutorial text order; no inferred command handler semantics.', 'source':str(tutorial.relative_to(ROOT)), 'sha256':sha(tutorial),'earlySha256':sha(early),'sameLinesAcrossVersions':True,'commands':commands,'commandTypes':literals,'dllHashes':{k:v['sha256'] for k,v in binary.items()}, 'count':len(commands),'dialogues':dialog,'distinctCommands':len(counts)}
 save('tutorial_command_sequence.json',tutorialdata)
 with (OUT/'tutorial_command_sequence.csv').open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.DictWriter(f,fieldnames=list(commands[0]));w.writeheader();w.writerows(commands)
 layouts=[]
 for p in sorted(native.rglob('*.lsc')):
  rel=str(p.relative_to(native));b=p.read_bytes();assert sha(p)==manifest[rel.lower()]['sha256'],rel
  assert len(b)%4==0;v=struct.unpack('<'+str(len(b)//4)+'i',b);i=7;tiles=[]
  for _ in range(v[6]):
   tile,count=v[i:i+2];i+=2;placements=[]
   for _ in range(count):
    x,y,nx,ny=v[i:i+4];i+=4;placements.append({'x':x,'y':y,'repeatX':nx,'repeatY':ny})
   tiles.append({'decorationId':tile,'placements':placements})
  count=v[i];i+=1;controls=[]
  for _ in range(count):
   button,x,y=v[i:i+3];i+=3;controls.append({'buttonId':button,'x':x,'y':y})
  tail=list(v[i:]);assert tail and len(tail)==1+tail[0]*3,(rel,tail)
  layouts.append({'source':rel,'sha256':sha(p),'bytes':len(b),'width':v[0],'height':v[1],'headerWords2To5':list(v[2:6]),'decorations':tiles,'controls':controls,'trailingRecordCount':tail[0],'trailingRecordsRaw':[tail[n:n+3] for n in range(1,len(tail),3)]})
 save('client_ui_layout_audit.json',{'scope':'All 90 installed Wlib/Glib LSC layouts; decoration repeats and button local coordinates. Header words 2..5 and trailing record meaning not assigned. Button action callbacks and actual rendering not verified by this parser.', 'layouts':layouts,'count':len(layouts),'controls':sum(len(r['controls']) for r in layouts),'trailingRecords':sum(r['trailingRecordCount'] for r in layouts)})
 assert len(commands)==420 and dialog==100 and len(layouts)==90
 assert len(resolved)==14 and len(unresolved)==12
 print(json.dumps({'resolvedHireIds':len(resolved),'remainingUnresolvedIds':len(unresolved),'missingPatternReferences':len(dangling),'tutorialCommands':len(commands),'dialogues':dialog,'commandTypes':len(counts),'layouts':len(layouts),'controls':sum(len(r['controls']) for r in layouts)},ensure_ascii=False))
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--native-root',type=Path,required=True);args=parser.parse_args();main(args.native_root)
