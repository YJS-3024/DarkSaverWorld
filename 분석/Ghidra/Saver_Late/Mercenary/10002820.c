
int FUN_10002820(int param_1)

{
  int iVar1;
  int *piVar2;
  
  iVar1 = 0;
  piVar2 = &DAT_10005500;
  do {
    if (*piVar2 == param_1) {
      return iVar1;
    }
    piVar2 = piVar2 + 0x61;
    iVar1 = iVar1 + 1;
  } while ((int)piVar2 < 0x10021bb0);
  return -1;
}

