
void __thiscall FUN_0046f7f0(void *this,CDDSurface *param_1)

{
  abilityrecord aVar1;
  int *piVar2;
  undefined1 auVar3 [4];
  abilityrecord *paVar4;
  CString *this_00;
  int iVar5;
  CString *extraout_ECX;
  CString *extraout_ECX_00;
  uint uVar6;
  uint uVar7;
  CString *this_01;
  int iVar8;
  CString *extraout_ECX_01;
  CString *this_02;
  int iVar9;
  byte *pbVar10;
  void **ppvVar11;
  abilityrecord *paVar12;
  undefined4 *puVar13;
  void *in_stack_00000014;
  undefined4 uStack0000001c;
  CDDSurface *in_stack_00000024;
  undefined4 uStack_12c;
  undefined4 auStack_118 [18];
  undefined4 uStack_d0;
  byte *in_stack_ffffff5c;
  undefined1 local_9c [4];
  int local_98;
  int local_94;
  int local_90;
  byte *local_8c;
  abilityrecord *local_88;
  void *local_84;
  int local_80;
  int local_7c;
  int local_78;
  void *local_6c [24];
  void *pvStack_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_004809c6;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CDDSurface::GetDescription(param_1,(_DDSURFACEDESC2 *)&local_88);
  local_6c[2] = CDDSurface::Lock(param_1);
  iVar8 = *(int *)((int)this + 0x184);
  CString::CString((CString *)&stack0xffffff58);
  local_4 = 0;
  local_9c = *(undefined1 (*) [4])((int)this + 200);
  local_98 = *(int *)((int)this + 0xcc);
  local_94 = *(int *)((int)this + 0xd0);
  OffsetRect((LPRECT)&stack0xffffff60,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  CString::Format(this_00,&stack0xffffff58);
  auVar3 = local_9c;
  piVar2 = *(int **)((int)this + 0xac);
  if (*piVar2 == 0) {
    ppvVar11 = &local_84;
    puVar13 = (undefined4 *)&stack0xfffffebc;
    for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
      *puVar13 = *ppvVar11;
      ppvVar11 = ppvVar11 + 1;
      puVar13 = puVar13 + 1;
    }
    thunk_FUN_0043ea00(piVar2,(int)auVar3,local_98,local_94,local_90,in_stack_ffffff5c,0);
    this_02 = extraout_ECX_00;
  }
  else {
    ppvVar11 = &local_84;
    puVar13 = (undefined4 *)&stack0xfffffebc;
    for (iVar5 = 0x1f; iVar5 != 0; iVar5 = iVar5 + -1) {
      *puVar13 = *ppvVar11;
      ppvVar11 = ppvVar11 + 1;
      puVar13 = puVar13 + 1;
    }
    thunk_FUN_0043e390(piVar2,(int)local_9c,local_98,local_94,local_90,in_stack_ffffff5c,0);
    this_02 = extraout_ECX;
  }
  pbVar10 = (byte *)(int)*(short *)(iVar8 + 0x9a);
  if (pbVar10 == (byte *)0xffffffff) {
    local_8c = pbVar10;
    if (*(int *)((int)this + 0x188) == -1) goto LAB_0046fbbe;
    this_02 = *(CString **)((int)this + 0x178);
    pbVar10 = *(byte **)(this_02 + *(int *)((int)this + 0x188) * 4);
    local_8c = pbVar10;
    if (pbVar10 == (byte *)0xffffffff) goto LAB_0046fbbe;
  }
  local_8c = pbVar10;
  paVar4 = GetCharAbility((int)pbVar10);
  CString::CString((CString *)&stack0xffffff60);
  local_9c = *(undefined1 (*) [4])((int)this + 0xd4);
  local_98 = *(int *)((int)this + 0xd8);
  local_94 = *(int *)((int)this + 0xdc);
  local_90 = *(int *)((int)this + 0xe0);
  OffsetRect((LPRECT)local_9c,*(int *)((int)this + 0x40),*(int *)((int)this + 0x44));
  if (paVar4 != (abilityrecord *)0x0) {
    local_88 = operator_new(0xff);
    *local_88 = (abilityrecord)0x0;
    if (paVar4[0xa0] != (abilityrecord)0x0) {
      uVar6 = 0xffffffff;
      paVar4 = paVar4 + 0xa0;
      do {
        paVar12 = paVar4;
        if (uVar6 == 0) break;
        uVar6 = uVar6 - 1;
        paVar12 = paVar4 + 1;
        aVar1 = *paVar4;
        paVar4 = paVar12;
      } while (aVar1 != (abilityrecord)0x0);
      uVar6 = ~uVar6;
      paVar4 = paVar12 + -uVar6;
      paVar12 = local_88;
      for (uVar7 = uVar6 >> 2; uVar7 != 0; uVar7 = uVar7 - 1) {
        *(undefined4 *)paVar12 = *(undefined4 *)paVar4;
        paVar4 = paVar4 + 4;
        paVar12 = paVar12 + 4;
      }
      for (uVar6 = uVar6 & 3; pbVar10 = local_8c, uVar6 != 0; uVar6 = uVar6 - 1) {
        *paVar12 = *paVar4;
        paVar4 = paVar4 + 1;
        paVar12 = paVar12 + 1;
      }
    }
    CString::Format((CString *)&stack0xffffff5c,(char *)&stack0xffffff5c);
    operator_delete(local_84);
    uStack_d0 = 0x46fa4f;
    CString::Format((CString *)local_9c,(char *)local_9c);
    CString::operator+=((CString *)local_9c,(CString *)&local_98);
    CString::Format((CString *)&local_98,(char *)&local_98);
    CString::operator+=((CString *)&local_98,(CString *)&local_94);
    CString::Format((CString *)&local_94,(char *)&local_94);
    CString::operator+=((CString *)&local_94,(CString *)&local_90);
    CString::Format((CString *)&local_90,(char *)&local_90);
    CString::operator+=((CString *)&local_90,(CString *)&local_8c);
    thunk_FUN_00470020(this,(int)pbVar10);
    CString::Format(this_01,(char *)&local_8c);
    CString::operator+=((CString *)&local_8c,(CString *)&local_88);
    pbVar10 = local_8c;
    piVar2 = *(int **)((int)this + 0xac);
    if (*piVar2 == 0) {
      ppvVar11 = local_6c;
      puVar13 = &uStack_12c;
      for (iVar8 = 0x1f; iVar8 != 0; iVar8 = iVar8 + -1) {
        *puVar13 = *ppvVar11;
        ppvVar11 = ppvVar11 + 1;
        puVar13 = puVar13 + 1;
      }
      thunk_FUN_0043ea00(piVar2,(int)local_84,local_80,local_7c,local_78,local_8c,0);
    }
    else {
      ppvVar11 = local_6c;
      puVar13 = &uStack_12c;
      for (iVar8 = 0x1f; iVar8 != 0; iVar8 = iVar8 + -1) {
        *puVar13 = *ppvVar11;
        ppvVar11 = ppvVar11 + 1;
        puVar13 = puVar13 + 1;
      }
      thunk_FUN_0043e390(piVar2,(int)local_84,local_80,local_7c,local_78,pbVar10,0);
    }
  }
  CString::~CString((CString *)&local_88);
  this_02 = extraout_ECX_01;
LAB_0046fbbe:
  CString::Format(this_02,(char *)&local_8c);
  piVar2 = *(int **)((int)this + 0xac);
  iVar8 = *(int *)((int)this + 0x44) + 0xf3;
  iVar5 = *(int *)((int)this + 0x40) + 0x74;
  local_9c = (undefined1  [4])0x2;
  if (*piVar2 == 0) {
    ppvVar11 = local_6c;
    puVar13 = auStack_118;
    for (iVar9 = 0x1f; ppvVar11 = ppvVar11 + 1, iVar9 != 0; iVar9 = iVar9 + -1) {
      *puVar13 = *ppvVar11;
      puVar13 = puVar13 + 1;
    }
    uStack_12c = 0x46fc4c;
    thunk_FUN_0043e0c0(piVar2,iVar5,iVar8,(byte *)local_88,0xffff);
  }
  else {
    ppvVar11 = local_6c;
    puVar13 = auStack_118;
    for (iVar9 = 0x1f; ppvVar11 = ppvVar11 + 1, iVar9 != 0; iVar9 = iVar9 + -1) {
      *puVar13 = *ppvVar11;
      puVar13 = puVar13 + 1;
    }
    uStack_12c = 0x46fc21;
    thunk_FUN_0043ddf0(piVar2,iVar5,iVar8,(byte *)local_88,0xff);
  }
  local_9c = (undefined1  [4])0x46fc59;
  CDDSurface::Unlock(in_stack_00000024);
  uStack0000001c = 0xffffffff;
  local_9c = (undefined1  [4])0x46fc6d;
  CString::~CString((CString *)&local_88);
  ExceptionList = in_stack_00000014;
  return;
}

