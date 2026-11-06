
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined4 __fastcall FUN_10010f20(char *param_1)

{
  undefined2 *puVar1;
  byte bVar2;
  undefined1 auVar3 [4];
  undefined1 uVar4;
  undefined1 uVar5;
  clock_t cVar6;
  character *pcVar7;
  CString *pCVar8;
  byte *pbVar9;
  int iVar10;
  int iVar11;
  WPARAM wParam;
  int extraout_ECX;
  undefined4 extraout_ECX_00;
  undefined3 uVar12;
  undefined4 extraout_ECX_01;
  int extraout_ECX_02;
  undefined4 extraout_ECX_03;
  int extraout_ECX_04;
  int extraout_ECX_05;
  byte *pbVar13;
  char *pcVar14;
  undefined4 *puVar15;
  undefined1 auVar16 [4];
  undefined4 *puVar17;
  int *piVar18;
  bool bVar19;
  time_t tVar20;
  undefined4 auStackY_2c4 [4];
  undefined4 uStackY_2b4;
  int *piVar21;
  CString CVar22;
  undefined4 uVar23;
  int *piVar24;
  int iVar25;
  byte *local_258;
  byte *local_254;
  LPARAM local_250;
  CString local_24c [4];
  undefined1 *local_248;
  int *local_244;
  char *local_240;
  undefined1 local_23c [4];
  undefined1 *local_238;
  int local_234;
  undefined4 local_230;
  undefined4 local_22c;
  int local_228;
  char local_224;
  undefined1 local_220 [4];
  undefined4 local_21c;
  undefined2 local_218;
  int local_214;
  int local_210;
  int local_20c;
  int local_208;
  undefined4 local_204;
  CDDSurface local_200 [140];
  int local_174;
  int local_170;
  int local_16c;
  undefined1 *local_168;
  undefined4 local_164;
  CDDSurface aCStack_160 [128];
  undefined4 auStack_e0 [25];
  undefined4 uStack_7c;
  undefined4 uStack_2c;
  undefined1 uStack_10;
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10015ab7;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  local_240 = param_1;
  CString::CString((CString *)&local_250);
  local_4 = 0;
  CString::CString((CString *)&local_254);
  local_4._0_1_ = 1;
  CString::CString((CString *)&local_258);
  local_4._0_1_ = 2;
  CString::CString(local_24c);
  local_4._0_1_ = 3;
  CDDSurface::CDDSurface(local_200);
  local_4._0_1_ = 4;
  uVar5 = (undefined1)local_4;
  local_4._0_1_ = 4;
  if (*(int *)(param_1 + 0x34) == 0) {
    DAT_1001a50c = 1;
    DAT_1001a508 = 1;
    *(int *)(param_1 + 0x34) = *(int *)(param_1 + 0x34) + 1;
  }
  if (DAT_1001c4d4 != 0) {
    local_4._0_1_ = 3;
    CDDSurface::~CDDSurface(local_200);
    local_4._0_1_ = 2;
    CString::~CString(local_24c);
    local_4._0_1_ = 1;
    CString::~CString((CString *)&local_258);
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString((CString *)&local_254);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_250);
    ExceptionList = local_c;
    return 1;
  }
  if (-1 < *(int *)(param_1 + 0x14)) {
    if (*(int *)(param_1 + 0x30) == 1) {
      if (DAT_1006c118 == 0) {
        *(undefined4 *)(param_1 + 0x1c) = *DAT_1006a708;
      }
      else {
        *(undefined4 *)(param_1 + 0x1c) = DAT_1006a708[1];
      }
    }
    if (*(int *)(param_1 + 0x1c) != 0) {
      local_4._0_1_ = 3;
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 1;
    }
    uVar4 = uVar5;
    if (*(int *)(param_1 + 0x20) != 0) {
      iVar11 = *(int *)(param_1 + 0x24);
      if (iVar11 < 0) {
        if (iVar11 != -3) {
          uVar4 = (undefined1)local_4;
          if (iVar11 == -2) {
            local_4._0_1_ = uVar5;
            pcVar7 = GetUnitInfoP(*(int *)(param_1 + 0x2c));
            if (((byte)pcVar7[0x100] & 1) != 0) {
              local_4._0_1_ = 3;
              CDDSurface::~CDDSurface(local_200);
              local_4._0_1_ = 2;
              CString::~CString(local_24c);
              local_4._0_1_ = 1;
              CString::~CString((CString *)&local_258);
              local_4 = (uint)local_4._1_3_ << 8;
              CString::~CString((CString *)&local_254);
              local_4 = 0xffffffff;
              CString::~CString((CString *)&local_250);
              ExceptionList = local_c;
              return 1;
            }
            param_1[0x20] = '\0';
            param_1[0x21] = '\0';
            param_1[0x22] = '\0';
            param_1[0x23] = '\0';
            goto LAB_10012be3;
          }
          goto LAB_100111d5;
        }
        local_4._0_1_ = uVar5;
        if (DAT_1006c104 == 0) goto LAB_100111f2;
      }
      else {
        cVar6 = clock();
        if (cVar6 - *(int *)(param_1 + 0x28) < *(int *)(param_1 + 0x24)) {
          local_4._0_1_ = 3;
          CDDSurface::~CDDSurface(local_200);
          local_4._0_1_ = 2;
          CString::~CString(local_24c);
          local_4._0_1_ = 1;
          CString::~CString((CString *)&local_258);
          local_4 = (uint)local_4._1_3_ << 8;
          CString::~CString((CString *)&local_254);
          local_4 = 0xffffffff;
          CString::~CString((CString *)&local_250);
          ExceptionList = local_c;
          return 1;
        }
      }
      param_1[0x20] = '\0';
      param_1[0x21] = '\0';
      param_1[0x22] = '\0';
      param_1[0x23] = '\0';
      uVar4 = (undefined1)local_4;
    }
LAB_100111d5:
    local_4._0_1_ = uVar4;
    uVar5 = (undefined1)local_4;
    if ((*(int *)(param_1 + 4) == 1) &&
       (pcVar7 = GetUnitInfoP(*(int *)(param_1 + 0x14)), uVar5 = (undefined1)local_4,
       ((byte)pcVar7[0x100] & 1) != 0)) {
LAB_100111f2:
      local_4._0_1_ = 3;
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 1;
    }
  }
  local_4._0_1_ = uVar5;
  CString::operator=((CString *)&local_250,
                     (CString *)(param_1 + *(int *)(param_1 + 0x34) * 4 + 0x50));
  local_238 = &stack0xfffffd94;
  iVar11 = extraout_ECX;
  CString::CString((CString *)&stack0xfffffd94,(CString *)&local_250);
  pCVar8 = FUN_1000f110(&local_244,iVar11);
  local_4._0_1_ = 5;
  CString::operator=((CString *)&local_254,pCVar8);
  local_4._0_1_ = 4;
  CString::~CString((CString *)&local_244);
  local_238 = &stack0xfffffd94;
  uVar23 = extraout_ECX_00;
  CString::CString((CString *)&stack0xfffffd94,(CString *)&local_250);
  pCVar8 = FUN_1000f310(&local_244,uVar23);
  local_4._0_1_ = 6;
  CString::operator=((CString *)&local_258,pCVar8);
  local_4._0_1_ = 4;
  CString::~CString((CString *)&local_244);
  *(int *)(param_1 + 0x34) = *(int *)(param_1 + 0x34) + 1;
  iVar11 = *(int *)(local_254 + -8);
  if (iVar11 == 0) {
LAB_10013c7a:
    param_1[0xc] = '\0';
    param_1[0xd] = '\0';
    param_1[0xe] = '\0';
    param_1[0xf] = '\0';
    param_1[0x1c] = '\0';
    param_1[0x1d] = '\0';
    param_1[0x1e] = '\0';
    param_1[0x1f] = '\0';
    if (*(int *)(param_1 + 4) == 2) {
      if (*(int *)(param_1 + 8) == 0) {
        param_1[4] = '\0';
        param_1[5] = '\0';
        param_1[6] = '\0';
        param_1[7] = '\0';
        thunk_FUN_10004150();
      }
      else {
        param_1[4] = '\0';
        param_1[5] = '\0';
        param_1[6] = '\0';
        param_1[7] = '\0';
        DAT_1001a8ec = 0;
        PostMessageA(DAT_1001c468,0x590,0,0);
        PostMessageA(DAT_1001c468,0x4cd,0,0);
      }
    }
    else {
      local_238 = (undefined1 *)CONCAT22(local_238._2_2_,0xffff);
      FUN_1000f070(local_238);
    }
    DAT_1006c118 = 0;
  }
  else {
    pbVar13 = &DAT_1001a890;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < *pbVar13;
      if (bVar2 != *pbVar13) {
LAB_10011327:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_1001132c;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < pbVar13[1];
      if (bVar2 != pbVar13[1]) goto LAB_10011327;
      pbVar9 = pbVar9 + 2;
      pbVar13 = pbVar13 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_1001132c:
    if (iVar10 == 0) goto LAB_10013c7a;
    pcVar14 = s_ENDGAME_1001a888;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10011361:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10011366;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10011361;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10011366:
    if (iVar10 == 0) {
      DAT_1001a8ec = iVar10;
      PostMessageA(DAT_1001c468,0x590,0,0);
      PostMessageA(DAT_1001c468,0x4cd,0,0);
      FUN_1000e750();
      goto LAB_10013cea;
    }
    if (DAT_1006c118 != 0) {
      pcVar14 = s_WAITMOUSEBUTTON_1001a878;
      pbVar9 = local_254;
      do {
        bVar2 = *pbVar9;
        uVar12 = (undefined3)((uint)iVar11 >> 8);
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)*pcVar14;
        if (bVar2 != *pcVar14) {
LAB_100113da:
          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
          goto LAB_100113df;
        }
        if (bVar2 == 0) break;
        bVar2 = pbVar9[1];
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)pcVar14[1];
        if (bVar2 != pcVar14[1]) goto LAB_100113da;
        pbVar9 = pbVar9 + 2;
        pcVar14 = pcVar14 + 2;
      } while (bVar2 != 0);
      iVar10 = 0;
LAB_100113df:
      if (iVar10 == 0) {
        local_248 = &stack0xfffffd94;
        param_1[0x20] = '\x01';
        param_1[0x21] = '\0';
        param_1[0x22] = '\0';
        param_1[0x23] = '\0';
        param_1[0x24] = -3;
        param_1[0x25] = -1;
        param_1[0x26] = -1;
        param_1[0x27] = -1;
        CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
        pCVar8 = FUN_1000f110(&local_244,iVar11);
        iVar11 = atoi(*(char **)pCVar8);
        *(int *)(param_1 + 0x2c) = iVar11;
        CString::~CString((CString *)&local_244);
        DAT_1006c0f0 = 0x10;
      }
      else {
        pcVar14 = s_WAITTOPMENU_1001a86c;
        pbVar9 = local_254;
        do {
          bVar2 = *pbVar9;
          uVar12 = (undefined3)((uint)iVar11 >> 8);
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)*pcVar14;
          if (bVar2 != *pcVar14) {
LAB_10011466:
            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
            goto LAB_1001146b;
          }
          if (bVar2 == 0) break;
          bVar2 = pbVar9[1];
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)pcVar14[1];
          if (bVar2 != pcVar14[1]) goto LAB_10011466;
          pbVar9 = pbVar9 + 2;
          pcVar14 = pcVar14 + 2;
        } while (bVar2 != 0);
        iVar10 = 0;
LAB_1001146b:
        if (iVar10 == 0) {
          local_248 = &stack0xfffffd94;
          param_1[0x20] = '\x01';
          param_1[0x21] = '\0';
          param_1[0x22] = '\0';
          param_1[0x23] = '\0';
          param_1[0x24] = -3;
          param_1[0x25] = -1;
          param_1[0x26] = -1;
          param_1[0x27] = -1;
          CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
          pCVar8 = FUN_1000f110(&local_244,iVar11);
          iVar11 = atoi(*(char **)pCVar8);
          *(int *)(param_1 + 0x2c) = iVar11;
          CString::~CString((CString *)&local_244);
          DAT_1006c104 = 0;
          DAT_1006c0f0 = 1;
          DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
          cVar6 = clock();
          *(clock_t *)(param_1 + 0x28) = cVar6;
          local_4._0_1_ = 3;
          CDDSurface::~CDDSurface(local_200);
          local_4._0_1_ = 2;
          CString::~CString(local_24c);
          local_4._0_1_ = 1;
          CString::~CString((CString *)&local_258);
          local_4 = (uint)local_4._1_3_ << 8;
          CString::~CString((CString *)&local_254);
          local_4 = 0xffffffff;
          CString::~CString((CString *)&local_250);
          ExceptionList = local_c;
          return 1;
        }
        pcVar14 = s_WAITMOVEMENU_1001a85c;
        pbVar9 = local_254;
        do {
          bVar2 = *pbVar9;
          uVar12 = (undefined3)((uint)iVar11 >> 8);
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)*pcVar14;
          if (bVar2 != *pcVar14) {
LAB_1001155f:
            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
            goto LAB_10011564;
          }
          if (bVar2 == 0) break;
          bVar2 = pbVar9[1];
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)pcVar14[1];
          if (bVar2 != pcVar14[1]) goto LAB_1001155f;
          pbVar9 = pbVar9 + 2;
          pcVar14 = pcVar14 + 2;
        } while (bVar2 != 0);
        iVar10 = 0;
