"""Direct-call and import inventory across supplied EXE/DLLs; no native execution."""
import re,json,hashlib,subprocess
from analyze_special_skills import ROOT,OUT,write

def main():
 binaries=sorted(ROOT.glob('*.dll'))+sorted(ROOT.glob('*.exe'))+sorted((ROOT/'MAP').glob('*.exe'))
 imports=[];calls=[];writers=[];snips=[];modules=[]
 for p in binaries:
  meta=subprocess.check_output(['objdump','-p',str(p)],text=True);asm=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',str(p)],text=True)
  network=[l.strip() for l in meta.splitlines() if re.search(r'WSOCK|WS2_|WININET|WINHTTP|urlmon|OnPacketReceive|CSocket|CAsyncSocket|InternetOpen|connect|recv|send',l,re.I)]
  modules.append(dict(file=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),network_named_import_export_hits=len(network),scope='static imports/exports and direct calls; MFC ordinal or dynamic resolution not ruled out'))
  for l in network:imports.append(dict(file=str(p.relative_to(ROOT)),entry=l))
  lines=asm.splitlines();instructions=[(int(m[1],16),m[2]) for l in lines if (m:=re.match(r'^\s*([0-9a-f]+):\s+(.*)$',l))]
  targets={0x43b840:'Saver application send',0x439320:'Saver frame sender',0x402900:'Saver alternate transport',0x43d0e0:'write_u8',0x43d0c0:'write_u16',0x43d100:'write_bytes',0x43d140:'write_u32'} if p.name.lower()=='saver.exe' else {0x1000e1e0:'GameCtl WM52C bridge'} if p.name.lower()=='gamectl.dll' else {0x10004f90:'userunit WM52C bridge'} if p.name.lower()=='userunit.dll' else {}
  for i,(va,insn) in enumerate(instructions):
   m=re.match(r'(?:call|jmp)\s+0x([0-9a-f]+)\b',insn)
   if not m or int(m[1],16) not in targets:continue
   target=int(m[1],16);label=targets[target];row=dict(file=p.name,callsite_va=hex(va),target_va=hex(target),target=label,scope='direct static callsite; not unique packet kind or function identity')
   if label.startswith('write_'):writers.append(row)
   else:
    calls.append(row);context=instructions[max(0,i-70):i+4];snips.append(f'{p.name} {hex(va)} -> {label}\n'+'\n'.join(f'{a:08x}: {b}' for a,b in context))
 write('패킷_모듈_통신조사.csv',modules);write('패킷_통신_이름단서.csv',imports);write('패킷_송신_직접호출.csv',calls);write('패킷_직렬화_직접호출.csv',writers);(OUT/'패킷_송신_호출문맥.asm.txt').write_text('\n\n'.join(snips))
 summary=dict(modules=len(modules),direct_send_calls=len(calls),writer_calls=len(writers),saver_application_send_calls=sum(r['file']=='Saver.exe' and r['target_va']=='0x43b840' for r in calls),gamectl_bridge_calls=sum(r['file']=='GameCtl.dll' for r in calls),userunit_bridge_calls=sum(r['file']=='userunit.dll' for r in calls),scope='exhaustive direct calls to listed known targets in selected supplied binaries; indirect/dynamic calls and unknown bridges outside coverage')
 (OUT/'패킷_호출조사요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
