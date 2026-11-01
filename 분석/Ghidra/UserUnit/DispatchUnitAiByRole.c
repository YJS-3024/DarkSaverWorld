/* Annotated Ghidra decompilation, original PE unchanged.
 * Entry: 0x10002420
 * Evidence: ev_42062843aa9d216e3bb09757eaf78d1839d091660b1c272f97533fcc945e61c3
 */

/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* void __cdecl _ai(int) */

void __cdecl DispatchUnitAiByRole(int param_1)

{
  clock_t cVar1;
  clock_t cVar2;
  
                    /* Original export _ai. Branch using unit modulo10: commander slots and soldier
                       slots use different AI execution paths. This selects AI behavior, not initial
                       spawn coordinates. */
  if (param_1 < 0) {
    _DAT_100e03e0 = 0;
    return;
  }
  cVar1 = clock();
  if (param_1 % 10 == 0) {
    DAT_10016020 = ExecuteCommanderAI(param_1,DAT_10016020);
  }
  else {
    DAT_10016020 = FUN_100041f0((int *)param_1,DAT_10016020);
  }
  cVar2 = clock();
  DAT_100e03f0 = DAT_100e03f0 + (cVar2 - cVar1);
  DAT_100e03f4 = DAT_100e03f4 + 1;
  _DAT_100e03e0 = 0;
  return;
}

