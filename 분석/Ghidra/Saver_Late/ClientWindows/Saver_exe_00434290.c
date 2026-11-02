
undefined4 __fastcall FUN_00434290(void *param_1)

{
  undefined4 *puVar1;
  int iVar2;
  CDDSurface *pCVar3;
  int iVar4;
  void *this;
  void *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047d7f8;
  local_c = ExceptionList;
  iVar4 = 0;
  if (*(int *)((int)param_1 + 0x80) != 0) {
    return 1;
  }
  ExceptionList = &local_c;
  local_10 = param_1;
  thunk_FUN_004340d0((int)param_1);
  local_4 = 0;
  this = DAT_004953e4;
  if (*(int *)((int)param_1 + 0x94) == 0) {
    this = DAT_004953e0;
  }
  thunk_FUN_00432f20(this,(CString *)&local_10);
  if (*(int *)((int)this + 0x18) == 0) {
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_10);
    ExceptionList = local_c;
    return 0;
  }
  thunk_FUN_00434710(param_1,*(undefined4 *)((int)this + 0x38),*(undefined4 *)((int)this + 0x3c));
  iVar2 = *(int *)((int)param_1 + 0x4c) * *(int *)((int)param_1 + 0x48);
  *(uint *)((int)param_1 + 0x8c) = (uint)((int)(iVar2 + (iVar2 >> 0x1f & 3U)) >> 2 < 0x12c00);
  if (*(int **)((int)param_1 + 8) != (int *)0x0) {
    (**(code **)(**(int **)((int)param_1 + 8) + 4))(1);
    *(undefined4 *)((int)param_1 + 8) = 0;
  }
  pCVar3 = thunk_FUN_00432690(this,0);
  *(CDDSurface **)((int)param_1 + 8) = pCVar3;
  *(undefined4 *)((int)param_1 + 0x30) = *(undefined4 *)((int)this + 0x40);
  *(undefined4 *)((int)param_1 + 0x34) = *(undefined4 *)((int)this + 0x44);
  *(undefined4 *)((int)param_1 + 0x38) = *(undefined4 *)((int)this + 0x48);
  *(undefined4 *)((int)param_1 + 0x3c) = *(undefined4 *)((int)this + 0x4c);
  iVar2 = *(int *)((int)param_1 + 0x58);
  if (0 < iVar2) {
    do {
      puVar1 = *(undefined4 **)(*(int *)((int)param_1 + 0x54) + iVar4 * 4);
      if (puVar1 != (undefined4 *)0x0) {
        (**(code **)*puVar1)(1);
      }
      iVar4 = iVar4 + 1;
    } while (iVar4 < iVar2);
  }
  CPtrArray::SetSize((CPtrArray *)((int)param_1 + 0x50),0,-1);
  thunk_FUN_00432ce0(this,(CPtrArray *)((int)param_1 + 0x50));
  iVar4 = *(int *)((int)param_1 + 0x6c);
  iVar2 = 0;
  if (0 < iVar4) {
    do {
      puVar1 = *(undefined4 **)(*(int *)((int)param_1 + 0x68) + iVar2 * 4);
      if (puVar1 != (undefined4 *)0x0) {
        (**(code **)*puVar1)(1);
      }
      iVar2 = iVar2 + 1;
    } while (iVar2 < iVar4);
  }
  CPtrArray::SetSize((CPtrArray *)((int)param_1 + 100),0,-1);
  thunk_FUN_00432e00(this,(CPtrArray *)((int)param_1 + 100));
  *(undefined4 *)((int)param_1 + 0x78) = 1;
  *(undefined4 *)((int)param_1 + 0x7c) = 0;
  CString::operator=((CString *)((int)param_1 + 0xc),(CString *)&local_10);
  *(undefined4 *)((int)param_1 + 0x80) = 1;
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_10);
  ExceptionList = local_c;
  return 1;
}

