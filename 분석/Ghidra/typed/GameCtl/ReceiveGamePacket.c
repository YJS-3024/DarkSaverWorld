/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10002d30; source SHA256 f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0
 */

/* Original export OnPacketReceive. Nonempty packet input is forwarded into the game packet
   dispatcher; no target execution has been observed.
   Source SHA256: f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0 */

void __cdecl ReceiveGamePacket(char *param_1,int param_2)

{
                    /* 0x2d30  62  ?OnPacketReceive@@YAXPADH@Z */
  if (0 < param_2) {
    DispatchGamePacket(param_1);
  }
  return;
}

