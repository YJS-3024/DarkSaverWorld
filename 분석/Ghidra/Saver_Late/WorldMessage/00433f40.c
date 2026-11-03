
void __fastcall FUN_00433f40(int param_1)

{
  int *piVar1;
  int iVar2;
  void *this;
  int iVar3;
  
  piVar1 = *(int **)(*(int *)(param_1 + 8) + 0x84);
  iVar2 = (**(code **)(*piVar1 + 0x60))(piVar1);
  if (iVar2 != 0) {
    this = DAT_004953e4;
    if (*(int *)(param_1 + 0x94) == 0) {
      this = DAT_004953e0;
    }
    if (*(int *)((int)this + 0x18) != 0) {
      iVar2 = thunk_FUN_00432ad0(this,*(int *)(param_1 + 8));
      if (iVar2 != 0) {
        iVar2 = *(int *)(param_1 + 0x6c);
        iVar3 = 0;
        if (0 < iVar2) {
          do {
            thunk_FUN_004204b0(*(int *)(*(int *)(param_1 + 0x68) + iVar3 * 4));
            iVar3 = iVar3 + 1;
          } while (iVar3 < iVar2);
        }
        iVar2 = *(int *)(param_1 + 0x58);
        iVar3 = 0;
        if (0 < iVar2) {
          do {
            thunk_FUN_00421010(*(int *)(*(int *)(param_1 + 0x54) + iVar3 * 4));
            iVar3 = iVar3 + 1;
          } while (iVar3 < iVar2);
        }
      }
    }
  }
  return;
}

