
undefined4 __thiscall FUN_0046ece0(void *this,CDDSurface *param_1)

{
  int *this_00;
  byte *pbVar1;
  int iVar2;
  undefined4 *puVar3;
  undefined4 *puVar4;
  undefined4 auStack_124 [27];
  undefined4 uStack_b8;
  char *pcVar5;
  CString *local_98;
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
  local_8 = &LAB_00480926;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CDDSurface::GetDescription(param_1,(_DDSURFACEDESC2 *)&local_88);
  local_64 = CDDSurface::Lock(param_1);
  local_8c = *(int *)((int)this + 0x44) + 0x10;
  local_90 = (byte *)(*(int *)((int)this + 0x40) + 0x78);
  CString::CString((CString *)&local_98);
  local_4 = 0;
  CString::CString(local_94);
  local_4 = CONCAT31(local_4._1_3_,1);
  if (*(int *)((int)this + 0xcc) == 0) {
    pcVar5 = &DAT_00490e80;
  }
  else {
    pcVar5 = &DAT_00490e88;
  }
  CString::operator=((CString *)&local_98,pcVar5);
  uStack_b8 = 0x46ed8d;
  CString::Format(local_98,(char *)local_94);
  pbVar1 = local_90;
  this_00 = *(int **)((int)this + 0xac);
  if (*this_00 == 0) {
    puVar3 = local_84;
    puVar4 = auStack_124;
    for (iVar2 = 0x1f; iVar2 != 0; iVar2 = iVar2 + -1) {
      *puVar4 = *puVar3;
      puVar3 = puVar3 + 1;
      puVar4 = puVar4 + 1;
    }
    thunk_FUN_0043e0c0(this_00,local_8c,local_88,pbVar1,0);
  }
  else {
    puVar3 = local_84;
    puVar4 = auStack_124;
    for (iVar2 = 0x1f; iVar2 != 0; iVar2 = iVar2 + -1) {
      *puVar4 = *puVar3;
      puVar3 = puVar3 + 1;
      puVar4 = puVar4 + 1;
    }
    thunk_FUN_0043ddf0(this_00,local_8c,local_88,local_90,0);
  }
  CDDSurface::Unlock(param_1);
  uStack_b8 = 0x46ee26;
  thunk_FUN_004237b0(*(void **)((int)this + 0xb8),param_1,0xffff,*(int *)((int)this + 0x40),
                     *(int *)((int)this + 0x44));
  CString::~CString((CString *)&local_90);
  CString::~CString(local_94);
  ExceptionList = local_8;
  return 1;
}

