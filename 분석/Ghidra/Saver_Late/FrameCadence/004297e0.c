
undefined4 __fastcall FUN_004297e0(int param_1)

{
  int iVar1;
  int iVar2;
  
  if (*(int *)(param_1 + 0xa8) == 0) {
    iVar1 = *(int *)(param_1 + 200);
    iVar2 = 0;
    if (0 < iVar1) {
      do {
        thunk_FUN_00421110(*(void **)(*(int *)(param_1 + 0xc4) + iVar2 * 4),DAT_00495390,
                           (POINT *)(param_1 + 0x98),(int *)(param_1 + 0xa0));
        iVar2 = iVar2 + 1;
      } while (iVar2 < iVar1);
    }
    return 1;
  }
  return 1;
}


/* incoming refs
00403544 UNCONDITIONAL_JUMP
*/
