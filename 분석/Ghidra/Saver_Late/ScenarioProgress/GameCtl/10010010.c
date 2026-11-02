
undefined4 FUN_10010010(void)

{
  CString *pCVar1;
  char *pcVar2;
  int iVar3;
  ulong uVar4;
  int iVar5;
  undefined4 *puVar6;
  char *pcVar7;
  undefined4 uStack_420;
  CString local_418 [8];
  CFile local_410 [4];
  CString local_40c [4];
  CString local_408 [4];
  CString local_404 [4];
  undefined4 local_400 [2];
  CString local_3f8 [992];
  void *local_18;
  int local_10;
  void *local_c;
  char *local_8;
  undefined4 local_4;
  
  local_8 = &LAB_100157bf;
  local_c = ExceptionList;
  local_4 = 0;
  uStack_420 = (char *)0x10010040;
  ExceptionList = &local_c;
  CFile::CFile(local_410);
  local_4._0_1_ = 1;
  uStack_420 = (char *)0x10010051;
  CString::CString(local_418);
  uStack_420 = s_Glib__1001a54c;
  local_4 = CONCAT31(local_4._1_3_,2);
  pCVar1 = (CString *)operator+(local_3f8,&DAT_1001a8b4);
  pcVar7 = (char *)&DAT_1006a73c;
  local_8 = (char *)CONCAT31(local_8._1_3_,3);
  pcVar2 = (char *)operator+(local_404,pCVar1);
  local_c = (void *)CONCAT31(local_c._1_3_,4);
  pCVar1 = (CString *)operator+(local_404,pcVar2);
  local_10._0_1_ = 5;
  CString::operator=((CString *)&stack0xfffffbdc,pCVar1);
  local_10._0_1_ = 4;
  CString::~CString(local_408);
  local_10._0_1_ = 3;
  CString::~CString(local_40c);
  local_10 = CONCAT31(local_10._1_3_,2);
  CString::~CString(local_404);
  iVar3 = CFile::Open((CFile *)&stack0xfffffbe4,pcVar7,2,(CFileException *)0x0);
  if (iVar3 == 0) {
LAB_10010116:
    iVar3 = CFile::Open((CFile *)&stack0xfffffbe4,pcVar7,0x1002,(CFileException *)0x0);
    if (iVar3 == 0) goto LAB_100101f8;
    puVar6 = local_400;
    for (iVar3 = 0xfa; iVar3 != 0; iVar3 = iVar3 + -1) {
      *puVar6 = 0;
      puVar6 = puVar6 + 1;
    }
    CFile::Write((CFile *)&stack0xfffffbe4,local_400,1000);
    CFile::Close((CFile *)&stack0xfffffbe4);
  }
  else {
    uVar4 = CFile::GetLength((CFile *)&stack0xfffffbe4);
    CFile::Close((CFile *)&stack0xfffffbe4);
    if (uVar4 < 1000) goto LAB_10010116;
  }
  iVar3 = FUN_1000f040(local_8,0x3e6);
  iVar5 = CFile::Open((CFile *)&stack0xfffffbe4,pcVar7,0,(CFileException *)0x0);
  if (iVar5 != 0) {
    CFile::Seek((CFile *)&stack0xfffffbe4,iVar3,0);
    CFile::Read((CFile *)&stack0xfffffbe4,(void *)((int)&uStack_420 + 3),1);
    CFile::Close((CFile *)&stack0xfffffbe4);
    if (uStack_420._3_1_ == '\0') {
      local_10._0_1_ = 1;
      CString::~CString((CString *)&stack0xfffffbdc);
      local_10 = (uint)local_10._1_3_ << 8;
      CFile::~CFile((CFile *)&stack0xfffffbe4);
      local_10 = 0xffffffff;
      CString::~CString((CString *)&local_8);
      ExceptionList = local_18;
      return 1;
    }
  }
LAB_100101f8:
  local_10._0_1_ = 1;
  CString::~CString((CString *)&stack0xfffffbdc);
  local_10 = (uint)local_10._1_3_ << 8;
  CFile::~CFile((CFile *)&stack0xfffffbe4);
  local_10 = 0xffffffff;
  CString::~CString((CString *)&local_8);
  ExceptionList = local_18;
  return 0;
}

