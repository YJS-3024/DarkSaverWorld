"""Trace every installed scenario actor appearance, lifecycle, trigger and AI reference.
Raw source occurrences are not a concurrent deployment or a reconstructed server rule.
"""
import collections,hashlib,json,re,struct,subprocess
from analyze_special_skills import BASE,ROOT,OUT,PE,table,write
from analyze_script_executor import ScriptHarness
from analyze_packet_requests import Harness
from decode_visuals import read_mrc
from unicorn.x86_const import UC_X86_REG_ECX,UC_X86_REG_EDI,UC_X86_REG_EBP,UC_X86_REG_EBX

def supplemental(root,abilities,maps,ai_index):
 inventory=[];positions=[];definitions=[];metadata=[];backups=[];binary=[];briefings=[]
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  for path in sorted(folder.iterdir()):
   if path.name.endswith('.utf8.txt'):continue
   raw=path.read_bytes();ext=path.suffix.lower()
   inventory.append(dict(archive=folder.name,file=path.name,extension=ext,size=len(raw),sha256=hashlib.sha256(raw).hexdigest()))
   if ext=='.all':
    # Preserve observations only: no installed consumer has yet established this editor-like format.
    for offset in range(0,len(raw)-3,4):
     value=struct.unpack_from('<I',raw,offset)[0]
     if value:binary.append(dict(archive=folder.name,file=path.name,offset=offset,u32=value,scope='raw aligned word; semantic meaning unconfirmed'))
    continue
   if ext not in ('.pos','.set','.bak','.srf'):continue
   text=raw.decode('cp949');section=''
   if ext=='.srf':briefings.append(dict(archive=folder.name,file=path.name,text=text));continue
   for i,line in enumerate(text.splitlines(),1):
    code=line.split(';',1)[0].strip();t=code.split();base=dict(archive=folder.name,file=path.name,line=i,raw=line)
    if ext=='.bak':
     if t and (t[0] in ('CHARPOS','CHARMOVE','KILLCHAR1','KILLCHAR2') or (t[0].isdigit() and len(t)>1 and 'CHAR' in t[1])):backups.append(dict(**base,scope='backup only; not merged into active script totals'))
     continue
    if not t:continue
    if t[0]=='999':continue
    if ext=='.pos':
     assert len(t)==3 and all(v.isdigit() for v in t),(base,t)
     positions.append(dict(**base,unit=int(t[0]),x=int(t[1]),y=int(t[2])))
    elif t[0].isdigit():
     assert len(t) in (2,3) and all(v.isdigit() for v in t),(base,t)
     definitions.append(dict(**base,unit=int(t[0]),ability_id=int(t[1]),extra_raw=t[2] if len(t)==3 else '',section_comment=section))
    else:metadata.append(dict(**base,key=t[0],tokens=json.dumps(t[1:],ensure_ascii=False)))
    if ';' in line:section=line.split(';',1)[1].strip()
    # Keep section header comments, including MONSTER SET, independently of executable code.
   # Locate source's own monster section label; no implicit numeric-slot enemy classification.
   section=''
   for line in text.splitlines():
    if 'MONSTER SET' in line:section='MONSTER SET'
   if ext=='.set' and section:
    for row in definitions:
     if row['archive']==folder.name and row['file']==path.name:row['section_comment']=section
 posindex=collections.defaultdict(list)
 for r in positions:
  m=maps.get(r['file'].rsplit('.',1)[0].lower());r['map_width']=m['width'] if m else '';r['map_height']=m['height'] if m else '';r['coordinate_in_map']=0<=r['x']<m['width'] and 0<=r['y']<m['height'] if m else ''
 for r in positions:posindex[r['archive'],r['unit']].append(r)
 joined=[]
 for r in definitions:
  ps=posindex[r['archive'],r['unit']]
  row=dict(r,position_candidates=json.dumps([{'file':p['file'],'line':p['line'],'x':p['x'],'y':p['y']} for p in ps]),ai_refs=json.dumps(ai_index[r['archive'],str(r['unit'])]),link_scope='same archive/unit textual join; installed runtime loading unproven')
  for n,tbl in abilities.items():
   a=tbl.get(str(r['ability_id']));row[n+'_found']=bool(a);row[n+'_tokens']=json.dumps(a,ensure_ascii=False) if a else ''
  joined.append(row)
 overview=[]
 for archive in sorted(set(r['archive'] for r in inventory)):
  defs=[r for r in definitions if r['archive']==archive];ps=[r for r in positions if r['archive']==archive]
  overview.append(dict(archive=archive,set_definitions=len(defs),pos_coordinates=len(ps),set_has_pos=sum(bool(posindex[r['archive'],r['unit']]) for r in defs),ai_slots=sum(k[0]==archive for k in ai_index),source_types=json.dumps(dict(collections.Counter(r['extension'] for r in inventory if r['archive']==archive)))))
 write('몬스터연결_시나리오대조.csv',overview)
 write('몬스터연결_전체자료목록.csv',inventory);write('몬스터연결_POS좌표.csv',positions);write('몬스터연결_SET편성.csv',joined);write('몬스터연결_SET설정.csv',metadata);write('몬스터연결_백업참고.csv',backups);write('몬스터연결_ALL원시값.csv',binary);write('몬스터연결_시나리오소개.csv',briefings)
 return dict(resource_files=len(inventory),resource_types=dict(collections.Counter(r['extension'] for r in inventory)),pos_rows=len(positions),pos_coordinates_in_map=sum(r['coordinate_in_map'] is True for r in positions),pos_map_missing=sum(r['coordinate_in_map']=='' for r in positions),set_rows=len(definitions),set_with_position=sum(bool(posindex[r['archive'],r['unit']]) for r in definitions),pos_without_set=sum((p['archive'],p['unit']) not in {(r['archive'],r['unit']) for r in definitions} for p in positions),backup_reference_rows=len(backups),set_metadata_rows=len(metadata),briefing_files=len(briefings),missing_set_abilities={n:sorted(set(r['ability_id'] for r in definitions if str(r['ability_id']) not in abilities[n])) for n in abilities})

