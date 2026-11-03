
void __thiscall FUN_0044a790(void *this,CDDSurface *param_1)

{
  int iVar1;
  int *this_00;
  int iVar2;
  undefined4 *puVar3;
  undefined4 *puVar4;
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
  local_8 = &LAB_0047e7fb;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CDDSurface::GetDescription(param_1,(_DDSURFACEDESC2 *)&local_88);
  local_64 = CDDSurface::Lock(param_1);
  CString::CString(local_94);
  local_8c = *(int *)((int)this + 0x44) + 0x10a;
  iVar1 = *(int *)((int)this + 0x40) + 0x72;
  local_4 = 0;
  uStack_b4 = 0x44a811;
  CString::Format(*(CString **)((int)this + 0xb0),(char *)local_94);
  this_00 = *(int **)((int)this + 0xac);
  if (*this_00 == 0) {
    puVar3 = local_84;
    puVar4 = auStack_120;
    for (iVar2 = 0x1f; iVar2 != 0; iVar2 = iVar2 + -1) {
      *puVar4 = *puVar3;
      puVar3 = puVar3 + 1;
      puVar4 = puVar4 + 1;
    }
    thunk_FUN_0043e0c0(this_00,iVar1,local_88,local_90,0xffff);
  }
  else {
    puVar3 = local_84;
    puVar4 = auStack_120;
    for (iVar2 = 0x1f; iVar2 != 0; iVar2 = iVar2 + -1) {
      *puVar4 = *puVar3;
      puVar3 = puVar3 + 1;
      puVar4 = puVar4 + 1;
    }
    thunk_FUN_0043ddf0(this_00,iVar1,local_88,local_90,0xff);
  }
  CDDSurface::Unlock(param_1);
  CString::~CString((CString *)&local_90);
  ExceptionList = local_8;
  return;
}

