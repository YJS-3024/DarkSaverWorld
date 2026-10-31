"""Summarize cross-version evidence without treating filename dates as release dates."""
import collections,json,re,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'분석/InstallerVersions'
def records(path,composite=False):
 result={};count=0
 for l in Path(path).read_text().splitlines():
  t=l.split()
  if t and t[0].isdigit():
   count+=1;key=':'.join(t[:2]) if composite else t[0];result[key]=t
 return count,result

def main():
 inv=json.loads((OUT/'installer_inventory.json').read_text());data=json.loads((OUT/'payload_data_comparison.json').read_text())['versions'];late=next(v for v in data if 'saver.exe' in v['containers']);baseline={(r['archive'].lower(),r['entry'].lower()):r for r in late['scriptsAndTables']};rows=[]
 for v in data:
  keymap={(r['archive'].lower(),r['entry'].lower()):r for r in v['scriptsAndTables']};delta=[];tables=[]
  for key,r in keymap.items():
   if key in baseline and r['sha256']!=baseline[key]['sha256']:delta.append({'archive':key[0],'entry':key[1],'sha256':r['sha256'],'lateSha256':baseline[key]['sha256']})
   if key[0]=='gameres/set.arc' and key[1] in ['ability.atr','levelabl.atr','magictbl.atr','magicptn.atr','solskill.atr']:
    comp=key[1]=='levelabl.atr';count,values=records(ROOT/r['decoded'],comp);_,final=records(ROOT/baseline[key]['decoded'],comp)
    tables.append({'table':key[1],'numericRows':count,'uniqueKeys':len(values),'lateOnlyKeys':sorted(set(final)-set(values)),'oldOnlyKeys':sorted(set(values)-set(final)),'changedCommonKeys':[k for k in values if k in final and values[k]!=final[k]]})
  oldRoster={(r['archive'].lower(),r['entry'].lower()):r['sha256'] for r in v['nativeRosterFiles']};lateRoster={(r['archive'].lower(),r['entry'].lower()):r['sha256'] for r in late['nativeRosterFiles']}
  binaries=next(r for r in inv['payloads'] if r['payloadId']==v['payloadId']);clients=[]
  for b in binaries['files']:
   if b['file'] in ['saver.exe','gamectl.dll','userunit.dll','setunit.dll','soundll.dll']:
    row={k:b[k] for k in ['file','sha256','peTimestamp']}
    if b['file']=='gamectl.dll':
     raw=Path(b['extractedFile']).read_bytes();row['literalCounts']={t:raw.count(t.encode()+b'\0') for t in ['TUTORIAL','CHARDIR','CHARSETSKILL','WAITFORMOVEEND','WAITMOVEEND','SETHP','SETMP']}
    clients.append(row)
  rows.append({'containers':v['containers'],'payloadId':v['payloadId'],'clients':clients,'tableComparison':tables,'changedScriptTableEntries':delta,'nativeRosterFiles':len(oldRoster),'rosterMatchesLate':oldRoster==lateRoster if oldRoster else None,'tutorialEntryHashes':sorted({r['sha256'] for r in v['tutorials']}),'decodeErrors':v['decodeErrors']})
 result={'scope':'Version-scoped source comparison. Patches are partial, not independently runnable full clients. Literal absence does not certify all UI behavior.','versions':rows}
 (OUT/'version_comparison_summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 for row in rows:
  print(row['containers'],'tableRows',[(t['table'],t['numericRows']) for t in row['tableComparison']],'changed text/table entries',len(row['changedScriptTableEntries']),'roster identical',row['rosterMatchesLate'])
if __name__=='__main__':main()