LAB_10011564:
        if (iVar10 == 0) {
          local_248 = &stack0xfffffd94;
          param_1[0x20] = '\x01';
          param_1[0x21] = '\0';
          param_1[0x22] = '\0';
          param_1[0x23] = '\0';
          param_1[0x24] = -3;
          param_1[0x25] = -1;
          param_1[0x26] = -1;
          param_1[0x27] = -1;
          CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
          pCVar8 = FUN_1000f110(&local_244,iVar11);
          iVar11 = atoi(*(char **)pCVar8);
          *(int *)(param_1 + 0x2c) = iVar11;
          CString::~CString((CString *)&local_244);
          DAT_1006c0f0 = 2;
        }
        else {
          pcVar14 = s_WAITATTACKMENU_1001a84c;
          pbVar9 = local_254;
          do {
            bVar2 = *pbVar9;
            uVar12 = (undefined3)((uint)iVar11 >> 8);
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)*pcVar14;
            if (bVar2 != *pcVar14) {
LAB_100115eb:
              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
              goto LAB_100115f0;
            }
            if (bVar2 == 0) break;
            bVar2 = pbVar9[1];
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)pcVar14[1];
            if (bVar2 != pcVar14[1]) goto LAB_100115eb;
            pbVar9 = pbVar9 + 2;
            pcVar14 = pcVar14 + 2;
          } while (bVar2 != 0);
          iVar10 = 0;
