
void __thiscall FUN_00453380(void *this,CDDSurface *param_1)

{
  int *piVar1;
  byte *pbVar2;
  LONG LVar3;
  int iVar4;
  _DDSURFACEDESC2 *p_Var5;
  undefined4 *puVar6;
  undefined4 auStack_13c [30];
  undefined4 uStack_c4;
  tagRECT local_9c;
  CString local_8c [4];
  _DDSURFACEDESC2 local_88 [36];
  void *local_64;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047ee7b;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_8c);
  local_4 = 0;
  CDDSurface::GetDescription(param_1,local_88);
  local_64 = CDDSurface::Lock(param_1);
  uStack_c4 = 0x4533ea;
  SetRect(&local_9c,0x21,5,0x71,0x15);
  OffsetRect(&local_9c,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  LVar3 = local_9c.left;
  piVar1 = *(int **)((int)this + 0xac);
  pbVar2 = *(byte **)((int)this + 0xb8);
  if (*piVar1 == 0) {
    p_Var5 = local_88;
    puVar6 = auStack_13c;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar6 = *(undefined4 *)p_Var5;
      p_Var5 = p_Var5 + 4;
      puVar6 = puVar6 + 1;
    }
    thunk_FUN_0043ea00(piVar1,LVar3,local_9c.top,local_9c.right,local_9c.bottom,pbVar2,0);
  }
  else {
    p_Var5 = local_88;
    puVar6 = auStack_13c;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar6 = *(undefined4 *)p_Var5;
      p_Var5 = p_Var5 + 4;
      puVar6 = puVar6 + 1;
    }
    thunk_FUN_0043e390(piVar1,LVar3,local_9c.top,local_9c.right,local_9c.bottom,pbVar2,0);
  }
  uStack_c4 = 0x4534bc;
  SetRect(&local_9c,0x20,4,0x70,0x14);
  OffsetRect(&local_9c,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  pbVar2 = *(byte **)((int)this + 0xb8);
  piVar1 = *(int **)((int)this + 0xac);
  if (*piVar1 == 0) {
    p_Var5 = local_88;
    puVar6 = auStack_13c;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar6 = *(undefined4 *)p_Var5;
      p_Var5 = p_Var5 + 4;
      puVar6 = puVar6 + 1;
    }
    thunk_FUN_0043ea00(piVar1,local_9c.left,local_9c.top,local_9c.right,local_9c.bottom,pbVar2,0x1f)
    ;
  }
  else {
    p_Var5 = local_88;
    puVar6 = auStack_13c;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar6 = *(undefined4 *)p_Var5;
      p_Var5 = p_Var5 + 4;
      puVar6 = puVar6 + 1;
    }
    thunk_FUN_0043e390(piVar1,local_9c.left,local_9c.top,local_9c.right,local_9c.bottom,pbVar2,0x1f)
    ;
  }
  CDDSurface::Unlock(param_1);
  local_4 = 0xffffffff;
  CString::~CString(local_8c);
  ExceptionList = local_c;
  return;
}

