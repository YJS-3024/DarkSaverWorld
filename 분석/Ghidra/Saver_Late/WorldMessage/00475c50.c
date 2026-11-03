
undefined4 __thiscall FUN_00475c50(void *this,undefined4 param_1,undefined4 param_2)

{
  int iVar1;
  undefined4 unaff_retaddr;
  
  iVar1 = thunk_FUN_00467f30();
  if (iVar1 == 0) {
    if (*(int *)((int)this + 0x58) == 0) {
      iVar1 = (**(code **)(*(int *)this + 8))(param_2);
      *(int *)((int)this + 0x40) = iVar1;
      if (iVar1 != -1) {
        (**(code **)(**(int **)(*(int *)((int)this + 0x18) + iVar1 * 4) + 0x24))
                  (unaff_retaddr,param_2);
        return 1;
      }
      return 0;
    }
  }
  else if (*(int *)((int)this + 0x58) == 0) {
    return 1;
  }
  thunk_FUN_00477190(this);
  return 1;
}