LAB_100115f0:
          if (iVar10 == 0) {
            local_248 = &stack0xfffffd94;
            param_1[0x20] = '\x01';
            param_1[0x21] = '\0';
            param_1[0x22] = '\0';
            param_1[0x23] = '\0';
            param_1[0x24] = -3;
            param_1[0x25] = -1;
            param_1[0x26] = -1;
            param_1[0x27] = -1;
            CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
            pCVar8 = FUN_1000f110(&local_244,iVar11);
            iVar11 = atoi(*(char **)pCVar8);
            *(int *)(param_1 + 0x2c) = iVar11;
            CString::~CString((CString *)&local_244);
            DAT_1006c0f0 = 6;
          }
          else {
            pcVar14 = s_WAITRESTMENU_1001a83c;
            pbVar9 = local_254;
            do {
              bVar2 = *pbVar9;
              uVar12 = (undefined3)((uint)iVar11 >> 8);
              iVar11 = CONCAT31(uVar12,bVar2);
              bVar19 = bVar2 < (byte)*pcVar14;
              if (bVar2 != *pcVar14) {
LAB_10011673:
                iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                goto LAB_10011678;
              }
              if (bVar2 == 0) break;
              bVar2 = pbVar9[1];
              iVar11 = CONCAT31(uVar12,bVar2);
              bVar19 = bVar2 < (byte)pcVar14[1];
              if (bVar2 != pcVar14[1]) goto LAB_10011673;
              pbVar9 = pbVar9 + 2;
              pcVar14 = pcVar14 + 2;
            } while (bVar2 != 0);
            iVar10 = 0;
LAB_10011678:
            if (iVar10 != 0) {
              pcVar14 = s_WAITMOVEPOS_1001a830;
              pbVar9 = local_254;
              do {
                bVar2 = *pbVar9;
                uVar12 = (undefined3)((uint)iVar11 >> 8);
                iVar11 = CONCAT31(uVar12,bVar2);
                bVar19 = bVar2 < (byte)*pcVar14;
                if (bVar2 != *pcVar14) {
LAB_100116ff:
                  iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                  goto LAB_10011704;
                }
                if (bVar2 == 0) break;
                bVar2 = pbVar9[1];
                iVar11 = CONCAT31(uVar12,bVar2);
                bVar19 = bVar2 < (byte)pcVar14[1];
                if (bVar2 != pcVar14[1]) goto LAB_100116ff;
                pbVar9 = pbVar9 + 2;
                pcVar14 = pcVar14 + 2;
              } while (bVar2 != 0);
              iVar10 = 0;
LAB_10011704:
              if (iVar10 == 0) {
                piVar18 = &local_230;
                iVar10 = 3;
                local_248 = &stack0xfffffd8c;
                CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                FUN_1000f3f0(iVar11,piVar18,iVar10);
                param_1[0x20] = '\x01';
                param_1[0x21] = '\0';
                param_1[0x22] = '\0';
                param_1[0x23] = '\0';
                param_1[0x24] = -3;
                param_1[0x25] = -1;
                param_1[0x26] = -1;
                param_1[0x27] = -1;
                *(int *)(param_1 + 0x2c) = local_230;
                DAT_1006c104 = 0;
                DAT_1006c0f0 = 3;
                DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
                DAT_1006c0f8 = local_22c;
                DAT_1006c0fc = local_228;
                cVar6 = clock();
                *(clock_t *)(param_1 + 0x28) = cVar6;
              }
              else {
                pcVar14 = s_WAITATTACKCHAR_1001a820;
                pbVar9 = local_254;
                do {
                  bVar2 = *pbVar9;
                  uVar12 = (undefined3)((uint)iVar11 >> 8);
                  iVar11 = CONCAT31(uVar12,bVar2);
                  bVar19 = bVar2 < (byte)*pcVar14;
                  if (bVar2 != *pcVar14) {
LAB_100117a4:
                    iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                    goto LAB_100117a9;
                  }
                  if (bVar2 == 0) break;
                  bVar2 = pbVar9[1];
                  iVar11 = CONCAT31(uVar12,bVar2);
                  bVar19 = bVar2 < (byte)pcVar14[1];
                  if (bVar2 != pcVar14[1]) goto LAB_100117a4;
                  pbVar9 = pbVar9 + 2;
                  pcVar14 = pcVar14 + 2;
                } while (bVar2 != 0);
                iVar10 = 0;
LAB_100117a9:
                if (iVar10 == 0) {
                  piVar18 = &local_230;
                  iVar10 = 2;
                  local_248 = &stack0xfffffd8c;
                  CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                  FUN_1000f3f0(iVar11,piVar18,iVar10);
                  param_1[0x20] = '\x01';
                  param_1[0x21] = '\0';
                  param_1[0x22] = '\0';
                  param_1[0x23] = '\0';
                  param_1[0x24] = -3;
                  param_1[0x25] = -1;
                  param_1[0x26] = -1;
                  param_1[0x27] = -1;
                  *(int *)(param_1 + 0x2c) = local_230;
                  DAT_1006c104 = 0;
                  DAT_1006c0f0 = 7;
                  DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
                  DAT_1006c0f8 = local_22c;
                  cVar6 = clock();
                  *(clock_t *)(param_1 + 0x28) = cVar6;
                }
                else {
                  pcVar14 = s_WAITKEYBOARD_1001a810;
                  pbVar9 = local_254;
                  do {
                    bVar2 = *pbVar9;
                    uVar12 = (undefined3)((uint)iVar11 >> 8);
                    iVar11 = CONCAT31(uVar12,bVar2);
                    bVar19 = bVar2 < (byte)*pcVar14;
                    if (bVar2 != *pcVar14) {
LAB_10011840:
                      iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                      goto LAB_10011845;
                    }
                    if (bVar2 == 0) break;
                    bVar2 = pbVar9[1];
                    iVar11 = CONCAT31(uVar12,bVar2);
                    bVar19 = bVar2 < (byte)pcVar14[1];
                    if (bVar2 != pcVar14[1]) goto LAB_10011840;
                    pbVar9 = pbVar9 + 2;
                    pcVar14 = pcVar14 + 2;
                  } while (bVar2 != 0);
                  iVar10 = 0;
LAB_10011845:
                  if (iVar10 == 0) {
                    local_238 = &stack0xfffffd94;
                    param_1[0x20] = '\x01';
                    param_1[0x21] = '\0';
                    param_1[0x22] = '\0';
                    param_1[0x23] = '\0';
                    param_1[0x24] = -3;
                    param_1[0x25] = -1;
                    param_1[0x26] = -1;
                    param_1[0x27] = -1;
                    CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                    pCVar8 = FUN_1000f110(&local_244,iVar11);
                    iVar11 = atoi(*(char **)pCVar8);
                    *(int *)(param_1 + 0x2c) = iVar11;
                    CString::~CString((CString *)&local_244);
                    DAT_1006c104 = 0;
                    DAT_1006c0f0 = 4;
                    pbVar13 = &DAT_1001a80c;
                    pbVar9 = local_258;
                    do {
                      bVar2 = *pbVar9;
                      bVar19 = bVar2 < *pbVar13;
                      if (bVar2 != *pbVar13) {
LAB_100118d1:
                        iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                        goto LAB_100118d6;
                      }
                      if (bVar2 == 0) break;
                      bVar2 = pbVar9[1];
                      bVar19 = bVar2 < pbVar13[1];
                      if (bVar2 != pbVar13[1]) goto LAB_100118d1;
                      pbVar9 = pbVar9 + 2;
                      pbVar13 = pbVar13 + 2;
                    } while (bVar2 != 0);
                    iVar11 = 0;
LAB_100118d6:
                    if (iVar11 == 0) {
                      DAT_1006c0f4 = 0x71;
                      cVar6 = clock();
                      *(clock_t *)(param_1 + 0x28) = cVar6;
                    }
                    else {
                      pbVar9 = &DAT_1001a808;
                      do {
                        bVar2 = *local_258;
                        bVar19 = bVar2 < *pbVar9;
                        if (bVar2 != *pbVar9) {
LAB_10011921:
                          iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                          goto LAB_10011926;
                        }
                        if (bVar2 == 0) break;
                        bVar2 = local_258[1];
                        bVar19 = bVar2 < pbVar9[1];
                        if (bVar2 != pbVar9[1]) goto LAB_10011921;
                        local_258 = local_258 + 2;
                        pbVar9 = pbVar9 + 2;
                      } while (bVar2 != 0);
                      iVar11 = 0;
LAB_10011926:
                      if (iVar11 == 0) {
                        DAT_1006c0f4 = 0x72;
                      }
                      cVar6 = clock();
                      *(clock_t *)(param_1 + 0x28) = cVar6;
                    }
                  }
                  else {
                    pcVar14 = s_WAITMOUSEMOVE_1001a7f8;
                    pbVar9 = local_254;
                    do {
                      bVar2 = *pbVar9;
                      uVar12 = (undefined3)((uint)iVar11 >> 8);
                      iVar11 = CONCAT31(uVar12,bVar2);
                      bVar19 = bVar2 < (byte)*pcVar14;
                      if (bVar2 != *pcVar14) {
LAB_1001196f:
                        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                        goto LAB_10011974;
                      }
                      if (bVar2 == 0) break;
                      bVar2 = pbVar9[1];
                      iVar11 = CONCAT31(uVar12,bVar2);
                      bVar19 = bVar2 < (byte)pcVar14[1];
                      if (bVar2 != pcVar14[1]) goto LAB_1001196f;
                      pbVar9 = pbVar9 + 2;
                      pcVar14 = pcVar14 + 2;
                    } while (bVar2 != 0);
                    iVar10 = 0;
LAB_10011974:
                    if (iVar10 == 0) {
                      local_248 = &stack0xfffffd94;
                      param_1[0x20] = '\x01';
                      param_1[0x21] = '\0';
                      param_1[0x22] = '\0';
                      param_1[0x23] = '\0';
                      param_1[0x24] = -3;
                      param_1[0x25] = -1;
                      param_1[0x26] = -1;
                      param_1[0x27] = -1;
                      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                      pCVar8 = FUN_1000f110(&local_244,iVar11);
                      iVar11 = atoi(*(char **)pCVar8);
                      *(int *)(param_1 + 0x2c) = iVar11;
                      CString::~CString((CString *)&local_244);
                      DAT_1006c0f0 = 5;
                      goto LAB_100119c9;
                    }
                    pcVar14 = s_WAITORDERMENU_1001a7e8;
                    pbVar9 = local_254;
                    do {
                      bVar2 = *pbVar9;
                      uVar12 = (undefined3)((uint)iVar11 >> 8);
                      iVar11 = CONCAT31(uVar12,bVar2);
                      bVar19 = bVar2 < (byte)*pcVar14;
                      if (bVar2 != *pcVar14) {
LAB_10011a0d:
                        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                        goto LAB_10011a12;
                      }
                      if (bVar2 == 0) break;
                      bVar2 = pbVar9[1];
                      iVar11 = CONCAT31(uVar12,bVar2);
                      bVar19 = bVar2 < (byte)pcVar14[1];
                      if (bVar2 != pcVar14[1]) goto LAB_10011a0d;
                      pbVar9 = pbVar9 + 2;
                      pcVar14 = pcVar14 + 2;
                    } while (bVar2 != 0);
                    iVar10 = 0;
LAB_10011a12:
                    if (iVar10 == 0) {
                      piVar18 = &local_230;
                      iVar10 = 2;
                      local_248 = &stack0xfffffd8c;
                      CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                      FUN_1000f3f0(iVar11,piVar18,iVar10);
                      param_1[0x20] = '\x01';
                      param_1[0x21] = '\0';
                      param_1[0x22] = '\0';
                      param_1[0x23] = '\0';
                      param_1[0x24] = -3;
                      param_1[0x25] = -1;
                      param_1[0x26] = -1;
                      param_1[0x27] = -1;
                      DAT_1006c0f8 = local_230;
                      *(int *)(param_1 + 0x2c) = local_22c;
                      DAT_1006c104 = 0;
                      DAT_1006c0f0 = DAT_1006c0f8 + 9;
                      DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
                      cVar6 = clock();
                      *(clock_t *)(param_1 + 0x28) = cVar6;
                    }
                    else {
                      pcVar14 = s_WAITMAGICMENU_1001a7d8;
                      pbVar9 = local_254;
                      do {
                        bVar2 = *pbVar9;
                        uVar12 = (undefined3)((uint)iVar11 >> 8);
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)*pcVar14;
                        if (bVar2 != *pcVar14) {
LAB_10011aac:
                          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                          goto LAB_10011ab1;
                        }
                        if (bVar2 == 0) break;
                        bVar2 = pbVar9[1];
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)pcVar14[1];
                        if (bVar2 != pcVar14[1]) goto LAB_10011aac;
                        pbVar9 = pbVar9 + 2;
                        pcVar14 = pcVar14 + 2;
                      } while (bVar2 != 0);
                      iVar10 = 0;
LAB_10011ab1:
                      if (iVar10 != 0) {
                        pcVar14 = s_WAITALLYBUTTON_1001a7c8;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10011b55:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10011b5a;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10011b55;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10011b5a:
                        if (iVar10 == 0) {
                          param_1[0x20] = '\x01';
                          param_1[0x21] = '\0';
                          param_1[0x22] = '\0';
                          param_1[0x23] = '\0';
                          param_1[0x24] = -3;
                          param_1[0x25] = -1;
                          param_1[0x26] = -1;
                          param_1[0x27] = -1;
                          param_1[0x2c] = -1;
                          param_1[0x2d] = -1;
                          param_1[0x2e] = -1;
                          param_1[0x2f] = -1;
                          DAT_1006c0f0 = 0xf;
                          DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
                          DAT_1006c104 = iVar10;
                          cVar6 = clock();
                          *(clock_t *)(param_1 + 0x28) = cVar6;
                          local_4._0_1_ = 3;
                          CDDSurface::~CDDSurface(local_200);
                          local_4._0_1_ = 2;
                          CString::~CString(local_24c);
                          local_4._0_1_ = 1;
                          CString::~CString((CString *)&local_258);
                          local_4 = (uint)local_4._1_3_ << 8;
                          CString::~CString((CString *)&local_254);
                          goto LAB_10013d39;
                        }
                        pcVar14 = s_SETTOPMENU_1001a7bc;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10011c0f:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10011c14;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10011c0f;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10011c14:
                        if (iVar10 == 0) {
                          FUN_10008d20();
                          goto LAB_10012be3;
                        }
                        pcVar14 = s_CHARMENU_1001a7b0;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10011c4f:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10011c54;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10011c4f;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10011c54:
                        if (iVar10 == 0) {
                          piVar18 = &local_230;
                          iVar10 = 3;
                          local_248 = &stack0xfffffd8c;
                          CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                          FUN_1000f3f0(iVar11,piVar18,iVar10);
                          local_21c = (undefined1 *)CONCAT22((undefined2)local_22c,(short)local_230)
                          ;
                          SetTickMenu();
                          goto LAB_10012be3;
                        }
                        pcVar14 = s_DISPLAYPOS_1001a7a4;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10011cdb:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10011ce4;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10011cdb;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10011ce4:
                        if (iVar10 == 0) {
                          pcVar14 = s_CLEAR_1001a79c;
                          do {
                            bVar2 = *local_258;
                            uVar12 = (undefined3)((uint)iVar11 >> 8);
                            iVar11 = CONCAT31(uVar12,bVar2);
                            bVar19 = bVar2 < (byte)*pcVar14;
                            if (bVar2 != *pcVar14) {
LAB_10011d1d:
                              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                              goto LAB_10011d21;
                            }
                            if (bVar2 == 0) break;
                            bVar2 = local_258[1];
                            iVar11 = CONCAT31(uVar12,bVar2);
                            bVar19 = bVar2 < (byte)pcVar14[1];
                            if (bVar2 != pcVar14[1]) goto LAB_10011d1d;
                            local_258 = local_258 + 2;
                            pcVar14 = pcVar14 + 2;
                          } while (bVar2 != 0);
                          iVar10 = 0;
LAB_10011d21:
                          if (iVar10 == 0) {
                            DAT_1001a4ec = -1;
                            DAT_1001a4f0 = -1;
                          }
                          else {
                            iVar10 = -1;
                            local_238 = &stack0xfffffd90;
                            CString::CString((CString *)&stack0xfffffd90,(CString *)&local_258);
                            pCVar8 = FUN_1000f110(&local_244,iVar11);
                            pcVar14 = (char *)FUN_10010f10((undefined4 *)pCVar8);
                            DAT_1001a4ec = FUN_1000f040(pcVar14,iVar10);
                            CString::~CString((CString *)&local_244);
                            local_238 = &stack0xfffffd94;
                            uVar23 = extraout_ECX_01;
                            CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                            pCVar8 = FUN_1000f310(&local_244,uVar23);
                            local_4._0_1_ = 7;
                            CString::operator=((CString *)&local_258,pCVar8);
                            local_4._0_1_ = 4;
                            CString::~CString((CString *)&local_244);
                            iVar10 = -1;
                            local_238 = &stack0xfffffd90;
                            iVar11 = extraout_ECX_02;
                            CString::CString((CString *)&stack0xfffffd90,(CString *)&local_258);
                            pCVar8 = FUN_1000f110(&local_244,iVar11);
                            pcVar14 = (char *)FUN_10010f10((undefined4 *)pCVar8);
                            DAT_1001a4f0 = FUN_1000f040(pcVar14,iVar10);
                            CString::~CString((CString *)&local_244);
                          }
                          local_4._0_1_ = 3;
                          CDDSurface::~CDDSurface(local_200);
                          local_4._0_1_ = 2;
                          CString::~CString(local_24c);
                          local_4._0_1_ = 1;
                          CString::~CString((CString *)&local_258);
                          local_4 = (uint)local_4._1_3_ << 8;
                          CString::~CString((CString *)&local_254);
                          local_4 = 0xffffffff;
                          CString::~CString((CString *)&local_250);
                          ExceptionList = local_c;
                          return 0;
                        }
                        pcVar14 = s_CLEARALLDIALOG_1001a78c;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10011e7b:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10011e82;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10011e7b;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10011e82:
                        if (iVar10 == 0) {
                          FUN_100029d0(0x4cb,7,0);
                          FUN_100029d0(0x4cb,1,0);
                          FUN_100029d0(0x4cb,9,0);
                          FUN_100029d0(0x4cb,3,0);
                          FUN_100029d0(0x4cb,5,0);
                          uStackY_2b4 = 0x10011ed8;
                          FUN_100029d0(0x4cb,0x13,0);
                          FUN_100029d0(0x4cb,0,0);
                          FUN_100029d0(0x4cb,0x18,0);
                          FUN_100029d0(0x4cb,0x19,0);
                          FUN_100029d0(0x4cb,0x1a,0);
                          FUN_100029d0(0x4cb,0x1b,0);
                          uStackY_2b4 = 0x10011f27;
                          FUN_100029d0(0x4cb,6,0);
                          FUN_100029d0(0x4cb,0x1c,0);
                          FUN_100029d0(0x4cb,4,0);
                          goto LAB_10012be3;
                        }
                        pcVar14 = s_SHOWDIALOG_1001a780;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10011f7b:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10011f80;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10011f7b;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10011f80:
                        if (iVar10 != 0) {
                          pcVar14 = s_HIDEDIALOG_1001a774;
                          pbVar9 = local_254;
                          do {
                            bVar2 = *pbVar9;
                            uVar12 = (undefined3)((uint)iVar11 >> 8);
                            iVar11 = CONCAT31(uVar12,bVar2);
                            bVar19 = bVar2 < (byte)*pcVar14;
                            if (bVar2 != *pcVar14) {
LAB_10011fb3:
                              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                              goto LAB_10011fb8;
                            }
                            if (bVar2 == 0) break;
                            bVar2 = pbVar9[1];
                            iVar11 = CONCAT31(uVar12,bVar2);
                            bVar19 = bVar2 < (byte)pcVar14[1];
                            if (bVar2 != pcVar14[1]) goto LAB_10011fb3;
                            pbVar9 = pbVar9 + 2;
                            pcVar14 = pcVar14 + 2;
                          } while (bVar2 != 0);
                          iVar10 = 0;
LAB_10011fb8:
                          if (iVar10 != 0) {
                            pcVar14 = s_SETVICTORY_1001a768;
                            pbVar9 = local_254;
                            do {
                              bVar2 = *pbVar9;
                              uVar12 = (undefined3)((uint)iVar11 >> 8);
                              iVar11 = CONCAT31(uVar12,bVar2);
                              bVar19 = bVar2 < (byte)*pcVar14;
                              if (bVar2 != *pcVar14) {
LAB_10011feb:
                                iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                goto LAB_10011ff0;
                              }
                              if (bVar2 == 0) break;
                              bVar2 = pbVar9[1];
                              iVar11 = CONCAT31(uVar12,bVar2);
                              bVar19 = bVar2 < (byte)pcVar14[1];
                              if (bVar2 != pcVar14[1]) goto LAB_10011feb;
                              pbVar9 = pbVar9 + 2;
                              pcVar14 = pcVar14 + 2;
                            } while (bVar2 != 0);
                            iVar10 = 0;
LAB_10011ff0:
                            if (iVar10 == 0) {
                              CString::operator=((CString *)&DAT_1006c0e0,(CString *)&local_258);
                              CString::Empty((CString *)&DAT_1006c0e4);
                              goto LAB_10012a94;
                            }
                            pcVar14 = s_SETALLYINFO_1001a75c;
                            pbVar9 = local_254;
                            do {
                              bVar2 = *pbVar9;
                              uVar12 = (undefined3)((uint)iVar11 >> 8);
                              iVar11 = CONCAT31(uVar12,bVar2);
                              bVar19 = bVar2 < (byte)*pcVar14;
                              if (bVar2 != *pcVar14) {
LAB_1001203d:
                                iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                goto LAB_10012042;
                              }
                              if (bVar2 == 0) break;
                              bVar2 = pbVar9[1];
                              iVar11 = CONCAT31(uVar12,bVar2);
                              bVar19 = bVar2 < (byte)pcVar14[1];
                              if (bVar2 != pcVar14[1]) goto LAB_1001203d;
                              pbVar9 = pbVar9 + 2;
                              pcVar14 = pcVar14 + 2;
                            } while (bVar2 != 0);
                            iVar10 = 0;
LAB_10012042:
                            if (iVar10 == 0) {
                              DAT_1006bd10 = 2;
                              _DAT_1006bd12 = 0;
                              lstrcpyA((LPSTR)&DAT_1006bd14,&DAT_1001a758);
                              DAT_1006bd3c = 2;
                              lstrcpyA(&DAT_1006bd3e,&DAT_1001a750);
                              DAT_1006c088._0_2_ = 2;
                              DAT_1006c088._2_2_ = 0;
                              DAT_1006c08c = 0;
                              _DAT_1006c08e = 2;
                              pbVar9 = &DAT_1001a574;
                              do {
                                bVar2 = *local_258;
                                bVar19 = bVar2 < *pbVar9;
                                if (bVar2 != *pbVar9) {
LAB_100120cf:
                                  iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                  goto LAB_100120d4;
                                }
                                if (bVar2 == 0) break;
                                bVar2 = local_258[1];
                                bVar19 = bVar2 < pbVar9[1];
                                if (bVar2 != pbVar9[1]) goto LAB_100120cf;
                                local_258 = local_258 + 2;
                                pbVar9 = pbVar9 + 2;
                              } while (bVar2 != 0);
                              iVar11 = 0;
LAB_100120d4:
                              _DAT_1006c090 = -(ushort)(iVar11 != 0) & 2;
                              puVar15 = &DAT_1006c088;
                              puVar17 = auStackY_2c4;
                              for (iVar11 = 0x15; iVar11 != 0; iVar11 = iVar11 + -1) {
                                *puVar17 = *puVar15;
                                puVar15 = puVar15 + 1;
                                puVar17 = puVar17 + 1;
                              }
                              *(undefined2 *)puVar17 = *(undefined2 *)puVar15;
                              SetAllyInfo();
                              goto LAB_10012a94;
                            }
                            goto LAB_100123c6;
                          }
                        }
                        DAT_1006c104 = 1;
                        pbVar13 = &DAT_1001a74c;
                        pbVar9 = local_258;
                        do {
                          bVar2 = *pbVar9;
                          bVar19 = bVar2 < *pbVar13;
                          if (bVar2 != *pbVar13) {
LAB_10012145:
                            iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_1001214a;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          bVar19 = bVar2 < pbVar13[1];
                          if (bVar2 != pbVar13[1]) goto LAB_10012145;
                          pbVar9 = pbVar9 + 2;
                          pbVar13 = pbVar13 + 2;
                        } while (bVar2 != 0);
                        iVar11 = 0;
LAB_1001214a:
                        if (iVar11 == 0) {
                          FUN_10002fb0(0x70);
                        }
                        else {
                          pbVar13 = &DAT_1001a80c;
                          pbVar9 = local_258;
                          do {
                            bVar2 = *pbVar9;
                            bVar19 = bVar2 < *pbVar13;
                            if (bVar2 != *pbVar13) {
LAB_10012189:
                              iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                              goto LAB_1001218e;
                            }
                            if (bVar2 == 0) break;
                            bVar2 = pbVar9[1];
                            bVar19 = bVar2 < pbVar13[1];
                            if (bVar2 != pbVar13[1]) goto LAB_10012189;
                            pbVar9 = pbVar9 + 2;
                            pbVar13 = pbVar13 + 2;
                          } while (bVar2 != 0);
                          iVar11 = 0;
LAB_1001218e:
                          if (iVar11 == 0) {
                            FUN_10002fb0(0x71);
                          }
                          else {
                            pbVar13 = &DAT_1001a808;
                            pbVar9 = local_258;
                            do {
                              bVar2 = *pbVar9;
                              bVar19 = bVar2 < *pbVar13;
                              if (bVar2 != *pbVar13) {
LAB_100121cd:
                                iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                goto LAB_100121d2;
                              }
                              if (bVar2 == 0) break;
                              bVar2 = pbVar9[1];
                              bVar19 = bVar2 < pbVar13[1];
                              if (bVar2 != pbVar13[1]) goto LAB_100121cd;
                              pbVar9 = pbVar9 + 2;
                              pbVar13 = pbVar13 + 2;
                            } while (bVar2 != 0);
                            iVar11 = 0;
LAB_100121d2:
                            if (iVar11 == 0) {
                              FUN_10002fb0(0x72);
                            }
                            else {
                              pbVar13 = &DAT_1001a748;
                              pbVar9 = local_258;
                              do {
                                bVar2 = *pbVar9;
                                bVar19 = bVar2 < *pbVar13;
                                if (bVar2 != *pbVar13) {
LAB_10012211:
                                  iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                  goto LAB_10012216;
                                }
                                if (bVar2 == 0) break;
                                bVar2 = pbVar9[1];
                                bVar19 = bVar2 < pbVar13[1];
                                if (bVar2 != pbVar13[1]) goto LAB_10012211;
                                pbVar9 = pbVar9 + 2;
                                pbVar13 = pbVar13 + 2;
                              } while (bVar2 != 0);
                              iVar11 = 0;
LAB_10012216:
                              if (iVar11 == 0) {
                                FUN_10002fb0(0x73);
                              }
                              else {
                                pbVar13 = &DAT_1001a744;
                                pbVar9 = local_258;
                                do {
                                  bVar2 = *pbVar9;
                                  bVar19 = bVar2 < *pbVar13;
                                  if (bVar2 != *pbVar13) {
LAB_10012255:
                                    iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                    goto LAB_1001225a;
                                  }
                                  if (bVar2 == 0) break;
                                  bVar2 = pbVar9[1];
                                  bVar19 = bVar2 < pbVar13[1];
                                  if (bVar2 != pbVar13[1]) goto LAB_10012255;
                                  pbVar9 = pbVar9 + 2;
                                  pbVar13 = pbVar13 + 2;
                                } while (bVar2 != 0);
                                iVar11 = 0;
LAB_1001225a:
                                if (iVar11 == 0) {
                                  FUN_10002fb0(0x74);
                                }
                                else {
                                  pbVar13 = &DAT_1001a740;
                                  pbVar9 = local_258;
                                  do {
                                    bVar2 = *pbVar9;
                                    bVar19 = bVar2 < *pbVar13;
                                    if (bVar2 != *pbVar13) {
LAB_10012299:
                                      iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                      goto LAB_1001229e;
                                    }
                                    if (bVar2 == 0) break;
                                    bVar2 = pbVar9[1];
                                    bVar19 = bVar2 < pbVar13[1];
                                    if (bVar2 != pbVar13[1]) goto LAB_10012299;
                                    pbVar9 = pbVar9 + 2;
                                    pbVar13 = pbVar13 + 2;
                                  } while (bVar2 != 0);
                                  iVar11 = 0;
LAB_1001229e:
                                  if (iVar11 == 0) {
                                    FUN_10002fb0(0x75);
                                  }
                                  else {
                                    pbVar13 = &DAT_1001a73c;
                                    pbVar9 = local_258;
                                    do {
                                      bVar2 = *pbVar9;
                                      bVar19 = bVar2 < *pbVar13;
                                      if (bVar2 != *pbVar13) {
LAB_100122dd:
                                        iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                        goto LAB_100122e2;
                                      }
                                      if (bVar2 == 0) break;
                                      bVar2 = pbVar9[1];
                                      bVar19 = bVar2 < pbVar13[1];
                                      if (bVar2 != pbVar13[1]) goto LAB_100122dd;
                                      pbVar9 = pbVar9 + 2;
                                      pbVar13 = pbVar13 + 2;
                                    } while (bVar2 != 0);
                                    iVar11 = 0;
LAB_100122e2:
                                    if (iVar11 == 0) {
                                      FUN_10002fb0(0x76);
                                    }
                                    else {
                                      pbVar13 = &DAT_1001a738;
                                      pbVar9 = local_258;
                                      do {
                                        bVar2 = *pbVar9;
                                        bVar19 = bVar2 < *pbVar13;
                                        if (bVar2 != *pbVar13) {
LAB_10012321:
                                          iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                          goto LAB_10012326;
                                        }
                                        if (bVar2 == 0) break;
                                        bVar2 = pbVar9[1];
                                        bVar19 = bVar2 < pbVar13[1];
                                        if (bVar2 != pbVar13[1]) goto LAB_10012321;
                                        pbVar9 = pbVar9 + 2;
                                        pbVar13 = pbVar13 + 2;
                                      } while (bVar2 != 0);
                                      iVar11 = 0;
LAB_10012326:
                                      if (iVar11 == 0) {
                                        FUN_10002fb0(0x77);
                                      }
                                      else {
                                        pbVar13 = &DAT_1001a734;
                                        pbVar9 = local_258;
                                        do {
                                          bVar2 = *pbVar9;
                                          bVar19 = bVar2 < *pbVar13;
                                          if (bVar2 != *pbVar13) {
LAB_10012365:
                                            iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                            goto LAB_1001236a;
                                          }
                                          if (bVar2 == 0) break;
                                          bVar2 = pbVar9[1];
                                          bVar19 = bVar2 < pbVar13[1];
                                          if (bVar2 != pbVar13[1]) goto LAB_10012365;
                                          pbVar9 = pbVar9 + 2;
                                          pbVar13 = pbVar13 + 2;
                                        } while (bVar2 != 0);
                                        iVar11 = 0;
LAB_1001236a:
                                        if (iVar11 == 0) {
                                          FUN_10002fb0(0x78);
                                        }
                                        else {
                                          pbVar9 = &DAT_1001a730;
                                          do {
                                            bVar2 = *local_258;
                                            bVar19 = bVar2 < *pbVar9;
                                            if (bVar2 != *pbVar9) {
LAB_100123a9:
                                              iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                              goto LAB_100123ae;
                                            }
                                            if (bVar2 == 0) break;
                                            bVar2 = local_258[1];
                                            bVar19 = bVar2 < pbVar9[1];
                                            if (bVar2 != pbVar9[1]) goto LAB_100123a9;
                                            local_258 = local_258 + 2;
                                            pbVar9 = pbVar9 + 2;
                                          } while (bVar2 != 0);
                                          iVar11 = 0;
LAB_100123ae:
                                          if (iVar11 == 0) {
                                            FUN_10002fb0(0x7b);
                                          }
                                        }
                                      }
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
LAB_10012be3:
                        local_4._0_1_ = 3;
                        CDDSurface::~CDDSurface(local_200);
                        local_4._0_1_ = 2;
                        CString::~CString(local_24c);
                        local_4._0_1_ = 1;
                        CString::~CString((CString *)&local_258);
                        local_4 = (uint)local_4._1_3_ << 8;
                        CString::~CString((CString *)&local_254);
                        local_4 = 0xffffffff;
                        CString::~CString((CString *)&local_250);
                        ExceptionList = local_c;
                        return 0;
                      }
                      piVar18 = &local_230;
                      iVar10 = 3;
                      local_248 = &stack0xfffffd8c;
                      CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                      FUN_1000f3f0(iVar11,piVar18,iVar10);
                      param_1[0x20] = '\x01';
                      param_1[0x21] = '\0';
                      param_1[0x22] = '\0';
                      param_1[0x23] = '\0';
                      param_1[0x24] = -3;
                      param_1[0x25] = -1;
                      param_1[0x26] = -1;
                      param_1[0x27] = -1;
                      DAT_1006c0f8 = local_230;
                      *(int *)(param_1 + 0x2c) = local_22c;
                      DAT_1006c0f0 = DAT_1006c0f8 + 0xc;
                      DAT_1006c104 = 0;
                      DAT_1006c0fc = local_228;
                      DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
                      cVar6 = clock();
                      *(clock_t *)(param_1 + 0x28) = cVar6;
                    }
                  }
                }
              }
              goto LAB_10013cea;
            }
            local_248 = &stack0xfffffd94;
            param_1[0x20] = '\x01';
            param_1[0x21] = '\0';
            param_1[0x22] = '\0';
            param_1[0x23] = '\0';
            param_1[0x24] = -3;
            param_1[0x25] = -1;
            param_1[0x26] = -1;
            param_1[0x27] = -1;
            CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
            pCVar8 = FUN_1000f110(&local_244,iVar11);
            iVar11 = atoi(*(char **)pCVar8);
            *(int *)(param_1 + 0x2c) = iVar11;
            CString::~CString((CString *)&local_244);
            DAT_1006c0f0 = 8;
          }
        }
      }
LAB_100119c9:
      DAT_1006c104 = 0;
      DAT_1006c0f4 = *(undefined4 *)(param_1 + 0x2c);
      cVar6 = clock();
      *(clock_t *)(param_1 + 0x28) = cVar6;
      goto LAB_10013cea;
    }
LAB_100123c6:
    pcVar14 = s_TUTORIAL_1001a724;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_100123f1:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_100123f6;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_100123f1;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_100123f6:
    if (iVar10 == 0) {
      setgamestatus(1);
      DAT_1006a738 = 0;
      DAT_1006c118 = 1;
      goto LAB_10012a94;
    }
    pcVar14 = s_DISABLE_1001a71c;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012449:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_1001244e;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012449;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_1001244e:
    if (iVar10 == 0) {
      pcVar14 = s_MOUSEMENU_1001a710;
      pbVar9 = local_258;
      do {
        bVar2 = *pbVar9;
        uVar12 = (undefined3)((uint)iVar11 >> 8);
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)*pcVar14;
        if (bVar2 != *pcVar14) {
LAB_10012483:
          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
          goto LAB_10012488;
        }
        if (bVar2 == 0) break;
        bVar2 = pbVar9[1];
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)pcVar14[1];
        if (bVar2 != pcVar14[1]) goto LAB_10012483;
        pbVar9 = pbVar9 + 2;
        pcVar14 = pcVar14 + 2;
      } while (bVar2 != 0);
      iVar10 = 0;
LAB_10012488:
      if (iVar10 == 0) {
        DAT_1006c114 = 0;
      }
      else {
        pcVar14 = s_KEYBOARD_1001a704;
        pbVar9 = local_258;
        do {
          bVar2 = *pbVar9;
          uVar12 = (undefined3)((uint)iVar11 >> 8);
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)*pcVar14;
          if (bVar2 != *pcVar14) {
LAB_100124c0:
            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
            goto LAB_100124c5;
          }
          if (bVar2 == 0) break;
          bVar2 = pbVar9[1];
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)pcVar14[1];
          if (bVar2 != pcVar14[1]) goto LAB_100124c0;
          pbVar9 = pbVar9 + 2;
          pcVar14 = pcVar14 + 2;
        } while (bVar2 != 0);
        iVar10 = 0;
LAB_100124c5:
        if (iVar10 == 0) {
          DAT_1006c10c = 0;
        }
        else {
          pcVar14 = s_MOUSEMOVE_1001a6f8;
          pbVar9 = local_258;
          do {
            bVar2 = *pbVar9;
            uVar12 = (undefined3)((uint)iVar11 >> 8);
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)*pcVar14;
            if (bVar2 != *pcVar14) {
LAB_100124fd:
              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
              goto LAB_10012502;
            }
            if (bVar2 == 0) break;
            bVar2 = pbVar9[1];
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)pcVar14[1];
            if (bVar2 != pcVar14[1]) goto LAB_100124fd;
            pbVar9 = pbVar9 + 2;
            pcVar14 = pcVar14 + 2;
          } while (bVar2 != 0);
          iVar10 = 0;
