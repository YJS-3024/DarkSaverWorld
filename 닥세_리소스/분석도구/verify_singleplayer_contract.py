"""Executable reference contract for authored local event/reward/save rules.
This is a Python analysis model, not Unity runtime code or original-server behavior.
"""
import copy,hashlib,json,os,tempfile
from pathlib import Path
from analyze_special_skills import OUT,write

def canonical(v):return json.dumps(v,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()
def profile():return json.loads((OUT/'싱글판_규칙프로필.json').read_text())
def new_state():return dict(profile_hash=profile()['profile_hash'],source_hash=profile()['source_manifest_sha256'],version=1,profile='singleplayer_scenario_v1',scenario='01',run='run1',gold=0,items={},steps=[],ledger={},tombstones={},rng=73,spawned=True,wave_pending=False,required=[400,410],time=0,generations={},clear=False,map_deltas={})
def predicate(kind,args,context,state):
 a=[int(v) for v in args]
 if kind in ('CHARPOS','CHARPOS3'):return context['cell']==a[:2]
 if kind=='CHARPOS2':return context.get('overrides',{}).get('charpos2:'+':'.join(map(str,a)),False)
 if kind=='CHARDEAD':return str(a[0]) in state['tombstones']
 if kind=='ITEM':return state['items'].get(str(a[0]),0)>0
 if kind in ('DUTY_STEP_TRUE','DUTY_STEP_FALSE'):return (a[:2] in state['steps'])==(kind=='DUTY_STEP_TRUE')
 if kind in ('ONLY_CHAR','ONLY_MONSTER'):
  record=state['tombstones'].get(str(a[0] if a else context['actor']),{});return record.get('killer_side')==('party' if kind=='ONLY_CHAR' else 'enemy')
 if kind=='CAUSE_OF_DEATH':return context.get('cause')==a[0]
 if kind=='ALL_MOB_DEAD':return state['spawned'] and bool(state['required']) and not state['wave_pending'] and all(str(v) in state['tombstones'] for v in state['required'])
 if kind=='RANDOM':return context.get('roll',101)<min(101,max(0,a[0]))
 if kind=='TICK':return state['time']>=a[0] and context.get('tick_window_unused',False)
 raise ValueError('unknown condition')
def groups(clauses,context,state):return any(all(predicate(k,a,context,state) for k,a in group) for group in clauses)
def roll(state):state['rng']=(1664525*state['rng']+1013904223)&0xffffffff;return state['rng']%101

def commit(state,key,actions):
 if key in state['ledger']:return state,False
 work=copy.deepcopy(state);outcome=[]
 for action in actions:
  op,*a=action
  if op=='USEITEM':
   item=str(a[0])
   if work['items'].get(item,0)<1:raise ValueError('missing item')
   work['items'][item]-=1
  elif op=='GETITEM':work['items'][str(a[0])]=work['items'].get(str(a[0]),0)+1
  elif op=='GOLD':
   if a[0]<0:raise ValueError('negative grant')
   work['gold']+=a[0]
  elif op=='STEP':
   if list(a) not in work['steps']:work['steps'].append(list(a))
  elif op=='ROLL_REWARD':
   value=roll(work);outcome.append(value)
   if value<a[0]:work['items'][str(a[1])]=work['items'].get(str(a[1]),0)+1
  elif op=='MAP':work['map_deltas'][a[0]]=a[1]
  elif op=='CLEAR':work['clear']=True;work['wave_pending']=False
  else:raise ValueError('unsupported action')
 work['ledger'][key]=dict(outcome=outcome);return work,True

def event_key(state,event,mode,actor='party-guid',activation=1):
 scope='campaign' if mode=='ONCE' else actor if mode=='CHARONCE' else state['run']+':'+str(activation)
 return f"{state['scenario']}:{event}:{scope}"
def envelope(state):return dict(payload=state,sha256=hashlib.sha256(canonical(state)).hexdigest())
def save(path,state,fail_before_replace=False):
 tmp=Path(str(path)+'.tmp');backup=Path(str(path)+'.bak')
 try:
  with tmp.open('wb') as f:f.write(canonical(envelope(state)));f.flush();os.fsync(f.fileno())
  if fail_before_replace:raise OSError('injected failure before replace')
  if path.exists():
   try:decode(path.read_bytes())
   except (ValueError,KeyError):pass
   else:
    bt=Path(str(backup)+'.tmp')
    with bt.open('wb') as f:f.write(path.read_bytes());f.flush();os.fsync(f.fileno())
    os.replace(bt,backup)
  os.replace(tmp,path)
 finally:
  if tmp.exists():tmp.unlink()
def decode(raw):
 obj=json.loads(raw);state=obj['payload']
 if state['version']!=1:raise RuntimeError('unsupported save version')
 if state['profile']!='singleplayer_scenario_v1' or state['profile_hash']!=profile()['profile_hash']:raise RuntimeError('profile mismatch')
 if state['source_hash']!=profile()['source_manifest_sha256']:raise RuntimeError('source mismatch')
 if hashlib.sha256(canonical(state)).hexdigest()!=obj['sha256']:raise ValueError('checksum')
 return state
def load(path):
 errors=[]
 for candidate in (path,Path(str(path)+'.bak')):
  try:return decode(candidate.read_bytes())
  except (OSError,ValueError,KeyError) as exc:errors.append(str(exc))
 raise ValueError('no valid save: '+repr(errors))
def regen(state,units):
 work=copy.deepcopy(state)
 if work['clear']:return work,[]
 spawned=[]
 for unit in units:
  key=str(unit)
  if key not in work['tombstones']:continue
  work['tombstones'].pop(key);work['generations'][key]=work['generations'].get(key,0)+1;spawned.append(unit)
 return work,spawned

def main():
 results=[]
 def check(name,condition):assert condition,name;results.append(dict(case=name,match=True,scope='authored Python reference contract; not Unity/original native'))
 s=new_state();ctx=dict(cell=[6,15],actor=7,cause=101,roll=50,tick_window_unused=True)
 check('unspawned_is_not_dead',not predicate('CHARDEAD',[400],ctx,s))
 check('same_group_AND',not groups([[('CHARPOS',[6,15]),('ITEM',[399])]],ctx,s))
 check('groups_OR',groups([[('ITEM',[399])],[('CHARPOS',[6,15])]],ctx,s))
 check('charpos3_only_authored_xy',predicate('CHARPOS3',[6,15,149,0,-1,-1],ctx,s))
 check('charpos2_default_false',not predicate('CHARPOS2',[1,1,5],ctx,s))
 check('charpos2_override_true',predicate('CHARPOS2',[1,1,5],dict(ctx,overrides={'charpos2:1:1:5':True}),s))
 check('false_duty_step',predicate('DUTY_STEP_FALSE',[1,2],ctx,s))
 s['steps']=[[1,2]];check('true_duty_step',predicate('DUTY_STEP_TRUE',[1,2],ctx,s))
 s['tombstones']={'400':dict(killer_side='party')};check('only_char_target',predicate('ONLY_CHAR',[400],ctx,s));check('only_monster_false',not predicate('ONLY_MONSTER',[400],ctx,s));check('cause_exact',predicate('CAUSE_OF_DEATH',['0101'],ctx,s))
 check('partial_dead_not_clear',not predicate('ALL_MOB_DEAD',[],ctx,s));s['tombstones']['410']={};check('all_dead_clear',predicate('ALL_MOB_DEAD',[],ctx,s));s['wave_pending']=True;check('pending_wave_blocks_clear',not predicate('ALL_MOB_DEAD',[],ctx,s))
 check('RANDOM50_boundary',not predicate('RANDOM',[50],ctx,s));check('RANDOM101_includes100',predicate('RANDOM',[101],dict(ctx,roll=100),s));check('tick_before_threshold',not predicate('TICK',[10],ctx,s));s['time']=10;check('tick_due',predicate('TICK',[10],ctx,s));check('tick_no_repeat_window',not predicate('TICK',[10],dict(ctx,tick_window_unused=False),s))
 s=new_state();snapshot=canonical(s)
 try:commit(s,'evt60',[('GOLD',100),('USEITEM',399),('GETITEM',303)])
 except ValueError:pass
 else:raise AssertionError('missing key accepted')
 check('consume_failure_rolls_back_everything',canonical(s)==snapshot)
 s['items']['399']=1;s,changed=commit(s,'evt60',[('USEITEM',399),('GETITEM',303),('GOLD',100),('STEP',1,2),('MAP','5,15',dict(sprite=1))]);check('full_atomic_reward',changed and s['items']['399']==0 and s['items']['303']==1 and s['gold']==100 and s['steps']==[[1,2]])
 copy1,changed=commit(s,'evt60',[('GETITEM',303),('GOLD',100)]);check('idempotent_repeat',not changed and copy1==s)
 k=event_key(s,1,'ONCE');check('once_same_across_actor',k==event_key(s,1,'ONCE','other'))
 check('charonce_distinct_members',event_key(s,1,'CHARONCE','a')!=event_key(s,1,'CHARONCE','b'))
 s2=copy.deepcopy(s);s2['run']='run2';check('once_survives_replay',event_key(s,1,'ONCE')==event_key(s2,1,'ONCE'));check('INF_new_run',event_key(s,1,'INF')!=event_key(s2,1,'INF'))
 s,changed=commit(s,'random_chest',[('ROLL_REWARD',50,300)]);rng=s['rng'];same,changed=commit(s,'random_chest',[('ROLL_REWARD',50,300)]);check('random_chest_no_reroll',not changed and same['rng']==rng)
 try:commit(s,'unsupported',[('GOLD',200),('UNKNOWN',1)])
 except ValueError:pass
 else:raise AssertionError('unknown action accepted')
 check('unknown_action_no_partial_gold',s['gold']==100)
 with tempfile.TemporaryDirectory(prefix='ds-local-contract-') as directory:
  p=Path(directory)/'save.json';save(p,s);loaded=load(p);check('real_file_roundtrip',loaded==s)
  same,changed=commit(loaded,'random_chest',[('ROLL_REWARD',50,300)]);check('reload_no_reroll',not changed and same['rng']==rng)
  newer=copy.deepcopy(s);newer['gold']=999
  try:save(p,newer,True)
  except OSError:pass
  check('failed_write_keeps_previous',load(p)==s and not Path(str(p)+'.tmp').exists())
  save(p,newer);p.write_text('{broken');check('corrupt_primary_uses_backup',load(p)==s)
  save(p,newer);p.write_text('{broken');check('repair_preserves_valid_backup',load(p)==s)
  Path(str(p)+'.bak').write_text('{broken')
  try:load(p)
  except ValueError:check('both_corrupt_fail_closed',True)
  else:raise AssertionError('corrupt reset accepted')
  future=copy.deepcopy(s);future['version']=999;p.write_bytes(canonical(envelope(future)))
  try:load(p)
  except RuntimeError:check('future_version_rejected',True)
  else:raise AssertionError('future version accepted')
  wrong=copy.deepcopy(s);wrong['profile']='other';p.write_bytes(canonical(envelope(wrong)))
  try:load(p)
  except RuntimeError:check('wrong_profile_rejected',True)
  else:raise AssertionError('wrong profile accepted')
 s=new_state();s['tombstones']={'400':{},'410':{}};s,units=regen(s,[400,410]);check('regen_only_dead_once',units==[400,410] and s['generations']=={'400':1,'410':1});again,units=regen(s,[400,410]);check('live_units_not_duplicated',not units and again==s)
 s['clear']=True;s['tombstones']['400']={};same,units=regen(s,[400]);check('clear_cancels_regen',not units and same==s)
 # Validate every generated row has explicit provenance and reproducible profile.
 plan=json.loads((OUT/'싱글판_배치보완안.json').read_text());check('104_scenario_plan',len(plan['scenarios'])==104);check('preserve610_source_rows',sum(r['provenance']=='installed SET/POS textual pair' for r in plan['units'])==610);check('all_generated_tagged',all(r['review'] and r['original_initial_runtime_verified'] is False for r in plan['units'] if r['provenance']=='authored singleplayer supplement'));check('one_profile_hash',all(r['profile_hash']==plan['profile_hash'] for r in plan['units']))
 # Each source AI slot in the supplemented maps must still have a defined unit; no capacity drop.
 from plan_singleplayer_scenarios import read
 all_ai=read('몬스터연결_AI.csv');by_scenario={s['scenario']:[r for r in plan['units'] if r['scenario']==s['scenario']] for s in plan['scenarios']}
 for scenario in plan['scenarios']:
  units=by_scenario[scenario['scenario']];keys={(r['spawn_wave'],r['x'],r['y']) for r in units}
  if scenario['source_status']!='populated SET':
   expected={int(r['unit']) for r in all_ai if r['archive']==scenario['archive']}
   check('no_AI_slot_dropped_'+scenario['scenario'],expected<={r['unit'] for r in units} and len(keys)==len(units))
   check('authored_cohort_cap_'+scenario['scenario'],all(sum(r['spawn_wave']==wave for r in units)<=12 for wave in {r['spawn_wave'] for r in units}))
 payload=profile();stored=payload.pop('profile_hash');check('profile_hash_reproducible',hashlib.sha256(json.dumps(payload,sort_keys=True,ensure_ascii=False).encode()).hexdigest()==stored)
 write('싱글판_규칙참조검증.csv',results);result=dict(cases=len(results),passed=len(results),scope='Python reference policy and real temporary file save faults; not original server logic, C# compilation, Unity PlayMode or full original choreography',unity_playmode_run=False)
 (OUT/'싱글판_규칙참조검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
