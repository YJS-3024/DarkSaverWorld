
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

void __cdecl FUN_1000f070(undefined4 param_1)

{
  undefined2 *puVar1;
  int iVar2;
  
  if ((DAT_100936d0 < 100) && ((short)param_1 != -1)) {
    (&DAT_100936d4)[DAT_100936d0 * 2] = (short)param_1;
    (&DAT_100936d6)[DAT_100936d0 * 2] = param_1._2_2_;
    DAT_100936d0 = DAT_100936d0 + 1;
  }
  if (((*(int *)((int)DAT_10093864 + 0xc) == 0) && (DAT_100936d4 != -1)) &&
     (DAT_10093864 != (void *)0x0)) {
    FUN_1000f710(DAT_10093864,_DAT_100936d4);
    iVar2 = 0;
    if (0 < DAT_100936d0) {
      puVar1 = &DAT_100936da;
      do {
        puVar1[-3] = puVar1[-1];
        puVar1[-2] = *puVar1;
        if (puVar1[-1] == -1) break;
        iVar2 = iVar2 + 1;
        puVar1 = puVar1 + 2;
      } while (iVar2 < DAT_100936d0);
      if (iVar2 == 99) {
        _DAT_10093860 = 0xffff;
      }
    }
    DAT_100936d0 = DAT_100936d0 + -1;
  }
  return;
}


/* incoming refs
10013cd8 UNCONDITIONAL_CALL
1000d8d9 UNCONDITIONAL_CALL
CALLER 1000d8a0 FUN_1000d8a0
  1000ec48 UNCONDITIONAL_CALL
CALLER 10010f20 FUN_10010f20
  10003883 UNCONDITIONAL_CALL
  10003896 UNCONDITIONAL_CALL
  1000dd87 UNCONDITIONAL_CALL
  10003bcc UNCONDITIONAL_CALL
  10003bdb UNCONDITIONAL_CALL
*/
