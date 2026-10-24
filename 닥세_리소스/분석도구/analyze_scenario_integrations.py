"""Scenario-scoped evidence graph; source intent is distinct from native execution.
No server rule reconstruction, implicit map alias, or automatic reward/spawn repair.
"""
import collections,csv,hashlib,json,re,struct,subprocess
from analyze_special_skills import BASE,ROOT,OUT,table,write
from decode_visuals import read_mrc
from analyze_script_executor import ScriptHarness
from analyze_packet_requests import Harness

def rows(name):
 return list(csv.DictReader((OUT/name).open(encoding='utf-8-sig')))
def integer(s):
 try:return int(s)
 except ValueError:return None
def dump(value):return json.dumps(value,ensure_ascii=False)
def parse_file(path,archive):
 blocks=[];current=None
 def new(event,start):
  return dict(id=f'{archive}/{path.name}:{start}',file=path.name,event=event,start_line=start,conditions=[],declarations=[],actions=[],notes=[],effective=True)
 if path.suffix.lower()!='.evt':current=new(None,1)
 for number,line in enumerate(path.read_bytes().decode('cp949').splitlines(),1):
  code=line.split(';',1)[0].strip();tokens=code.split()
  if not tokens:continue
  if tokens[0]=='EVENT':
   if current:blocks.append(current)
   current=new(integer(tokens[1]),number);continue
  if current is None:continue
  entry=dict(line=number,raw=line,tokens=tokens)
  if tokens[0].isdigit() and path.suffix.lower()=='.evt':
   if len(tokens)>1:current['conditions'].append(dict(entry,group=int(tokens[0]),kind=tokens[1],args=tokens[2:]))
   else:current['notes'].append(dict(entry,kind='bare numeric source line'))
  elif tokens[0] in ('COMMANDER','CHAR','ALL','INF','ONCE','CHARONCE'):
   current['declarations'].append(entry)
  else:current['actions'].append(entry)
 if current:blocks.append(current)
 if path.suffix.lower()=='.evt':
  last={b['event']:b['id'] for b in blocks}
  for b in blocks:b['effective']=last[b['event']]==b['id']
 return blocks

