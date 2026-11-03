
void __thiscall FUN_00463170(void *this,CDDSurface *param_1)

{
  int *piVar1;
  byte *pbVar2;
  int iVar3;
  CDDSurface *pCVar4;
  int iVar5;
  int iVar6;
  _DDSURFACEDESC2 *p_Var7;
  undefined4 *puVar8;
  undefined4 auStack_138 [26];
  undefined4 uStack_d0;
  LONG LStack_cc;
  LONG LStack_c8;
  tagRECT local_a8;
  int local_98;
  undefined4 local_94;
  undefined4 local_90;
  undefined4 local_8c;
  undefined4 local_88;
  int iStack_80;
  _DDSURFACEDESC2 a_Stack_7c [36];
  void *pvStack_58;
  
  local_98 = *(int *)((int)this + 0x13c) + 4;
  if ((*(short *)(*(int *)((int)this + 0x13c) + 0x3e) != 0) && (*(int *)((int)this + 0xf8) != 0)) {
    local_94 = 0x20;
    local_90 = 0x20;
    local_8c = 0x40;
    local_88 = 0x40;
    local_a8.left = *(LONG *)((int)this + 0x38c);
    local_a8.top = *(LONG *)((int)this + 0x390);
    local_a8.right = *(LONG *)((int)this + 0x394);
    local_a8.bottom = *(LONG *)((int)this + 0x398);
    LStack_c8 = 0x4631f6;
    OffsetRect(&local_a8,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    if (*(int *)((int)this + 0xec) != -1) {
      iVar3 = thunk_FUN_00477a90(*(int *)((int)this + 0xec));
      pCVar4 = GetCharSurface(iVar3,-1);
      if (pCVar4 != (CDDSurface *)0x0) {
        LStack_c8 = local_a8.top;
        LStack_cc = local_a8.left;
        uStack_d0 = 0x46323c;
        CDDSurface::BltFast(param_1);
      }
    }
    local_a8.left = *(LONG *)((int)this + 0x39c);
    local_a8.top = *(LONG *)((int)this + 0x3a0);
    local_a8.right = *(LONG *)((int)this + 0x3a4);
    local_a8.bottom = *(LONG *)((int)this + 0x3a8);
    LStack_c8 = 0x463279;
    OffsetRect(&local_a8,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    iVar3 = thunk_FUN_00477a90((int)*(short *)(local_98 + 6));
    pCVar4 = GetCharSurface(iVar3,-1);
    if (pCVar4 != (CDDSurface *)0x0) {
      LStack_c8 = local_a8.top;
      LStack_cc = local_a8.left;
      uStack_d0 = 0x4632af;
      CDDSurface::BltFast(param_1);
    }
    local_a8.left = *(LONG *)((int)this + 0x3ac);
    local_a8.top = *(LONG *)((int)this + 0x3b0);
    local_a8.right = *(LONG *)((int)this + 0x3b4);
    local_a8.bottom = *(LONG *)((int)this + 0x3b8);
    LStack_c8 = 0x4632e3;
    OffsetRect(&local_a8,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    if (*(int *)((int)this + 0xe8) != -1) {
      iVar3 = thunk_FUN_00477a90(*(int *)((int)this + 0xe8));
      pCVar4 = GetCharSurface(iVar3,-1);
      if (pCVar4 != (CDDSurface *)0x0) {
        LStack_c8 = local_a8.top;
        LStack_cc = local_a8.left;
        uStack_d0 = 0x46331c;
        CDDSurface::BltFast(param_1);
      }
    }
    local_a8.left = *(LONG *)((int)this + 0x3bc);
    local_a8.top = *(LONG *)((int)this + 0x3c0);
    local_a8.right = *(LONG *)((int)this + 0x3c4);
    local_a8.bottom = *(LONG *)((int)this + 0x3c8);
    LStack_c8 = 0x463350;
    OffsetRect(&local_a8,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
    iVar3 = *(int *)((int)this + *(int *)((int)this + 0xfc) * 4 + 0x100);
    if (iVar3 != -1) {
      iVar3 = thunk_FUN_00477a90(iVar3);
      pCVar4 = GetCharSurface(iVar3,-1);
      if (pCVar4 != (CDDSurface *)0x0) {
        LStack_c8 = local_a8.top;
        LStack_cc = local_a8.left;
        uStack_d0 = 0x463390;
        CDDSurface::BltFast(param_1);
      }
    }
    CDDSurface::GetDescription(param_1,a_Stack_7c);
    pvStack_58 = CDDSurface::Lock(param_1);
    local_98 = ((*(int *)((int)this + 0x3d8) - *(int *)((int)this + 0x3d0)) + -0x1c) / 3;
    iVar3 = *(int *)((int)this + 0x3cc) + 2 + *(int *)((int)this + 0x40);
    piVar1 = *(int **)((int)this + 0xac);
    iVar6 = *(int *)((int)this + 0x3d0) + local_98 + *(int *)((int)this + 0x44);
    pbVar2 = *(byte **)((int)this + 0x128);
    iStack_80 = iVar6;
    if (*piVar1 == 0) {
      p_Var7 = a_Stack_7c;
      puVar8 = auStack_138;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar8 = *(undefined4 *)p_Var7;
        p_Var7 = p_Var7 + 4;
        puVar8 = puVar8 + 1;
      }
      thunk_FUN_0043e0c0(piVar1,iVar3,iVar6,pbVar2,0);
    }
    else {
      p_Var7 = a_Stack_7c;
      puVar8 = auStack_138;
      for (iVar6 = 0x1f; iVar6 != 0; iVar6 = iVar6 + -1) {
        *puVar8 = *(undefined4 *)p_Var7;
        p_Var7 = p_Var7 + 4;
        puVar8 = puVar8 + 1;
      }
      thunk_FUN_0043ddf0(piVar1,iVar3,iStack_80,pbVar2,0);
    }
    pbVar2 = *(byte **)((int)this + 300);
    piVar1 = *(int **)((int)this + 0xac);
    iVar6 = *(int *)((int)this + 0x3d0) + 0xe + local_98 * 2 + *(int *)((int)this + 0x44);
    iVar3 = *(int *)((int)this + 0x3cc) + 2 + *(int *)((int)this + 0x40);
    if (*piVar1 == 0) {
      p_Var7 = a_Stack_7c;
      puVar8 = auStack_138;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar8 = *(undefined4 *)p_Var7;
        p_Var7 = p_Var7 + 4;
        puVar8 = puVar8 + 1;
      }
      thunk_FUN_0043e0c0(piVar1,iVar3,iVar6,pbVar2,0);
    }
    else {
      p_Var7 = a_Stack_7c;
      puVar8 = auStack_138;
      for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
        *puVar8 = *(undefined4 *)p_Var7;
        p_Var7 = p_Var7 + 4;
        puVar8 = puVar8 + 1;
      }
      thunk_FUN_0043ddf0(piVar1,iVar3,iVar6,pbVar2,0);
    }
    CDDSurface::Unlock(param_1);
  }
  return;
}

