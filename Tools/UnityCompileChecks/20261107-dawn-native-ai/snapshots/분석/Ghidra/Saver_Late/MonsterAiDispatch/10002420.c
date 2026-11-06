
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* Original export _ai. Branch using unit modulo10: commander slots and soldier slots use different
   AI execution paths. This selects AI behavior, not initial spawn coordinates.
   Source SHA256: 74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b */

void __cdecl DispatchUnitAiByRole(int param_1)

{
  clock_t cVar1;
  clock_t cVar2;
  
                    /* 0x2420  86  ?_ai@@YAXH@Z */
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


/* incoming refs
1001413c DATA
Entry Point EXTERNAL
1000240c UNCONDITIONAL_CALL
CALLER 100023c0 aiexcute
  10014144 DATA
  Entry Point EXTERNAL
*/
