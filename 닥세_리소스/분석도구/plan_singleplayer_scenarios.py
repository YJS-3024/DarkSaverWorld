"""Concrete singleplayer design proposal, distinct from recovered original placements.
Retains actual populated SET/POS rows. Missing rosters get deterministic, versioned proposals.
Does not modify Unity, claim original enemy identities, or mark Unity playback tested.
"""
import collections,csv,hashlib,json,math,shutil
from pathlib import Path
from PIL import Image,ImageDraw
from analyze_special_skills import ROOT,OUT,BASE,table,write
from decode_visuals import read_mrc

def read(name):return list(csv.DictReader((OUT/name).open(encoding='utf-8-sig')))
def stable(value):return hashlib.sha256(value.encode()).hexdigest()

def main():
 scenarios=json.loads((OUT/'시나리오통합_전체.json').read_text());abilities={int(t[0]):t for t in table('ability')};terrain=table('TileAttb');manifest=read('몬스터연결_전체자료목록.csv')
 rules=dict(schema_version=1,profile_id='singleplayer_scenario_v1',provenance='authored singleplayer design; not recovered server rules',placement=dict(source_rosters='preserve every populated SET/POS row; flag collisions separately',missing_rosters='include AI slots and condition-required enemy slots400..799; default301 leaders/401 followers only when no literal source identity',staging='max12 active enemies per authored cohort, or available unique cells if fewer; other units wait for next wave; never drop required slots',followers='two authored followers for AI slots ending0, if slots are unused',empty_ai='training fallback400..402/410..412; authored',entry='authored nearest anchor inside largest traversable component; do not preserve isolated prologue actor placement as actual entry',positions='nearest unique ground-category0 TileAttb cell in entry-connected component; source AI AREA center is only an authored anchor',reserved='party entry, position-trigger/reward tiles, sprite origins, existing slots; not complete visual collision proof',terrain_overrides={'22':1,'24':1},terrain_override_scope='authored floor cost1 for unloaded terrain rows22/24;126/127 void blocked; not recovered native collision',confidence='all generated rows require play/balance review; never original initial deployment'),conditions=dict(group='same numeric group AND; groups OR; chosen singleplayer policy',CHARPOS='actor exact XY',CHARPOS3='actor exact firstXY; extra operands retained as metadata and no automatic transfer; explicit local scene transition actions only',CHARPOS2='opaque authored quest flag charpos2:<args>; default false; four legacy conditions need content override',CHARDEAD='death tombstone exists; missing/unspawned is false',DUTY_STEP_TRUE='quest,step pair in persistent reached set',DUTY_STEP_FALSE='quest,step pair absent from persistent reached set',ITEM='party shared item count>=1',ONLY_CHAR='target death record killer_side=party',ONLY_MONSTER='target death record killer_side=enemy; absent target uses event actor',CAUSE_OF_DEATH='exact stored action cause tag; no numeric decoding invented',ALL_MOB_DEAD='initial encounter was spawned and all required enemies dead, no pending wave',rng_algorithm='LCG32 a1664525 c1013904223; output next_u32 modulo101; seed deterministically from save-slot/scenario/run/event; model seed73 is fixture',RANDOM='seeded integer0..100 < clamped threshold, persisted per activation attempt',TICK='mission virtual seconds; one activation per period; no offline catch-up'),events=dict(ONCE='one completed activation per save-slot/scenario, including empty-chest outcome',CHARONCE='one completed activation per persistent party-member GUID/scenario',INF='repeat only after completion, predicate exit/reentry or next tick period',order='source final event definitions, numeric event ID order; clear before regen in same tick',actions='source ordinal order, queue nested activation, no reentrant double execution',unknown='invalid/unsupported commands fail closed into a persisted review flag; no silent reward/clear'),rewards=dict(transaction='consume requirements, RNG outcome, grant item/gold, map changes, quest flags and completion marker committed atomically',GETITEM='grant one unit of source item ID; chosen singleplayer behavior, original native main step does not directly grant',USEITEM='consume one; abort whole transaction if unavailable',GOLD='grant source amount locally; do not reinterpret as item ID based on text',conflict='01 EVENT51 GOLD399 + key message stays flagged; no automatic key399 grant',repeat='idempotency key includes save-slot/scenario/event/actor scope/activation; reward ledger persists',legacy_drop='existing prototype40% quest drop remains distinct from script RANDOM%101'),respawn=dict(scenario='no initial-enemy respawn on ordinary reentry/load; explicit replay creates new run and rewards no campaign unique objects twice',REGEN='authored: revive up to N completely dead10-slot groups, same planned cells if free; move to nearest valid cell if occupied',period='source TICK periods in virtual seconds; delay while scene dialogue/clear in progress',limits='one active instance per unit+generation; no boss/unique reward respawn; no reward from repeated farming waves',clear='cancel all pending waves and commit clear before respawn'),save=dict(unity_wire_shape='JsonUtility integration must use sorted List records for items/units/ledgers/map deltas, not Dictionary; Python dict is logical reference only',migration='future savev4 preserves prototypev3 gold/party/quest/field progress in its own namespace; initialize original-scenario state empty, never infer original scenario clears from defeated counter',version='new scenario subdocument v1; existing prototype version3 remains unchanged; future integration migrates explicitly',keys=['profile_hash','source_hash','save_slot_guid','scenario_id','run_guid','actor_guid','units_and_generations','death_tombstones','inventory','gold','quest_steps','once_charonce','event_activation_ledger','loot_rng_state','virtual_time','next_wave_at','map_deltas','scene_cursor_and_wait','clear_flags'],write='canonical JSON checksum, same-directory temporary file and atomic replace; keep last valid backup; durable fsync is a separate platform concern',load='validate version/checksum/profile; no reroll, reward repetition, instant offline respawn, or defaulting corrupted data to valid new game',checkpoint='after completed reward/event transaction, victory/return, safe field save; not in the middle of unresolved transaction'),timing=dict(scene_DO='virtual unscaled active milliseconds; deliberately enforce delays even where legacy negative actor bypasses',body='native333ms0,1,2,1; simulation clock freezes on explicit pause',dialogue='three lines/page; source mode0 notice expiry>10000ms; persist cursor',prototype='existing field0.12s/step, enemy0.55s, AP1.15/0.85 per second are prototype values, not recovered originals',CHAR_DIR='enable explicit cardinal orientation as authored enhancement; native main step ignored source CHARDIR',MOVEPOSITION='same immediate centered-camera placement as SETPOSITION, per native path'))
 rules['source_manifest_sha256']=hashlib.sha256((OUT/'몬스터연결_전체자료목록.csv').read_bytes()).hexdigest()
 profile_hash=stable(json.dumps(rules,sort_keys=True,ensure_ascii=False));rules['profile_hash']=profile_hash
 plans=[];summary=[];issues=[];unity=[];preserved=0;generated=0;species_defaults=0;empty_sets=[]
 imagefiles={p.stem.lower() for d in (BASE/'설치본_추출/ARC_해제/gameres').glob('char*.arc') for p in d.iterdir() if p.suffix.lower()=='.bmp'}
 for s in scenarios:
  name=s['scenario'];archive=s['archive'];p=ROOT/'MAP'/s['map_file'];m=read_mrc(p);w,h=m['width'],m['height'];known=s['initial_set'];pos=s['initial_positions'];ais=s['ai'];sp=s['scripted_appearances'];blocks=s['blocks'];source_files=[v for v in manifest if v['archive']==archive]
  has_set=any(v['extension']=='.set' for v in source_files)
  if has_set and not known:empty_sets.append(name)
  # Only rows explicitly loaded by TileAttb, ground profile0, finite positive movement costs.
  passable={(x,y) for y in range(1,h-1) for x in range(1,w-1) if (lambda a:(0<=a<len(terrain) and 0<int(terrain[a][3])<99) or a in (22,24))(m['tiles'][y*w+x][3]-128 if m['tiles'][y*w+x][3]>127 else m['tiles'][y*w+x][3])}
  assert passable,(name,'no conservative terrain cells')
  anchors=[(int(r['x']),int(r['y'])) for r in pos if int(r['unit'])==0]
  if not anchors:anchors=[(int(r['x']),int(r['y'])) for r in sp if r['ability_token']=='COM0' and r['file'].lower().endswith('.deo') and r['map_reference']==name]
  desired=anchors[-1] if anchors else (w//2,h-2);remaining=set(passable);components=[]
  while remaining:
   root_cell=min(remaining);part={root_cell};queue=collections.deque([root_cell]);remaining.remove(root_cell)
   while queue:
    x,y=queue.popleft()
    for q in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
     if q in remaining:remaining.remove(q);part.add(q);queue.append(q)
   components.append(part)
  component=max(components,key=lambda part:(len(part),-min(abs(q[0]-desired[0])+abs(q[1]-desired[1]) for q in part)))
  entry=min(component,key=lambda q:(abs(q[0]-desired[0])+abs(q[1]-desired[1]),q[1],q[0]))
  protected={entry}|{(r['x'],r['y']) for b in blocks for r in b['trigger_positions']}|{(r['x'],r['y']) for b in blocks for r in b['object_modifications']}|{(v['x'],v['y']) for v in m['sprites']}
  occupied=set();unit_plans=[];source_units={int(r['unit']) for r in known};ai_by=collections.defaultdict(list)
  for r in ais:ai_by[int(r['unit'])].append(r)
  for r in known:
   unit=int(r['unit']);ability=int(r['ability_id']);ps=json.loads(r['position_candidates']);assert ps
   x,y=int(ps[0]['x']),int(ps[0]['y']);row=dict(unit=unit,ability_id=ability,x=x,y=y,provenance='installed SET/POS textual pair',original_initial_runtime_verified=False,source_refs=[f"{archive}/{r['file']}:{r['line']}"]+[f"{archive}/{v['file']}:{v['line']}" for v in ps],species_basis='SET literal',spawn_wave=0,position_basis='POS exact; retained even if different from conservative design terrain',review=[])
   if (x,y) in occupied:row['review'].append('source position overlap preserved')
   if (x,y) not in component:row['review'].append('source position outside conservative entry component; needs scene/terrain review')
   occupied.add((x,y));unit_plans.append(row);preserved+=1
  if not known:
   units=set(ai_by)
   for b in blocks:
    if not b['effective']:continue
    for c in b['conditions']:
     if c['kind']=='CHARDEAD' and c['args'] and c['args'][0].isdigit() and 400<=int(c['args'][0])<800:units.add(int(c['args'][0]))
   if not units:units={400,401,402,410,411,412}
   for leader in list(sorted(units)):
    if leader%10==0:
     for follower in (leader+1,leader+2):units.add(follower)
   stage=0;stage_cells=set();cohort_capacity=min(12,len(component-protected));assert cohort_capacity>0,(name,'no spawn cells')
   if cohort_capacity<4:issues.append(dict(scenario=name,kind='small_component_manual_review',reference=str(cohort_capacity),detail='Too few cells for convincing battle; source terrain/gates or authored arena needs Unity review'))
   for unit in sorted(units):
    typed=[r for r in sp if int(r['unit'])==unit and r['ability_token'].isdigit() and int(r['ability_token']) in abilities]
    ability=int(typed[0]['ability_token']) if typed else (301 if unit%10==0 else 401);basis='same archive/slot scripted identity, reused as authored initial design' if typed else 'authored default wild-dog301/stray-dog401; no source species evidence';refs=[r['appearance_id'] for r in typed]
    profile=ai_by.get(unit) or ai_by.get(unit//10*10) or []
    area=next((r for r in profile if r['selector']=='AREA'),None)
    if area:
     fields=[int(v.strip()) for v in area['selector_args'].split(',')];anchor=tuple(fields[:2]);refs.append(area['ai_id']);position_basis='AI AREA center used only as authored placement anchor'
    else:anchor=(w//2,h//3);position_basis='authored upper-map fallback anchor'
    if unit%10:anchor=(anchor[0]+(-1 if unit%2 else 1),anchor[1]+unit%3)
    if len(stage_cells)>=cohort_capacity:stage+=1;stage_cells=set()
    candidates=component-protected-stage_cells
    assert candidates,(name,unit,'capacity')
    x,y=min(candidates,key=lambda q:(abs(q[0]-anchor[0])+abs(q[1]-anchor[1]),q[1],q[0]));occupied.add((x,y));stage_cells.add((x,y));review=['authored species/position; not original initial deployment','ground category0 conservative placement; visual footprint/balance needs Unity play']
    if typed and len({r['ability_token'] for r in typed})>1:review.append('multiple script species candidates: deterministic first occurrence selected')
    if not typed:species_defaults+=1
    row=dict(unit=unit,ability_id=ability,x=x,y=y,spawn_wave=stage,provenance='authored singleplayer supplement',original_initial_runtime_verified=False,source_refs=refs,species_basis=basis,position_basis=position_basis,review=review);unit_plans.append(row);generated+=1
  assert len({r['unit'] for r in unit_plans})==len(unit_plans),(name,'duplicate unit ID')
  if not known:assert len({(r['spawn_wave'],r['x'],r['y']) for r in unit_plans})==len(unit_plans) and all((r['x'],r['y']) in component-protected for r in unit_plans)
  for r in unit_plans:
   assert r['ability_id'] in abilities
   display=abilities[r['ability_id']][1];r['display_id']=int(display);r['name_tokens']=[v for v in abilities[r['ability_id']] if not v.lstrip('-').isdigit()];r['image_exists']=(display+'r').lower() in imagefiles
   assert 0<=r['x']<w and 0<=r['y']<h
   if not r['image_exists']:issues.append(dict(scenario=name,kind='image_missing',reference=str(r['ability_id']),detail='No exact displayR.bmp candidate'))
   r.update(scenario=name,archive=archive,profile_hash=profile_hash)
   plans.append(r)
  summary.append(dict(scenario=name,archive=archive,source_status='populated SET' if known else 'empty SET' if has_set else 'SET absent',source_units=len(known),planned_units=len(unit_plans),generated_units=0 if known else len(unit_plans),spawn_cohorts=max(r['spawn_wave'] for r in unit_plans)+1,initial_cohort_units=sum(r['spawn_wave']==0 for r in unit_plans),entry_x=entry[0],entry_y=entry[1],component_cells=len(component),source_script_count=len(sp),plan_status='data-reviewed; Unity play/balance not run'))
  if name in ('01','201','324'):
   img=Image.new('RGB',(w*8,h*8),(40,40,40));d=ImageDraw.Draw(img)
   for x,y in component:d.rectangle((x*8,y*8,x*8+7,y*8+7),fill=(105,130,105))
   for x,y in protected:
    if 0<=x<w and 0<=y<h:d.rectangle((x*8,y*8,x*8+7,y*8+7),fill=(170,135,65))
   for r in unit_plans:
    if r['spawn_wave']!=0:continue
    d.rectangle((r['x']*8+1,r['y']*8+1,r['x']*8+6,r['y']*8+6),fill=(230,70,70))
   d.rectangle((entry[0]*8,entry[1]*8,entry[0]*8+7,entry[1]*8+7),fill=(60,130,255));review=Image.new('RGB',(max(320,w*8),h*8+30),(245,245,245));review.paste(img,(0,30));ImageDraw.Draw(review).text((4,4),f'DESIGN {name} / initial wave / NOT UNITY',fill=(0,0,0));review.save(OUT/f'싱글판_배치검토_{name}.png')
  for stage in ('entry_intro','combat_and_movement','loot_and_once','save_reload','victory_and_reentry'):
   unity.append(dict(scenario=name,stage=stage,status='NOT_RUN',blocker='Unity Editor unavailable; existing prototype has no104-scenario loader/event interpreter',expected={'entry_intro':f"load{name}; initial cohort {sum(r['spawn_wave']==0 for r in unit_plans)} actors, correct source/override labels,640x480 logical viewport, intro once and no unit duplication",'combat_and_movement':'reachable movement, occupancy uniqueness, wave transitions only after prior cohort death, pause freezes mission clock/body frame/AI, test30/60/120fps','loot_and_once':'visit trigger coordinates; consume prerequisite atomically, grant exact source item/gold, once/charonce survives repeated entry and reload; compare map changes','save_reload':'roundtrip units/deaths/generations/RNG/ledger/map deltas/scene cursor; no reward replay or offline wave catchup; corrupt primary recovers backup','victory_and_reentry':'clear cancels pending waves, one victory transaction, return to hub, reentry preserves unique loot/quest flags; explicit replay creates new run'}[stage],proof_required='Unity test XML + Editor/player log + screenshot + before/after scenario save hash'))
 assert preserved==610 and len(summary)==104 and sum(r['source_status']=='populated SET' for r in summary)==16
 assert sum(r['source_status']=='SET absent' for r in summary)==85 and len(empty_sets)==3
 (OUT/'싱글판_규칙프로필.json').write_text(json.dumps(rules,ensure_ascii=False,indent=2)+'\n');(OUT/'싱글판_배치보완안.json').write_text(json.dumps(dict(profile_hash=profile_hash,source_manifest_sha256=rules['source_manifest_sha256'],scenarios=summary,units=plans),ensure_ascii=False,indent=2)+'\n')
 write('싱글판_시나리오보완목록.csv',summary);write('싱글판_배치보완안.csv',[{k:json.dumps(v,ensure_ascii=False) if isinstance(v,(list,dict)) else v for k,v in r.items()} for r in plans]);write('싱글판_Unity플레이검증계획.csv',unity)
 if issues:write('싱글판_배치보완_주의.csv',issues)
 else:(OUT/'싱글판_배치보완_주의.csv').write_text('scenario,kind,reference,detail\n',encoding='utf-8-sig')
 runtime=BASE.parents[0]/'Assets/_Game/Scripts';editor_candidates=[shutil.which(n) for n in ('Unity','unity-editor','UnityHub')];editor_known=[str(p) for parent in (Path('/opt/Unity/Hub/Editor'),Path('/opt/unity/Editor')) if parent.exists() for p in parent.glob('**/Unity') if p.is_file()]
 audit=dict(editor_on_path=editor_candidates,editor_known_paths=editor_known,restricted_root_path_not_inspected=True,project_version_file=(BASE.parents[0]/'ProjectSettings/ProjectVersion.txt').exists(),prototype_save_version=3,current_missing_features=['scenario ID/run GUID in save','event completion/loot transaction ledger','persistent RNG state','per-unit deaths and generations','map mutation persistence','104-scenario deployment loader','original script predicate/action interpreter'],existing_prototype_limits=['hardcoded3 enemies; small generated field/battle grid','unscaled real-time/AP cadence differs from proposed paused virtual clock','JSON WriteAllText lacks atomic replace/checksum/backup','JsonUtility cannot directly persist dictionary-shaped proposed ledgers; serialize typed list records'],scope='static source audit and environment availability; no Unity compilation/PlayMode')
 (OUT/'싱글판_Unity실행점검.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n')
 result=dict(scenarios=104,populated_rosters=16,missing_set_scenarios=85,empty_set_scenarios=empty_sets,supplemented_scenarios=88,preserved_source_rows=preserved,generated_rows=generated,default_species_rows=species_defaults,total_rows=len(plans),max_authored_cohorts=max(r['spawn_cohorts'] for r in summary if r['source_status']!='populated SET'),issues=dict(collections.Counter(r['kind'] for r in issues)),unity_cases_planned=len(unity),unity_cases_run=0,profile_hash=profile_hash,scope='authored deterministic singleplayer data proposal and static review; not recovered original server data, Unity implementation or balanced gameplay')
 (OUT/'싱글판_보완분석.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
