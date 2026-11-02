
void __fastcall FUN_0041f430(int param_1)

{
  int iVar1;
  CString *pCVar2;
  int iVar3;
  void *pvVar4;
  char *unaff_EBP;
  int iVar5;
  int *piVar6;
  char *local_6c;
  CString local_68 [4];
  int local_64;
  int *local_60;
  CStdioFile local_5c [20];
  BB_CompressDllMng local_48 [4];
  BB_CompressDllMng local_44 [52];
  void *local_10;
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047c654;
  local_c = ExceptionList;
  iVar5 = 0;
  ExceptionList = &local_c;
  *(undefined4 *)(param_1 + 0xc) = 0;
  if (*(void **)(param_1 + 0x10) != (void *)0x0) {
    operator_delete(*(void **)(param_1 + 0x10));
    *(undefined4 *)(param_1 + 0x10) = 0;
  }
  pCVar2 = *(CString **)(param_1 + 0x14);
  if (pCVar2 != (CString *)0x0) {
    CString::~CString(pCVar2);
    operator_delete(pCVar2);
    *(undefined4 *)(param_1 + 0x14) = 0;
  }
  BB_CompressDllMng::BB_CompressDllMng(local_44);
  local_4 = 0;
  iVar1 = BB_CompressDllMng::Initialize(local_44);
  if (iVar1 != 0) {
    CString::CString((CString *)&local_6c,s_midiset_txt_0048fe94);
    local_4._0_1_ = 1;
    CString::CString((CString *)&local_64,s_gameres_set_arc_0048fe80);
    local_4 = CONCAT31(local_4._1_3_,2);
    pCVar2 = (CString *)BB_CompressDllMng::WriteTmpFile(local_44,local_68,(CString *)&local_64);
    local_8._0_1_ = 3;
    CString::operator=((CString *)&stack0xffffff90,pCVar2);
    local_8._0_1_ = 2;
    CString::~CString((CString *)&local_6c);
    if (*(int *)(unaff_EBP + -8) != 0) {
      CStdioFile::CStdioFile(local_5c);
      local_8._0_1_ = 4;
      iVar1 = CStdioFile::Open(local_5c,unaff_EBP,0x4000,(CFileException *)0x0);
      if (iVar1 != 0) {
        CString::CString((CString *)&local_6c);
        local_8 = (undefined1 *)CONCAT31(local_8._1_3_,5);
        iVar1 = CStdioFile::ReadString(local_5c,(CString *)&local_6c);
        while ((iVar1 != 0 && (iVar5 + -2 < *(int *)(param_1 + 0xc)))) {
          local_64 = iVar5 + 1;
          if (local_64 == 1) {
            iVar5 = atoi(local_6c);
            *(int *)(param_1 + 0xc) = iVar5;
            pvVar4 = operator_new(iVar5 * 4);
            iVar5 = *(int *)(param_1 + 0xc);
            *(void **)(param_1 + 0x10) = pvVar4;
            local_60 = operator_new(iVar5 * 4 + 4);
            local_8._0_1_ = 6;
            if (local_60 == (int *)0x0) {
              piVar6 = (int *)0x0;
            }
            else {
              piVar6 = local_60 + 1;
              *local_60 = iVar5;
              FUN_00479d72(piVar6,4,iVar5,CString::CString);
            }
            *(int **)(param_1 + 0x14) = piVar6;
            local_8 = (undefined1 *)CONCAT31(local_8._1_3_,5);
            iVar5 = local_64;
          }
          else {
            iVar1 = iVar5 + -1;
            iVar5 = local_64;
            if ((iVar1 != *(int *)(param_1 + 0xc)) && (*local_6c != '\0')) {
              iVar1 = iVar1 * 4;
              iVar3 = atoi(local_6c);
              *(int *)(*(int *)(param_1 + 0x10) + iVar1) = iVar3;
              iVar3 = CString::ReverseFind((CString *)&local_6c,' ');
              CString::operator=((CString *)(*(int *)(param_1 + 0x14) + iVar1),local_6c + iVar3 + 1)
              ;
            }
          }
          iVar1 = CStdioFile::ReadString(local_5c,(CString *)&local_6c);
        }
        CStdioFile::Close(local_5c);
        local_8._0_1_ = 4;
        CString::~CString((CString *)&local_6c);
      }
      BB_CompressDllMng::DeleteTmpFile(local_48,(CString *)&stack0xffffff90);
      CString::operator=((CString *)&stack0xffffff90,&DAT_0048fe78);
      iVar5 = 0;
      if (0 < *(int *)(param_1 + 0xc)) {
        do {
          pCVar2 = (CString *)operator+((CString *)&local_64,(CString *)&stack0xffffff90);
          local_c._0_1_ = 7;
          CString::operator=((CString *)(*(int *)(param_1 + 0x14) + iVar5 * 4),pCVar2);
          local_c = (void *)CONCAT31(local_c._1_3_,4);
          CString::~CString(local_68);
          iVar5 = iVar5 + 1;
        } while (iVar5 < *(int *)(param_1 + 0xc));
      }
      local_8._0_1_ = 2;
      CStdioFile::~CStdioFile(local_5c);
      local_8._0_1_ = 1;
      CString::~CString(local_68);
      local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
      CString::~CString((CString *)&stack0xffffff90);
      local_8 = (undefined1 *)0xffffffff;
      BB_CompressDllMng::~BB_CompressDllMng(local_48);
      ExceptionList = local_10;
      return;
    }
    local_8._0_1_ = 1;
    CString::~CString(local_68);
    local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
    CString::~CString((CString *)&stack0xffffff90);
  }
  local_8 = (undefined1 *)0xffffffff;
  BB_CompressDllMng::~BB_CompressDllMng(local_48);
  ExceptionList = local_10;
  return;
}

