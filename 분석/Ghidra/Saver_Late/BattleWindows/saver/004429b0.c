
undefined4 __thiscall FUN_004429b0(void *this,int param_1)

{
  int iVar1;
  int iVar2;
  
  iVar2 = 0;
  if (0 < *(int *)((int)this + 0x18)) {
    do {
      iVar1 = (**(code **)(**(int **)(*(int *)((int)this + 8) + iVar2 * 4) + 4))();
      if (iVar1 == param_1) {
        return 1;
      }
      iVar2 = iVar2 + 1;
    } while (iVar2 < *(int *)((int)this + 0x18));
  }
  return 0;
}

