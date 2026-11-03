
void __cdecl FUN_100070c0(int param_1)

{
  CString *pCVar1;
  int iVar2;
  char *unaff_ESI;
  int iVar3;
  undefined4 *puVar4;
  int unaff_retaddr;
  CString local_30 [4];
  CString local_2c [4];
  CString local_28 [4];
  CString local_24 [20];
  void *local_10;
  void *pvStack_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_100150b0;
  pvStack_c = ExceptionList;
  ExceptionList = &pvStack_c;
  CString::CString(local_2c);
  iVar3 = 0;
  local_4 = 0;
  CString::CString(local_30);
  local_4 = CONCAT31(local_4._1_3_,1);
  if (DAT_1001c4f8 == 0) {
    if (DAT_1001a8b0 == (BB_CompressDllMng *)0x0) goto LAB_100072b3;
    pCVar1 = (CString *)operator+(local_24,&DAT_1001a8b4);
    local_8._0_1_ = 2;
    CString::operator=(local_30,pCVar1);
    local_8._0_1_ = 1;
    CString::~CString(local_28);
    CString::operator=((CString *)&stack0xffffffcc,s_help_txt_1001a2b0);
    pCVar1 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_1001a8b0,local_28,local_30);
    local_8._0_1_ = 3;
    CString::operator=((CString *)&stack0xffffffcc,pCVar1);
    local_8._0_1_ = 1;
    CString::~CString(local_28);
    CStdioFile::CStdioFile((CStdioFile *)local_24);
    local_8._0_1_ = 4;
    iVar2 = CStdioFile::Open((CStdioFile *)local_24,unaff_ESI,0x4000,(CFileException *)0x0);
    if (iVar2 == 0) {
      local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
      CStdioFile::~CStdioFile((CStdioFile *)local_24);
      goto LAB_100072b3;
    }
    CString::CString(local_2c);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,5);
    pCVar1 = (CString *)&DAT_1001a8f8;
    do {
      iVar2 = CStdioFile::ReadString((CStdioFile *)local_24,local_2c);
      if (iVar2 == 0) break;
      CString::operator=(pCVar1,local_2c);
      pCVar1 = pCVar1 + 4;
      iVar3 = iVar3 + 1;
    } while ((int)pCVar1 < 0x1001aa88);
    DAT_1001c4d0 = iVar3;
    if (iVar3 < 100) {
      CString::operator=((CString *)(&DAT_1001a8f8 + iVar3 * 4),&DAT_1001a2a8);
    }
    CStdioFile::Close((CStdioFile *)local_24);
    BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffcc);
    DAT_1001c4f8 = 1;
    local_8._0_1_ = 4;
    CString::~CString(local_2c);
    local_8 = (undefined1 *)CONCAT31(local_8._1_3_,1);
    CStdioFile::~CStdioFile((CStdioFile *)local_24);
  }
  if (DAT_1001c4b4 <= DAT_1001c4b4 + 0x12) {
    puVar4 = (undefined4 *)(&DAT_1001a8f8 + DAT_1001c4b4 * 4);
    iVar3 = DAT_1001c4b4;
    do {
      (*DAT_1001c450)(DAT_1001c470,unaff_retaddr + 0x32,
                      param_1 + 0x29 + (iVar3 - DAT_1001c4b4) * 0x14,*puVar4,0x101010,1);
      iVar3 = iVar3 + 1;
      puVar4 = puVar4 + 1;
    } while (iVar3 <= DAT_1001c4b4 + 0x12);
  }
LAB_100072b3:
  local_8 = (undefined1 *)((uint)local_8 & 0xffffff00);
  CString::~CString((CString *)&stack0xffffffcc);
  local_8 = (undefined1 *)0xffffffff;
  CString::~CString(local_30);
  ExceptionList = local_10;
  return;
}

