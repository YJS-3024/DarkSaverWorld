/* Typed Ghidra reconstruction; not original source or an executable patch.
 * Entry 0x10003da0; source SHA256 f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0
 */

/* Read the scenario .srf resource and parse its briefing/condition text. Do not interpret the
   existence of a briefing as a complete initial monster roster.
   Source SHA256: f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0 */

void LoadScenarioBriefing(void)

{
  byte bVar1;
  CString *pCVar2;
  int iVar3;
  byte *pbVar4;
  byte *pbVar5;
  bool bVar6;
  char *pcVar7;
  char *local_30;
  byte *local_2c;
  CString local_28 [4];
  CStdioFile local_24 [20];
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_10014e60;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString((CString *)&local_30);
  local_4 = 0;
  CString::CString(local_28);
  local_4 = CONCAT31(local_4._1_3_,1);
  if (DAT_1006a738 < 0) {
    if (DAT_1006a738 == -99) {
      pcVar7 = &DAT_1001a204;
    }
    else {
      pcVar7 = &DAT_1001a200;
    }
    CString::operator=((CString *)&DAT_1001bdd8,pcVar7);
    CString::Empty((CString *)&DAT_1001bddc);
  }
  else {
    CString::operator=((CString *)&DAT_1001bdd8,&DAT_1001a220);
    CString::operator=((CString *)&DAT_1001bddc,&DAT_1001a210);
  }
  if (DAT_1001a8b0 == (BB_CompressDllMng *)0x0) goto LAB_1000401a;
  CString::operator=((CString *)&local_30,(CString *)&DAT_1001a8b4);
  CString::operator+=((CString *)&local_30,&DAT_1001a1f8);
  CString::operator+=((CString *)&local_30,(CString *)&DAT_1006c0e8);
  CString::operator+=((CString *)&local_30,s_Set_arc_1001a1f0);
  operator+((CString *)&local_2c,(char *)&DAT_1006c0e8);
  local_8._0_1_ = 2;
  pCVar2 = (CString *)
           BB_CompressDllMng::WriteTmpFile(DAT_1001a8b0,local_28,(CString *)&stack0xffffffcc);
  local_8._0_1_ = 3;
  CString::operator=((CString *)&local_30,pCVar2);
  local_8._0_1_ = 2;
  CString::~CString(local_28);
  if (*(int *)(local_30 + -8) == 0) {
LAB_10003ffc:
    BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&local_30);
  }
  else {
    CStdioFile::CStdioFile(local_24);
    local_8._0_1_ = 4;
    iVar3 = CStdioFile::Open(local_24,local_30,0x4000,(CFileException *)0x0);
    if (iVar3 != 0) {
      iVar3 = CStdioFile::ReadString(local_24,(CString *)&stack0xffffffcc);
      while (iVar3 != 0) {
        pCVar2 = FUN_1000f200(local_28,(CString *)&stack0xffffffcc);
        local_8._0_1_ = 5;
        CString::operator=((CString *)&local_2c,pCVar2);
        local_8._0_1_ = 4;
        CString::~CString(local_28);
        pbVar5 = &DAT_1001a1e4;
        pbVar4 = local_2c;
        do {
          bVar1 = *pbVar4;
          bVar6 = bVar1 < *pbVar5;
          if (bVar1 != *pbVar5) {
LAB_10003f71:
            iVar3 = (1 - (uint)bVar6) - (uint)(bVar6 != 0);
            goto LAB_10003f76;
          }
          if (bVar1 == 0) break;
          bVar1 = pbVar4[1];
          bVar6 = bVar1 < pbVar5[1];
          if (bVar1 != pbVar5[1]) goto LAB_10003f71;
          pbVar4 = pbVar4 + 2;
          pbVar5 = pbVar5 + 2;
        } while (bVar1 != 0);
        iVar3 = 0;
LAB_10003f76:
        if (iVar3 == 0) {
          CString::operator=((CString *)&DAT_1001bdd8,(CString *)&stack0xffffffcc);
        }
        pbVar5 = &DAT_1001a1e0;
        pbVar4 = local_2c;
        do {
          bVar1 = *pbVar4;
          bVar6 = bVar1 < *pbVar5;
          if (bVar1 != *pbVar5) {
LAB_10003fb6:
            iVar3 = (1 - (uint)bVar6) - (uint)(bVar6 != 0);
            goto LAB_10003fbb;
          }
          if (bVar1 == 0) break;
          bVar1 = pbVar4[1];
          bVar6 = bVar1 < pbVar5[1];
          if (bVar1 != pbVar5[1]) goto LAB_10003fb6;
          pbVar4 = pbVar4 + 2;
          pbVar5 = pbVar5 + 2;
        } while (bVar1 != 0);
        iVar3 = 0;
LAB_10003fbb:
        if (iVar3 == 0) {
          CString::operator=((CString *)&DAT_1001bddc,(CString *)&stack0xffffffcc);
        }
        iVar3 = CStdioFile::ReadString(local_24,(CString *)&stack0xffffffcc);
      }
      CStdioFile::Close(local_24);
      local_8._0_1_ = 2;
      CStdioFile::~CStdioFile(local_24);
      goto LAB_10003ffc;
    }
    local_8._0_1_ = 2;
    CStdioFile::~CStdioFile(local_24);
  }
  local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
  CString::~CString((CString *)&local_30);
LAB_1000401a:
  local_8 = (undefined1 *)((uint)local_8 & 0xffffff00);
  CString::~CString((CString *)&local_2c);
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString((CString *)&stack0xffffffcc);
  ExceptionList = local_10;
  return;
}

