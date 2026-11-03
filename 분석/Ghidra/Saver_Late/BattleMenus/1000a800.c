
undefined4 __fastcall FUN_1000a800(void *param_1)

{
  int iVar1;
  CDDSurface *pCVar2;
  bool bVar3;
  character *pcVar4;
  clock_t cVar5;
  CDDSurface *pCVar6;
  int *piVar7;
  int extraout_ECX;
  LONG *pLVar8;
  uint uVar9;
  bool bVar10;
  int iVar11;
  int iVar12;
  int local_28;
  tagRECT local_20;
  undefined4 local_10;
  undefined4 local_c;
  undefined4 local_8;
  undefined4 local_4;
  
  iVar12 = DAT_1001c464;
  iVar11 = DAT_1001c460;
  pcVar4 = GetUnitInfoP(*(int *)((int)param_1 + 0xc));
  FUN_10008d40(0,0,0x280,0x1e0);
  if ((((*(int *)pcVar4 == 0) || (*(int *)(pcVar4 + 4) == 0)) || (((byte)pcVar4[0x100] & 1) != 0))
     || (((iVar1 = *(int *)(pcVar4 + 0x70), iVar1 < iVar11 + 1 ||
          (*(int *)(pcVar4 + 0x74) < iVar12 + 1)) ||
         ((DAT_1001a0b8 + -2 + iVar11 < iVar1 ||
          (DAT_1001a0bc + -1 + iVar12 < *(int *)(pcVar4 + 0x74))))))) {
    return 0;
  }
  *(int *)((int)param_1 + 0x20) = (iVar1 - iVar11) * 0x20;
  *(int *)((int)param_1 + 0x24) = (*(int *)(pcVar4 + 0x74) - iVar12) * 0x20;
  _drawactivecharacter(*(int *)((int)param_1 + 0xc));
  if (*(int *)((int)param_1 + 0x10) < 1) {
    FUN_10008d20();
    return 1;
  }
  local_28 = 0;
  if (0 < *(int *)((int)param_1 + 0x10)) {
    do {
      bVar3 = false;
      pLVar8 = (LONG *)((local_28 + 0xe) * 0x10 + (int)param_1);
      local_20.left = *pLVar8;
      local_20.top = pLVar8[1];
      local_20.right = pLVar8[2];
      local_20.bottom = pLVar8[3];
      cVar5 = clock();
      pCVar2 = *(CDDSurface **)((int)param_1 + 0x3c);
      pCVar6 = GetItemSurface();
      if (pCVar2 == pCVar6) {
        clock();
      }
      else if (pCVar2 == (CDDSurface *)DAT_1001c47c) {
        uVar9 = cVar5 / 300 & 0x80000001;
        bVar10 = uVar9 == 0;
        if ((int)uVar9 < 0) {
          bVar10 = (uVar9 - 1 | 0xfffffffe) == 0xffffffff;
        }
        if (bVar10) {
          local_20.top = local_20.top + 0xa0;
          local_20.bottom = local_20.bottom + 0xa0;
        }
      }
      if (*(int *)((int)param_1 + 0x1f8) == 0x5f4) {
        switch(local_28) {
        case 0:
          if (((byte)pcVar4[0x10c] & 1) == 0) {
            iVar12 = 0xa0;
            iVar11 = 0x80;
LAB_1000ab07:
            SetRect(&local_20,iVar11,0x80,iVar12,0xa0);
            bVar3 = true;
          }
          break;
        case 1:
          if (((byte)pcVar4[0x10c] & 2) == 0) {
            iVar12 = 0xc0;
            iVar11 = 0xa0;
            goto LAB_1000ab07;
          }
          break;
        case 2:
          if ((DAT_1006c118 != 0) && (((byte)pcVar4[0x10c] & 0x80) == 0)) {
            iVar12 = 0x180;
            iVar11 = 0x160;
            goto LAB_1000ab07;
          }
          break;
        case 3:
          if (((byte)pcVar4[0x10c] & 0x20) == 0) {
            iVar12 = 0x120;
            iVar11 = 0x100;
            goto LAB_1000ab07;
          }
          break;
        case 4:
          if (((byte)pcVar4[0x10c] & 8) == 0) {
            iVar12 = 0xe0;
            iVar11 = 0xc0;
            goto LAB_1000ab07;
          }
          break;
        case 5:
          if (((byte)pcVar4[0x10c] & 0x10) == 0) {
            iVar12 = 0x140;
            iVar11 = 0x120;
            goto LAB_1000ab07;
          }
          break;
        case 6:
          if ((DAT_1006c118 != 0) && (((byte)pcVar4[0x10c] & 0x40) == 0)) {
            iVar12 = 0x160;
            iVar11 = 0x140;
            goto LAB_1000ab07;
          }
          break;
        case 7:
          if (((byte)pcVar4[0x10c] & 4) == 0) {
            iVar12 = 0x100;
            iVar11 = 0xe0;
            goto LAB_1000ab07;
          }
        }
      }
      if (*(int *)((int)param_1 + 0x3c) == DAT_1001c47c) {
        CDDSurface::BltFast(DAT_1001c470,
                            *(int *)((local_28 + 4) * 0x10 + (int)param_1) +
                            *(int *)((int)param_1 + 0x20),
                            *(int *)(local_28 * 0x10 + 0x44 + (int)param_1) +
                            *(int *)((int)param_1 + 0x24),*(int *)((int)param_1 + 0x3c),&local_20);
      }
      if (!bVar3) {
        piVar7 = (int *)((local_28 + 4) * 0x10 + (int)param_1);
        local_20.left = *piVar7;
        local_20.top = piVar7[1];
        local_20.right = piVar7[2];
        local_20.bottom = piVar7[3];
        OffsetRect(&local_20,*(int *)((int)param_1 + 0x20),*(int *)((int)param_1 + 0x24));
        if ((((local_20.left < *(int *)((int)param_1 + 0x30)) &&
             (*(int *)((int)param_1 + 0x30) < local_20.right)) &&
            (local_20.top < *(int *)((int)param_1 + 0x34))) &&
           (*(int *)((int)param_1 + 0x34) < local_20.bottom)) {
          local_10 = 0;
          local_c = 0;
          local_8 = 0x20;
          local_4 = 0x20;
          CDDSurface::BltFast(DAT_1001c470,local_20.left,local_20.top,DAT_1001c480,&local_10);
          iVar11 = extraout_ECX;
          CString::CString((CString *)&stack0xffffffc0,
                           (CString *)((int)param_1 + local_28 * 4 + 0x180));
          FUN_1000a740(param_1,iVar11);
        }
      }
      local_28 = local_28 + 1;
    } while (local_28 < *(int *)((int)param_1 + 0x10));
  }
  return 1;
}

