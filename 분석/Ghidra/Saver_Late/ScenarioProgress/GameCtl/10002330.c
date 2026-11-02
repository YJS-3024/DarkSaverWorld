
void __thiscall FUN_10002330(void *this,CDDSurface *param_1)

{
  undefined4 *puVar1;
  int iVar2;
  int iVar3;
  int local_20;
  int local_1c;
  undefined4 local_10;
  undefined4 local_c;
  undefined4 local_8;
  undefined4 local_4;
  
  local_10 = 0x20;
  local_c = 0x20;
  local_8 = 0x40;
  local_4 = 0x40;
  local_20 = 0;
  if (0 < *(int *)((int)this + 0x14)) {
    local_1c = 0;
    do {
      puVar1 = (undefined4 *)(*(int *)((int)this + 0x1c) + local_1c);
      if (*(int *)(*(int *)((int)this + 0x1c) + 0xc + local_1c) != 0) {
        CDDSurface::BltFast(param_1,((*(int *)((int)this + 0x20) - puVar1[1]) *
                                    *(int *)((int)this + 0x2c)) / *(int *)((int)this + 0x28) +
                                    puVar1[1],
                            ((*(int *)((int)this + 0x24) - puVar1[2]) * *(int *)((int)this + 0x2c))
                            / *(int *)((int)this + 0x28) + puVar1[2],*puVar1,&local_10,1);
      }
      local_20 = local_20 + 1;
      local_1c = local_1c + 0x10;
    } while (local_20 < *(int *)((int)this + 0x14));
  }
  local_20 = 0;
  if (0 < *(int *)((int)this + 0x14)) {
    local_1c = 0;
    do {
      iVar3 = *(int *)((int)this + 0x1c) + local_1c;
      iVar2 = *(int *)(iVar3 + 4);
      iVar3 = *(int *)(iVar3 + 8);
      CAlphaEffectorForChar::DrawAlphaEffect
                (*(CAlphaEffectorForChar **)((int)this + 0x18),param_1,
                 ((*(int *)((int)this + 0x20) - iVar2) * *(int *)((int)this + 0x2c)) /
                 *(int *)((int)this + 0x28) + iVar2 + 0x10,
                 ((*(int *)((int)this + 0x24) - iVar3) * *(int *)((int)this + 0x2c)) /
                 *(int *)((int)this + 0x28) + iVar3 + 0x10);
      local_20 = local_20 + 1;
      local_1c = local_1c + 0x10;
    } while (local_20 < *(int *)((int)this + 0x14));
  }
  return;
}

