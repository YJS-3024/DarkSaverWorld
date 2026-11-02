
undefined4 __thiscall FUN_10002460(void *this,CDDSurface *param_1)

{
  bool bVar1;
  int iVar2;
  undefined3 extraout_var;
  
  iVar2 = *(int *)((int)this + 0x10);
  if (iVar2 == 1) {
    bVar1 = FUN_100025c0(this,param_1);
    if (CONCAT31(extraout_var,bVar1) != 0) {
      *(undefined4 *)((int)this + 0x10) = 2;
      FUN_100024d0(this,2);
    }
  }
  else if (iVar2 == 2) {
    iVar2 = FUN_10002600(this,param_1);
    if (iVar2 != 0) {
      *(undefined4 *)((int)this + 0x10) = 3;
      FUN_100024d0(this,3);
      return 0;
    }
  }
  else if (iVar2 == 3) {
    return 1;
  }
  return 0;
}

