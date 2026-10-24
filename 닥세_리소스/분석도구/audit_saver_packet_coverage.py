"""Reconcile every direct Saver application-send site with an audited wire shape.
Field labels are storage/types where product terminology cannot be established.
"""
import re,json,csv,subprocess,hashlib
from analyze_special_skills import ROOT,OUT,write
# u8 opcode bytes are included explicitly; h/u32 are LE values; s is u8 length + raw bytes.
SPECS={
0x4013b0:('67','u32','file_ack'),0x401400:('68','u32','file_retry'),0x401a40:('62','h,s','file_upload_start'),0x401c60:('64','u32,u32,s','file_metadata'),0x401d10:('65 / 66','u32,u32,u32,bytes[N] / s','file_chunk_or_completion'),0x401eb0:('69','s','file_cancel'),0x401f30:('60','h,s','file_download_start'),
0x4061a0:('93','','ui_action'),0x406620:('9D','','ui_action'),0x406c10:('9B / 9C','s','this+E0 selects opcode'),0x4071f0:('72','h,h','ui_values'),0x408400:('71','h,s','ui_value_and_text'),0x408930:('00','','periodic_counter_over_3000'),0x409140:('D8','','ui_action'),0x40a1c0:('4B','h','ui_selected_value'),0x40a2d0:('4C','h,h','ui_selected_values'),0x40cc20:('D6','h','this+C8[this+3FC]'),0x40cc90:('D9','','ui_action'),0x40da90:('C1','h','parent+4 nonzero=>5 else6'),0x40f200:('C1','h','parent+4 nonzero=>3 else4'),0x40f280:('7F','h,h,u32','numeric_ui_value'),0x40f640:('2C 29','h,h,h','ui_values'),0x40fab0:('C3','h','ui_value'),0x40fd50:('AC','u32','ui_value'),0x4119d0:('58','h','input_record'),0x413910:('3E','h,s','ui_value_and_text'),0x413e10:('AF','h,h','ui_values'),0x414500:('E7','h,h','this+B8; flag0=>2 else1'),0x414970:('7D','h','ui_value'),0x415230:('A0','h','ui_value'),0x4155b0:('9C','s','ui_text'),0x415790:('BA','','ui_state_update'),0x4159f0:('9B / BB','s','argument nonzero=>9B elseBB'),0x416070:('23','h,s,s,s,h','mode0=>100,1=>101,2=>103,other=>100'),0x4166e0:('AE','h','ui_value'),0x416d50:('2C 53','u32','ui_value'),0x417500:('A7','h','ui_value'),0x418b80:('50','s','scenario_name_this+C8'),0x418fd0:('9E','h','ui_value'),0x419d80:('82','h,h,h','unit_id;stat_index;change'),0x419f40:('84','h,h,h','argument;unit_id;selected_entry'),0x41a080:('83','h,h','unit_id;selected_entry'),0x41a110:('85','h,h','unit_id;selected_entry'),0x41a210:('86','h','ui_value'),0x41a640:('88','h','ui_value'),0x41a6a0:('89','h','ui_value'),0x41a700:('81','h,h','unit_id;mode_selected_value'),0x41a7d0:('8A','h','ui_value'),0x41c650:('3B','h,h','ui_values'),0x41c9c0:('42','h,h','ui_values'),0x41d310:('3B','h,h','ui_values'),0x41f4f0:('AD','h,h','ui_values'),0x420800:('40','h,h,h,h,h','unit_id;four_input_values'),0x420930:('A2','h,h,h','ui_values'),0x4209b0:('2C 26','h,h','ui_values'),0x4210d0:('A9','h,h','valid_slot_first_record_values'),0x4223d0:('2C 30','h','ui_value'),0x4226f0:('2C 31','h,h','this+BC;flag0=>2 else1'),0x422b70:('95 / 96','u32','this+CC selects opcode;numeric_text'),0x423810:('4D','h,h,h','ui_values'),0x423940:('4E','h,h','ui_values'),0x424520:('9A','h,h,h','ui_values'),0x4245c0:('99','h,h,h','ui_values'),0x4247f0:('97','h','ui_value'),0x424d60:('90','','ui_action'),0x4258d0:('A4','h','ui_value'),
0x42a020:('5C 5C','','scenario_kickoff'),0x42d640:('2C 20','','world_state_request'),0x42d990:('2C 22','h,h,h','unit_id;X;Y gated by this+60'),0x42da30:('7A','u8,s','chat'),0x431a20:('2C 25','h,h,h,h','unit_id;X;Y;selection'),0x431af0:('2C 27','','world_list_request'),0x431b40:('2C 40','','world_list_request'),0x432310:('7A','u8,s,s','chat_with_recipient'),0x432600:('2C 55','h,h,h,h','movement_helper_values'),0x434d00:('21','s,s,h','login_related_values'),0x434dc0:('20','s','resource_string_EF1A'),0x436680:('90','','scene_action'),0x43ac20:('2A','h,h,h','map_number;X;Y clipped_nonnegative'),0x43acc0:('25','','app_action'),0x43b2f0:('5C 61','','scenario_control'),0x43b8a0:('6F','u32','receive_handler_reply'),0x43c170:('2A','h,h,h','failed_2B_recovery'),0x43c360:('2A','h,h,h','7C_echo'),0x43c4a0:('5B','','scene_end'),0x43cd10:('passthrough','bytes[N]','WM52C_GameCtl_bridge'),0x43cf30:('2D','h,h,h','2D_echo'),0x43cff0:('19','s,s,h','login'),0x43d680:('52','h,h,[mode2:s],s,u32,s','scenario_request'),0x43d810:('54','h,h,[mode2:s],h,u32','scenario_selection')}

