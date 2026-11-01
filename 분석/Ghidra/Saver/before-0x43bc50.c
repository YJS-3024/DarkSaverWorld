/* Ghidra decompilation; not recovered original source. Evidence: ev_17911eb33dddbe3ec3845c9b36c303a63d104c2bb88109a62bd80cf50dd868a8 */

undefined4 __cdecl FUN_0043bc50(char *param_1,int param_2)

{
  int iVar1;
  AFX_MODULE_STATE *pAVar2;
  CString *pCVar3;
  CString *this;
  CString *this_00;
  CString local_44 [4];
  CString local_40 [4];
  CString local_3c [4];
  CString local_38 [4];
  CString local_34 [4];
  BB_CompressDllMng local_30 [48];
  
  iVar1 = atoi(param_1);
  pAVar2 = AfxGetModuleState();
  if (*(short *)(*(int *)(pAVar2 + 4) + 0xd2) != iVar1) {
    CString::CString(local_40);
    CString::CString(local_44);
    CString::CString((CString *)&param_1);
    CString::Format(local_40,(char *)local_40);
    CString::Format(this,(char *)local_40);
    BB_CompressDllMng::BB_CompressDllMng(local_30);
    iVar1 = BB_CompressDllMng::Initialize(local_30);
    if (iVar1 == 0) {
      BB_CompressDllMng::~BB_CompressDllMng(local_30);
      CString::~CString((CString *)&stack0x0000000c);
      CString::~CString(local_3c);
      CString::~CString(local_38);
      return 0;
    }
    pCVar3 = (CString *)BB_CompressDllMng::WriteTmpFile(local_30,local_34,local_38);
    CString::operator=((CString *)&param_2,pCVar3);
    CString::~CString(local_38);
    if (*(int *)(param_2 + -8) == 0) {
      BB_CompressDllMng::~BB_CompressDllMng((BB_CompressDllMng *)local_34);
      CString::~CString((CString *)&param_2);
      CString::~CString(local_40);
      CString::~CString(local_3c);
      return 0;
    }
    BB_CompressDllMng::DeleteTmpFile((BB_CompressDllMng *)local_34,(CString *)&param_2);
    CString::Format(this_00,(char *)local_40);
    pCVar3 = (CString *)BB_CompressDllMng::WriteTmpFile(local_30,local_34,local_38);
    CString::operator=((CString *)&param_2,pCVar3);
    CString::~CString(local_38);
    if (*(int *)(param_2 + -8) == 0) {
      BB_CompressDllMng::~BB_CompressDllMng((BB_CompressDllMng *)local_34);
      CString::~CString((CString *)&param_2);
      CString::~CString(local_40);
      CString::~CString(local_3c);
      return 0;
    }
    BB_CompressDllMng::DeleteTmpFile((BB_CompressDllMng *)local_34,(CString *)&param_2);
    BB_CompressDllMng::~BB_CompressDllMng((BB_CompressDllMng *)local_34);
    CString::~CString((CString *)&param_2);
    CString::~CString(local_40);
    CString::~CString(local_3c);
  }
  return 1;
}

