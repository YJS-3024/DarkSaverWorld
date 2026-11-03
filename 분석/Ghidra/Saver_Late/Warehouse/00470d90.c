
undefined4 __thiscall FUN_00470d90(void *this,CDDSurface *param_1)

{
  int iVar1;
  int *this_00;
  int iVar2;
  undefined4 *puVar3;
  undefined4 *puVar4;
  undefined4 auStack_120 [26];
  undefined4 uStack_b8;
  CString local_94 [4];
  byte *local_90;
  int local_8c;
  int local_88;
  undefined4 local_84 [8];
  void *local_64;
  void *pvStack_c;
  undefined *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &DAT_00480a2b;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  thunk_FUN_0044a790(this,param_1);
  if (*(int *)((int)*(void **)((int)this + 0x354) + 0x28) != 0) {
    uStack_b8 = 0x470ddb;
    thunk_FUN_00437c40(*(void **)((int)this + 0x354),param_1,*(int *)((int)this + 0x40),
                       *(int *)((int)this + 0x44),0);
  }
  thunk_FUN_00470f70(this,param_1);
  thunk_FUN_0044a310(this,param_1);
  if (*(int *)((int)this + 0x36c) != 0) {
    thunk_FUN_0044a490(this,param_1);
  }
  thunk_FUN_0044ac40(this,param_1);
  CDDSurface::GetDescription(param_1,(_DDSURFACEDESC2 *)&local_88);
  local_64 = CDDSurface::Lock(param_1);
  local_8c = *(int *)((int)this + 0x44) + 0x10a;
  iVar1 = *(int *)((int)this + 0x40) + 0x96;
  CString::CString(local_94);
  local_4 = 0;
  CString::Format(*(CString **)((int)this + 0x370),(char *)local_94);
  this_00 = *(int **)((int)this + 0xac);
  if (*this_00 == 0) {
    puVar3 = local_84;
    puVar4 = auStack_120;
    for (iVar2 = 0x1f; iVar2 != 0; iVar2 = iVar2 + -1) {
      *puVar4 = *puVar3;
      puVar3 = puVar3 + 1;
      puVar4 = puVar4 + 1;
    }
    thunk_FUN_0043e0c0(this_00,iVar1,local_88,local_90,0);
  }
  else {
    puVar3 = local_84;
    puVar4 = auStack_120;
    for (iVar2 = 0x1f; iVar2 != 0; iVar2 = iVar2 + -1) {
      *puVar4 = *puVar3;
      puVar3 = puVar3 + 1;
      puVar4 = puVar4 + 1;
    }
    thunk_FUN_0043ddf0(this_00,iVar1,local_88,local_90,0);
  }
  CDDSurface::Unlock(param_1);
  CString::~CString((CString *)&local_90);
  ExceptionList = local_8;
  return 1;
}