LAB_10012502:
          if (iVar10 == 0) {
            DAT_1006c110 = 0;
          }
        }
      }
    }
    pcVar14 = s_ENABLE_1001a6f0;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012536:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_1001253b;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012536;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_1001253b:
    if (iVar10 == 0) {
      pcVar14 = s_MOUSEMENU_1001a710;
      pbVar9 = local_258;
      do {
        bVar2 = *pbVar9;
        uVar12 = (undefined3)((uint)iVar11 >> 8);
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)*pcVar14;
        if (bVar2 != *pcVar14) {
LAB_10012570:
          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
          goto LAB_10012575;
        }
        if (bVar2 == 0) break;
        bVar2 = pbVar9[1];
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)pcVar14[1];
        if (bVar2 != pcVar14[1]) goto LAB_10012570;
        pbVar9 = pbVar9 + 2;
        pcVar14 = pcVar14 + 2;
      } while (bVar2 != 0);
      iVar10 = 0;
LAB_10012575:
      if (iVar10 == 0) {
        DAT_1006c114 = 1;
      }
      else {
        pcVar14 = s_KEYBOARD_1001a704;
        pbVar9 = local_258;
        do {
          bVar2 = *pbVar9;
          uVar12 = (undefined3)((uint)iVar11 >> 8);
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)*pcVar14;
          if (bVar2 != *pcVar14) {
LAB_100125b5:
            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
            goto LAB_100125ba;
          }
          if (bVar2 == 0) break;
          bVar2 = pbVar9[1];
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)pcVar14[1];
          if (bVar2 != pcVar14[1]) goto LAB_100125b5;
          pbVar9 = pbVar9 + 2;
          pcVar14 = pcVar14 + 2;
        } while (bVar2 != 0);
        iVar10 = 0;
