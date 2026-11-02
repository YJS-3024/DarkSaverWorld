
void __thiscall FUN_0046b860(void *this,CDDSurface *param_1,CDDSurface *param_2)

{
  int *piVar1;
  byte *pbVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  undefined4 *puVar6;
  undefined4 *puVar7;
  undefined4 auStack_120 [27];
  undefined4 uStack_b4;
  CString local_94 [4];
  byte *local_90;
  int local_8c;
  int local_88;
  undefined4 local_84 [8];
  void *local_64;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0048069b;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CDDSurface::GetDescription(param_1,(_DDSURFACEDESC2 *)&local_88);
  local_64 = CDDSurface::Lock(param_1);
  local_8c = *(int *)((int)this + 0x44) + 0xc1;
  iVar3 = *(int *)((int)this + 0x40) + 0x68;
  CString::CString(local_94);
  local_4 = 0;
  uStack_b4 = 0x46b8e1;
  CString::Format(*(CString **)((int)this + 0xb0),(char *)local_94);
  piVar1 = *(int **)((int)this + 0xac);
  if (*piVar1 == 0) {
    puVar6 = local_84;
    puVar7 = auStack_120;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *puVar6;
      puVar6 = puVar6 + 1;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar3,local_88,local_90,0xffff);
  }
  else {
    puVar6 = local_84;
    puVar7 = auStack_120;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar7 = *puVar6;
      puVar6 = puVar6 + 1;
      puVar7 = puVar7 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar3,local_88,local_90,0xff);
  }
  pbVar2 = *(byte **)((int)this + 0x324);
  if (*(int *)(pbVar2 + -8) != 0) {
    piVar1 = *(int **)((int)this + 0xac);
    iVar3 = *(int *)((int)this + 0x40) + 0xe6;
    iVar4 = *(int *)((int)this + 0x44) + 0xc0;
    if (*piVar1 == 0) {
      puVar6 = local_84;
      puVar7 = auStack_120;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar7 = *puVar6;
        puVar6 = puVar6 + 1;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043e0c0(piVar1,iVar3,iVar4,pbVar2,0);
    }
    else {
      puVar6 = local_84;
      puVar7 = auStack_120;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar7 = *puVar6;
        puVar6 = puVar6 + 1;
        puVar7 = puVar7 + 1;
      }
      thunk_FUN_0043ddf0(piVar1,iVar3,iVar4,pbVar2,0);
    }
  }
  CDDSurface::Unlock(param_2);
  CString::~CString((CString *)&local_90);
  ExceptionList = local_8;
  return;
}

