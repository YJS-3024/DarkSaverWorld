
undefined4 __thiscall
FUN_004453b0(void *this,CDDSurface *param_1,undefined4 param_2,CDDSurface *param_3)

{
  int *piVar1;
  byte *pbVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  int *piVar6;
  int *piVar7;
  int aiStack_11c [26];
  undefined4 uStack_b4;
  CString local_94 [4];
  byte *local_90;
  byte *local_8c;
  int local_88;
  int local_84 [8];
  void *local_64;
  void *pvStack_c;
  undefined1 *puStack_8;
  void *local_4;
  
  local_4 = (void *)0xffffffff;
  puStack_8 = &LAB_0047e2cb;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CDDSurface::GetDescription(param_1,(_DDSURFACEDESC2 *)&local_88);
  local_64 = CDDSurface::Lock(param_1);
  CString::CString(local_94);
  local_8c = (byte *)(*(int *)((int)this + 200) + *(int *)((int)this + 0x44));
  iVar4 = *(int *)((int)this + 0xc4) + *(int *)((int)this + 0x40);
  local_4 = (void *)0x0;
  uStack_b4 = 0x445438;
  CString::Format(*(CString **)((int)this + 0xb0),(char *)local_94);
  iVar5 = local_88;
  piVar1 = *(int **)((int)this + 0xac);
  if (*piVar1 == 0) {
    piVar6 = local_84;
    piVar7 = (int *)&stack0xfffffee0;
    for (iVar3 = 0x1f; iVar3 != 0; iVar3 = iVar3 + -1) {
      *piVar7 = *piVar6;
      piVar6 = piVar6 + 1;
      piVar7 = piVar7 + 1;
    }
    thunk_FUN_0043e0c0(piVar1,iVar4,iVar5,local_90,0xffff);
  }
  else {
    piVar6 = local_84;
    piVar7 = (int *)&stack0xfffffee0;
    for (iVar3 = 0x1f; iVar3 != 0; iVar3 = iVar3 + -1) {
      *piVar7 = *piVar6;
      piVar6 = piVar6 + 1;
      piVar7 = piVar7 + 1;
    }
    thunk_FUN_0043ddf0(piVar1,iVar4,iVar5,local_90,0xff);
  }
  if (*(int *)((int)this + 0xc0) != 0) {
    local_88 = *(int *)((int)this + 0xd0) + *(int *)((int)this + 0x44);
    iVar5 = *(int *)((int)this + 0xcc) + *(int *)((int)this + 0x40);
    uStack_b4 = 0x4454ea;
    CString::Format(*(CString **)((int)this + 0xb8),(char *)&local_90);
    pbVar2 = local_8c;
    piVar1 = *(int **)((int)this + 0xac);
    if (*piVar1 == 0) {
      piVar6 = local_84;
      piVar7 = aiStack_11c;
      for (iVar4 = 0x1f; piVar6 = piVar6 + 1, iVar4 != 0; iVar4 = iVar4 + -1) {
        *piVar7 = *piVar6;
        piVar7 = piVar7 + 1;
      }
      thunk_FUN_0043e0c0(piVar1,iVar5,local_84[0],pbVar2,0xffff);
    }
    else {
      piVar6 = local_84;
      piVar7 = aiStack_11c;
      for (iVar4 = 0x1f; piVar6 = piVar6 + 1, iVar4 != 0; iVar4 = iVar4 + -1) {
        *piVar7 = *piVar6;
        piVar7 = piVar7 + 1;
      }
      thunk_FUN_0043ddf0(piVar1,iVar5,local_84[0],pbVar2,0xff);
    }
  }
  CDDSurface::Unlock(param_3);
  CString::~CString((CString *)&local_8c);
  ExceptionList = local_4;
  return 1;
}

