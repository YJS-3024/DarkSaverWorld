
undefined4 __thiscall FUN_0046f5a0(void *this,CDDSurface *param_1)

{
  int iVar1;
  int iVar2;
  
  iVar1 = *(int *)((int)this + 0x9c);
  iVar2 = iVar1 + 1;
  *(int *)((int)this + 0x9c) = iVar2;
  if (iVar1 != 0) {
    if (iVar2 == 2) {
      thunk_FUN_0046f7f0(this,param_1);
    }
    *(undefined4 *)((int)this + 0x9c) = 0;
    return 1;
  }
  thunk_FUN_00470300(this,param_1);
  thunk_FUN_0046f620(this,param_1);
  return 0;
}

