
/* void __cdecl AIStart(int) */

void __cdecl AIStart(int param_1)

{
  int iVar1;
  
                    /* 0x2310  7  ?AIStart@@YAXH@Z */
  DAT_100e4298 = *(undefined4 *)(DAT_1010b3e4 + 0x88);
  DAT_100e429c = *(undefined4 *)(DAT_1010b3e4 + 0x8c);
  if ((param_1 < 800) && ((399 < param_1 || (param_1 % 10 != 0)))) {
    iVar1 = FUN_100022c0(param_1);
    if ((-1 < *(int *)(&DAT_1003d5a0 +
                      (g_RuntimeCharacters[param_1].tileY + g_RuntimeCharacters[param_1].tileX * 200
                      ) * 4)) && (iVar1 < 0)) {
      *(int *)(&DAT_1003d3b0 + DAT_100e03e8 * 4) = param_1;
      DAT_100e03e8 = (DAT_100e03e8 + 1) % 100;
    }
  }
  return;
}


/* incoming refs
10014000 DATA
Entry Point EXTERNAL
1000c941 UNCONDITIONAL_CALL
100105eb UNCONDITIONAL_CALL
10001435 UNCONDITIONAL_CALL
100014c4 UNCONDITIONAL_CALL
1000154b UNCONDITIONAL_CALL
CALLER 10010210 FUN_10010210
  10010fb9 UNCONDITIONAL_CALL
CALLER 10001370 setsolai
  1001419c DATA
  Entry Point EXTERNAL
CALLER 1000c740 SetTickMenu
  10014124 DATA
  Entry Point EXTERNAL
*/
