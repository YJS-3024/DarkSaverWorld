/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10001ed0; source SHA256 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca
 */

/* Read common magic-related definitions using original table filenames and error handling; no
   synthetic gameplay rules are introduced.
   Source SHA256: 71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca */

undefined4 LoadMagicDefinitionTables(void)

{
  byte bVar1;
  byte *_Str;
  CString *pCVar2;
  undefined4 *puVar3;
  int iVar4;
  uint uVar5;
  uint uVar6;
  int iVar7;
  undefined *puVar8;
  byte *pbVar9;
  int *piVar10;
  byte *pbVar11;
  bool bVar12;
  char *local_44;
  char *local_40;
  CString local_3c [4];
  CString local_38 [4];
  char *local_34;
  int local_30;
  int local_2c;
  CString local_28 [4];
  CString local_24 [4];
  CStdioFile local_20 [20];
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_10003418;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString((CString *)&local_40);
  iVar7 = 0;
  local_4 = 0;
  CString::CString(local_3c);
  local_4._0_1_ = 1;
  CString::CString((CString *)&local_34);
  local_4._0_1_ = 2;
  _Str = operator_new(0x200);
  pbVar9 = _Str;
  for (iVar4 = 0x80; iVar4 != 0; iVar4 = iVar4 + -1) {
    pbVar9[0] = 0;
    pbVar9[1] = 0;
    pbVar9[2] = 0;
    pbVar9[3] = 0;
    pbVar9 = pbVar9 + 4;
  }
  CString::CString(local_38,s_GameRes_set_arc_10005088);
  local_4._0_1_ = 3;
  CString::CString((CString *)&local_44,s_magictbl_atr_10005284);
  local_4._0_1_ = 4;
  pCVar2 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_10269874,(CString *)&local_2c,local_38);
  local_4._0_1_ = 5;
  CString::operator=((CString *)&local_44,pCVar2);
  local_4._0_1_ = 4;
  CString::~CString((CString *)&local_2c);
  CStdioFile::CStdioFile(local_20);
  local_4 = CONCAT31(local_4._1_3_,6);
  iVar4 = CStdioFile::Open(local_20,local_44,0x4000,(CFileException *)0x0);
  if (iVar4 != 0) {
    CStdioFile::ReadString(local_20,(CString *)&local_40);
    puVar8 = &DAT_1002dd18;
    do {
      iVar4 = CStdioFile::ReadString(local_20,(CString *)&local_40);
      if (iVar4 == 0) break;
      if (*(int *)(local_40 + -8) != 0) {
        sscanf(local_40,s__ld__s__d__d__d__d__d__d__d__d___10005250,puVar8 + -0x138,puVar8 + -0x134,
               puVar8 + -0x34,puVar8 + -0x30,puVar8 + -0x2c,puVar8 + -0x28,puVar8 + -0x24,
               puVar8 + -0x20,puVar8 + -0x1c,puVar8 + -0x18,puVar8 + -0x14,puVar8 + -0x10,
               puVar8 + -0xc,puVar8 + -8,puVar8 + -4,puVar8,puVar8 + 4);
      }
      puVar8 = puVar8 + 0x140;
    } while ((int)puVar8 < 0x1007bdd9);
    CStdioFile::Close(local_20);
    BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&local_44);
    CString::operator=((CString *)&local_44,s_magicptn_atr_10005240);
    pCVar2 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_10269874,(CString *)&local_2c,local_38);
    local_4._0_1_ = 7;
    CString::operator=((CString *)&local_44,pCVar2);
    local_4 = CONCAT31(local_4._1_3_,6);
    CString::~CString((CString *)&local_2c);
    iVar4 = CStdioFile::Open(local_20,local_44,0x4000,(CFileException *)0x0);
    if (iVar4 != 0) {
      CStdioFile::ReadString(local_20,(CString *)&local_40);
      do {
        iVar4 = CStdioFile::ReadString(local_20,(CString *)&local_40);
        if (iVar4 == 0) break;
        if (*(int *)(local_40 + -8) != 0) {
          CString::TrimLeft((CString *)&local_40);
          CString::TrimRight((CString *)&local_40);
          sscanf(local_40,&DAT_1000523c,&local_30);
          pbVar9 = _Str;
          for (iVar4 = 0x80; iVar4 != 0; iVar4 = iVar4 + -1) {
            pbVar9[0] = 0;
            pbVar9[1] = 0;
            pbVar9[2] = 0;
            pbVar9[3] = 0;
            pbVar9 = pbVar9 + 4;
          }
          iVar4 = local_30 * 0x148;
          sscanf(local_40,s__ld__s__d__d__d__d__d__d__d__d___10005200,&DAT_10021bb0 + iVar4,_Str,
                 &DAT_10021cb4 + iVar4,&DAT_10021cb8 + iVar4,&DAT_10021cbc + iVar4,
                 &DAT_10021cc0 + iVar4,&DAT_10021cc4 + iVar4,&DAT_10021cc8 + iVar4,
                 &DAT_10021ce4 + iVar4,&DAT_10021ccc + iVar4,&DAT_10021cd0 + iVar4,
                 &DAT_10021cd4 + iVar4,&DAT_10021cd8 + iVar4,&DAT_10021cdc + iVar4,
                 &DAT_10021ce0 + iVar4,&DAT_10021ce8 + iVar4,&DAT_10021cec + iVar4,
                 &DAT_10021cf0 + iVar4,&DAT_10021cf4 + iVar4);
          uVar5 = 0xffffffff;
          pbVar9 = _Str;
          do {
            pbVar11 = pbVar9;
            if (uVar5 == 0) break;
            uVar5 = uVar5 - 1;
            pbVar11 = pbVar9 + 1;
            bVar1 = *pbVar9;
            pbVar9 = pbVar11;
          } while (bVar1 != 0);
          uVar5 = ~uVar5;
          pbVar9 = pbVar11 + -uVar5;
          pbVar11 = &DAT_10021bb4 + local_30 * 0x148;
          for (uVar6 = uVar5 >> 2; uVar6 != 0; uVar6 = uVar6 - 1) {
            *(undefined4 *)pbVar11 = *(undefined4 *)pbVar9;
            pbVar9 = pbVar9 + 4;
            pbVar11 = pbVar11 + 4;
          }
          for (uVar5 = uVar5 & 3; uVar5 != 0; uVar5 = uVar5 - 1) {
            *pbVar11 = *pbVar9;
            pbVar9 = pbVar9 + 1;
            pbVar11 = pbVar11 + 1;
          }
          FUN_10001160(&DAT_10021bb4 + local_30 * 0x148);
        }
        iVar7 = iVar7 + 1;
      } while (iVar7 < 1000);
      CStdioFile::Close(local_20);
      BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&local_44);
      CString::operator=((CString *)&local_44,s_solskill_atr_100051f0);
      pCVar2 = (CString *)
               BB_CompressDllMng::WriteTmpFile(DAT_10269874,(CString *)&local_2c,local_38);
      local_4._0_1_ = 8;
      CString::operator=((CString *)&local_44,pCVar2);
      local_4 = CONCAT31(local_4._1_3_,6);
      CString::~CString((CString *)&local_2c);
      iVar4 = CStdioFile::Open(local_20,local_44,0x4000,(CFileException *)0x0);
      if (iVar4 != 0) {
        CStdioFile::ReadString(local_20,(CString *)&local_40);
        puVar3 = &DAT_10005500;
        do {
          *puVar3 = 0xffffffff;
          puVar3 = puVar3 + 0x61;
        } while ((int)puVar3 < 0x10021bb0);
        puVar8 = &DAT_1000567c;
        do {
          iVar4 = CStdioFile::ReadString(local_20,(CString *)&local_40);
          if (iVar4 == 0) break;
          if (*(int *)(local_40 + -8) != 0) {
            pbVar9 = _Str;
            for (iVar4 = 0x80; iVar4 != 0; iVar4 = iVar4 + -1) {
              pbVar9[0] = 0;
              pbVar9[1] = 0;
              pbVar9[2] = 0;
              pbVar9[3] = 0;
              pbVar9 = pbVar9 + 4;
            }
            sscanf(local_40,s__d__s__d__s__d_100051e0,puVar8 + -0x17c,puVar8 + -0x178,puVar8,_Str,
                   puVar8 + 4);
            uVar5 = 0xffffffff;
            iVar4 = 0;
            pbVar9 = _Str;
            do {
              if (uVar5 == 0) break;
              uVar5 = uVar5 - 1;
              bVar1 = *pbVar9;
              pbVar9 = pbVar9 + 1;
            } while (bVar1 != 0);
            iVar7 = 1;
            local_2c = 1;
            if (0 < (int)(~uVar5 - 1)) {
              do {
                if (_Str[iVar4] == 0x2c) {
                  _Str[iVar4] = 0x20;
                  iVar7 = iVar7 + 1;
                }
                iVar4 = iVar4 + 1;
              } while (iVar4 < (int)(~uVar5 - 1));
              local_2c = iVar7;
              if (iVar7 != 1) {
                *(int *)(puVar8 + -0x78) = iVar7;
                CString::operator=(local_3c,(char *)_Str);
                if (0 < iVar7) {
                  piVar10 = (int *)(puVar8 + -0x74);
                  iVar4 = local_2c;
                  do {
                    pCVar2 = (CString *)GetStrHead((char *)local_28);
                    local_4._0_1_ = 9;
                    CString::operator=((CString *)&local_34,pCVar2);
                    local_4._0_1_ = 6;
                    CString::~CString(local_28);
                    pCVar2 = (CString *)GetStrTails((char *)local_24);
                    local_4._0_1_ = 10;
                    CString::operator=(local_3c,pCVar2);
                    local_4 = CONCAT31(local_4._1_3_,6);
                    CString::~CString(local_24);
                    iVar7 = atoi(local_34);
                    *piVar10 = iVar7;
                    piVar10 = piVar10 + 1;
                    iVar4 = iVar4 + -1;
                  } while (iVar4 != 0);
                }
                goto LAB_10002494;
              }
            }
            pbVar11 = &DAT_100051dc;
            pbVar9 = _Str;
            do {
              bVar1 = *pbVar9;
              bVar12 = bVar1 < *pbVar11;
              if (bVar1 != *pbVar11) {
LAB_100023dd:
                iVar4 = (1 - (uint)bVar12) - (uint)(bVar12 != 0);
                goto LAB_100023e2;
              }
              if (bVar1 == 0) break;
              bVar1 = pbVar9[1];
              bVar12 = bVar1 < pbVar11[1];
              if (bVar1 != pbVar11[1]) goto LAB_100023dd;
              pbVar9 = pbVar9 + 2;
              pbVar11 = pbVar11 + 2;
            } while (bVar1 != 0);
            iVar4 = 0;
LAB_100023e2:
            if (iVar4 == 0) {
              *(undefined4 *)(puVar8 + -0x78) = 0;
            }
            else {
              *(undefined4 *)(puVar8 + -0x78) = 1;
              iVar4 = atoi((char *)_Str);
              *(int *)(puVar8 + -0x74) = iVar4;
            }
          }
LAB_10002494:
          puVar8 = puVar8 + 0x184;
        } while ((int)puVar8 < 0x10021d2c);
        CStdioFile::Close(local_20);
        DAT_10269850 = 1;
        BB_CompressDllMng::DeleteTmpFile(DAT_10269874,(CString *)&local_44);
        operator_delete(_Str);
        local_4._0_1_ = 4;
        CStdioFile::~CStdioFile(local_20);
        local_4._0_1_ = 3;
        CString::~CString((CString *)&local_44);
        local_4._0_1_ = 2;
        CString::~CString(local_38);
        local_4._0_1_ = 1;
        CString::~CString((CString *)&local_34);
        local_4 = (uint)local_4._1_3_ << 8;
        CString::~CString(local_3c);
        local_4 = 0xffffffff;
        CString::~CString((CString *)&local_40);
        ExceptionList = local_c;
        return 1;
      }
    }
  }
  operator_delete(_Str);
  local_4._0_1_ = 4;
  CStdioFile::~CStdioFile(local_20);
  local_4._0_1_ = 3;
  CString::~CString((CString *)&local_44);
  local_4._0_1_ = 2;
  CString::~CString(local_38);
  local_4._0_1_ = 1;
  CString::~CString((CString *)&local_34);
  local_4 = (uint)local_4._1_3_ << 8;
  CString::~CString(local_3c);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_40);
  ExceptionList = local_c;
  return 0;
}

