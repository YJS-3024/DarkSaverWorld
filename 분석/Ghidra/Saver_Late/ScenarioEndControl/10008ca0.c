
void __fastcall FUN_10008ca0(int *param_1)

{
  int iVar1;
  int iVar2;
  
  if (param_1[0x6b] == 0) {
    if (DAT_1006c118 != 0) goto LAB_10008ced;
    *param_1 = 0;
  }
  else {
    if (param_1[0x6b] != 1) goto LAB_10008ced;
    iVar1 = param_1[0x6c];
    iVar2 = iVar1 + 3;
    param_1[0x6c] = iVar2;
    if (iVar2 < param_1[0x6d]) goto LAB_10008ced;
    if (DAT_1006c118 == 0) {
      *param_1 = 0;
      goto LAB_10008ced;
    }
    param_1[0x6c] = iVar1;
  }
  param_1[1] = 0;
LAB_10008ced:
  if ((*param_1 == 0) && (param_1[6] == 1)) {
    FUN_100029d0(0x4ca,8,0);
    FUN_100029d0(0x4d0,8,0xa006e);
  }
  return;
}


/* incoming refs
10002e41 UNCONDITIONAL_CALL
10008aa1 UNCONDITIONAL_CALL
10002ed9 UNCONDITIONAL_CALL
10008c73 UNCONDITIONAL_CALL
10004976 UNCONDITIONAL_CALL
CALLER 10008ad0 FUN_10008ad0
  10003bba UNCONDITIONAL_CALL
CALLER 100089f0 FUN_100089f0
  10010e88 UNCONDITIONAL_CALL
CALLER 10004830 DlgLButtonDown
  100189a0 DATA
  Entry Point EXTERNAL
CALLER 10002eb0 OnRButtonDown
  10018a40 DATA
  Entry Point EXTERNAL
CALLER 10002e10 OnLButtonDown
  10018a2c DATA
  Entry Point EXTERNAL
*/
