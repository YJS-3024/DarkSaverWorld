/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x10002d30
 * Evidence: ev_d4ebaf147a9933e5a3cc9e24ded28fb4885c58b62941b71cd6afecd7d3ff5d6f
 */

/* void __cdecl OnPacketReceive(char *,int) */

void __cdecl ReceiveGamePacket(char *param_1,int param_2)

{
                    /* Original export OnPacketReceive. Nonempty packet input is forwarded into the
                       game packet dispatcher; no target execution has been observed. */
  if (0 < param_2) {
    FUN_1000eb10(param_1);
  }
  return;
}

