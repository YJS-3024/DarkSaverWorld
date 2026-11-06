
void __fastcall FUN_00442b70(void *param_1)

{
  CWinApp *this;
  AFX_MODULE_STATE *pAVar1;
  CString *pCVar2;
  void *pvVar3;
  int iVar4;
  int unaff_EBX;
  void *local_14;
  char local_10 [4];
  void *local_c;
  undefined1 *local_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  local_8 = &LAB_0047e058;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  pAVar1 = AfxGetModuleState();
  this = *(CWinApp **)(pAVar1 + 4);
  CString::CString((CString *)&local_14);
  local_4 = 0;
  pCVar2 = (CString *)CWinApp::GetProfileStringA(this,local_10,"Dlg Information","LiveGameDlgs");
  local_8._0_1_ = 1;
  CString::operator=((CString *)&stack0xffffffe8,pCVar2);
  local_8 = (undefined1 *)((uint)local_8._1_3_ << 8);
  CString::~CString((CString *)&local_14);
  do {
    pvVar3 = (void *)CString::Find((CString *)&stack0xffffffe8,' ');
    if (pvVar3 != (void *)0xffffffff) {
      iVar4 = atoi((char *)((int)pvVar3 + unaff_EBX + 1));
      if ((iVar4 != 8) && (iVar4 != 0x1c)) {
        thunk_FUN_00442370(param_1,iVar4);
      }
    }
    pCVar2 = (CString *)CString::Right((CString *)&stack0xffffffe8,(int)&local_14);
    local_c._0_1_ = 2;
    CString::operator=((CString *)&stack0xffffffe4,pCVar2);
    local_c = (void *)((uint)local_c._1_3_ << 8);
    CString::~CString((CString *)&stack0xffffffe8);
  } while (pvVar3 != (void *)0xffffffff);
  local_c = pvVar3;
  CString::~CString((CString *)&stack0xffffffe4);
  ExceptionList = local_14;
  return;
}


/* incoming refs
*/