def main():
 root=BASE/'설치본_추출/ARC_해제/MAP'
 native_bodies={(r['archive'],r['file'],int(r['event'])):r for r in rows('맵_이벤트_클라이언트본문.csv')}
 maps={p.stem.lower():(p,read_mrc(p)) for p in (ROOT/'MAP').iterdir() if p.suffix.lower()=='.mrc'}
 dispatch={r['command'] for r in rows('이벤트_실행기_명령분기.csv')}
 sets=rows('몬스터연결_SET편성.csv');positions=rows('몬스터연결_POS좌표.csv');spawns=rows('몬스터연결_등장.csv');ais=rows('몬스터연결_AI.csv')
 item_tables={name:{int(t[0]):t for t in table(name)} for name in ('itemtbl','itemtbl1')}
 source_inventory=rows('몬스터연결_전체자료목록.csv')
 for record in source_inventory:
  assert hashlib.sha256((root/record['archive']/record['file']).read_bytes()).hexdigest()==record['sha256'],record
 scenarios=[];overview=[];event_rows=[];rewards=[];locations=[];issues=[];actors=[];objects=[];timelines=[];condition_rows=[];acquisitions=[]
 def issue(archive,kind,ref,detail):issues.append(dict(archive=archive,kind=kind,reference=ref,detail=detail))
 for folder in sorted(root.iterdir()):
  if not folder.is_dir():continue
  archive=folder.name;scenario=archive.removesuffix('set.arc');m=maps.get(scenario.lower());brief=[]
  files=[r for r in source_inventory if r['archive']==archive];blocks=[]
  for p in sorted(folder.iterdir()):
   if p.suffix.lower() in ('.evt','.deo','.dee'):blocks+=parse_file(p,archive)
   if p.suffix.lower()=='.srf':brief=p.read_bytes().decode('cp949').splitlines()
  if not any(r['extension']=='.deo' for r in files):issue(archive,'opening_source_absent',scenario,'No DEO file in this archive; may be intentionally absent')
  if not any(r['extension']=='.dee' for r in files):issue(archive,'ending_source_absent',scenario,'No DEE file in this archive; may be intentionally absent')
  if not m:issue(archive,'map_missing',scenario,'No exact scenario-name MRC; no alias invented')
  if not any(r['extension']=='.set' for r in files):issue(archive,'set_missing',scenario,'No installed SET initial roster; scripted appearances/AI do not establish all initial actors')
  if not any(r['extension']=='.pos' for r in files):issue(archive,'pos_missing',scenario,'No installed POS initial coordinate table')
  sprite_lookup=collections.defaultdict(list)
  if m:
   for index,sprite in enumerate(m[1]['sprites']):
    row=dict(archive=archive,map=m[0].name,sprite_ordinal=index,**sprite);objects.append(row);sprite_lookup[sprite['x'],sprite['y']].append(row)
  def position_detail(map_id,x,y,basis):
   ref=maps.get(map_id.lower());inside=bool(ref and 0<=x<ref[1]['width'] and 0<=y<ref[1]['height'])
   return dict(map=map_id,x=x,y=y,map_basis=basis,in_bounds=inside if ref else None,tile_raw=list(ref[1]['tiles'][y*ref[1]['width']+x]) if inside else None,sprites=[dict(ordinal=i,**s) for i,s in enumerate(ref[1]['sprites']) if s['x']==x and s['y']==y] if ref else [])
  for block in blocks:
   native=native_bodies.get((archive,block['file'],block['event'])) if block['event'] is not None else None
   block['native_body_valid']=native['valid']=='1' if native and block['effective'] else None
   if block['effective'] and block['event'] is not None:assert native is not None,(archive,block['id'])
   if not block['effective']:issue(archive,'duplicate_event_overwritten',block['id'],'Later definition of same numeric event ID is used by installed event loader')
   if native and block['effective'] and native['valid']!='1':issue(archive,'native_event_invalid',block['id'],'Native parsed event definition is invalid')
   group=collections.defaultdict(list)
   for c in block['conditions']:
    group[c['group']].append(c)
    condition_rows.append(dict(archive=archive,block_id=block['id'],effective=block['effective'],group=c['group'],line=c['line'],kind=c['kind'],args=dump(c['args'])))
   block['condition_groups']=[dict(group=k,clauses=v,combination_semantics='raw source grouping; server evaluator not reconstructed') for k,v in group.items()]
   triggers=[]
   for c in block['conditions']:
    # CHARPOS2 is not a two-coordinate condition; CHARPOS3 retains opaque extra operands.
    if c['kind'] in ('CHARPOS','CHARPOS3') and len(c['args'])>=2 and all(integer(v) is not None for v in c['args'][:2]):
     x,y=map(int,c['args'][:2]);triggers.append(dict(condition_line=c['line'],condition_kind=c['kind'],group=c['group'],coordinate_semantics='source position candidate; extra operands remain opaque',**position_detail(scenario,x,y,'scenario-name candidate for EVT')))
   block['trigger_positions']=triggers
   scene=scenario;scene_basis='scenario-name candidate';stack=[];pending=None;random_line=None;modifications=[];block_rewards=[];branch_errors=[]
   for action in block['actions']:
    t=action['tokens'];cmd=t[0]
    if cmd=='LOADSCENE':scene=t[1];scene_basis='explicit LOADSCENE'
    if cmd=='RANDOM':random_line=dict(line=action['line'],args=t[1:])
    if cmd in ('TEST','TESTNOT'):pending=dict(command=cmd,line=action['line'],preceding_RANDOM=random_line)
    if cmd=='(':
     stack.append(pending or dict(command='unassociated block',line=action['line']));pending=None
    if cmd==')':
     if stack:stack.pop()
     else:branch_errors.append(action['line'])
    action.update(scene=scene,scene_basis=scene_basis,branch_path=[dict(b) for b in stack],main_dispatch_reference=cmd in dispatch)
    if cmd in ('GETITEM','GOLD','USEITEM'):
     arg=integer(t[1]) if len(t)>1 else None
     reward=dict(archive=archive,block_id=block['id'],event=block['event'],effective=block['effective'],line=action['line'],command=cmd,value=arg,branch_path=dump(action['branch_path']),trigger_positions=dump(triggers),native_scope='GOLD -> outgoing request 3A, not direct balance mutation' if cmd=='GOLD' else 'USEITEM -> outgoing request 49, not direct inventory mutation' if cmd=='USEITEM' else 'source operation; not handled by main scene-step path tested here',itemtbl_found='',itemtbl_text='',itemtbl1_found='',itemtbl1_text='')
     if cmd in ('GETITEM','USEITEM'):
      for name,tbl in item_tables.items():
       it=tbl.get(arg);reward[name+'_found']=it is not None;reward[name+'_text']=dump([v for v in it if not re.fullmatch(r'[+-]?\d+',v)]) if it else ''
      if arg is not None and not any(arg in tbl for tbl in item_tables.values()):issue(archive,'reward_item_missing',f"{block['id']}:{action['line']}",f'{cmd} {arg}: ID absent in both item tables')
     block_rewards.append(reward);rewards.append(reward)
    if cmd in ('CHANGESPRITE','CHANGETILE','CHANGETILE2') and len(t)>2 and integer(t[1]) is not None and integer(t[2]) is not None:
     x,y=map(int,t[1:3]);modifications.append(dict(line=action['line'],command=cmd,args=t[1:],**position_detail(scene,x,y,scene_basis)))
    timelines.append(dict(archive=archive,block_id=block['id'],event=block['event'],effective=block['effective'],line=action['line'],command=cmd,args=dump(t[1:]),scene=scene,branch_path=dump(action['branch_path']),main_dispatch_reference=cmd in dispatch))
   if stack or branch_errors:issue(archive,'unbalanced_branch_source',block['id'],dump(dict(open_blocks=stack,extra_closings=branch_errors)))
   chest=any('상자' in a['raw'] for a in block['actions']);positive=[r for r in block_rewards if r['command'] in ('GETITEM','GOLD')]
   if block_rewards or chest:
    acquisitions.append(dict(archive=archive,block_id=block['id'],event=block['event'],effective=block['effective'],native_body_valid=block['native_body_valid'],source_kind='chest text' if chest else 'position-associated source event' if triggers else 'scene or non-position event',declarations=dump([d['tokens'][0] for d in block['declarations']]),trigger_positions=dump(triggers),object_targets=dump(modifications),source_conditions=dump(block['condition_groups']),rewards=dump(block_rewards),messages=dump([dict(line=a['line'],text=a['raw']) for a in block['actions'] if a['tokens'][0] in ('MESSAGE','MASSAGE','MESSGE')]),repeat_scope='ONCE/CHARONCE/INF preserved as source declarations; persistence rules not inferred'))
   block.update(reward_operations=block_rewards,object_modifications=modifications,chest_text_present=chest)
   if chest and not positive:issue(archive,'chest_text_without_reward_opcode',block['id'],'Source mentions chest but contains no GETITEM/GOLD; may be intentionally empty, do not invent a reward')
   if positive and not triggers:issue(archive,'reward_without_position_condition',block['id'],'Reward is scene/other-condition scoped; no CHARPOS/CHARPOS3 trigger tile to assign')
   if '열쇠를 얻었습니다' in '\n'.join(a['raw'] for a in block['actions']) and any(r['command']=='GOLD' for r in block_rewards):issue(archive,'reward_text_opcode_conflict',block['id'],'Text says key acquired but source also contains GOLD; source intent and native request must remain distinct')
   for kind,coords in [('condition_position',triggers),('object_mutation_position',modifications)]:
    for c in coords:
     locations.append(dict(archive=archive,block_id=block['id'],effective=block['effective'],role=kind,source_line=c.get('condition_line',c.get('line')),map=c['map'],x=c['x'],y=c['y'],in_bounds=c['in_bounds'],tile_raw=dump(c['tile_raw']),sprite_matches=dump(c['sprites']),related_rewards=dump([dict(command=r['command'],value=r['value'],line=r['line'],branch_path=json.loads(r['branch_path'])) for r in block_rewards])))
     if c['in_bounds'] is False:issue(archive,'coordinate_outside_map',block['id'],f"{kind}: {c['map']} ({c['x']},{c['y']})")
   event_rows.append(dict(archive=archive,block_id=block['id'],file=block['file'],event=block['event'],effective=block['effective'],native_body_valid=block['native_body_valid'],declarations=dump([d['tokens'][0] for d in block['declarations']]),conditions=dump(block['condition_groups']),trigger_positions=dump(triggers),reward_operations=dump(block_rewards),object_modifications=dump(modifications),chest_text_present=chest,action_count=len(block['actions'])))
  a_sets=[r for r in sets if r['archive']==archive];a_pos=[r for r in positions if r['archive']==archive];a_spawns=[r for r in spawns if r['archive']==archive];a_ai=[r for r in ais if r['archive']==archive]
  if not a_sets and any(r['extension']=='.set' for r in files):issue(archive,'empty_set_roster',scenario,'SET exists but contains no numeric roster rows; cannot treat this as a recovered deployment')
  slots=sorted({int(r['unit']) for r in a_sets+a_pos+a_spawns+a_ai})
  for slot in slots:
   se=[r for r in a_sets if int(r['unit'])==slot];po=[r for r in a_pos if int(r['unit'])==slot];sp=[r for r in a_spawns if int(r['unit'])==slot];ai=[r for r in a_ai if int(r['unit'])==slot]
   known_types=sorted({int(r['ability_id']) for r in se}|{int(r['ability_token']) for r in sp if r['ability_token'].isdigit()})
   complete=bool(se and po);scripted=bool(sp);status='SET_POS plus scripted appearances' if complete and scripted else 'SET_POS textual deployment' if complete else 'scripted appearances only' if scripted else 'position only' if po else 'AI only; species and spawn unavailable'
   actors.append(dict(archive=archive,unit=slot,evidence=status,set_lines=dump([dict(file=r['file'],line=r['line'],ability=r['ability_id'],extra=r['extra_raw']) for r in se]),pos_candidates=dump([dict(file=r['file'],line=r['line'],x=r['x'],y=r['y']) for r in po]),appearance_refs=dump([r['appearance_id'] for r in sp]),ability_candidates=dump(known_types),dynamic_type=any(r['ability_token'].startswith('COM') for r in sp),ai_refs=dump([r['ai_id'] for r in ai])))
   if ai and not complete and not scripted:issue(archive,'ai_slot_without_spawn_or_type',str(slot),'AI record exists but no SET+POS pair or scripted CHARPOS for this slot')
   if len(known_types)>1:issue(archive,'slot_type_reuse',str(slot),f'Types {known_types}: source occurrences are not simultaneous units')
  effective=[b for b in blocks if b['effective']];a_rewards=[r for r in rewards if r['archive']==archive and r['effective']];a_locations=[r for r in locations if r['archive']==archive and r['effective']]
  scenarios.append(dict(archive=archive,scenario=scenario,title=brief[0] if brief and not brief[0].startswith(';') else None,map_file=m[0].name if m else None,map_size=[m[1]['width'],m[1]['height']] if m else None,source_files=files,blocks=blocks,actor_slots=[r for r in actors if r['archive']==archive],map_objects=[r for r in objects if r['archive']==archive],issues=[r for r in issues if r['archive']==archive],initial_set=a_sets,initial_positions=a_pos,scripted_appearances=a_spawns,ai=a_ai,interpretation='Evidence graph; no global execution order, server trigger semantics, automatic grants, or concurrent actor count inferred'))
  overview.append(dict(archive=archive,scenario=scenario,title=scenarios[-1]['title'],map_file=scenarios[-1]['map_file'],set_rows=len(a_sets),pos_rows=len(a_pos),scripted_appearances=len(a_spawns),ai_rows=len(a_ai),event_definitions=sum(b['event'] is not None for b in effective),scene_blocks=sum(b['event'] is None for b in effective),reward_operations=len(a_rewards),reward_events=len({r['block_id'] for r in a_rewards if r['event'] is not None}),position_linked_reward_events=len({r['block_id'] for r in a_rewards if json.loads(r['trigger_positions'])}),chest_text_events=sum(b['chest_text_present'] for b in effective),map_sprites=len(m[1]['sprites']) if m else 0,issues=dump(dict(collections.Counter(r['kind'] for r in issues if r['archive']==archive)))))
 # Reconcile all source occurrences with prior independent extractors.
 assert sum(r['scripted_appearances'] for r in overview)==len(spawns)
 assert sum(r['set_rows'] for r in overview)==len(sets)
 assert sum(r['pos_rows'] for r in overview)==len(positions)
 assert len(condition_rows)==2099
 assert sum(r['event_definitions'] for r in overview)==len(native_bodies)
 assert collections.Counter(r['command'] for r in rewards)==collections.Counter(GETITEM=159,GOLD=26,USEITEM=5)
 for name,data in [('시나리오통합_목록.csv',overview),('시나리오통합_이벤트.csv',event_rows),('시나리오통합_획득지점.csv',acquisitions),('시나리오통합_획득소비.csv',rewards),('시나리오통합_타일연결.csv',locations),('시나리오통합_편성좌표.csv',actors),('시나리오통합_배치오브젝트.csv',objects),('시나리오통합_연출순서.csv',timelines),('시나리오통합_조건그룹.csv',condition_rows),('시나리오통합_누락점검.csv',issues)]:write(name,data)
 (OUT/'시나리오통합_전체.json').write_text(dump(scenarios)+'\n')
 # Native dispatcher boundary and actual GOLD request serializer, with external API capture.
 checks=[]
 # Source-grounded integration fixtures cover the distinctions that can corrupt a singleplayer port.
 one=next(b for s in scenarios if s['archive']=='01set.arc' for b in s['blocks'] if b['file']=='01.evt' and b['event']==1)
 assert {(p['x'],p['y']) for p in one['trigger_positions']}=={(6,15),(5,16),(5,15)}
 assert one['object_modifications'][0]['sprites'][0]['index']==0 and (one['object_modifications'][0]['x'],one['object_modifications'][0]['y'])==(5,15)
 assert one['reward_operations'][0]['command']=='GOLD' and one['reward_operations'][0]['value']==100
 assert json.loads(one['reward_operations'][0]['branch_path'])[0]['preceding_RANDOM']['args']==['50']
 key=next(b for s in scenarios if s['archive']=='01set.arc' for b in s['blocks'] if b['event']==60)
 assert [(r['command'],r['value']) for r in key['reward_operations']]==[('USEITEM',399),('GETITEM',303)]
 assert any(c['kind']=='ITEM' and c['args']==['399'] for c in key['conditions'])
 ten=next(b for s in scenarios if s['archive']=='10set.arc' for b in s['blocks'] if b['event']==51)
 assert not ten['trigger_positions'] and sum(c['kind']=='CHARPOS2' for c in ten['conditions'])==4
 assert sum(not b['effective'] for s in scenarios for b in s['blocks'])==3
 structural_checks=4
 for command in ('GETITEM 300','USEITEM 399','ONCE','CHARONCE','REGEN 2','WARPCHAR 15 10','GOLD 100'):
  h=ScriptHarness();h.lines([command,'END']);got=[]
  def capture():got.append(('gold',h.args(1)[0]));h.ret()
  def consume():got.append(('use',h.args(1)[0]));h.ret()
  h.stubs[0x1000e340]=capture;h.stubs[0x1000e390]=consume
  try:cursor=h.step()
  except Exception as exc:raise RuntimeError(command) from exc
  assert cursor==2 and got==([('gold',100)] if command=='GOLD 100' else [('use',399)] if command=='USEITEM 399' else []),(command,cursor,got)
  checks.append(dict(case=command,result='native scene step consumes line; GOLD/USEITEM request helpers captured; remaining listed source operations consume a line in this dispatcher',match=True))
 for item_id in sorted({r['value'] for r in rewards if r['command']=='USEITEM'}):
  h=Harness('GameCtl.dll');h.stubs={0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x1000e1e0:h.send};h.run(0x1000e390,(item_id,));assert h.sent==[b'\x49'+struct.pack('<H',item_id)]
  checks.append(dict(case=f'useitem_request_{item_id}',result='49 followed by little-endian item short; final send captured',match=True))
 for amount in sorted({r['value'] for r in rewards if r['command']=='GOLD'}):
  h=Harness('GameCtl.dll');h.stubs={0x10014634:h.alloc,0x1001461c:lambda:h.ret(),0x1000e1e0:h.send};h.run(0x1000e340,(amount,))
  assert h.sent==[b'\x3a'+struct.pack('<I',amount)]
  checks.append(dict(case=f'gold_request_{amount}',result='3A followed by little-endian amount dword; final send captured; no local balance mutation',match=True))
 write('시나리오통합_원본명령대조.csv',checks)
 (OUT/'시나리오통합_근거.asm.txt').write_text('\n'.join(subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(ROOT/'GameCtl.dll')],text=True) for a,b in [(0x10013891,0x100138de),(0x1000e340,0x1000e3d4)]))
 result=dict(scenarios=len(scenarios),effective_event_definitions=sum(r['event_definitions'] for r in overview),source_event_definitions=sum(b['event'] is not None for s in scenarios for b in s['blocks']),scene_blocks=sum(r['scene_blocks'] for r in overview),condition_rows=len(condition_rows),source_reward_counts=dict(collections.Counter(r['command'] for r in rewards)),effective_reward_counts=dict(collections.Counter(r['command'] for r in rewards if r['effective'])),position_linked_reward_blocks=len({r['block_id'] for r in rewards if r['effective'] and json.loads(r['trigger_positions'])}),tile_links=len(locations),acquisition_source_blocks=len(acquisitions),effective_acquisition_source_blocks=sum(r['effective'] for r in acquisitions),structural_checks=structural_checks,actor_slot_records=len(actors),map_sprite_records=len(objects),issues=dict(collections.Counter(r['kind'] for r in issues)),native_cases=len(checks),native_mismatches=0,scope='Installed source evidence integration and selected isolated native paths; not original Windows gameplay or server condition/reward decisions',source_files_hash_verified=len(source_inventory),source_manifest_sha256=hashlib.sha256((OUT/'몬스터연결_전체자료목록.csv').read_bytes()).hexdigest())
 (OUT/'시나리오통합검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(dump(result))
if __name__=='__main__':main()
