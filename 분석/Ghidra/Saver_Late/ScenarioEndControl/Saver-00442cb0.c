
void __fastcall FUN_00442cb0(void *param_1)

{
  int iVar1;
  AFX_MODULE_STATE *pAVar2;
  int iVar3;
  int iVar4;
  CString local_18 [4];
  char *local_14;
  undefined4 local_10;
  CWinApp *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e080;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  pAVar2 = AfxGetModuleState();
  local_10 = *(undefined4 *)(pAVar2 + 4);
  CString::CString(local_18);
  iVar1 = *(int *)((int)param_1 + 0xc);
  local_4 = 0;
  thunk_FUN_00442a00(param_1,1000);
  thunk_FUN_00442a00(param_1,0x3e9);
  thunk_FUN_00442a00(param_1,8);
  thunk_FUN_00442a00(param_1,0x1c);
  CString::Format(local_18,(char *)local_18);
  CString::CString((CString *)&local_10);
  iVar4 = 0;
  if (0 < iVar1) {
    do {
      iVar3 = (**(code **)(**(int **)(*(int *)((int)param_1 + 8) + iVar4 * 4) + 4))();
      if ((((iVar3 != 1000) && (iVar3 != 8)) && (iVar3 != 0x1c)) && (iVar3 != 0x3e9)) {
        thunk_FUN_00434c20(*(int **)(*(int *)((int)param_1 + 8) + iVar4 * 4));
        (**(code **)(**(int **)(*(int *)((int)param_1 + 8) + iVar4 * 4) + 4))();
        CString::Format((CString *)&local_14,(char *)&local_14);
        if (*(int *)(*(int *)(*(int *)((int)param_1 + 8) + iVar4 * 4) + 0x78) != 0) {
          CString::operator+=((CString *)&local_14,(CString *)&local_10);
        }
      }
      iVar4 = iVar4 + 1;
    } while (iVar4 < iVar1);
  }
  CWinApp::WriteProfileStringA(local_c,"Dlg Information","LiveGameDlgs",local_14);
  CString::~CString((CString *)&local_10);
  CString::~CString((CString *)&local_14);
  ExceptionList = puStack_8;
  return;
}


/* incoming refs
*/
