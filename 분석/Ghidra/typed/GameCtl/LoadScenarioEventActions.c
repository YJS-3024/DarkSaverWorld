/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10013d60; source SHA256 f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0
 */

/* Read the scenario .evt and populate client event action storage. Numeric-prefixed condition lines
   are distinguished from executable action lines; this is not an initial unit roster loader.
   Source SHA256: f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0 */

void LoadScenarioEventActions(void)

{
  byte bVar1;
  undefined4 *puVar2;
  CString *pCVar3;
  char *pcVar4;
  int iVar5;
  byte *pbVar6;
  int iVar7;
  int extraout_ECX;
  undefined4 extraout_ECX_00;
  int extraout_ECX_01;
  undefined3 uVar9;
  int *piVar8;
  byte *unaff_EBP;
  int iVar10;
  char *unaff_ESI;
  byte *pbVar11;
  bool bVar12;
  undefined4 uVar13;
  CString local_44 [4];
  CString local_40 [4];
  CString local_3c [4];
  CString local_38 [4];
  undefined1 *local_34;
  CString local_30 [8];
  CString local_28 [12];
  void *local_1c;
  int local_14;
  undefined1 local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_10015b38;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_44);
  iVar10 = 0;
  local_4 = 0;
  CString::CString(local_3c);
  local_4._0_1_ = 1;
  CString::CString((CString *)&local_34);
  local_4._0_1_ = 2;
  CString::CString(local_38);
  local_4._0_1_ = 3;
  CString::CString(local_40);
  local_4 = CONCAT31(local_4._1_3_,4);
  puVar2 = &DAT_1006c120;
  do {
    *puVar2 = 0;
    puVar2 = puVar2 + 0x193;
  } while ((int)puVar2 < 0x100936d0);
  if (DAT_1001a8b0 != (BB_CompressDllMng *)0x0) {
    pCVar3 = (CString *)operator+(local_28,&DAT_1001a8b4);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,5);
    pcVar4 = (char *)operator+(local_30,pCVar3);
    local_c = (void *)CONCAT31(local_c._1_3_,6);
    pCVar3 = (CString *)operator+(local_38,pcVar4);
    local_10 = 7;
    CString::operator=(local_44,pCVar3);
    local_10 = 6;
    CString::~CString(local_3c);
    local_10 = 5;
    CString::~CString(local_38);
    local_10 = 4;
    CString::~CString((CString *)&local_34);
    pCVar3 = (CString *)operator+((CString *)&local_34,(char *)&DAT_1006c0e8);
    local_14._0_1_ = 8;
    CString::operator=((CString *)&stack0xffffffb0,pCVar3);
    local_14._0_1_ = 4;
    CString::~CString(local_38);
    pCVar3 = (CString *)
             BB_CompressDllMng::WriteTmpFile(DAT_1001a8b0,local_38,(CString *)&stack0xffffffb8);
    local_14._0_1_ = 9;
    CString::operator=((CString *)&stack0xffffffb0,pCVar3);
    local_14 = CONCAT31(local_14._1_3_,4);
    CString::~CString(local_38);
    if (*(int *)(unaff_ESI + -8) != 0) {
      CStdioFile::CStdioFile((CStdioFile *)local_30);
      local_14 = CONCAT31(local_14._1_3_,10);
      iVar5 = CStdioFile::Open((CStdioFile *)local_30,unaff_ESI,0x4000,(CFileException *)0x0);
      if (iVar5 != 0) {
        iVar5 = CStdioFile::ReadString((CStdioFile *)local_30,(CString *)&stack0xffffffac);
        while (iVar5 != 0) {
          CString::TrimLeft((CString *)&stack0xffffffac);
          CString::TrimRight((CString *)&stack0xffffffac);
          local_34 = &stack0xffffff98;
          iVar5 = extraout_ECX;
          CString::CString((CString *)&stack0xffffff98,(CString *)&stack0xffffffac);
          pCVar3 = FUN_1000f110(local_38,iVar5);
          local_14._0_1_ = 0xb;
          CString::operator=((CString *)&stack0xffffffb4,pCVar3);
          local_14._0_1_ = 10;
          CString::~CString(local_38);
          local_34 = &stack0xffffff98;
          uVar13 = extraout_ECX_00;
          CString::CString((CString *)&stack0xffffff98,(CString *)&stack0xffffffac);
          pCVar3 = FUN_1000f310(local_3c,uVar13);
          local_14._0_1_ = 0xc;
          CString::operator=(local_44,pCVar3);
          local_14 = CONCAT31(local_14._1_3_,10);
          CString::~CString(local_3c);
          pcVar4 = s_EVENT_1001a898;
          pbVar6 = unaff_EBP;
          iVar5 = extraout_ECX_01;
          do {
            bVar1 = *pbVar6;
            uVar9 = (undefined3)((uint)iVar5 >> 8);
            iVar5 = CONCAT31(uVar9,bVar1);
            bVar12 = bVar1 < (byte)*pcVar4;
            if (bVar1 != *pcVar4) {
LAB_10013fc4:
              iVar7 = (1 - (uint)bVar12) - (uint)(bVar12 != 0);
              goto LAB_10013fc9;
            }
            if (bVar1 == 0) break;
            bVar1 = pbVar6[1];
            iVar5 = CONCAT31(uVar9,bVar1);
            bVar12 = bVar1 < (byte)pcVar4[1];
            if (bVar1 != pcVar4[1]) goto LAB_10013fc4;
            pbVar6 = pbVar6 + 2;
            pcVar4 = pcVar4 + 2;
          } while (bVar1 != 0);
          iVar7 = 0;
LAB_10013fc9:
          if ((iVar7 == 0) && (iVar10 < 1)) {
            iVar10 = -99;
            local_34 = &stack0xffffff94;
            CString::CString((CString *)&stack0xffffff94,local_44);
            pCVar3 = FUN_1000f110(local_40,iVar5);
            pcVar4 = (char *)FUN_10010f10((undefined4 *)pCVar3);
            iVar10 = FUN_1000f040(pcVar4,iVar10);
            CString::~CString(local_40);
            iVar5 = 399;
            (&DAT_1006c120)[iVar10 * 0x193] = 1;
            *(int *)(&DAT_1006c124 + iVar10 * 0x64c) = iVar10;
            pCVar3 = (CString *)(&DAT_1006c130 + iVar10 * 0x193);
            do {
              CString::Empty(pCVar3);
              pCVar3 = pCVar3 + 4;
              iVar5 = iVar5 + -1;
            } while (iVar5 != 0);
          }
          else {
            pbVar11 = &DAT_1001a890;
            pbVar6 = unaff_EBP;
            do {
              bVar1 = *pbVar6;
              bVar12 = bVar1 < *pbVar11;
              if (bVar1 != *pbVar11) {
LAB_10014076:
                iVar5 = (1 - (uint)bVar12) - (uint)(bVar12 != 0);
                goto LAB_1001407b;
              }
              if (bVar1 == 0) break;
              bVar1 = pbVar6[1];
              bVar12 = bVar1 < pbVar11[1];
              if (bVar1 != pbVar11[1]) goto LAB_10014076;
              pbVar6 = pbVar6 + 2;
              pbVar11 = pbVar11 + 2;
            } while (bVar1 != 0);
            iVar5 = 0;
LAB_1001407b:
            if (iVar5 == 0) {
              iVar5 = 399;
              pCVar3 = (CString *)(&DAT_1006c130 + iVar10 * 0x193);
              do {
                if (*(int *)(*(int *)pCVar3 + -8) == 0) {
                  CString::operator=(pCVar3,(CString *)&stack0xffffffb4);
                }
                pCVar3 = pCVar3 + 4;
                iVar5 = iVar5 + -1;
              } while (iVar5 != 0);
              iVar10 = 0;
            }
            else {
              if (*(int *)(unaff_EBP + -8) != 0) {
                pbVar11 = &DAT_1001a894;
                pbVar6 = unaff_EBP;
                do {
                  bVar1 = *pbVar6;
                  bVar12 = bVar1 < *pbVar11;
                  if (bVar1 != *pbVar11) {
LAB_100140ee:
                    iVar5 = (1 - (uint)bVar12) - (uint)(bVar12 != 0);
                    goto LAB_100140f3;
                  }
                  if (bVar1 == 0) break;
                  bVar1 = pbVar6[1];
                  bVar12 = bVar1 < pbVar11[1];
                  if (bVar1 != pbVar11[1]) goto LAB_100140ee;
                  pbVar6 = pbVar6 + 2;
                  pbVar11 = pbVar11 + 2;
                } while (bVar1 != 0);
                iVar5 = 0;
LAB_100140f3:
                if (iVar5 != 0) {
                  if ((0 < iVar10) && (iVar5 = FUN_1000f040((char *)unaff_EBP,-99), iVar5 < 1)) {
                    iVar5 = 1;
                    piVar8 = &DAT_1006c130 + iVar10 * 0x193;
                    do {
                      if (*(int *)(*piVar8 + -8) == 0) {
                        CString::operator=((CString *)(iVar5 * 4 + iVar10 * 0x64c + 0x1006c12c),
                                           (CString *)&stack0xffffffac);
                        break;
                      }
                      iVar5 = iVar5 + 1;
                      piVar8 = piVar8 + 1;
                    } while (iVar5 < 100);
                  }
                  goto LAB_10014180;
                }
              }
              if (0 < iVar10) {
                (&DAT_1006c120)[iVar10 * 0x193] = 0;
                iVar10 = 0;
              }
            }
          }
LAB_10014180:
          iVar5 = CStdioFile::ReadString((CStdioFile *)local_30,(CString *)&stack0xffffffac);
        }
        CStdioFile::Close((CStdioFile *)local_30);
        BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
      }
      local_14 = CONCAT31(local_14._1_3_,4);
      CStdioFile::~CStdioFile((CStdioFile *)local_30);
    }
  }
  local_14._0_1_ = 3;
  CString::~CString((CString *)&stack0xffffffb0);
  local_14._0_1_ = 2;
  CString::~CString((CString *)&stack0xffffffb8);
  local_14._0_1_ = 1;
  CString::~CString(local_44);
  local_14 = (uint)local_14._1_3_ << 8;
  CString::~CString((CString *)&stack0xffffffb4);
  local_14 = 0xffffffff;
  CString::~CString((CString *)&stack0xffffffac);
  ExceptionList = local_1c;
  return;
}

