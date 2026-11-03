
undefined4 __thiscall FUN_00472670(int param_1,CDDSurface *param_2)

{
  int *piVar1;
  byte *pbVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  _DDSURFACEDESC2 *p_Var6;
  undefined4 *puVar7;
  undefined4 auStack_12c [27];
  undefined4 uStack_c0;
  undefined4 uStack_bc;
  _DDSURFACEDESC2 a_Stack_8c [36];
  void *pvStack_68;
  
  CDDSurface::BltFast(param_2);
  iVar3 = *(int *)(param_1 + 0x44) + 0x23;
  iVar5 = *(int *)(param_1 + 0x40) + 0x120;
  CDDSurface::GetDescription(param_2,a_Stack_8c);
  pvStack_68 = CDDSurface::Lock(param_2);
  if (*(int *)(param_1 + 0xb8) == 0) {
    piVar1 = *(int **)(param_1 + 0xac);
    pbVar2 = *(byte **)(param_1 + 0xb4);
    if (*piVar1 == 0) {
      p_Var6 = a_Stack_8c;
      puVar7 = auStack_12c;
      for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043e0c0(piVar1,iVar5,iVar3,pbVar2,0xffff);
    }
    else {
      p_Var6 = a_Stack_8c;
      puVar7 = auStack_12c;
      for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043ddf0(piVar1,iVar5,iVar3,pbVar2,0xff);
    }
  }
  else {
    uStack_bc = 0x47277a;
    OffsetRect((LPRECT)&stack0xffffff64,*(int *)(param_1 + 0x40),*(int *)(param_1 + 0x44));
    piVar1 = *(int **)(param_1 + 0xac);
    pbVar2 = *(byte **)(param_1 + 0xb4);
    uStack_bc = 0;
    uStack_c0 = 0;
    if (*piVar1 == 0) {
      p_Var6 = a_Stack_8c;
      puVar7 = (undefined4 *)&stack0xfffffec4;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043ea00(piVar1,0x5a,10,0x221,0x4a,pbVar2,0xffff);
    }
    else {
      p_Var6 = a_Stack_8c;
      puVar7 = (undefined4 *)&stack0xfffffec4;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar7 = *(undefined4 *)p_Var6;
        p_Var6 = p_Var6 + 4;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043e390(piVar1,0x5a,10,0x221,0x4a,pbVar2,0xff);
    }
  }
  CDDSurface::Unlock(param_2);
  return 1;
}

