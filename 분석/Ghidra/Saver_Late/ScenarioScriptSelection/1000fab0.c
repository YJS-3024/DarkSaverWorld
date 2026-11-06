
void __fastcall FUN_1000fab0(int param_1)

{
  CString *pCVar1;
  char *pcVar2;
  int iVar3;
  undefined1 *puVar4;
  int iVar5;
  char *unaff_EBX;
  undefined1 *unaff_ESI;
  char *unaff_retaddr;
  undefined *puVar6;
  CString local_4c [4];
  CString local_48 [4];
  CString local_44 [4];
  CString local_40 [4];
  CString local_3c [4];
  CString local_38 [20];
  CStdioFile local_24 [16];
  undefined1 local_14;
  void *local_10;
  void *local_c;
  char *local_8;
  undefined4 local_4;
  
  local_8._0_1_ = 0x70;
  local_8._1_3_ = 0x100157;
  local_c = ExceptionList;
  local_4 = 0;
  ExceptionList = &local_c;
  CString::CString(local_48);
  local_4._0_1_ = 1;
  CString::CString(local_4c);
  local_4 = CONCAT31(local_4._1_3_,2);
  if (DAT_1001a8b0 == (BB_CompressDllMng *)0x0) goto LAB_1000ffc4;
  if (DAT_1006c118 == 0) {
    pCVar1 = (CString *)operator+(local_38,&DAT_1001a8b4);
    local_8 = (char *)CONCAT31(local_8._1_3_,3);
    pcVar2 = (char *)operator+(local_40,pCVar1);
    local_c = (void *)CONCAT31(local_c._1_3_,4);
    pCVar1 = (CString *)operator+(local_48,pcVar2);
    local_10._0_1_ = 5;
    CString::operator=((CString *)&stack0xffffffac,pCVar1);
    local_10._0_1_ = 4;
    CString::~CString(local_4c);
    local_10._0_1_ = 3;
    CString::~CString(local_48);
    local_10 = (void *)CONCAT31(local_10._1_3_,2);
    CString::~CString(local_44);
    CString::operator=((CString *)&stack0xffffffa8,(CString *)&DAT_1006c0e8);
    iVar3 = _stricmp(local_8,&DAT_1001a274);
    if (iVar3 == 0) {
      pCVar1 = (CString *)operator+(local_44,(CString *)&stack0xffffffa8);
      local_14 = 6;
      CString::operator=((CString *)&stack0xffffffa4,pCVar1);
      local_14 = 2;
      CString::~CString(local_48);
    }
    puVar6 = &DAT_1001a538;
    pCVar1 = (CString *)operator+(local_38,(char *)local_48);
    pcVar2 = &stack0x00000004;
    local_4 = CONCAT31(local_4._1_3_,7);
    pCVar1 = (CString *)operator+(local_38,pCVar1);
    local_8._0_1_ = 8;
    CString::operator=((CString *)&stack0xffffffb0,pCVar1);
    local_8._0_1_ = 7;
    CString::~CString(local_3c);
    local_8._0_1_ = 2;
    CString::~CString(local_40);
    pCVar1 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_1001a8b0,local_3c,local_4c);
    local_8._0_1_ = 9;
    CString::operator=((CString *)&stack0xffffffb0,pCVar1);
    local_8._0_1_ = 2;
    CString::~CString(local_3c);
    if (*(int *)(unaff_EBX + -8) == 0) {
      iVar3 = _stricmp(unaff_retaddr,&DAT_1001a490);
      if (iVar3 == 0) {
        thunk_FUN_10004150();
        goto LAB_1000fea1;
      }
      iVar3 = _stricmp(unaff_retaddr,&DAT_1001a274);
      if (iVar3 == 0) {
        DAT_1001a8ec = 0;
        *(undefined4 *)(DAT_1006a708 + 0x18) = 0;
        PostMessageA(DAT_1001c468,0x590,0,0);
        PostMessageA(DAT_1001c468,0x4cd,0,0);
        BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
        goto LAB_1000ffc4;
      }
    }
    iVar3 = _stricmp(unaff_retaddr,&DAT_1001a490);
    if (iVar3 == 0) {
      puVar4 = FUN_1000f7b0(pcVar2,puVar6,unaff_ESI);
      if (puVar4 != (undefined1 *)0x0) {
        thunk_FUN_10004150();
        BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
        goto LAB_1000ffc4;
      }
    }
    CString::CString(local_48);
    local_8._0_1_ = 10;
    CStdioFile::CStdioFile((CStdioFile *)local_38);
    local_8._0_1_ = 0xb;
    iVar3 = CStdioFile::Open((CStdioFile *)local_38,unaff_EBX,0x4000,(CFileException *)0x0);
    if (iVar3 == 0) {
      BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
      local_8._0_1_ = 10;
      CStdioFile::~CStdioFile((CStdioFile *)local_38);
      local_8._0_1_ = 2;
      CString::~CString(local_48);
      goto LAB_1000ffc4;
    }
    iVar3 = 1;
    pCVar1 = (CString *)(param_1 + 0x54);
    do {
      iVar5 = CStdioFile::ReadString((CStdioFile *)local_38,local_48);
      if (iVar5 == 0) break;
      CString::operator=(pCVar1,local_48);
      iVar3 = iVar3 + 1;
      pCVar1 = pCVar1 + 4;
    } while (iVar3 < 1000);
    CStdioFile::Close((CStdioFile *)local_38);
    BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
    local_8._0_1_ = 10;
    CStdioFile::~CStdioFile((CStdioFile *)local_38);
    local_8._0_1_ = 2;
    CString::~CString(local_48);
  }
  else {
    pCVar1 = (CString *)operator+(local_38,&DAT_1001a8b4);
    local_8._0_1_ = 0xc;
    CString::operator=(local_4c,pCVar1);
    local_8._0_1_ = 2;
    CString::~CString(local_3c);
    CString::operator=((CString *)&stack0xffffffb0,s_tutorial_deo_1001a514);
    pCVar1 = (CString *)BB_CompressDllMng::WriteTmpFile(DAT_1001a8b0,local_3c,local_4c);
    local_8._0_1_ = 0xd;
    CString::operator=((CString *)&stack0xffffffb0,pCVar1);
    local_8._0_1_ = 2;
    CString::~CString(local_3c);
    if (*(int *)(unaff_EBX + -8) == 0) {
      DAT_1001a8ec = 0;
      *(undefined4 *)(DAT_1006a708 + 0x18) = 0;
      PostMessageA(DAT_1001c468,0x590,0,0);
      PostMessageA(DAT_1001c468,0x4cd,0,0);
LAB_1000fea1:
      BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
      goto LAB_1000ffc4;
    }
    CStdioFile::CStdioFile(local_24);
    local_8._0_1_ = 0xe;
    iVar3 = CStdioFile::Open(local_24,unaff_EBX,0x4000,(CFileException *)0x0);
    if (iVar3 == 0) {
      BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
      local_8._0_1_ = 2;
      CStdioFile::~CStdioFile(local_24);
      goto LAB_1000ffc4;
    }
    CString::CString(local_44);
    local_8 = (char *)CONCAT31(local_8._1_3_,0xf);
    iVar3 = 1;
    pCVar1 = (CString *)(param_1 + 0x54);
    do {
      iVar5 = CStdioFile::ReadString(local_24,local_44);
      if (iVar5 == 0) break;
      CString::operator=(pCVar1,local_44);
      iVar3 = iVar3 + 1;
      pCVar1 = pCVar1 + 4;
    } while (iVar3 < 1000);
    CStdioFile::Close(local_24);
    BB_CompressDllMng::DeleteTmpFile(DAT_1001a8b0,(CString *)&stack0xffffffb0);
    local_8._0_1_ = 0xe;
    CString::~CString(local_44);
    local_8._0_1_ = 2;
    CStdioFile::~CStdioFile(local_24);
  }
  *(undefined4 *)(param_1 + 0xc) = 1;
  *(undefined4 *)(param_1 + 0x1c) = 0;
  *(undefined4 *)(param_1 + 0x34) = 0;
  *(undefined4 *)(param_1 + 4) = 2;
  *(undefined4 *)(param_1 + 0x30) = 0;
  iVar3 = _stricmp(unaff_retaddr,&DAT_1001a274);
  if (iVar3 == 0) {
    *(undefined4 *)(param_1 + 8) = 1;
  }
  else {
    *(undefined4 *)(param_1 + 8) = 0;
  }
  *(undefined4 *)(DAT_1006a708 + 0x18) = 0;
  userclearstatusall();
  setgamestatus(0);
LAB_1000ffc4:
  local_8._0_1_ = 1;
  CString::~CString((CString *)&stack0xffffffb0);
  local_8 = (char *)((uint)local_8._1_3_ << 8);
  CString::~CString(local_4c);
  local_8 = (char *)0xffffffff;
  CString::~CString((CString *)&stack0x00000000);
  ExceptionList = local_10;
  return;
}

