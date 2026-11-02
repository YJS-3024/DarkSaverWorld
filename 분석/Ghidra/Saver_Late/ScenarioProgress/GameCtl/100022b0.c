
undefined4 __thiscall FUN_100022b0(void *this,CDDSurface *param_1)

{
  int iVar1;
  clock_t cVar2;
  int iVar3;
  undefined4 uVar4;
  
  cVar2 = clock();
  iVar1 = *(int *)((int)this + 0x30);
  iVar3 = cVar2 - *(int *)((int)this + 0x34);
  if (iVar1 < iVar3) {
    cVar2 = clock();
    *(clock_t *)((int)this + 0x34) = cVar2;
  }
  if (*(int *)((int)this + 0x2c) <= *(int *)((int)this + 0x28)) {
    FUN_10002330(this,param_1);
    if (iVar1 < iVar3) {
      *(int *)((int)this + 0x2c) = *(int *)((int)this + 0x2c) + 1;
    }
    if (*(int *)((int)this + 0x2c) == *(int *)((int)this + 0x28) + 1) {
      *(undefined4 *)((int)this + 0x10) = 1;
      FUN_100024d0(this,1);
    }
    return 0;
  }
  uVar4 = FUN_10002460(this,param_1);
  return uVar4;
}

