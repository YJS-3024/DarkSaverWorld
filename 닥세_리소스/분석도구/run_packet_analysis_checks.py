"""Reproduce packet-analysis checks. Default checks do not create network sockets."""
import argparse,hashlib,json,os,subprocess,sys,time
from pathlib import Path
SCRIPTS=['analyze_packet_requests.py','analyze_world_packets.py','analyze_initialization_flow.py','analyze_packet_meanings.py','analyze_result_fields.py','analyze_result_display.py','analyze_packet_catalog.py','analyze_saver_messages.py','analyze_local_packet_helpers.py','analyze_file_transfer_packets.py','verify_packet_framing.py','verify_native_receive.py','analyze_map_entry.py','analyze_map_entry_details.py','analyze_map_entry_continuation.py','analyze_scenario_entry_response.py','verify_remaining_saver_senders.py','verify_userunit_packet_senders.py','verify_scenario_ui_requests.py','inventory_packet_paths.py','audit_saver_packet_coverage.py','verify_local_packet_router.py','audit_installer_communications.py','analyze_singleplayer_state_updates.py','analyze_singleplayer_inventory_dialogue.py','analyze_movement_pathfinding.py','analyze_map_movement_updates.py','analyze_map_runtime_completion.py','verify_complete_mrc_loader.py','analyze_combat_attack_lifecycle.py','analyze_combat_body_selection.py','analyze_script_executor.py','analyze_map_event_loading.py','analyze_monster_placements.py','analyze_scenario_integrations.py','analyze_presentation_timing.py','analyze_dialogue_layout.py','analyze_font_alias_runtime.py']
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--loopback',action='store_true',help='Also run the explicit 127.0.0.1 TCP check');args=parser.parse_args()
 base=Path(__file__).resolve().parent;root=base.parents[1];results=[]
 for name in SCRIPTS+(['verify_local_packet_transport.py'] if args.loopback else []):
  p=base/name;started=time.monotonic()
  try:
   r=subprocess.run([sys.executable,str(p)],cwd=root,env=os.environ.copy(),text=True,capture_output=True,timeout=180)
   row=dict(script=name,script_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),exit_code=r.returncode,seconds=round(time.monotonic()-started,2),output=r.stdout[-3000:],error=r.stderr[-3000:])
  except subprocess.TimeoutExpired:row=dict(script=name,exit_code=-1,seconds=180,error='timeout')
  results.append(row);print(name,'PASS' if row['exit_code']==0 else 'FAIL',flush=True)
 summary=dict(suites=len(results),passed=sum(r['exit_code']==0 for r in results),loopback_requested=args.loopback,scope='Isolated native analysis/static inventory; not full original Windows application',results=results)
 (root/'닥세_리소스/분석결과/패킷_통합회귀검증.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n')
 return 0 if summary['passed']==summary['suites'] else 1
if __name__=='__main__':sys.exit(main())
