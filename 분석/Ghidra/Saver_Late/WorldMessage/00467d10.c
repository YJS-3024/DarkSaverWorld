
undefined4 __thiscall FUN_00467d10(void *this,CDDSurface *param_1)

{
  int iVar1;
  int *piVar2;
  byte *pbVar3;
  int iVar4;
  int iVar5;
  _DDSURFACEDESC2 *p_Var6;
  undefined4 *puVar7;
  undefined4 auStack_11c [27];
  undefined4 uStack_b0;
  undefined4 uStack_ac;
  tagRECT local_8c;
  _DDSURFACEDESC2 local_7c [36];
  void *local_58;
  
  iVar1 = *(int *)((int)this + 0x40) + 0x120;
  iVar5 = *(int *)((int)this + 0x44) + 0x23;
  CDDSurface::GetDescription(param_1,local_7c);
  local_58 = CDDSurface::Lock(param_1);
  if (*(int *)((int)this + 0xb8) == 0) {
    piVar2 = *(int **)((int)this + 0xac);
    pbVar3 = *(byte **)((int)this + 0xb4);
    if (*piVar2 == 0) {
      p_Var6 = local_7c;
      puVar7 = auStack_11c;
      for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043e0c0(piVar2,iVar1,iVar5,pbVar3,0xffff);
    }
    else {
      p_Var6 = local_7c;
      puVar7 = auStack_11c;
      for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043ddf0(piVar2,iVar1,iVar5,pbVar3,0xff);
    }
  }
  else {
    local_8c.left = 0x14;
    local_8c.right = 0x21e;
    local_8c.top = 10;
    local_8c.bottom = 0x4a;
    uStack_ac = 0x467de9;
    OffsetRect(&local_8c,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    piVar2 = *(int **)((int)this + 0xac);
    pbVar3 = *(byte **)((int)this + 0xb4);
    uStack_ac = 0;
    uStack_b0 = 0;
    if (*piVar2 == 0) {
      p_Var6 = local_7c;
      puVar7 = (undefined4 *)&stack0xfffffed4;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043ea00(piVar2,local_8c.left,local_8c.top,local_8c.right,local_8c.bottom,pbVar3,
                         0xffff);
    }
    else {
      p_Var6 = local_7c;
      puVar7 = (undefined4 *)&stack0xfffffed4;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043e390(piVar2,local_8c.left,local_8c.top,local_8c.right,local_8c.bottom,pbVar3,
                         0xff);
    }
  }
  CDDSurface::Unlock(param_1);
  return 1;
}