def main():
 root=BASE/'설치본_추출/ARC_해제/MAP'
 abilities={n:{t[0]:t for t in table(n)} for n in ('ability','ability1')}
 magic={t[0]:t for t in table('Magictbl')}
 maps={p.stem.lower():read_mrc(p) for p in (ROOT/'MAP').iterdir() if p.suffix.lower()=='.mrc'}
 images=collections.defaultdict(list)
 for d in (BASE/'설치본_추출/ARC_해제/gameres').glob('char*.arc'):
  for p in d.iterdir():
   if p.suffix.lower()=='.bmp':images[p.stem.lower()].append(str(p.relative_to(BASE)))
 final_headers={}
 files=[];spawns=[];actions=[];triggers=[];ai=[];weights=[];unparsed=[];references=[]
 commands={'CHARPOS','CHARMOVE','CHARDIR','KILLCHAR1','KILLCHAR2','CHARABILITY','ATTACK','MAGIC','HPSET','MPSET','WAITMOVEEND','DO2'}
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  for path in sorted(folder.iterdir()):
   if path.suffix.lower() not in ('.ai','.evt','.deo','.dee'):continue
   raw=path.read_bytes();lines=raw.decode('cp949').splitlines();event='';event_start=0;scene='';block=[];lastspawn={};generation=collections.Counter()
   files.append(dict(archive=folder.name,file=path.name,sha256=hashlib.sha256(raw).hexdigest()))
   for lineno,original in enumerate(lines,1):
    s=original.split(';',1)[0].strip()
    if not s:continue
    base=dict(archive=folder.name,file=path.name,line=lineno,event=event,event_start=event_start,scene=scene,raw=original)
    if path.suffix.lower()=='.ai':
     if s=='999':continue
     m=re.fullmatch(r'(\d+)\s+(OD\w+)\s+([A-Z_]+)\s*\(([^)]*)\)\s*(.*)',s)
     if not m:unparsed.append(base);continue
     unit,mode,selector,arg,tail=m.groups();wid=f'{folder.name}/{path.name}:{lineno}'
     parts=list(re.finditer(r'([A-Z_0-9]+)\s*\(\s*([+-]?\d+)\s*\)',tail))
     rest=re.sub(r'([A-Z_0-9]+)\s*\(\s*([+-]?\d+)\s*\)','',tail).strip(' ,\t')
     assert not rest,(base,rest)
     ai.append(dict(**base,ai_id=wid,unit=unit,mode=mode,native_mode={'ODNORMAL':2,'ODATTACK':4,'ODDEFENCE':8}.get(mode,-1),selector=selector,selector_args=arg,actions_raw=tail,action_count=len(parts),weight_sum=sum(int(p[2]) for p in parts)))
     for p in parts:
      skill=p[1].removeprefix('MAGIC_') if p[1].startswith('MAGIC_') else ''
      weights.append(dict(ai_id=wid,action=p[1],weight=int(p[2]),magic_id=skill,magic_found=skill in magic if skill else '',magic_name=magic.get(skill,['',''])[1] if skill else ''))
     continue
    t=s.split();cmd=t[0]
    if cmd=='EVENT':
     event=t[1];event_start=lineno;final_headers[folder.name,path.name,event]=lineno;scene='';block=[];lastspawn={};continue
    if cmd=='LOADSCENE':scene=t[1];lastspawn={};continue
    base.update(event=event,event_start=event_start,scene=scene)
    if cmd.isdigit():
     if len(t)>1 and path.suffix.lower()=='.evt':triggers.append(dict(**base,condition_kind=t[1],condition_tokens=json.dumps(t[2:],ensure_ascii=False),scope='source condition; not executed by client action loader'))
     continue
    if cmd in ('TEST','TESTNOT','RANDOM'):block.append(s)
    if cmd in ('(',')'):block.append(cmd)
    if cmd not in commands:continue
    unit=t[2] if cmd=='CHARPOS' and len(t)>2 else (t[1] if len(t)>1 else '')
    if cmd=='CHARPOS':
     assert len(t)==5,(path,lineno,s)
     generation[unit]+=1;sid=f'{folder.name}/{path.name}:{lineno}';lastspawn[unit]=sid
     row=dict(**base,appearance_id=sid,unit=unit,ability_token=t[1],x=int(t[3]),y=int(t[4]),source_generation=generation[unit],control_prefix=json.dumps(block,ensure_ascii=False),type_resolution='runtime COM selection' if t[1].startswith('COM') else 'literal ability ID')
     for n,tbl in abilities.items():
      a=tbl.get(t[1]);row[n+'_found']=bool(a);row[n+'_display_id']=a[1] if a else '';row[n+'_image_candidates']=json.dumps(images.get((a[1]+'r').lower(),[]) if a else [],ensure_ascii=False);row[n+'_text_tokens']=json.dumps([v for v in a[2:] if not re.fullmatch(r'[+-]?\d+',v)],ensure_ascii=False) if a else ''
     spawns.append(row)
    actions.append(dict(**base,command=cmd,unit=unit,args=json.dumps(t[1:],ensure_ascii=False),preceding_appearance_same_source=lastspawn.get(unit,''),control_prefix=json.dumps(block,ensure_ascii=False)))
 assert not unparsed,unparsed
 ai_index=collections.defaultdict(list);spawn_index=collections.defaultdict(list)
 for r in ai:ai_index[r['archive'],r['unit']].append(r['ai_id'])
 for r in spawns:spawn_index[r['archive'],r['unit']].append(r['appearance_id'])
 for r in spawns:
  scene=r['scene'] or r['file'].rsplit('.',1)[0];m=maps.get(scene.lower());r['map_reference']=scene;r['map_reference_basis']='explicit LOADSCENE' if r['scene'] else 'filename candidate only';r['map_width']=m['width'] if m else '';r['map_height']=m['height'] if m else '';r['coordinate_in_map']=0<=r['x']<m['width'] and 0<=r['y']<m['height'] if m else '';r['final_client_event_definition']=r['event_start']==final_headers.get((r['archive'],r['file'],r['event'])) if r['file'].lower().endswith('.evt') else 'not event file'
 for r in spawns:r['ai_refs_same_archive_unit']=json.dumps(ai_index[r['archive'],r['unit']]);r['ai_link_scope']='same archive/slot candidate; not proof of simultaneous runtime use'
 for key in sorted(set(ai_index)|set(spawn_index)):
  references.append(dict(archive=key[0],unit=key[1],ai_refs=json.dumps(ai_index[key]),appearance_refs=json.dumps(spawn_index[key]),evidence='AI and scripted appearances' if ai_index[key] and spawn_index[key] else 'AI only; spawn/type not established' if ai_index[key] else 'scripted actor only; no matching AI source'))
 for name,rows in [('몬스터연결_원본파일.csv',files),('몬스터연결_등장.csv',spawns),('몬스터연결_행동생명주기.csv',actions),('몬스터연결_발생조건.csv',triggers),('몬스터연결_AI.csv',ai),('몬스터연결_AI행동.csv',weights),('몬스터연결_슬롯대조.csv',references)]:write(name,rows)
 # Execute original CHARPOS helper for each distinct argument list. Only external CString/CRT and final unit API are modeled.
 cases=[];h=ScriptHarness();del h.stubs[0x10010440]
 def strcmp():
  a,b=h.args(2);read=lambda p:bytes(h.u.mem_read(p,128)).split(b'\0')[0];h.ret(0 if read(a)==read(b) else 1)
 h.put(0x10016194,0x300f0900);h.stubs[0x300f0900]=strcmp
 def mid():
  obj=h.u.reg_read(UC_X86_REG_ECX);out,start,count=h.args(3);h.put(out,h.string(h.bytes(obj)[start:start+count]));h.ret(out,12)
 h.stubs[0x10014736]=mid
 def fmt():
  out,formatp,unit,ability,tail=h.args(5);assert PE(ROOT/'GameCtl.dll').string(formatp)=='%d %d %s';s=bytes(h.u.mem_read(tail,128)).split(b'\0')[0];h.put(out,h.string(f'{unit} {ability} '.encode()+s));h.ret()
 h.stubs[0x1001464c]=fmt
 captured=[]
 def spawn():captured.append(bytes(h.u.mem_read(h.args(1)[0],128)).split(b'\0')[0].decode());h.ret()
 h.put(0x100161f4,0x300f0910);h.stubs[0x300f0910]=spawn
 for i in range(5):h.u.mem_write(0x1001aa8e+412*i,struct.pack('<h',100+i))
 for arg in sorted(set(' '.join(r['raw'].strip().split()[1:]) for r in spawns)):
  t=arg.split();expected=f'{t[1]} {100+int(t[0][3:]) if t[0].startswith("COM") else int(t[0])} {t[2]} {t[3]}'
  h.run(0x10010440,(h.string(arg.encode()),),h.context)
  assert captured[-1]==expected,(arg,captured[-1],expected)
  cases.append(dict(case=arg,expected_dmSetChar=expected,actual_dmSetChar=captured[-1],match=True))
 write('몬스터연결_원본명령대조.csv',cases)
 constructor=[];u=Harness('userunit.dll');u.stubs[0x10011840]=lambda:u.ret();out=0x30000000
 for unit in (0,301,400,401,607,700,800,999):
  for ability,display,category,x,y in ((401,401,2,12,28),(801,801,3,19,7),(999,999,0,-1,0)):
   u.run(0x1000ee40,(out,unit,ability,display,category,x&0xffffffff,y&0xffffffff,unit//50))
   expected={0:unit,4:ability,8:display,0x18:category,0x48:x&0xffffffff,0x4c:y&0xffffffff,0x60:unit//50,0xac:1}
   assert all(u.get(out+k)==v for k,v in expected.items())
   constructor.append(dict(unit=unit,ability=ability,display=display,category=category,x=x,y=y,group=unit//50,match=True))
 write('몬스터연결_unitinfo대조.csv',constructor)
 mode_cases=[]
 for mode,expected in [('ODNORMAL',2),('ODATTACK',4),('ODDEFENCE',8),('ODATTACKL',-1)]:
  u.u.mem_write(out,bytes(64));u.put(out,400);text=out+0x100;u.u.mem_write(text,mode.encode()+b'\0')
  u.u.reg_write(UC_X86_REG_EDI,text);u.u.reg_write(UC_X86_REG_EBP,out);u.u.reg_write(UC_X86_REG_EBX,0);u.stubs[0x10002e08]=lambda:u.ret();u.run(0x10002d47)
  if expected==-1:assert u.get(out)==0xffffffff
  else:assert u.get(out+4)==expected
  mode_cases.append(dict(mode=mode,native_mode=expected,match=True))
 write('몬스터연결_AI모드대조.csv',mode_cases)
 ranges=[('userunit.dll',0x10002690,0x10002e80),('GameCtl.dll',0x10010440,0x10010690),('userunit.dll',0x1000ea60,0x1000eef0),('userunit.dll',0x100015c0,0x10001680)]
 (OUT/'몬스터연결_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/n)],text=True) for n,a,b in ranges))
 extra=supplemental(root,abilities,maps,ai_index)
 result=dict(supplemental=extra,source_files=len(files),source_types=dict(collections.Counter(r['file'].split('.')[-1].lower() for r in files)),appearance_occurrences=len(spawns),dynamic_COM_occurrences=sum(r['ability_token'].startswith('COM') for r in spawns),action_counts=dict(collections.Counter(r['command'] for r in actions)),condition_rows=len(triggers),ai_rows=len(ai),ai_modes=dict(collections.Counter(r['mode'] for r in ai)),ai_selectors=dict(collections.Counter(r['selector'] for r in ai)),ai_empty_actions=sum(not r['action_count'] for r in ai),ai_weight_sum_distribution=dict(collections.Counter(r['weight_sum'] for r in ai)),slot_evidence=dict(collections.Counter(r['evidence'] for r in references)),missing_magic_ids=sorted(set(r['magic_id'] for r in weights if r['magic_id'] and not r['magic_found'])),literal_ability_missing={n:sorted(set(r['ability_token'] for r in spawns if r['ability_token'].isdigit() and not r[n+'_found'])) for n in abilities},native_CHARPOS_cases=len(cases),unitinfo_cases=len(constructor),ai_mode_cases=len(mode_cases),cases=len(cases)+len(constructor)+len(mode_cases),mismatches=0,scope='All raw installed scenario source occurrences; native CHARPOS parsing with modeled CString/CRT and final dmSetChar capture, COM fixture values100..104. Not full app/AI executor/server deployment.',server_internal_decisions_excluded=True)
 (OUT/'몬스터연결검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
