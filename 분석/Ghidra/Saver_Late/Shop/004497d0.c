
undefined4 __thiscall FUN_004497d0(void *this,undefined4 param_1,CDDSurface *param_2)

{
  int iVar1;
  int iVar2;
  int *this_00;
  int iVar3;
  _DDSURFACEDESC2 *p_Var4;
  undefined4 *puVar5;
  undefined4 auStack_118 [25];
  undefined4 uStack_b4;
  CString local_8c [4];
  byte *local_88;
  _DDSURFACEDESC2 local_84 [36];
  void *local_60;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047e70b;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_8c);
  local_4 = 0;
  uStack_b4 = 0x449829;
  CString::Format(*(CString **)((int)this + 0xb8),(char *)local_8c);
  iVar1 = *(int *)((int)this + 0x44) + 0x13;
  iVar2 = *(int *)((int)this + 0x40) + 0x108;
  CDDSurface::GetDescription(param_2,local_84);
  local_60 = CDDSurface::Lock(param_2);
  this_00 = *(int **)((int)this + 0xac);
  if (*this_00 == 0) {
    p_Var4 = local_84;
    puVar5 = auStack_118;
    for (iVar3 = 0x1f; iVar3 != 0; iVar3 = iVar3 + -1) {
      *puVar5 = *(undefined4 *)p_Var4;
      p_Var4 = p_Var4 + 4;
      puVar5 = puVar5 + 1;
    }
    thunk_FUN_0043e0c0(this_00,iVar2,iVar1,local_88,0);
  }
  else {
    p_Var4 = local_84;
    puVar5 = auStack_118;
    for (iVar3 = 0x1f; iVar3 != 0; iVar3 = iVar3 + -1) {
      *puVar5 = *(undefined4 *)p_Var4;
      p_Var4 = p_Var4 + 4;
      puVar5 = puVar5 + 1;
    }
    thunk_FUN_0043ddf0(this_00,iVar2,iVar1,local_88,0);
  }
  CDDSurface::Unlock(param_2);
  CString::~CString((CString *)&local_88);
  ExceptionList = local_8;
  return 1;
}

