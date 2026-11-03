
undefined4 __thiscall FUN_00470020(void *this,int param_1)

{
  int iVar1;
  int *piVar2;
  
  iVar1 = 0;
  if (0 < *(int *)((int)this + 0x174)) {
    piVar2 = *(int **)((int)this + 0x178);
    do {
      if (*piVar2 == param_1) {
        return *(undefined4 *)(*(int *)((int)this + 0x17c) + iVar1 * 4);
      }
      iVar1 = iVar1 + 1;
      piVar2 = piVar2 + 1;
    } while (iVar1 < *(int *)((int)this + 0x174));
  }
  return 0;
}

