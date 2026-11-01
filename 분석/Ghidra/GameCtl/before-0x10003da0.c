/* Ghidra decompilation; not recovered original source. Evidence: ev_01c092259c6ff0a74c26ac4c35dd3d36c691f94b8ffcbd49a89fdc99c61ca5f4 */

void FUN_10003da0(void)

{
  byte bVar1;
  CString *pCVar2;
  int iVar3;
  byte *pbVar4;
  char *unaff_EBX;
  byte *pbVar5;
  bool bVar6;
  char *pcVar7;
  byte *local_30;
  CString local_2c [4];
  CString local_28 [20];
  void *local_14;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_10014e60;
  local_c = ExceptionList;
  ExceptionList = &local_c;
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
  operator+(local_2c,(char *)&DAT_1006c0e8);
  local_8 = (undefined1 *)CONCAT31(local_8._1_3_,2);
  pCVar2 = (CString *)
           BB_CompressDllMng::WriteTmpFile(DAT_1001a8b0,local_28,(CString *)&stack0xffffffcc);
  local_c._0_1_ = 3;
  CString::operator=((CString *)&stack0xffffffcc,pCVar2);
  local_c._0_1_ = 2;
  CString::~CString(local_2c);
  if (*(int *)(unaff_EBX + -8) == 0) {
LAB_10003ffc:
    BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffcc);
  }
  else {
    CStdioFile::CStdioFile((CStdioFile *)local_28);
    local_c._0_1_ = 4;
    iVar3 = CStdioFile::Open((CStdioFile *)local_28,unaff_EBX,0x4000,(CFileException *)0x0);
    if (iVar3 != 0) {
      iVar3 = CStdioFile::ReadString((CStdioFile *)local_28,(CString *)&stack0xffffffc8);
      while (iVar3 != 0) {
        pCVar2 = FUN_1000f200(local_2c,(CString *)&stack0xffffffc8);
        local_c._0_1_ = 5;
        CString::operator=((CString *)&local_30,pCVar2);
        local_c._0_1_ = 4;
        CString::~CString(local_2c);
        pbVar5 = &DAT_1001a1e4;
        pbVar4 = local_30;
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
          CString::operator=((CString *)&DAT_1001bdd8,(CString *)&stack0xffffffc8);
        }
        pbVar5 = &DAT_1001a1e0;
        pbVar4 = local_30;
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
          CString::operator=((CString *)&DAT_1001bddc,(CString *)&stack0xffffffc8);
        }
        iVar3 = CStdioFile::ReadString((CStdioFile *)local_28,(CString *)&stack0xffffffc8);
      }
      CStdioFile::Close((CStdioFile *)local_28);
      local_c._0_1_ = 2;
      CStdioFile::~CStdioFile((CStdioFile *)local_28);
      goto LAB_10003ffc;
    }
    local_c._0_1_ = 2;
    CStdioFile::~CStdioFile((CStdioFile *)local_28);
  }
  local_c = (void *)CONCAT31(local_c._1_3_,1);
  CString::~CString((CString *)&stack0xffffffcc);
LAB_1000401a:
  local_c = (void *)((uint)local_c & 0xffffff00);
  CString::~CString((CString *)&local_30);
  local_c = (void *)0xffffffff;
  CString::~CString((CString *)&stack0xffffffc8);
  ExceptionList = local_14;
  return;
}