def main():
 asm=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',str(ROOT/'Saver.exe')],text=True)
 ins=[(int(m[1],16),m[2]) for l in asm.splitlines() if (m:=re.match(r'\s*([0-9a-f]+):\s+(.*)',l))]
 rows=[];evidence=[]
 # Explicit function boundaries independently matched to opcode-writing sequences.
 for i,(a,t) in enumerate(ins):
  if t!='call   0x43b840':continue
  entry=max(x for x in SPECS if x<=a)
  # Main receive dispatcher has many intervening methods; limit that row to its reply block.
  lo=0x43bce0 if a==0x43bd1a else entry
  seq=[(v,q) for v,q in ins if lo<=v<=a]
  op,shape,meaning=SPECS[entry]
  if a==0x401df7:op,shape='65','u32,u32,u32,bytes[N]'
  if a==0x401e5b:op,shape='66','s'
  size='variable'
  if '/' not in op and '[' not in shape and 's' not in shape and op!='passthrough':
   size=str(len(op.split())+sum({'h':2,'u8':1,'u32':4}.get(x.strip(),0) for x in shape.split(',') if x.strip()))
  row=dict(callsite_va=hex(a),entry_va=hex(entry),opcode=op,wire_fields_after_opcode=shape,wire_length=size,source_role=meaning,analysis='static serialization and branch inspection; product names not inferred')
  rows.append(row)
  evidence.append(f'\n{a:08X} entry={entry:08X} opcode={op}\n'+'\n'.join(f'{v:08X}: {q}' for v,q in seq))
 assert len(rows)==91,len(rows)
 # Every callsite from the independent earlier inventory must be accounted for exactly once.
 with (OUT/'패킷_송신_직접호출.csv').open(encoding='utf-8-sig') as f:
  prior={r['callsite_va'] for r in csv.DictReader(f) if r['file']=='Saver.exe' and r['target_va']=='0x43b840'}
 assert prior=={r['callsite_va'] for r in rows}
 write('패킷_Saver_송신전수_명세.csv',rows)
 (OUT/'패킷_Saver_송신전수_근거.asm.txt').write_text('\n'.join(evidence)+'\n')
 (OUT/'패킷_Saver_송신전수요약.json').write_text(json.dumps(dict(direct_calls=len(rows),functions=len({r['entry_va'] for r in rows}),unclassified_direct_calls=0,binary_sha256=hashlib.sha256((ROOT/'Saver.exe').read_bytes()).hexdigest(),scope='All direct calls to known application sender in supplied Saver.exe; UI product terminology and runtime sessions are separate'),indent=2)+'\n')
 print(json.dumps(dict(direct_calls=len(rows),functions=len({r['entry_va'] for r in rows}),unclassified_direct_calls=0)))
if __name__=='__main__':main()
