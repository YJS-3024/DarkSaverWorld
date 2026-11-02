
void __fastcall FUN_004587c0(int param_1)

{
  int iVar1;
  char *pcVar2;
  int local_14;
  undefined4 *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047f358;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  pcVar2 = operator_new(200);
  local_14 = 0;
  CString::CString((CString *)&local_10);
  local_4 = 0;
  CWnd::GetWindowTextA(*(CWnd **)(param_1 + 0xcc),(CString *)&local_10);
  iVar1 = *(int *)(param_1 + 0xb4);
  thunk_FUN_0043b9a0((int)pcVar2,&local_14,0x3e);
  thunk_FUN_0043b970((int)pcVar2,&local_14,*(undefined2 *)(iVar1 + 4));
  thunk_FUN_0043b9a0((int)pcVar2,&local_14,(char)local_10[-2]);
  thunk_FUN_0043b9d0((int)pcVar2,&local_14,local_10,local_10[-2]);
  thunk_FUN_004132d0(DAT_004952a0,local_14,pcVar2);
  operator_delete(pcVar2);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_10);
  ExceptionList = local_c;
  return;
}

