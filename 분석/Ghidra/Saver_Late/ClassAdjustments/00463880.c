
void __thiscall FUN_00463880(void *this,CDDSurface *param_1,undefined4 param_2,CDDSurface *param_3)

{
  int *piVar1;
  byte *pbVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  _DDSURFACEDESC2 *p_Var6;
  undefined4 *puVar7;
  undefined4 *puVar8;
  undefined4 auStack_124 [26];
  undefined4 uStack_bc;
  CString local_9c [4];
  byte *local_98;
  byte *local_94;
  int local_90;
  int local_8c;
  _DDSURFACEDESC2 local_88 [4];
  undefined4 local_84 [8];
  void *local_64;
  void *pvStack_c;
  undefined1 *puStack_8;
  void *local_4;
  
  local_4 = (void *)0xffffffff;
  puStack_8 = &LAB_0048007b;
  pvStack_c = ExceptionList;
  local_90 = *(int *)((int)this + 0x13c) + 4;
  ExceptionList = &pvStack_c;
  CDDSurface::GetDescription(param_1,local_88);
  local_64 = CDDSurface::Lock(param_1);
  local_8c = ((*(int *)((int)this + 0x388) - *(int *)((int)this + 0x380)) + -0x1c) / 3;
  piVar1 = *(int **)((int)this + 0xac);
  iVar5 = *(int *)((int)this + 0x380) + local_8c + *(int *)((int)this + 0x44);
  iVar3 = *(int *)((int)this + 0x37c) + 2 + *(int *)((int)this + 0x40);
  if (*piVar1 == 0) {
    p_Var6 = local_88;
    puVar7 = (undefined4 *)&stack0xfffffed4;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar3,iVar5,*(byte **)((int)this + 0x130),0);
  }
  else {
    p_Var6 = local_88;
    puVar7 = (undefined4 *)&stack0xfffffed4;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar3,iVar5,*(byte **)((int)this + 0x130),0);
  }
  iVar5 = *(int *)((int)this + 0x37c) +
          (*(int *)((int)this + 900) - *(int *)((int)this + 0x37c)) / 2 + *(int *)((int)this + 0x40)
  ;
  piVar1 = *(int **)((int)this + 0xac);
  iVar3 = *(int *)((int)this + 0x380) + local_8c + *(int *)((int)this + 0x44);
  pbVar2 = *(byte **)((int)this + 0x134);
  local_94 = (byte *)iVar3;
  if (*piVar1 == 0) {
    p_Var6 = local_88;
    puVar7 = (undefined4 *)&stack0xfffffed4;
    for (iVar3 = 0x1f; iVar3 != 0; iVar3 = iVar3 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar5,(int)local_94,pbVar2,0);
  }
  else {
    p_Var6 = local_88;
    puVar7 = (undefined4 *)&stack0xfffffed4;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar5,iVar3,pbVar2,0);
  }
  piVar1 = *(int **)((int)this + 0xac);
  iVar3 = *(int *)((int)this + 0x37c) + 2 + *(int *)((int)this + 0x40);
  iVar5 = *(int *)((int)this + 0x380) + 0xe + local_8c * 2 + *(int *)((int)this + 0x44);
  if (*piVar1 == 0) {
    p_Var6 = local_88;
    puVar7 = (undefined4 *)&stack0xfffffed4;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar3,iVar5,*(byte **)((int)this + 0x138),0);
  }
  else {
    p_Var6 = local_88;
    puVar7 = (undefined4 *)&stack0xfffffed4;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *(undefined4 *)p_Var6;
      p_Var6 = p_Var6 + 4;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar3,iVar5,*(byte **)((int)this + 0x138),0);
  }
  CString::CString(local_9c);
  local_94 = (byte *)(*(int *)((int)this + 0x378) + *(int *)((int)this + 0x44));
  iVar3 = *(int *)((int)this + 0x374) + *(int *)((int)this + 0x40);
  local_4 = (void *)0x0;
  uStack_bc = 0x463aba;
  CString::Format(local_9c,(char *)local_9c);
  piVar1 = *(int **)((int)this + 0xac);
  if (*piVar1 == 0) {
    puVar7 = local_84;
    puVar8 = (undefined4 *)&stack0xfffffed8;
    for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
      *puVar8 = *puVar7;
      puVar7 = puVar7 + 1;
      puVar8 = puVar8 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar3,local_90,local_98,0);
  }
  else {
    puVar7 = local_84;
    puVar8 = (undefined4 *)&stack0xfffffed8;
    for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
      *puVar8 = *puVar7;
      puVar7 = puVar7 + 1;
      puVar8 = puVar8 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar3,local_90,local_98,0);
  }
  local_90 = *(int *)((int)this + 0x440) + *(int *)((int)this + 0x44);
  iVar5 = *(int *)((int)this + 0x43c) + *(int *)((int)this + 0x40);
  CString::Format((CString *)&local_98,(char *)&local_98);
  iVar3 = local_8c;
  pbVar2 = local_94;
  piVar1 = *(int **)((int)this + 0xac);
  if (*piVar1 == 0) {
    puVar7 = local_84;
    puVar8 = auStack_124;
    for (iVar4 = 0x1f; puVar7 = puVar7 + 1, iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar8 = *puVar7;
      puVar8 = puVar8 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar5,iVar3,local_94,0);
  }
  else {
    puVar7 = local_84;
    puVar8 = auStack_124;
    for (iVar4 = 0x1f; puVar7 = puVar7 + 1, iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar8 = *puVar7;
      puVar8 = puVar8 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar5,iVar3,pbVar2,0);
  }
  CDDSurface::Unlock(param_3);
  CString::~CString((CString *)&local_94);
  ExceptionList = local_4;
  return;
}

