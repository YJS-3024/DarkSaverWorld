
undefined4 __fastcall FUN_1000ac50(void *param_1)

{
  int iVar1;
  int iVar2;
  CString *pCVar3;
  character *pcVar4;
  itemrecord *piVar5;
  clock_t cVar6;
  int extraout_ECX;
  int extraout_ECX_00;
  int extraout_ECX_01;
  uint uVar7;
  int iVar8;
  int *piVar9;
  bool bVar10;
  CString local_38 [4];
  undefined4 local_34;
  CString *local_30;
  undefined1 local_2c [12];
  int local_20;
  int local_1c;
  int local_18;
  undefined4 local_14;
  undefined4 local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_100152c8;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_38);
  iVar2 = DAT_1001c464;
  iVar8 = DAT_1001c460;
  local_4 = 0;
  pcVar4 = GetUnitInfoP(*(int *)((int)param_1 + 0xc));
  FUN_10008d40(0,0,0x280,0x1e0);
  if (((((*(int *)pcVar4 == 0) || (*(int *)(pcVar4 + 4) == 0)) || (((byte)pcVar4[0x100] & 1) != 0))
      || ((iVar1 = *(int *)(pcVar4 + 0x70), iVar1 < iVar8 + 1 ||
          (*(int *)(pcVar4 + 0x74) < iVar2 + 1)))) ||
     ((DAT_1001a0b8 + -2 + iVar8 < iVar1 || (DAT_1001a0bc + -1 + iVar2 < *(int *)(pcVar4 + 0x74)))))
  {
    local_4 = 0xffffffff;
    CString::~CString(local_38);
    ExceptionList = local_c;
    return 0;
  }
  *(int *)((int)param_1 + 0x20) = (iVar1 - iVar8) * 0x20;
  *(int *)((int)param_1 + 0x24) = (*(int *)(pcVar4 + 0x74) - iVar2) * 0x20;
  _drawactivecharacter(*(int *)((int)param_1 + 0xc));
  iVar8 = 0;
  local_30 = (CString *)((int)param_1 + 0x180);
  local_34 = 0;
  piVar9 = (int *)((int)param_1 + 0xe4);
  do {
    iVar8 = iVar8 + *(int *)((int)param_1 + 0x200);
    if (*(int *)((int)param_1 + 0x1fc) <= iVar8) break;
    piVar5 = GetItemTable(*(int *)((int)param_1 + iVar8 * 8 + 0x204));
    pCVar3 = local_30;
    *piVar9 = (*(int *)(piVar5 + 4) / 100) * 0x20;
    iVar8 = (*(int *)(piVar5 + 4) % 100) * 0x20;
    piVar9[-1] = iVar8;
    piVar9[1] = iVar8 + 0x20;
    piVar9[2] = *piVar9 + 0x20;
    CString::Format((CString *)(piVar5 + 0x94),(char *)local_30);
    local_2c._4_4_ = piVar9[-1];
    local_20 = piVar9[1];
    local_2c._8_4_ = *piVar9;
    local_1c = piVar9[2];
    CDDSurface::BltFast(DAT_1001c470,piVar9[-0x29] + *(int *)((int)param_1 + 0x20),
                        piVar9[-0x28] + *(int *)((int)param_1 + 0x24),
                        *(undefined4 *)((int)param_1 + 0x3c));
    CString::Format((CString *)&local_34,(char *)&local_34);
    local_20 = piVar9[-0x28];
    local_1c = piVar9[-0x27];
    local_2c._8_4_ = piVar9[-0x29];
    local_18 = piVar9[-0x26];
    OffsetRect((LPRECT)(local_2c + 8),*(int *)((int)param_1 + 0x20),*(int *)((int)param_1 + 0x24));
    if (((int)local_2c._8_4_ < *(int *)((int)param_1 + 0x30)) &&
       (*(int *)((int)param_1 + 0x30) < local_1c)) {
      if ((local_20 < *(int *)((int)param_1 + 0x34)) && (*(int *)((int)param_1 + 0x34) < local_18))
      {
        local_14 = 0;
        local_10 = 0;
        local_c = (void *)0x20;
        local_8 = (undefined1 *)0x20;
        CDDSurface::BltFast(DAT_1001c470,local_2c._8_4_,local_20);
        local_2c._4_4_ = &stack0xffffffbc;
        iVar8 = extraout_ECX;
        CString::CString((CString *)&stack0xffffffbc,pCVar3);
        FUN_1000a740(param_1,iVar8);
      }
    }
    local_2c._4_4_ = pCVar3 + 4;
    iVar8 = local_2c._0_4_ + 1;
    piVar9 = piVar9 + 4;
    local_2c._0_4_ = iVar8;
  } while (iVar8 < 6);
  if (0 < *(int *)((int)param_1 + 0x200)) {
    SetRect((LPRECT)((int)param_1 + 0x140),0x20,0x80,0x40,0xa0);
    CString::operator=((CString *)((int)param_1 + 0x198),&DAT_1001a400);
    local_2c._0_4_ = ((LPRECT)((int)param_1 + 0x140))->left;
    local_2c._4_4_ = *(int *)((int)param_1 + 0x144);
    local_2c._8_4_ = *(LONG *)((int)param_1 + 0x148);
    local_20 = *(int *)((int)param_1 + 0x14c);
    cVar6 = clock();
    uVar7 = cVar6 / 300 & 0x80000001;
    bVar10 = uVar7 == 0;
    if ((int)uVar7 < 0) {
      bVar10 = (uVar7 - 1 | 0xfffffffe) == 0xffffffff;
    }
    if (bVar10) {
      local_2c._4_4_ = local_2c._4_4_ + 0xa0;
      local_20 = local_20 + 0xa0;
    }
    CDDSurface::BltFast(DAT_1001c470,*(int *)((int)param_1 + 0xa0) + *(int *)((int)param_1 + 0x20),
                        *(int *)((int)param_1 + 0xa4) + *(int *)((int)param_1 + 0x24),DAT_1001c47c,
                        local_2c);
    local_2c._0_4_ = *(int *)((int)param_1 + 0xa0);
    local_2c._4_4_ = *(undefined4 *)((int)param_1 + 0xa4);
    local_2c._8_4_ = *(int *)((int)param_1 + 0xa8);
    local_20 = *(int *)((int)param_1 + 0xac);
    OffsetRect((LPRECT)local_2c,*(int *)((int)param_1 + 0x20),*(int *)((int)param_1 + 0x24));
    if (((int)local_2c._0_4_ < *(int *)((int)param_1 + 0x30)) &&
       (*(int *)((int)param_1 + 0x30) < (int)local_2c._8_4_)) {
      if (((int)local_2c._4_4_ < *(int *)((int)param_1 + 0x34)) &&
         (*(int *)((int)param_1 + 0x34) < local_20)) {
        local_1c = 0;
        local_18 = 0;
        local_14 = 0x20;
        local_10 = 0x20;
        CDDSurface::BltFast(DAT_1001c470,local_2c._0_4_,local_2c._4_4_,DAT_1001c480,&local_1c);
        local_30 = (CString *)&stack0xffffffb4;
        iVar8 = extraout_ECX_00;
        CString::CString((CString *)&stack0xffffffb4,(CString *)((int)param_1 + 0x198));
        FUN_1000a740(param_1,iVar8);
      }
    }
  }
  if (*(int *)((int)param_1 + 0x200) + 6 < *(int *)((int)param_1 + 0x1fc)) {
    SetRect((LPRECT)((int)param_1 + 0x150),0x40,0x80,0x60,0xa0);
    CString::operator=((CString *)((int)param_1 + 0x19c),&DAT_1001a3f0);
    local_2c._0_4_ = ((LPRECT)((int)param_1 + 0x150))->left;
    local_2c._4_4_ = *(int *)((int)param_1 + 0x154);
    local_2c._8_4_ = *(LONG *)((int)param_1 + 0x158);
    local_20 = *(int *)((int)param_1 + 0x15c);
    cVar6 = clock();
    uVar7 = cVar6 / 300 & 0x80000001;
    bVar10 = uVar7 == 0;
    if ((int)uVar7 < 0) {
      bVar10 = (uVar7 - 1 | 0xfffffffe) == 0xffffffff;
    }
    if (bVar10) {
      local_2c._4_4_ = local_2c._4_4_ + 0xa0;
      local_20 = local_20 + 0xa0;
    }
    CDDSurface::BltFast(DAT_1001c470,*(int *)((int)param_1 + 0xb0) + *(int *)((int)param_1 + 0x20),
                        *(int *)((int)param_1 + 0xb4) + *(int *)((int)param_1 + 0x24),DAT_1001c47c,
                        local_2c);
    local_2c._0_4_ = *(int *)((int)param_1 + 0xb0);
    local_2c._4_4_ = *(undefined4 *)((int)param_1 + 0xb4);
    local_2c._8_4_ = *(int *)((int)param_1 + 0xb8);
    local_20 = *(int *)((int)param_1 + 0xbc);
    OffsetRect((LPRECT)local_2c,*(int *)((int)param_1 + 0x20),*(int *)((int)param_1 + 0x24));
    if (((int)local_2c._0_4_ < *(int *)((int)param_1 + 0x30)) &&
       (*(int *)((int)param_1 + 0x30) < (int)local_2c._8_4_)) {
      if (((int)local_2c._4_4_ < *(int *)((int)param_1 + 0x34)) &&
         (*(int *)((int)param_1 + 0x34) < local_20)) {
        local_1c = 0;
        local_18 = 0;
        local_14 = 0x20;
        local_10 = 0x20;
        CDDSurface::BltFast(DAT_1001c470,local_2c._0_4_,local_2c._4_4_,DAT_1001c480,&local_1c);
        local_30 = (CString *)&stack0xffffffb4;
        iVar8 = extraout_ECX_01;
        CString::CString((CString *)&stack0xffffffb4,(CString *)((int)param_1 + 0x19c));
        FUN_1000a740(param_1,iVar8);
      }
    }
  }
  local_4 = 0xffffffff;
  CString::~CString(local_38);
  ExceptionList = local_c;
  return 1;
}

