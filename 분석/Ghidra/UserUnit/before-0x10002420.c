/* Ghidra decompilation; not recovered original source. Evidence: ev_c30a0109d854b996488a61a99676d0305e738b8ecf522a80647d9ffa322f9b66 */

/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */
/* void __cdecl _ai(int) */

void __cdecl _ai(int param_1)

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

