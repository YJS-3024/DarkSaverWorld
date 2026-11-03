
undefined4 __thiscall FUN_004619a0(void *this,CDDSurface *param_1)

{
  int iVar1;
  int iVar2;
  CDDSurface *unaff_ESI;
  undefined4 unaff_EDI;
  
  iVar1 = *(int *)((int)this + 0x9c);
  iVar2 = iVar1 + 1;
  *(int *)((int)this + 0x9c) = iVar2;
  if (iVar1 != 0) {
    if (iVar2 == 2) {
      thunk_FUN_004635c0(this,param_1);
      thunk_FUN_00463720(this,param_1);
      thunk_FUN_00464380(this,param_1);
    }
    *(undefined4 *)((int)this + 0x9c) = 0;
    return 1;
  }
  thunk_FUN_004626f0(this);
  thunk_FUN_00463880(this,param_1,unaff_EDI,unaff_ESI);
  thunk_FUN_00463ce0(this,param_1);
  thunk_FUN_004640d0(this,param_1);
  thunk_FUN_00462c50(this,param_1);
  thunk_FUN_00463170(this,param_1);
  return 0;
}

