
void __thiscall FUN_00462c50(void *this,CDDSurface *param_1)

{
  int iVar1;
  int iVar2;
  int *piVar3;
  int iVar4;
  CDDSurface *pCVar5;
  abilityrecord *paVar6;
  int iVar7;
  int iVar8;
  int iVar9;
  byte *pbVar10;
  _DDSURFACEDESC2 *p_Var11;
  undefined4 *puVar12;
  CString *pCVar13;
  byte *unaff_EDI;
  undefined4 *puVar14;
  undefined4 auStack_14c [24];
  undefined4 uStack_ec;
  int iStack_e8;
  int iStack_e4;
  tagRECT local_a8;
  _DDSURFACEDESC2 a_Stack_98 [4];
  undefined4 auStack_94 [8];
  void *pvStack_74;
  void *pvStack_14;
  void *pvStack_c;
  undefined1 *puStack_8;
  CDDSurface *pCStack_4;
  
  pCStack_4 = (CDDSurface *)0xffffffff;
  puStack_8 = &LAB_0048005b;
  pvStack_c = ExceptionList;
  iVar9 = *(int *)((int)this + 0x13c);
  local_a8.left = *(LONG *)((int)this + 0x1b4);
  local_a8.top = *(LONG *)((int)this + 0x1b8);
  local_a8.right = *(LONG *)((int)this + 0x1bc);
  local_a8.bottom = *(LONG *)((int)this + 0x1c0);
  ExceptionList = &pvStack_c;
  OffsetRect(&local_a8,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  CDDSurface::BltFast(param_1);
  local_a8.left = 0x20;
  local_a8.top = 0x20;
  iVar1 = *(int *)((int)this + 0x1c4);
  local_a8.right = 0x40;
  local_a8.bottom = 0x40;
  iVar2 = *(int *)((int)this + 0x1c8);
  iStack_e4 = 0x462d1c;
  OffsetRect((LPRECT)&stack0xffffff48,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  iVar4 = thunk_FUN_00477a90((int)*(short *)(iVar9 + 10));
  pCVar5 = GetCharSurface(iVar4,-1);
  if (pCVar5 != (CDDSurface *)0x0) {
    uStack_ec = 0x462d52;
    iStack_e8 = iVar1;
    iStack_e4 = iVar2;
    CDDSurface::BltFast(param_1);
  }
  CDDSurface::GetDescription(param_1,a_Stack_98);
  pvStack_74 = CDDSurface::Lock(param_1);
  iVar7 = *(int *)((int)this + 0x1d8) + *(int *)((int)this + 0x44);
  iVar4 = *(int *)((int)this + 0x1d4) + *(int *)((int)this + 0x40);
  CString::CString((CString *)&stack0xffffff3c);
  pvStack_14 = (void *)0x0;
  CString::operator=((CString *)&stack0xffffff3c,(char *)(iVar9 + 0xbb));
  piVar3 = *(int **)((int)this + 0xac);
  if (*piVar3 == 0) {
    p_Var11 = a_Stack_98;
    puVar12 = (undefined4 *)&stack0xfffffeac;
    for (iVar8 = 0x1f; iVar8 != 0; iVar8 = iVar8 + -1) {
      *puVar12 = *(undefined4 *)p_Var11;
      p_Var11 = p_Var11 + 4;
      puVar12 = puVar12 + 1;
    }
    thunk_FUN_0043e0c0(piVar3,iVar4,iVar7,unaff_EDI,0);
  }
  else {
    p_Var11 = a_Stack_98;
    puVar12 = (undefined4 *)&stack0xfffffeac;
    for (iVar8 = 0x1f; iVar8 != 0; iVar8 = iVar8 + -1) {
      *puVar12 = *(undefined4 *)p_Var11;
      p_Var11 = p_Var11 + 4;
      puVar12 = puVar12 + 1;
    }
    thunk_FUN_0043ddf0(piVar3,iVar4,iVar7,unaff_EDI,0);
  }
  iVar4 = *(int *)((int)this + 0x1dc) + *(int *)((int)this + 0x40);
  iVar7 = *(int *)((int)this + 0x1e0) + *(int *)((int)this + 0x44);
  paVar6 = GetCharAbility((int)*(short *)(iVar9 + 10));
  CString::operator=((CString *)&stack0xffffff3c,(char *)(paVar6 + 0xa0));
  piVar3 = *(int **)((int)this + 0xac);
  if (*piVar3 == 0) {
    p_Var11 = a_Stack_98;
    puVar12 = (undefined4 *)&stack0xfffffeac;
    for (iVar9 = 0x1f; iVar9 != 0; iVar9 = iVar9 + -1) {
      *puVar12 = *(undefined4 *)p_Var11;
      p_Var11 = p_Var11 + 4;
      puVar12 = puVar12 + 1;
    }
    thunk_FUN_0043e0c0(piVar3,iVar4,iVar7,unaff_EDI,0);
  }
  else {
    p_Var11 = a_Stack_98;
    puVar12 = (undefined4 *)&stack0xfffffeac;
    for (iVar9 = 0x1f; iVar9 != 0; iVar9 = iVar9 + -1) {
      *puVar12 = *(undefined4 *)p_Var11;
      p_Var11 = p_Var11 + 4;
      puVar12 = puVar12 + 1;
    }
    thunk_FUN_0043ddf0(piVar3,iVar4,iVar7,unaff_EDI,0);
  }
  pbVar10 = (byte *)(*(int *)((int)this + 0x1e4) + *(int *)((int)this + 0x40));
  iVar9 = *(int *)((int)this + 0x1e8) + *(int *)((int)this + 0x44);
  iStack_e4 = 0x462f02;
  CString::Format((CString *)&stack0xffffff3c,(char *)&stack0xffffff3c);
  piVar3 = *(int **)((int)this + 0xac);
  if (*piVar3 == 0) {
    puVar12 = auStack_94;
    puVar14 = (undefined4 *)&stack0xfffffeb0;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar14 = *puVar12;
      puVar12 = puVar12 + 1;
      puVar14 = puVar14 + 1;
    }
    thunk_FUN_0043e0c0(piVar3,iVar9,iVar1,pbVar10,0);
  }
  else {
    puVar12 = auStack_94;
    puVar14 = (undefined4 *)&stack0xfffffeb0;
    for (iVar4 = 0x1f; iVar4 != 0; iVar4 = iVar4 + -1) {
      *puVar14 = *puVar12;
      puVar12 = puVar12 + 1;
      puVar14 = puVar14 + 1;
    }
    thunk_FUN_0043ddf0(piVar3,iVar9,iVar1,pbVar10,0);
  }
  pbVar10 = (byte *)(*(int *)((int)this + 0x1ec) + *(int *)((int)this + 0x40));
  pCVar13 = *(CString **)((int)this + 0x44) + *(int *)((int)this + 0x1f0);
  CString::Format(*(CString **)((int)this + 0x44),&stack0xffffff40);
  piVar3 = *(int **)((int)this + 0xac);
  if (*piVar3 == 0) {
    puVar12 = auStack_94;
    puVar14 = auStack_14c;
    for (iVar9 = 0x1f; puVar12 = puVar12 + 1, iVar9 != 0; iVar9 = iVar9 + -1) {
      *puVar14 = *puVar12;
      puVar14 = puVar14 + 1;
    }
    thunk_FUN_0043e0c0(piVar3,(int)pCVar13,iVar2,pbVar10,0);
  }
  else {
    puVar12 = auStack_94;
    puVar14 = auStack_14c;
    for (iVar9 = 0x1f; puVar12 = puVar12 + 1, iVar9 != 0; iVar9 = iVar9 + -1) {
      *puVar14 = *puVar12;
      puVar14 = puVar14 + 1;
    }
    thunk_FUN_0043ddf0(piVar3,(int)pCVar13,iVar2,pbVar10,0);
  }
  CDDSurface::Unlock(pCStack_4);
  pvStack_c = (void *)0xffffffff;
  CString::~CString((CString *)&stack0xffffff44);
  ExceptionList = pvStack_14;
  return;
}

