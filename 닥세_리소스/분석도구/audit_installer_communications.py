"""Hash-deduplicated PE inventory across every extracted install group and nested archives."""
import hashlib,re,json,subprocess
from pathlib import Path
from analyze_special_skills import BASE,ROOT,OUT,PE,write

def main():
 groups={}
 for p in sorted((BASE/'설치본_추출').rglob('*')):
  if p.suffix.lower() in ['.exe','.dll'] and p.is_file():groups.setdefault(hashlib.sha256(p.read_bytes()).hexdigest(),[]).append(p)
 rows=[];apis=[];evidence=[]
 for digest,paths in groups.items():
  p=paths[0];meta=subprocess.check_output(['objdump','-p',str(p)],text=True);selected=[]
  for line in meta.splitlines():
   if re.search(r'WSOCK|WS2_|WININET|GetProcAddress|LoadLibrary|Internet|Ftp|SerialSend|ModuleInit|AddTrigData|AddResponseData|GetConnectStatus|WriteFile|ReadFile|CommState|CommTimeout|SendMessageA',line,re.I):
    selected.append(line.strip());apis.append(dict(file=p.name,sha256=digest,entry=line.strip()))
  role={'NetGame.exe':'launcher; FTP WinINet; serial module bindings; WinSock imports also present','NGpdll.dll':'serial/modem: SerialSend, trigger/response API, Windows COM operations','UpGrade.exe':'process/file updater; no named WinSock/WinINet import','charview.exe':'resource viewer; communication imports inspected','Saver.exe':'game application and MFC transport','GameCtl.dll':'game WM52C packet bridge','userunit.dll':'unit WM52C packet bridge','DManage.dll':'graphics/archive dynamic functions; no named game socket API','Compress.dll':'archive and Windows GUI runtime messages','MapCheckSum.exe':'local map checksum utility'}.get(p.name,'support library; imports inspected')
  rows.append(dict(file=p.name,sha256=digest,copies=len(paths),representative=str(p.relative_to(BASE)),role=role,api_evidence_count=len(selected)))
  evidence.append(p.name+'\n'+'\n'.join(selected))
 assert len(rows)==12,len(rows)
 # The dynamic functions resolved by DManage are all archive-file API names.
 p=PE(ROOT/'DManage.dll')
 for va in [0x100130ac,0x100130a4,0x10013094,0x10013088,0x1001307c,0x10013070,0x10013064,0x10013054,0x10013048,0x1001303c]:apis.append(dict(file='DManage.dll',sha256=hashlib.sha256(p.b).hexdigest(),entry='dynamic archive export: '+p.string(va)))
 write('패킷_설치본전체_모듈분류.csv',rows);write('패킷_설치본전체_통신단서.csv',apis)
 (OUT/'패킷_설치본전체_통신근거.txt').write_text('\n\n'.join(evidence)+'\n')
 result=dict(unique_pe_modules=len(rows),physical_pe_files=sum(len(v) for v in groups.values()),scope='All extracted PE files by SHA256; imports/exports classification is not proof of every runtime dynamic target',external_connections=0)
 (OUT/'패킷_설치본전체_통신조사.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
