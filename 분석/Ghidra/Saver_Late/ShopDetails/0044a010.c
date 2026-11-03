
undefined4 __thiscall FUN_0044a010(void *this,CDDSurface *param_1)

{
  int iVar1;
  
  iVar1 = *(int *)((int)this + 0x9c);
  *(int *)((int)this + 0x9c) = iVar1 + 1;
  if (iVar1 == 0) {
    thunk_FUN_0044a790(this,param_1);
    if (*(int *)((int)*(void **)((int)this + 0x354) + 0x28) != 0) {
      thunk_FUN_00437c40(*(void **)((int)this + 0x354),param_1,*(int *)((int)this + 0x40),
                         *(int *)((int)this + 0x44),0);
    }
    thunk_FUN_0044aa70(this,param_1);
    return 0;
  }
  if (iVar1 != 1) {
    if (iVar1 == 2) {
      thunk_FUN_0044ac40(this,param_1);
    }
    *(undefined4 *)((int)this + 0x9c) = 0;
    return 1;
  }
  thunk_FUN_0044a310(this,param_1);
  thunk_FUN_0044a490(this,param_1);
  return 0;
}

