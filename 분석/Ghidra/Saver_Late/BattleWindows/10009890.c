
/* WARNING: Type propagation algorithm not settling */

void __thiscall FUN_10009890(void *this,int param_1,int param_2,int param_3)

{
  undefined4 uVar1;
  byte bVar2;
  itemrecord *piVar3;
  character *pcVar4;
  magicrecord *pmVar5;
  int iVar6;
  uint uVar7;
  int iVar8;
  int *piVar9;
  uint uVar10;
  int iVar11;
  short *psVar12;
  int iVar13;
  undefined4 *puVar14;
  CString local_88 [4];
  CString local_84 [4];
  CString local_80 [4];
  int local_7c;
  int local_78;
  int local_74;
  undefined4 local_70;
  undefined2 local_6c;
  undefined2 local_68;
  undefined2 local_66;
  undefined2 local_64;
  undefined2 local_62;
  tagRECT local_60;
  short local_50;
  undefined2 local_4e;
  undefined2 local_4c;
  undefined2 local_4a;
  uint local_44;
  void *local_40;
  short local_38;
  undefined2 local_36;
  undefined2 local_34;
  undefined2 local_32;
  uint local_2c;
  void *local_28;
  undefined2 local_24;
  undefined2 local_22;
  short local_20;
  undefined2 local_1e;
  undefined2 local_1c;
  undefined2 local_1a;
  uint local_14;
  void *local_10;
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10015294;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_80);
  local_4 = 0;
  CString::CString(local_84);
  local_4._0_1_ = 1;
  CString::CString(local_88);
  *(int *)((int)this + 0x30) = param_1;
  *(int *)((int)this + 0x38) = param_3;
  *(int *)((int)this + 0x34) = param_2;
  local_4._0_1_ = 2;
  iVar13 = (int)(param_1 + (param_1 >> 0x1f & 0x1fU)) >> 5;
  iVar11 = (int)(param_2 + (param_2 >> 0x1f & 0x1fU)) >> 5;
  FUN_1000a710(this);
  switch(*(undefined4 *)this) {
  case 0:
    iVar11 = mainmapgetchar(((int)(*(int *)((int)this + 0x30) +
                                  (*(int *)((int)this + 0x30) >> 0x1f & 0x1fU)) >> 5) + DAT_1001c460
                            ,((int)(*(int *)((int)this + 0x34) +
                                   (*(int *)((int)this + 0x34) >> 0x1f & 0x1fU)) >> 5) +
                             DAT_1001c464);
    DAT_1001a380 = iVar11;
    if (-1 < iVar11) {
      if (DAT_1006c118 != 0) {
        if ((DAT_1006c104 == 0) && (iVar11 != DAT_1006c0f4)) break;
        if (DAT_1006c0f0 == 1) {
          DAT_1006c104 = 1;
        }
      }
      pcVar4 = GetUnitInfoP(iVar11);
      if (*(int *)(pcVar4 + 100) == DAT_1006a738) {
        FUN_1000be00(iVar11);
      }
    }
    break;
  case 1:
    iVar11 = 0;
    if (0 < *(int *)((int)this + 0x10)) {
      do {
        piVar9 = (int *)((iVar11 + 4) * 0x10 + (int)this);
        local_60.left = *piVar9;
        local_60.top = piVar9[1];
        local_60.right = piVar9[2];
        local_60.bottom = piVar9[3];
        OffsetRect(&local_60,*(int *)((int)this + 0x20),*(int *)((int)this + 0x24));
        if ((((*(int *)((int)this + 0x30) <= local_60.left) ||
             (local_60.right <= *(int *)((int)this + 0x30))) ||
            (*(int *)((int)this + 0x34) <= local_60.top)) ||
           (local_60.bottom <= *(int *)((int)this + 0x34))) goto LAB_10009d3c;
        pcVar4 = GetUnitInfoP(*(int *)((int)this + 0xc));
        if (*(int *)((int)this + 0x1f8) != 0x5f4) goto switchD_10009cd7_default;
        switch(iVar11) {
        case 0:
          bVar2 = (byte)pcVar4[0x10c] & 1;
          break;
        case 1:
          if (((byte)pcVar4[0x10c] & 2) == 0) goto LAB_10009d3c;
          goto switchD_10009cd7_default;
        case 2:
          if (DAT_1006c118 != 0) {
            bVar2 = (byte)pcVar4[0x10c] & 0x80;
            break;
          }
          goto switchD_10009cd7_default;
        case 3:
          bVar2 = (byte)pcVar4[0x10c] & 0x20;
          break;
        case 4:
          bVar2 = (byte)pcVar4[0x10c] & 8;
          break;
        case 5:
          bVar2 = (byte)pcVar4[0x10c] & 0x10;
          break;
        case 6:
          if (DAT_1006c118 != 0) {
            bVar2 = (byte)pcVar4[0x10c] & 0x40;
            break;
          }
          goto switchD_10009cd7_default;
        case 7:
          bVar2 = (byte)pcVar4[0x10c] & 4;
          break;
        default:
          goto switchD_10009cd7_default;
        }
        if (bVar2 != 0) {
switchD_10009cd7_default:
          iVar13 = *(int *)((int)this + 0x1f8);
          if (iVar13 == 0x5f9) {
            if ((DAT_1006c118 == 0) || (DAT_1006c104 != 0)) {
              setsolai(*(int *)((int)this + 0xc),*(int *)((int)this + iVar11 * 4 + 0x1a8));
              goto LAB_1000a632;
            }
            if (DAT_1006c0f0 == 10) {
              if (iVar11 == 0) {
                DAT_1006c104 = 1;
              }
            }
            else if ((DAT_1006c0f0 == 0xb) && (iVar11 == 2)) {
              DAT_1006c104 = 1;
            }
            break;
          }
          if ((iVar13 != 0x5f7) && (iVar13 != 0x5fc)) {
            FUN_1000c3a0(*(int *)((int)this + iVar11 * 4 + 0x1a8),
                         *(undefined4 *)((int)this + iVar11 * 4 + 0x1d0),*(int *)((int)this + 0xc));
            break;
          }
          if ((DAT_1006c118 != 0) && (DAT_1006c104 == 0)) {
            if (DAT_1006c0f0 == 0xd) {
              DAT_1006c104 = 1;
            }
            else if (DAT_1006c0f0 != 0xe) break;
          }
          pmVar5 = GetMagicTbl(*(int *)((int)this + iVar11 * 4 + 0x1d0));
          pcVar4 = GetUnitInfoP(*(int *)((int)this + 0xc));
          if (*(int *)(pcVar4 + 0x28) < *(int *)(pmVar5 + 4)) {
            local_70 = &stack0xffffff64;
            CString::CString((CString *)&stack0xffffff64,&DAT_1001a3dc);
            FUN_10008790(DAT_1006a708,0x28,0x82,0x231,0x41);
            goto LAB_1000a632;
          }
          FUN_1000c3a0(*(int *)((int)this + iVar11 * 4 + 0x1a8),
                       *(undefined4 *)((int)this + iVar11 * 4 + 0x1d0),*(int *)((int)this + 0xc));
          break;
        }
LAB_10009d3c:
        iVar11 = iVar11 + 1;
      } while (iVar11 < *(int *)((int)this + 0x10));
    }
    break;
  case 2:
    switch(*(undefined4 *)((int)this + 0x1f8)) {
    case 0x5f5:
      if (DAT_1006c118 != 0) {
        if (DAT_1006c0f0 != 3) goto switchD_1000992d_default;
        local_74 = DAT_1001c460 + iVar13;
        local_78 = DAT_1001c464 + iVar11;
        if ((local_74 != DAT_1006c0f8) || (local_78 != DAT_1006c0fc)) goto switchD_1000992d_default;
        DAT_1006c104 = 1;
      }
      if (*(int *)(&DAT_10043604 + (DAT_1001c464 + (DAT_1001c460 + iVar13) * 200 + iVar11) * 4) < 1)
      goto switchD_1000992d_default;
      local_78 = DAT_1001c464 + iVar11;
      piVar9 = (int *)((int)this + 0xc);
      local_74 = DAT_1001c460 + iVar13;
      iVar6 = GetMoveable(piVar9,&local_74,&local_78);
      if (iVar6 != 0) {
        iVar6 = MoveCharPath((_PGSMOVECHARPATH *)&local_50,*piVar9,DAT_1001c460 + iVar13,
                             DAT_1001c464 + iVar11);
        if (iVar6 != 0) {
          FUN_1000e2e0(&local_50);
        }
        MoveChar(*piVar9,DAT_1001c460 + iVar13,DAT_1001c464 + iVar11);
        pcVar4 = GetUnitInfoP(*piVar9);
        SetMenuFlag(*piVar9,*(uint *)(pcVar4 + 0x10c) & 0xfffffffe);
      }
      break;
    case 0x5f6:
      if (DAT_1006c118 != 0) {
        if ((DAT_1006c0f0 != 7) ||
           (iVar6 = mainmapgetchar(DAT_1001c460 + iVar13,DAT_1001c464 + iVar11),
           iVar6 != DAT_1006c0f8)) goto switchD_1000992d_default;
        DAT_1006c104 = 1;
      }
      if ((*(int *)(&DAT_10043604 + (DAT_1001c464 + (DAT_1001c460 + iVar13) * 200 + iVar11) * 4) < 1
          ) || (iVar11 = mainmapgetchar(DAT_1001c460 + iVar13,DAT_1001c464 + iVar11), iVar11 < 0))
      goto switchD_1000992d_default;
      pcVar4 = GetUnitInfoP(iVar11);
      local_7c = GetAttrTable3(*(int *)(pcVar4 + 0x70),*(int *)(pcVar4 + 0x74));
      iVar13 = ally4(*(int *)((int)this + 0xc),*(int *)(pcVar4 + 0x10));
      if (iVar13 == 0) goto switchD_1000992d_default;
      pcVar4 = GetUnitInfoP(*(int *)((int)this + 0xc));
      iVar13 = GetAttrTable3(*(int *)(pcVar4 + 0x70),*(int *)(pcVar4 + 0x74));
      local_68 = *(undefined2 *)((int)this + 0xc);
      local_64 = (undefined2)iVar13;
      local_62 = (undefined2)local_7c;
      local_66 = (short)iVar11;
      FUN_1000e380(&local_68);
      local_70 = (undefined1 *)CONCAT22((short)iVar11,*(undefined2 *)((int)this + 0xc));
      local_6c = 0;
      AttackChar((_PGSATTACKFX *)&local_70);
      pcVar4 = GetUnitInfoP(*(int *)((int)this + 0xc));
      SetMenuFlag(*(int *)((int)this + 0xc),*(uint *)(pcVar4 + 0x10c) & 0xffffffc4);
      break;
    default:
      goto switchD_1000992d_default;
    case 0x5ff:
      if (*(int *)(&DAT_10043604 + (DAT_1001c464 + (DAT_1001c460 + iVar13) * 200 + iVar11) * 4) < 1)
      goto switchD_1000992d_default;
      iVar6 = mainmapgetchar(DAT_1001c460 + iVar13,DAT_1001c464 + iVar11);
      if ((DAT_1006c118 != 0) && (DAT_1006c104 == 0)) {
        if ((DAT_1006c0f0 != 0xe) || (iVar6 != DAT_1006c0fc)) goto switchD_1000992d_default;
        DAT_1006c104 = 1;
      }
      pmVar5 = GetMagicTbl(*(int *)((int)this + 0x14));
      if ((0 < *(int *)(pmVar5 + 0xc)) && (0 < *(int *)(pmVar5 + 8))) {
        local_7c = DAT_1006a70c[7];
        uVar1 = DAT_1006a70c[6];
        local_70 = operator_new(0x524);
        local_4._0_1_ = 4;
        if (local_70 == (undefined1 *)0x0) {
          DAT_1006a70c = (undefined4 *)0x0;
        }
        else {
          DAT_1006a70c = FUN_10009220(local_70,DAT_1006a70c);
        }
        local_4._0_1_ = 2;
        FUN_1000b940(DAT_1006a70c,*(int *)((int)this + 0xc),*(undefined4 *)((int)this + 0x14));
        DAT_1006a70c[0x7e] = 0x600;
        DAT_1006a70c[6] = uVar1;
        DAT_1006a70c[7] = local_7c;
        DAT_1006a70c[10] = DAT_1001c460 + iVar13;
        DAT_1006a70c[0xb] = DAT_1001c464 + iVar11;
        goto switchD_1000992d_default;
      }
      iVar6 = *(int *)(pmVar5 + 8);
      if (((iVar6 == -1) || (iVar6 == -3)) || (iVar8 = DAT_1001c460 + iVar13, iVar6 == -4)) {
        uVar7 = *(int *)((int)this + 0x30) - *(int *)((int)this + 0x20);
        uVar10 = (int)uVar7 >> 0x1f;
        local_7c = (uVar7 ^ uVar10) - uVar10;
        uVar7 = *(int *)((int)this + 0x34) - *(int *)((int)this + 0x24);
        uVar10 = (int)uVar7 >> 0x1f;
        if ((int)((uVar7 ^ uVar10) - uVar10) < local_7c) {
          iVar8 = (*(int *)((int)this + 0x20) < *(int *)((int)this + 0x30)) - 2;
        }
        else {
          iVar8 = (*(int *)((int)this + 0x34) <= *(int *)((int)this + 0x24)) - 4;
        }
      }
      local_7c = mainmapgetchar(DAT_1001c460 + iVar13,DAT_1001c464 + iVar11);
      if (((((byte)pmVar5[0x10] & 1) == 0) &&
          (iVar13 = ally3(*(int *)((int)this + 0xc),local_7c), iVar13 != 0)) ||
         ((((byte)pmVar5[0x10] & 2) == 0 &&
          (iVar13 = ally4(*(int *)((int)this + 0xc),local_7c), iVar13 != 0))))
      goto switchD_1000992d_default;
      iVar11 = CheckMagicTarget((_PGSMAGIC *)&local_50,*(int *)((int)this + 0x14),
                                *(int *)((int)this + 0xc),iVar8,DAT_1001c464 + iVar11);
      if (iVar11 != 0) {
        if (*(int *)((int)this + 0x18) < 1) {
          FUN_1000e4c0(&local_50,0);
          psVar12 = &local_50;
          puVar14 = (undefined4 *)&stack0xffffff54;
          for (iVar11 = 5; iVar11 != 0; iVar11 = iVar11 + -1) {
            *puVar14 = *(undefined4 *)psVar12;
            psVar12 = psVar12 + 2;
            puVar14 = puVar14 + 1;
          }
          Magic();
          pcVar4 = GetUnitInfoP(*(int *)((int)this + 0xc));
          SetMenuFlag(*(int *)((int)this + 0xc),*(uint *)(pcVar4 + 0x10c) & 0xffffffc4);
          local_28 = local_40;
          local_2c._0_1_ = (char)local_44;
        }
        else {
          local_22 = *(undefined2 *)((int)this + 0x1c);
          local_24 = (undefined2)*(int *)((int)this + 0x18);
          local_20 = local_50;
          local_1a = local_4a;
          local_1e = local_4e;
          local_1c = local_4c;
          local_14 = CONCAT31(local_14._1_3_,(char)local_44);
          if ((char)local_44 != '\0') {
            local_10 = operator_new((local_44 & 0xff) << 1);
            iVar11 = 0;
            if ((char)local_14 != '\0') {
              do {
                iVar11 = iVar11 + 1;
                *(undefined2 *)((int)local_10 + iVar11 * 2 + -2) =
                     *(undefined2 *)((int)local_40 + iVar11 * 2 + -2);
              } while (iVar11 < (int)(local_14 & 0xff));
            }
          }
          FUN_1000e610(&local_24);
          if ((char)local_14 != '\0') {
            operator_delete(local_10);
          }
          psVar12 = &local_50;
          puVar14 = (undefined4 *)&stack0xffffff54;
          for (iVar11 = 5; iVar11 != 0; iVar11 = iVar11 + -1) {
            *puVar14 = *(undefined4 *)psVar12;
            psVar12 = psVar12 + 2;
            puVar14 = puVar14 + 1;
          }
          Magic();
          local_28 = local_40;
          local_2c._0_1_ = (char)local_44;
        }
        goto joined_r0x1000a623;
      }
      break;
    case 0x600:
      if (*(int *)(&DAT_10043604 + (DAT_1001c464 + (DAT_1001c460 + iVar13) * 200 + iVar11) * 4) < 1)
      goto switchD_1000992d_default;
      pmVar5 = GetMagicTbl(*(int *)((int)this + 0x14));
      iVar11 = mainmapgetchar(DAT_1001c460 + iVar13,DAT_1001c464 + iVar11);
      if (((((byte)pmVar5[0x10] & 1) == 0) &&
          (iVar13 = ally3(*(int *)((int)this + 0xc),iVar11), iVar13 != 0)) ||
         ((((byte)pmVar5[0x10] & 2) == 0 &&
          (iVar11 = ally4(*(int *)((int)this + 0xc),iVar11), iVar11 != 0))))
      goto switchD_1000992d_default;
      iVar11 = CheckMagicTarget((_PGSMAGIC *)&local_38,*(int *)((int)this + 0x14),
                                *(int *)((int)this + 0xc),*(int *)((int)this + 0x28),
                                *(int *)((int)this + 0x2c));
      if (iVar11 != 0) {
        if (*(int *)((int)this + 0x18) < 1) {
          FUN_1000e4c0(&local_38,0);
          psVar12 = &local_38;
          puVar14 = (undefined4 *)&stack0xffffff54;
          for (iVar11 = 5; iVar11 != 0; iVar11 = iVar11 + -1) {
            *puVar14 = *(undefined4 *)psVar12;
            psVar12 = psVar12 + 2;
            puVar14 = puVar14 + 1;
          }
          Magic();
          pcVar4 = GetUnitInfoP(*(int *)((int)this + 0xc));
          SetMenuFlag(*(int *)((int)this + 0xc),*(uint *)(pcVar4 + 0x10c) & 0xffffffc4);
        }
        else {
          local_22 = *(undefined2 *)((int)this + 0x1c);
          local_24 = (undefined2)*(int *)((int)this + 0x18);
          local_1e = local_36;
          local_20 = local_38;
          local_1c = local_34;
          local_1a = local_32;
          local_14 = CONCAT31(local_14._1_3_,(char)local_2c);
          if ((char)local_2c != '\0') {
            local_10 = operator_new((local_2c & 0xff) << 1);
            iVar11 = 0;
            if ((char)local_14 != '\0') {
              do {
                iVar11 = iVar11 + 1;
                *(undefined2 *)((int)local_10 + iVar11 * 2 + -2) =
                     *(undefined2 *)((int)local_28 + iVar11 * 2 + -2);
              } while (iVar11 < (int)(local_14 & 0xff));
            }
          }
          FUN_1000e610(&local_24);
          if ((char)local_14 != '\0') {
            operator_delete(local_10);
          }
          psVar12 = &local_38;
          puVar14 = (undefined4 *)&stack0xffffff54;
          for (iVar11 = 5; iVar11 != 0; iVar11 = iVar11 + -1) {
            *puVar14 = *(undefined4 *)psVar12;
            psVar12 = psVar12 + 2;
            puVar14 = puVar14 + 1;
          }
          Magic();
        }
joined_r0x1000a623:
        if ((char)local_2c != '\0') {
          operator_delete(local_28);
        }
      }
    }
LAB_1000a632:
    FUN_10008d20();
    break;
  case 3:
    local_7c = 0;
    *(undefined4 *)((int)this + 0x18) = 0xffffffff;
    *(undefined4 *)((int)this + 0x1c) = 0xffffffff;
    piVar9 = (int *)((int)this + 0x40);
    do {
      iVar11 = *(int *)((int)this + 0x200) + local_7c;
      if (*(int *)((int)this + 0x1fc) <= iVar11) break;
      piVar3 = GetItemTable(*(int *)((int)this + iVar11 * 8 + 0x204));
      local_60.left = *piVar9;
      local_60.top = piVar9[1];
      local_60.right = piVar9[2];
      local_60.bottom = piVar9[3];
      OffsetRect(&local_60,*(int *)((int)this + 0x20),*(int *)((int)this + 0x24));
      if ((((local_60.left < *(int *)((int)this + 0x30)) &&
           (*(int *)((int)this + 0x30) < local_60.right)) &&
          (local_60.top < *(int *)((int)this + 0x34))) &&
         ((*(int *)((int)this + 0x34) < local_60.bottom && (0 < *(int *)(piVar3 + 0x30))))) {
        local_70 = operator_new(0x524);
        local_4._0_1_ = 3;
        if (local_70 == (undefined1 *)0x0) {
          DAT_1006a70c = (undefined4 *)0x0;
        }
        else {
          DAT_1006a70c = FUN_10009220(local_70,DAT_1006a70c);
        }
        local_4._0_1_ = 2;
        FUN_1000b940(DAT_1006a70c,*(int *)((int)this + 0xc),*(undefined4 *)(piVar3 + 0x30));
        DAT_1006a70c[0x7e] = 0x5ff;
        DAT_1006a70c[6] = *(undefined4 *)((int)this + iVar11 * 8 + 0x204);
        DAT_1006a70c[7] = *(undefined4 *)((int)this + iVar11 * 8 + 0x208);
      }
      piVar9 = piVar9 + 4;
      local_7c = local_7c + 1;
    } while (local_7c < 6);
    if (0 < *(int *)((int)this + 0x200)) {
      local_60.left = *(int *)((int)this + 0xa0);
      local_60.top = *(int *)((int)this + 0xa4);
      local_60.right = *(int *)((int)this + 0xa8);
      local_60.bottom = *(int *)((int)this + 0xac);
      OffsetRect(&local_60,*(int *)((int)this + 0x20),*(int *)((int)this + 0x24));
      if (((local_60.left < *(int *)((int)this + 0x30)) &&
          (*(int *)((int)this + 0x30) < local_60.right)) &&
         ((local_60.top < *(int *)((int)this + 0x34) &&
          ((*(int *)((int)this + 0x34) < local_60.bottom &&
           (iVar11 = *(int *)((int)this + 0x200) + -6, *(int *)((int)this + 0x200) = iVar11,
           iVar11 < 0)))))) {
        *(undefined4 *)((int)this + 0x200) = 0;
      }
    }
    if (*(int *)((int)this + 0x200) + 5 < *(int *)((int)this + 0x1fc)) {
      local_60.left = *(int *)((int)this + 0xb0);
      local_60.top = *(int *)((int)this + 0xb4);
      local_60.right = *(int *)((int)this + 0xb8);
      local_60.bottom = *(int *)((int)this + 0xbc);
      OffsetRect(&local_60,*(int *)((int)this + 0x20),*(int *)((int)this + 0x24));
      if ((((local_60.left < *(int *)((int)this + 0x30)) &&
           (*(int *)((int)this + 0x30) < local_60.right)) &&
          (local_60.top < *(int *)((int)this + 0x34))) &&
         (*(int *)((int)this + 0x34) < local_60.bottom)) {
        iVar13 = *(int *)((int)this + 0x200) + 6;
        iVar11 = *(int *)((int)this + 0x1fc) + -6;
        *(int *)((int)this + 0x200) = iVar13;
        if (iVar11 < iVar13) {
          *(int *)((int)this + 0x200) = iVar11;
        }
      }
    }
  }
switchD_1000992d_default:
  local_4._0_1_ = 1;
  CString::~CString(local_88);
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString(local_84);
  local_4 = 0xffffffff;
  CString::~CString(local_80);
  ExceptionList = local_c;
  return;
}