LAB_100125ba:
        if (iVar10 == 0) {
          DAT_1006c10c = 1;
        }
        else {
          pcVar14 = s_MOUSEMOVE_1001a6f8;
          pbVar9 = local_258;
          do {
            bVar2 = *pbVar9;
            uVar12 = (undefined3)((uint)iVar11 >> 8);
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)*pcVar14;
            if (bVar2 != *pcVar14) {
LAB_100125f7:
              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
              goto LAB_100125fc;
            }
            if (bVar2 == 0) break;
            bVar2 = pbVar9[1];
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)pcVar14[1];
            if (bVar2 != pcVar14[1]) goto LAB_100125f7;
            pbVar9 = pbVar9 + 2;
            pcVar14 = pcVar14 + 2;
          } while (bVar2 != 0);
          iVar10 = 0;
LAB_100125fc:
          if (iVar10 == 0) {
            DAT_1006c110 = 1;
          }
        }
      }
    }
    pcVar14 = s_LOADSCENE_1001a6e4;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012635:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_1001263a;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012635;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_1001263a:
    if (iVar10 == 0) {
      if ((DAT_1001c49c == (code *)0x0) || (iVar11 = (*DAT_1001c49c)(), iVar11 == 0)) {
        DAT_1001c46c = 0;
      }
      else {
        CDDSurface::CDDSurface(aCStack_160);
        local_4._0_1_ = 8;
        CString::CString((CString *)&local_234);
        local_4 = CONCAT31(local_4._1_3_,9);
        DAT_1001c46c = 1;
        pCVar8 = (CString *)operator+((CString *)&local_240,&DAT_1001a8b4);
        puStack_8 = (undefined1 *)CONCAT31(puStack_8._1_3_,10);
        pcVar14 = (char *)operator+((CString *)&local_240,pCVar8);
        local_c = (void *)CONCAT31(local_c._1_3_,0xb);
        pCVar8 = (CString *)operator+(local_24c,pcVar14);
        uStack_10 = 0xc;
        CString::operator=((CString *)&local_240,pCVar8);
        uStack_10 = 0xb;
        CString::~CString((CString *)&local_250);
        uStack_10 = 10;
        CString::~CString((CString *)&local_248);
        uStack_10 = 9;
        CString::~CString(local_24c);
        iVar11 = CDDSurface::CreateFromImage((CDDSurface *)&local_16c,DAT_1001c48c,local_240,0);
        if (iVar11 == 0) {
          uStack_7c = 100;
          uStack_2c = 0;
          (**(code **)(**(int **)(DAT_1001c484 + 0x84) + 0x14))();
        }
        else {
          CDDSurface::SetColorKey((CDDSurface *)&local_16c,0);
          puVar15 = auStack_e0;
          for (iVar11 = 0x19; iVar11 != 0; iVar11 = iVar11 + -1) {
            *puVar15 = 0;
            puVar15 = puVar15 + 1;
          }
          auStack_e0[0] = 100;
          CDDSurface::GetRect((CDDSurface *)&local_16c,(tagRECT *)local_23c);
          SetRect((LPRECT)local_220,0x8a,0,0xee,100);
          CDDSurface::Blt(DAT_1001c484,(tagRECT *)local_220,(CDDSurface *)&local_16c,
                          (tagRECT *)local_23c,0x8000,(_DDBLTFX *)auStack_e0);
        }
        FUN_10003d10(*(int *)(DAT_1001c490 + 0x88) / 2,*(int *)(DAT_1001c490 + 0x8c) / 2);
        local_4._0_1_ = 8;
        CString::~CString((CString *)&local_234);
        local_4._0_1_ = 4;
        CDDSurface::~CDDSurface(aCStack_160);
      }
      goto LAB_10012a94;
    }
    pcVar14 = s_CHANGESPRITE_1001a6d4;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_1001287e:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012883;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_1001287e;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012883:
    if (iVar10 == 0) {
      local_248 = &stack0xfffffd94;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      FUN_10010450();
      goto LAB_10012a94;
    }
    pcVar14 = s_CHANGETILE2_1001a6c8;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_100128cf:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_100128d4;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_100128cf;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_100128d4:
    if (iVar10 == 0) {
      local_248 = &stack0xfffffd94;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      FUN_10010500();
      goto LAB_10013cea;
    }
    pbVar13 = &DAT_1001a6c4;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < *pbVar13;
      if (bVar2 != *pbVar13) {
LAB_10012920:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012925;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < pbVar13[1];
      if (bVar2 != pbVar13[1]) goto LAB_10012920;
      pbVar9 = pbVar9 + 2;
      pbVar13 = pbVar13 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012925:
    if (iVar10 == 0) {
LAB_10013c6b:
      DAT_1001a50c = 1;
LAB_10012a94:
      local_4._0_1_ = 3;
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 0;
    }
    pbVar13 = &DAT_1001a6c0;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < *pbVar13;
      if (bVar2 != *pbVar13) {
LAB_10012958:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_1001295d;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < pbVar13[1];
      if (bVar2 != pbVar13[1]) goto LAB_10012958;
      pbVar9 = pbVar9 + 2;
      pbVar13 = pbVar13 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_1001295d:
    if (iVar10 == 0) goto LAB_10013c6b;
    if ((DAT_1001a50c == 0) ||
       (((*(int *)(param_1 + 4) == 1 &&
         (iVar11 = DAT_1006a738, *(int *)(param_1 + 0x18) != DAT_1006a738)) ||
        (*(int *)(param_1 + 0x14) < 0)))) goto LAB_10012a94;
    pbVar13 = &DAT_1001a6b8;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < *pbVar13;
      if (bVar2 != *pbVar13) {
LAB_100129c1:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_100129c6;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < pbVar13[1];
      if (bVar2 != pbVar13[1]) goto LAB_100129c1;
      pbVar9 = pbVar9 + 2;
      pbVar13 = pbVar13 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_100129c6:
    if (iVar10 == 0) {
      DAT_1001a50c = DAT_1001a508;
      goto LAB_10012a94;
    }
    pcVar14 = s_TESTNOT_1001a6b0;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012a06:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012a0b;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012a06;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012a0b:
    if (iVar10 == 0) {
      bVar19 = DAT_1001a508 == 0;
LAB_10012a8b:
      DAT_1001a50c = (uint)bVar19;
      goto LAB_10012a94;
    }
    pcVar14 = s_TESTSEL_1001a6a8;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012a45:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012a4a;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012a45;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012a4a:
    if (iVar10 == 0) {
      local_248 = &stack0xfffffd94;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      pCVar8 = FUN_1000f110(&local_240,iVar11);
      iVar11 = atoi(*(char **)pCVar8);
      CString::~CString((CString *)&local_240);
      bVar19 = *(int *)(param_1 + 0x38) == iVar11;
      goto LAB_10012a8b;
    }
    pbVar13 = &DAT_1001a6a4;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < *pbVar13;
      if (bVar2 != *pbVar13) {
LAB_10012b1e:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012b23;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < pbVar13[1];
      if (bVar2 != pbVar13[1]) goto LAB_10012b1e;
      pbVar9 = pbVar9 + 2;
      pbVar13 = pbVar13 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012b23:
    if (iVar10 == 0) {
LAB_1001349b:
      local_4._0_1_ = 3;
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 0;
    }
    pbVar13 = &DAT_1001a6a0;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < *pbVar13;
      if (bVar2 != *pbVar13) {
LAB_10012b56:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012b5b;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < pbVar13[1];
      if (bVar2 != pbVar13[1]) goto LAB_10012b56;
      pbVar9 = pbVar9 + 2;
      pbVar13 = pbVar13 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012b5b:
    if (iVar10 == 0) goto LAB_1001349b;
    pcVar14 = s_SCENECLEAR_1001a694;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012b8e:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012b93;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012b8e;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012b93:
    if (iVar10 == 0) {
      pcVar14 = s_ALONE_1001a68c;
      do {
        bVar2 = *local_258;
        bVar19 = bVar2 < (byte)*pcVar14;
        if (bVar2 != *pcVar14) {
LAB_10012bcc:
          iVar11 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
          goto LAB_10012bd1;
        }
        if (bVar2 == 0) break;
        bVar2 = local_258[1];
        bVar19 = bVar2 < (byte)pcVar14[1];
        if (bVar2 != pcVar14[1]) goto LAB_10012bcc;
        local_258 = local_258 + 2;
        pcVar14 = pcVar14 + 2;
      } while (bVar2 != 0);
      iVar11 = 0;
LAB_10012bd1:
      if (iVar11 == 0) {
        CVar22 = (CString)0x2;
      }
      else {
        CVar22 = (CString)0x1;
      }
      FUN_1000eab0(CVar22);
      goto LAB_10012be3;
    }
    pcVar14 = s_RANDOM_1001a684;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012c6d:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012c72;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012c6d;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012c72:
    if (iVar10 == 0) {
      tVar20 = time((time_t *)0x0);
      srand((uint)tVar20);
      iVar11 = atoi((char *)local_258);
      iVar10 = rand();
      local_4._0_1_ = 3;
      DAT_1001a508 = (uint)(iVar10 % 0x65 < iVar11);
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 0;
    }
    pcVar14 = s_RANDOMSELECT_1001a674;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012d3c:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012d41;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012d3c;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012d41:
    if (iVar10 == 0) {
      tVar20 = time((time_t *)0x0);
      srand((uint)tVar20);
      local_248 = &stack0xfffffd94;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      iVar11 = FUN_10014230();
      *(int *)(param_1 + 0x38) = iVar11;
      local_4._0_1_ = 3;
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 0;
    }
    pcVar14 = s_MESSAGE_1001a66c;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012dfe:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012e03;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012dfe;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012e03:
    if (iVar10 == 0) {
      iVar11 = FUN_100029d0(0x4d1,8,0);
      if (iVar11 != 0) {
        local_4._0_1_ = 3;
        CDDSurface::~CDDSurface(local_200);
        local_4._0_1_ = 2;
        CString::~CString(local_24c);
        local_4._0_1_ = 1;
        CString::~CString((CString *)&local_258);
        local_4 = (uint)local_4._1_3_ << 8;
        CString::~CString((CString *)&local_254);
        local_4 = 0xffffffff;
        CString::~CString((CString *)&local_250);
        ExceptionList = local_c;
        return 0;
      }
      local_248 = &stack0xfffffd94;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      FUN_100109e0();
      param_1[0x30] = '\x01';
      param_1[0x31] = '\0';
      param_1[0x32] = '\0';
      param_1[0x33] = '\0';
      param_1[0x1c] = '\x01';
      param_1[0x1d] = '\0';
      param_1[0x1e] = '\0';
      param_1[0x1f] = '\0';
      local_4._0_1_ = 3;
      CDDSurface::~CDDSurface(local_200);
      local_4._0_1_ = 2;
      CString::~CString(local_24c);
      local_4._0_1_ = 1;
      CString::~CString((CString *)&local_258);
      local_4 = (uint)local_4._1_3_ << 8;
      CString::~CString((CString *)&local_254);
      local_4 = 0xffffffff;
      CString::~CString((CString *)&local_250);
      ExceptionList = local_c;
      return 1;
    }
    pcVar14 = s_PLAYMIDI_1001a660;
    pbVar9 = local_254;
    do {
      bVar2 = *pbVar9;
      uVar12 = (undefined3)((uint)iVar11 >> 8);
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)*pcVar14;
      if (bVar2 != *pcVar14) {
LAB_10012f2b:
        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
        goto LAB_10012f30;
      }
      if (bVar2 == 0) break;
      bVar2 = pbVar9[1];
      iVar11 = CONCAT31(uVar12,bVar2);
      bVar19 = bVar2 < (byte)pcVar14[1];
      if (bVar2 != pcVar14[1]) goto LAB_10012f2b;
      pbVar9 = pbVar9 + 2;
      pcVar14 = pcVar14 + 2;
    } while (bVar2 != 0);
    iVar10 = 0;
LAB_10012f30:
    if (iVar10 == 0) {
      local_248 = &stack0xfffffd94;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      pCVar8 = FUN_1000f110(&local_240,iVar11);
      local_4._0_1_ = 0xd;
      CString::operator=((CString *)&local_250,pCVar8);
      local_4._0_1_ = 4;
      CString::~CString((CString *)&local_240);
      local_248 = &stack0xfffffd94;
      uVar23 = extraout_ECX_03;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      pCVar8 = FUN_1000f310(&local_240,uVar23);
      local_4._0_1_ = 0xe;
      CString::operator=((CString *)&local_258,pCVar8);
      local_4._0_1_ = 4;
      CString::~CString((CString *)&local_240);
      local_248 = &stack0xfffffd94;
      iVar11 = extraout_ECX_04;
      CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
      pCVar8 = FUN_1000f110(&local_240,iVar11);
      local_4._0_1_ = 0xf;
      CString::operator=((CString *)&local_258,pCVar8);
      local_4._0_1_ = 4;
      CString::~CString((CString *)&local_240);
      wParam = FUN_1000f040((char *)local_258,1);
      SendMessageA(DAT_1001c468,0x591,wParam,local_250);
    }
    else {
      pcVar14 = s_STOPMIDI_1001a654;
      pbVar9 = local_254;
      do {
        bVar2 = *pbVar9;
        uVar12 = (undefined3)((uint)iVar11 >> 8);
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)*pcVar14;
        if (bVar2 != *pcVar14) {
LAB_10013051:
          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
          goto LAB_10013056;
        }
        if (bVar2 == 0) break;
        bVar2 = pbVar9[1];
        iVar11 = CONCAT31(uVar12,bVar2);
        bVar19 = bVar2 < (byte)pcVar14[1];
        if (bVar2 != pcVar14[1]) goto LAB_10013051;
        pbVar9 = pbVar9 + 2;
        pcVar14 = pcVar14 + 2;
      } while (bVar2 != 0);
      iVar10 = 0;
LAB_10013056:
      if (iVar10 == 0) {
        PostMessageA(DAT_1001c468,0x590,0,0);
      }
      else {
        pcVar14 = s_SETPOSITION_1001a648;
        pbVar9 = local_254;
        do {
          bVar2 = *pbVar9;
          uVar12 = (undefined3)((uint)iVar11 >> 8);
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)*pcVar14;
          if (bVar2 != *pcVar14) {
LAB_1001309e:
            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
            goto LAB_100130a3;
          }
          if (bVar2 == 0) break;
          bVar2 = pbVar9[1];
          iVar11 = CONCAT31(uVar12,bVar2);
          bVar19 = bVar2 < (byte)pcVar14[1];
          if (bVar2 != pcVar14[1]) goto LAB_1001309e;
          pbVar9 = pbVar9 + 2;
          pcVar14 = pcVar14 + 2;
        } while (bVar2 != 0);
        iVar10 = 0;
LAB_100130a3:
        if (iVar10 == 0) {
          piVar18 = &local_230;
          iVar10 = 2;
          local_248 = &stack0xfffffd8c;
          CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
          FUN_1000f3f0(iVar11,piVar18,iVar10);
          FUN_10003d10(local_230,local_22c);
        }
        else {
          pcVar14 = s_FIRSTCLEAR_1001a63c;
          pbVar9 = local_254;
          do {
            bVar2 = *pbVar9;
            uVar12 = (undefined3)((uint)iVar11 >> 8);
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)*pcVar14;
            if (bVar2 != *pcVar14) {
LAB_10013106:
              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
              goto LAB_1001310b;
            }
            if (bVar2 == 0) break;
            bVar2 = pbVar9[1];
            iVar11 = CONCAT31(uVar12,bVar2);
            bVar19 = bVar2 < (byte)pcVar14[1];
            if (bVar2 != pcVar14[1]) goto LAB_10013106;
            pbVar9 = pbVar9 + 2;
            pcVar14 = pcVar14 + 2;
          } while (bVar2 != 0);
          iVar10 = 0;
LAB_1001310b:
          if (iVar10 == 0) {
            local_248 = &stack0xfffffd94;
            CString::CString((CString *)&stack0xfffffd94,(CString *)&DAT_1006c0e8);
            iVar11 = FUN_10010010();
            DAT_1001a508 = (uint)(iVar11 != 0);
          }
          else {
            pcVar14 = s_SAVECLEAR_1001a630;
            pbVar9 = local_254;
            do {
              bVar2 = *pbVar9;
              uVar12 = (undefined3)((uint)iVar11 >> 8);
              iVar11 = CONCAT31(uVar12,bVar2);
              bVar19 = bVar2 < (byte)*pcVar14;
              if (bVar2 != *pcVar14) {
LAB_10013164:
                iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                goto LAB_10013169;
              }
              if (bVar2 == 0) break;
              bVar2 = pbVar9[1];
              iVar11 = CONCAT31(uVar12,bVar2);
              bVar19 = bVar2 < (byte)pcVar14[1];
              if (bVar2 != pcVar14[1]) goto LAB_10013164;
              pbVar9 = pbVar9 + 2;
              pcVar14 = pcVar14 + 2;
            } while (bVar2 != 0);
            iVar10 = 0;
LAB_10013169:
            if (iVar10 == 0) {
              local_248 = &stack0xfffffd94;
              CString::CString((CString *)&stack0xfffffd94,(CString *)&DAT_1006c0e8);
              FUN_10010250();
            }
            else {
              pcVar14 = s_MOVEPOSITION_1001a620;
              pbVar9 = local_254;
              do {
                bVar2 = *pbVar9;
                uVar12 = (undefined3)((uint)iVar11 >> 8);
                iVar11 = CONCAT31(uVar12,bVar2);
                bVar19 = bVar2 < (byte)*pcVar14;
                if (bVar2 != *pcVar14) {
LAB_100131b5:
                  iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                  goto LAB_100131ba;
                }
                if (bVar2 == 0) break;
                bVar2 = pbVar9[1];
                iVar11 = CONCAT31(uVar12,bVar2);
                bVar19 = bVar2 < (byte)pcVar14[1];
                if (bVar2 != pcVar14[1]) goto LAB_100131b5;
                pbVar9 = pbVar9 + 2;
                pcVar14 = pcVar14 + 2;
              } while (bVar2 != 0);
              iVar10 = 0;
LAB_100131ba:
              if (iVar10 == 0) {
                piVar18 = &local_230;
                iVar10 = 2;
                local_248 = &stack0xfffffd8c;
                CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                FUN_1000f3f0(iVar11,piVar18,iVar10);
                FUN_10003d10(local_230,local_22c);
              }
              else {
                pbVar13 = &DAT_1001a61c;
                pbVar9 = local_254;
                do {
                  bVar2 = *pbVar9;
                  uVar12 = (undefined3)((uint)iVar11 >> 8);
                  iVar11 = CONCAT31(uVar12,bVar2);
                  bVar19 = bVar2 < *pbVar13;
                  if (bVar2 != *pbVar13) {
LAB_1001321d:
                    iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                    goto LAB_10013222;
                  }
                  if (bVar2 == 0) break;
                  bVar2 = pbVar9[1];
                  iVar11 = CONCAT31(uVar12,bVar2);
                  bVar19 = bVar2 < pbVar13[1];
                  if (bVar2 != pbVar13[1]) goto LAB_1001321d;
                  pbVar9 = pbVar9 + 2;
                  pbVar13 = pbVar13 + 2;
                } while (bVar2 != 0);
                iVar10 = 0;
LAB_10013222:
                if (iVar10 == 0) {
                  local_248 = &stack0xfffffd94;
                  param_1[0x20] = '\x01';
                  param_1[0x21] = '\0';
                  param_1[0x22] = '\0';
                  param_1[0x23] = '\0';
                  CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                  pCVar8 = FUN_1000f110(&local_240,iVar11);
                  iVar11 = atoi(*(char **)pCVar8);
                  *(int *)(param_1 + 0x24) = iVar11;
                  CString::~CString((CString *)&local_240);
                  cVar6 = clock();
                  *(clock_t *)(param_1 + 0x28) = cVar6;
                }
                else {
                  pbVar13 = &DAT_1001a618;
                  pbVar9 = local_254;
                  do {
                    bVar2 = *pbVar9;
                    uVar12 = (undefined3)((uint)iVar11 >> 8);
                    iVar11 = CONCAT31(uVar12,bVar2);
                    bVar19 = bVar2 < *pbVar13;
                    if (bVar2 != *pbVar13) {
LAB_10013299:
                      iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                      goto LAB_1001329e;
                    }
                    if (bVar2 == 0) break;
                    bVar2 = pbVar9[1];
                    iVar11 = CONCAT31(uVar12,bVar2);
                    bVar19 = bVar2 < pbVar13[1];
                    if (bVar2 != pbVar13[1]) goto LAB_10013299;
                    pbVar9 = pbVar9 + 2;
                    pbVar13 = pbVar13 + 2;
                  } while (bVar2 != 0);
                  iVar10 = 0;
LAB_1001329e:
                  if (iVar10 != 0) {
                    pcVar14 = s_WAITMOVEEND_1001a60c;
                    pbVar9 = local_254;
                    do {
                      bVar2 = *pbVar9;
                      uVar12 = (undefined3)((uint)iVar11 >> 8);
                      iVar11 = CONCAT31(uVar12,bVar2);
                      bVar19 = bVar2 < (byte)*pcVar14;
                      if (bVar2 != *pcVar14) {
LAB_100132d1:
                        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                        goto LAB_100132d6;
                      }
                      if (bVar2 == 0) break;
                      bVar2 = pbVar9[1];
                      iVar11 = CONCAT31(uVar12,bVar2);
                      bVar19 = bVar2 < (byte)pcVar14[1];
                      if (bVar2 != pcVar14[1]) goto LAB_100132d1;
                      pbVar9 = pbVar9 + 2;
                      pcVar14 = pcVar14 + 2;
                    } while (bVar2 != 0);
                    iVar10 = 0;
LAB_100132d6:
                    if (iVar10 != 0) {
                      pcVar14 = s_CHARPOS_1001a604;
                      pbVar9 = local_254;
                      do {
                        bVar2 = *pbVar9;
                        uVar12 = (undefined3)((uint)iVar11 >> 8);
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)*pcVar14;
                        if (bVar2 != *pcVar14) {
LAB_10013309:
                          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                          goto LAB_1001330e;
                        }
                        if (bVar2 == 0) break;
                        bVar2 = pbVar9[1];
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)pcVar14[1];
                        if (bVar2 != pcVar14[1]) goto LAB_10013309;
                        pbVar9 = pbVar9 + 2;
                        pcVar14 = pcVar14 + 2;
                      } while (bVar2 != 0);
                      iVar10 = 0;
LAB_1001330e:
                      if (iVar10 == 0) {
                        local_248 = &stack0xfffffd94;
                        CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                        ExecuteCharPosCommand();
                        local_4._0_1_ = 3;
                        CDDSurface::~CDDSurface(local_200);
                        local_4._0_1_ = 2;
                        CString::~CString(local_24c);
                        local_4._0_1_ = 1;
                        CString::~CString((CString *)&local_258);
                        local_4 = (uint)local_4._1_3_ << 8;
                        CString::~CString((CString *)&local_254);
                        local_4 = 0xffffffff;
                        CString::~CString((CString *)&local_250);
                        ExceptionList = local_c;
                        return 0;
                      }
                      pcVar14 = s_CHARABILITY_1001a5f8;
                      pbVar9 = local_254;
                      do {
                        bVar2 = *pbVar9;
                        uVar12 = (undefined3)((uint)iVar11 >> 8);
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)*pcVar14;
                        if (bVar2 != *pcVar14) {
LAB_100133b4:
                          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                          goto LAB_100133b9;
                        }
                        if (bVar2 == 0) break;
                        bVar2 = pbVar9[1];
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)pcVar14[1];
                        if (bVar2 != pcVar14[1]) goto LAB_100133b4;
                        pbVar9 = pbVar9 + 2;
                        pcVar14 = pcVar14 + 2;
                      } while (bVar2 != 0);
                      iVar10 = 0;
LAB_100133b9:
                      if (iVar10 == 0) {
                        local_248 = &stack0xfffffd94;
                        CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                        pCVar8 = FUN_1000f110(&local_240,iVar11);
                        local_4._0_1_ = 0x10;
                        iVar11 = atoi(*(char **)pCVar8);
                        dmSetAbility(iVar11);
                        local_4._0_1_ = 4;
                        CString::~CString((CString *)&local_240);
                        local_4._0_1_ = 3;
                        CDDSurface::~CDDSurface(local_200);
                        local_4._0_1_ = 2;
                        CString::~CString(local_24c);
                        local_4._0_1_ = 1;
                        CString::~CString((CString *)&local_258);
                        local_4 = (uint)local_4._1_3_ << 8;
                        CString::~CString((CString *)&local_254);
                        local_4 = 0xffffffff;
                        CString::~CString((CString *)&local_250);
                        ExceptionList = local_c;
                        return 0;
                      }
                      pcVar14 = s_CHARSETMAGIC_1001a5e8;
                      pbVar9 = local_254;
                      do {
                        bVar2 = *pbVar9;
                        uVar12 = (undefined3)((uint)iVar11 >> 8);
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)*pcVar14;
                        if (bVar2 != *pcVar14) {
LAB_10013492:
                          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                          goto LAB_10013497;
                        }
                        if (bVar2 == 0) break;
                        bVar2 = pbVar9[1];
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)pcVar14[1];
                        if (bVar2 != pcVar14[1]) goto LAB_10013492;
                        pbVar9 = pbVar9 + 2;
                        pcVar14 = pcVar14 + 2;
                      } while (bVar2 != 0);
                      iVar10 = 0;
LAB_10013497:
                      if (iVar10 == 0) goto LAB_1001349b;
                      pcVar14 = s_CHARMOVE_1001a5dc;
                      pbVar9 = local_254;
                      do {
                        bVar2 = *pbVar9;
                        uVar12 = (undefined3)((uint)iVar11 >> 8);
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)*pcVar14;
                        if (bVar2 != *pcVar14) {
LAB_10013525:
                          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                          goto LAB_1001352a;
                        }
                        if (bVar2 == 0) break;
                        bVar2 = pbVar9[1];
                        iVar11 = CONCAT31(uVar12,bVar2);
                        bVar19 = bVar2 < (byte)pcVar14[1];
                        if (bVar2 != pcVar14[1]) goto LAB_10013525;
                        pbVar9 = pbVar9 + 2;
                        pcVar14 = pcVar14 + 2;
                      } while (bVar2 != 0);
                      iVar10 = 0;
LAB_1001352a:
                      if (iVar10 == 0) {
                        piVar18 = &local_230;
                        iVar10 = 3;
                        local_248 = &stack0xfffffd8c;
                        CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                        FUN_1000f3f0(iVar11,piVar18,iVar10);
                        MoveChar(local_230,local_22c,local_228);
                      }
                      else {
                        pcVar14 = s_HPSET_1001a5d4;
                        pbVar9 = local_254;
                        do {
                          bVar2 = *pbVar9;
                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)*pcVar14;
                          if (bVar2 != *pcVar14) {
LAB_10013593:
                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                            goto LAB_10013598;
                          }
                          if (bVar2 == 0) break;
                          bVar2 = pbVar9[1];
                          iVar11 = CONCAT31(uVar12,bVar2);
                          bVar19 = bVar2 < (byte)pcVar14[1];
                          if (bVar2 != pcVar14[1]) goto LAB_10013593;
                          pbVar9 = pbVar9 + 2;
                          pcVar14 = pcVar14 + 2;
                        } while (bVar2 != 0);
                        iVar10 = 0;
LAB_10013598:
                        if (iVar10 == 0) {
                          piVar18 = &local_230;
                          iVar11 = 2;
                          local_248 = &stack0xfffffd8c;
                          piVar21 = piVar18;
                          CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                          FUN_1000f3f0((int)piVar18,piVar21,iVar11);
                          local_23c = (undefined1  [4])
                                      CONCAT22((undefined2)local_22c,(short)local_230);
                          SetHpDisplay();
                        }
                        else {
                          pcVar14 = s_MPSET_1001a5cc;
                          pbVar9 = local_254;
                          do {
                            bVar2 = *pbVar9;
                            uVar12 = (undefined3)((uint)iVar11 >> 8);
                            iVar11 = CONCAT31(uVar12,bVar2);
                            bVar19 = bVar2 < (byte)*pcVar14;
                            if (bVar2 != *pcVar14) {
LAB_1001360b:
                              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                              goto LAB_10013610;
                            }
                            if (bVar2 == 0) break;
                            bVar2 = pbVar9[1];
                            iVar11 = CONCAT31(uVar12,bVar2);
                            bVar19 = bVar2 < (byte)pcVar14[1];
                            if (bVar2 != pcVar14[1]) goto LAB_1001360b;
                            pbVar9 = pbVar9 + 2;
                            pcVar14 = pcVar14 + 2;
                          } while (bVar2 != 0);
                          iVar10 = 0;
LAB_10013610:
                          if (iVar10 == 0) {
                            piVar18 = &local_230;
                            iVar10 = 2;
                            local_248 = &stack0xfffffd8c;
                            CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258);
                            FUN_1000f3f0(iVar11,piVar18,iVar10);
                            local_23c = (undefined1  [4])
                                        CONCAT22((undefined2)local_22c,(short)local_230);
                            SetMpDisplay();
                          }
                          else {
                            pcVar14 = s_KILLCHAR1_1001a5c0;
                            pbVar9 = local_254;
                            do {
                              bVar2 = *pbVar9;
                              uVar12 = (undefined3)((uint)iVar11 >> 8);
                              iVar11 = CONCAT31(uVar12,bVar2);
                              bVar19 = bVar2 < (byte)*pcVar14;
                              if (bVar2 != *pcVar14) {
LAB_10013683:
                                iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                goto LAB_10013688;
                              }
                              if (bVar2 == 0) break;
                              bVar2 = pbVar9[1];
                              iVar11 = CONCAT31(uVar12,bVar2);
                              bVar19 = bVar2 < (byte)pcVar14[1];
                              if (bVar2 != pcVar14[1]) goto LAB_10013683;
                              pbVar9 = pbVar9 + 2;
                              pcVar14 = pcVar14 + 2;
                            } while (bVar2 != 0);
                            iVar10 = 0;
LAB_10013688:
                            if (iVar10 == 0) {
                              local_248 = &stack0xfffffd94;
                              CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                              pCVar8 = FUN_1000f110(&local_240,iVar11);
                              local_4._0_1_ = 0x11;
                              iVar11 = atoi(*(char **)pCVar8);
                              dmKillChar(iVar11);
                              local_4._0_1_ = 4;
                              CString::~CString((CString *)&local_240);
                            }
                            else {
                              pcVar14 = s_KILLCHAR2_1001a5b4;
                              pbVar9 = local_254;
                              do {
                                bVar2 = *pbVar9;
                                uVar12 = (undefined3)((uint)iVar11 >> 8);
                                iVar11 = CONCAT31(uVar12,bVar2);
                                bVar19 = bVar2 < (byte)*pcVar14;
                                if (bVar2 != *pcVar14) {
LAB_10013703:
                                  iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                  goto LAB_10013708;
                                }
                                if (bVar2 == 0) break;
                                bVar2 = pbVar9[1];
                                iVar11 = CONCAT31(uVar12,bVar2);
                                bVar19 = bVar2 < (byte)pcVar14[1];
                                if (bVar2 != pcVar14[1]) goto LAB_10013703;
                                pbVar9 = pbVar9 + 2;
                                pcVar14 = pcVar14 + 2;
                              } while (bVar2 != 0);
                              iVar10 = 0;
LAB_10013708:
                              if (iVar10 == 0) {
                                local_248 = &stack0xfffffd94;
                                CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                                pCVar8 = FUN_1000f110(&local_240,iVar11);
                                local_4._0_1_ = 0x12;
                                iVar11 = atoi(*(char **)pCVar8);
                                dmKillChar2(iVar11);
                                local_4._0_1_ = 4;
                                CString::~CString((CString *)&local_240);
                              }
                              else {
                                pcVar14 = s_ATTACK_1001a5ac;
                                pbVar9 = local_254;
                                do {
                                  bVar2 = *pbVar9;
                                  uVar12 = (undefined3)((uint)iVar11 >> 8);
                                  iVar11 = CONCAT31(uVar12,bVar2);
                                  bVar19 = bVar2 < (byte)*pcVar14;
                                  if (bVar2 != *pcVar14) {
LAB_10013783:
                                    iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                    goto LAB_10013788;
                                  }
                                  if (bVar2 == 0) break;
                                  bVar2 = pbVar9[1];
                                  iVar11 = CONCAT31(uVar12,bVar2);
                                  bVar19 = bVar2 < (byte)pcVar14[1];
                                  if (bVar2 != pcVar14[1]) goto LAB_10013783;
                                  pbVar9 = pbVar9 + 2;
                                  pcVar14 = pcVar14 + 2;
                                } while (bVar2 != 0);
                                iVar10 = 0;
LAB_10013788:
                                if (iVar10 == 0) {
                                  piVar18 = &local_230;
                                  iVar11 = 3;
                                  local_248 = &stack0xfffffd8c;
                                  piVar21 = piVar18;
                                  CString::CString((CString *)&stack0xfffffd8c,(CString *)&local_258
                                                  );
                                  FUN_1000f3f0((int)piVar18,piVar21,iVar11);
                                  local_21c = (undefined1 *)
                                              CONCAT22((undefined2)local_22c,(short)local_230);
                                  local_218 = (undefined2)local_228;
                                  AttackChar((_PGSATTACKFX *)&local_21c);
                                }
                                else {
                                  pcVar14 = s_MAGIC_1001a5a4;
                                  pbVar9 = local_254;
                                  do {
                                    bVar2 = *pbVar9;
                                    uVar12 = (undefined3)((uint)iVar11 >> 8);
                                    iVar11 = CONCAT31(uVar12,bVar2);
                                    bVar19 = bVar2 < (byte)*pcVar14;
                                    if (bVar2 != *pcVar14) {
LAB_10013805:
                                      iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                      goto LAB_1001380a;
                                    }
                                    if (bVar2 == 0) break;
                                    bVar2 = pbVar9[1];
                                    iVar11 = CONCAT31(uVar12,bVar2);
                                    bVar19 = bVar2 < (byte)pcVar14[1];
                                    if (bVar2 != pcVar14[1]) goto LAB_10013805;
                                    pbVar9 = pbVar9 + 2;
                                    pcVar14 = pcVar14 + 2;
                                  } while (bVar2 != 0);
                                  iVar10 = 0;
LAB_1001380a:
                                  if (iVar10 == 0) {
                                    if (*(int *)(param_1 + 4) == 1) {
                                      piVar18 = &local_214;
                                      iVar11 = 4;
                                      local_238 = &stack0xfffffd8c;
                                      piVar21 = piVar18;
                                      CString::CString((CString *)&stack0xfffffd8c,
                                                       (CString *)&local_258);
                                      FUN_1000f3f0((int)piVar18,piVar21,iVar11);
                                      iVar11 = CheckMagicTarget((_PGSMAGIC *)&local_230,local_214,
                                                                local_210,local_20c,local_208);
                                      if (iVar11 != 0) {
                                        FUN_1000e4c0((short *)&local_230,0);
                                        puVar15 = &local_230;
                                        puVar17 = (undefined4 *)&stack0xfffffd84;
                                        for (iVar11 = 5; iVar11 != 0; iVar11 = iVar11 + -1) {
                                          *puVar17 = *puVar15;
                                          puVar15 = puVar15 + 1;
                                          puVar17 = puVar17 + 1;
                                        }
                                        Magic();
                                        if (local_224 != '\0') {
                                          operator_delete((void *)local_220);
                                        }
                                      }
                                    }
                                    else {
                                      piVar18 = &local_234;
                                      piVar21 = (int *)local_23c;
                                      local_238 = &stack0xfffffd8c;
                                      piVar24 = piVar18;
                                      CString::CString((CString *)&stack0xfffffd8c,
                                                       (CString *)&local_258);
                                      FUN_1000f510((int)piVar18,piVar21,piVar24);
                                      local_230 = CONCAT22((short)*(int *)((int)local_23c + 4),
                                                           *(undefined2 *)local_23c);
                                      local_22c = CONCAT22((short)*(int *)((int)local_23c + 0xc),
                                                           (short)*(int *)((int)local_23c + 8));
                                      local_224 = (char)local_234 + -4;
                                      if (local_224 != '\0') {
                                        local_220 = (undefined1  [4])operator_new(local_234 * 2 - 8)
                                        ;
                                        iVar11 = 0;
                                        if (local_234 != 4 && -1 < local_234 + -4) {
                                          iVar10 = 0x10;
                                          do {
                                            iVar11 = iVar11 + 1;
                                            puVar1 = (undefined2 *)((int)local_23c + iVar10);
                                            iVar10 = iVar10 + 4;
                                            *(undefined2 *)((int)local_220 + iVar11 * 2 + -2) =
                                                 *puVar1;
                                          } while (iVar11 < local_234 + -4);
                                        }
                                      }
                                      if (local_23c != (undefined1  [4])0x0) {
                                        operator_delete((void *)local_23c);
                                      }
                                      puVar15 = &local_230;
                                      puVar17 = (undefined4 *)&stack0xfffffd84;
                                      for (iVar11 = 5; iVar11 != 0; iVar11 = iVar11 + -1) {
                                        *puVar17 = *puVar15;
                                        puVar15 = puVar15 + 1;
                                        puVar17 = puVar17 + 1;
                                      }
                                      Magic();
                                      if (local_224 != '\0') {
                                        operator_delete((void *)local_220);
                                      }
                                    }
                                  }
                                  else {
                                    pcVar14 = s_GETGENERAL_1001a598;
                                    pbVar9 = local_254;
                                    do {
                                      bVar2 = *pbVar9;
                                      uVar12 = (undefined3)((uint)iVar11 >> 8);
                                      iVar11 = CONCAT31(uVar12,bVar2);
                                      bVar19 = bVar2 < (byte)*pcVar14;
                                      if (bVar2 != *pcVar14) {
LAB_100139b4:
                                        iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                        goto LAB_100139b9;
                                      }
                                      if (bVar2 == 0) break;
                                      bVar2 = pbVar9[1];
                                      iVar11 = CONCAT31(uVar12,bVar2);
                                      bVar19 = bVar2 < (byte)pcVar14[1];
                                      if (bVar2 != pcVar14[1]) goto LAB_100139b4;
                                      pbVar9 = pbVar9 + 2;
                                      pcVar14 = pcVar14 + 2;
                                    } while (bVar2 != 0);
                                    iVar10 = 0;
LAB_100139b9:
                                    if (iVar10 == 0) {
                                      iVar11 = atoi((char *)local_258);
                                      FUN_1000ea30((short)iVar11);
                                    }
                                    else {
                                      pbVar13 = &DAT_1001a590;
                                      pbVar9 = local_254;
                                      do {
                                        bVar2 = *pbVar9;
                                        uVar12 = (undefined3)((uint)iVar11 >> 8);
                                        iVar11 = CONCAT31(uVar12,bVar2);
                                        bVar19 = bVar2 < *pbVar13;
                                        if (bVar2 != *pbVar13) {
LAB_10013a01:
                                          iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                          goto LAB_10013a06;
                                        }
                                        if (bVar2 == 0) break;
                                        bVar2 = pbVar9[1];
                                        iVar11 = CONCAT31(uVar12,bVar2);
                                        bVar19 = bVar2 < pbVar13[1];
                                        if (bVar2 != pbVar13[1]) goto LAB_10013a01;
                                        pbVar9 = pbVar9 + 2;
                                        pbVar13 = pbVar13 + 2;
                                      } while (bVar2 != 0);
                                      iVar10 = 0;
LAB_10013a06:
                                      if (iVar10 == 0) {
                                        iVar11 = atoi((char *)local_258);
                                        FUN_1000e420(iVar11);
                                      }
                                      else {
                                        pcVar14 = s_USEITEM_1001a588;
                                        pbVar9 = local_254;
                                        do {
                                          bVar2 = *pbVar9;
                                          uVar12 = (undefined3)((uint)iVar11 >> 8);
                                          iVar11 = CONCAT31(uVar12,bVar2);
                                          bVar19 = bVar2 < (byte)*pcVar14;
                                          if (bVar2 != *pcVar14) {
LAB_10013a4e:
                                            iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                            goto LAB_10013a53;
                                          }
                                          if (bVar2 == 0) break;
                                          bVar2 = pbVar9[1];
                                          iVar11 = CONCAT31(uVar12,bVar2);
                                          bVar19 = bVar2 < (byte)pcVar14[1];
                                          if (bVar2 != pcVar14[1]) goto LAB_10013a4e;
                                          pbVar9 = pbVar9 + 2;
                                          pcVar14 = pcVar14 + 2;
                                        } while (bVar2 != 0);
                                        iVar10 = 0;
LAB_10013a53:
                                        if (iVar10 == 0) {
                                          iVar11 = atoi((char *)local_258);
                                          FUN_1000e470((short)iVar11);
                                        }
                                        else {
                                          pcVar14 = s_COMBINE_CLASS_1001a578;
                                          do {
                                            bVar2 = *local_254;
                                            uVar12 = (undefined3)((uint)iVar11 >> 8);
                                            iVar11 = CONCAT31(uVar12,bVar2);
                                            bVar19 = bVar2 < (byte)*pcVar14;
                                            if (bVar2 != *pcVar14) {
LAB_10013a9f:
                                              iVar10 = (1 - (uint)bVar19) - (uint)(bVar19 != 0);
                                              goto LAB_10013aa4;
                                            }
                                            if (bVar2 == 0) break;
                                            bVar2 = local_254[1];
                                            iVar11 = CONCAT31(uVar12,bVar2);
                                            bVar19 = bVar2 < (byte)pcVar14[1];
                                            if (bVar2 != pcVar14[1]) goto LAB_10013a9f;
                                            local_254 = local_254 + 2;
                                            pcVar14 = pcVar14 + 2;
                                          } while (bVar2 != 0);
                                          iVar10 = 0;
LAB_10013aa4:
                                          if (iVar10 == 0) {
                                            piVar18 = &local_174;
                                            iVar10 = 5;
                                            local_238 = &stack0xfffffd8c;
                                            DAT_1001c4d4 = 1;
                                            CString::CString((CString *)&stack0xfffffd8c,
                                                             (CString *)&local_258);
                                            FUN_1000f3f0(iVar11,piVar18,iVar10);
                                            local_22c = local_16c;
                                            local_230 = local_170;
                                            local_248 = local_168;
                                            local_204 = local_164;
                                            local_244 = operator_new(local_174 * 4);
                                            local_23c = (undefined1  [4])operator_new(local_174 * 8)
                                            ;
                                            if (local_23c == (undefined1  [4])0x0) {
                                              local_23c = (undefined1  [4])0x0;
                                            }
                                            CString::CString((CString *)&local_234);
                                            local_4._0_1_ = 0x13;
                                            CString::CString((CString *)&local_238);
                                            local_4._0_1_ = 0x14;
                                            iVar11 = 0;
                                            auVar16 = local_23c;
                                            piVar18 = local_244;
                                            do {
                                              iVar10 = *(int *)(local_240 + 0x34);
                                              *(int *)(local_240 + 0x34) = iVar10 + 1;
                                              CString::operator=((CString *)&local_234,
                                                                 (CString *)
                                                                 (local_240 + iVar10 * 4 + 0x50));
                                              piVar21 = &local_214;
                                              iVar25 = 3;
                                              local_21c = &stack0xfffffd8c;
                                              iVar10 = extraout_ECX_05;
                                              CString::CString((CString *)&stack0xfffffd8c,
                                                               (CString *)&local_234);
                                              FUN_1000f3f0(iVar10,piVar21,iVar25);
                                              auVar3 = local_23c;
                                              piVar21 = local_244;
                                              *piVar18 = local_214;
                                              *(int *)auVar16 = local_210;
                                              *(int *)((int)auVar16 + 4) = local_20c;
                                              iVar11 = iVar11 + 1;
                                              piVar18 = piVar18 + 1;
                                              auVar16 = (undefined1  [4])((int)auVar16 + 8);
                                              local_174 = local_174 + -1;
                                            } while (local_174 != 0);
                                            FUN_10002070(iVar11,local_244,(undefined4 *)local_23c,
                                                         local_230,local_22c,local_248,local_204);
                                            operator_delete((void *)auVar3);
                                            operator_delete(piVar21);
                                            local_4._0_1_ = 0x13;
                                            CString::~CString((CString *)&local_238);
                                            local_4._0_1_ = 4;
                                            CString::~CString((CString *)&local_234);
                                          }
                                        }
                                      }
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                      goto LAB_10013cea;
                    }
                  }
                  local_21c = &stack0xfffffd94;
                  param_1[0x20] = '\x01';
                  param_1[0x21] = '\0';
                  param_1[0x22] = '\0';
                  param_1[0x23] = '\0';
                  param_1[0x24] = -2;
                  param_1[0x25] = -1;
                  param_1[0x26] = -1;
                  param_1[0x27] = -1;
                  CString::CString((CString *)&stack0xfffffd94,(CString *)&local_258);
                  pCVar8 = FUN_1000f110(&local_248,iVar11);
                  iVar11 = atoi(*(char **)pCVar8);
                  *(int *)(param_1 + 0x2c) = iVar11;
                  CString::~CString((CString *)&local_248);
                  cVar6 = clock();
                  *(clock_t *)(param_1 + 0x28) = cVar6;
                }
              }
            }
          }
        }
      }
    }
  }
LAB_10013cea:
  local_4._0_1_ = 3;
  CDDSurface::~CDDSurface(local_200);
  local_4._0_1_ = 2;
  CString::~CString(local_24c);
  local_4._0_1_ = 1;
  CString::~CString((CString *)&local_258);
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString((CString *)&local_254);
LAB_10013d39:
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_250);
  ExceptionList = local_c;
  return 1;
}


/* incoming refs
10003883 UNCONDITIONAL_CALL
10003896 UNCONDITIONAL_CALL
1000dd87 UNCONDITIONAL_CALL
10003bcc UNCONDITIONAL_CALL
10003bdb UNCONDITIONAL_CALL
CALLER 1000dcf0 FUN_1000dcf0
  1000ec89 UNCONDITIONAL_CALL
CALLER 10003840 Update
  10018aa0 DATA
  Entry Point EXTERNAL
*/
