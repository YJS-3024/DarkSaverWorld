
void __fastcall FUN_0046ef40(int param_1)

{
  char *pcVar1;
  int iVar2;
  undefined1 uVar3;
  int local_14;
  char *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_00480948;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  pcVar1 = operator_new(0x14);
  local_14 = 0;
  CString::CString((CString *)&local_10);
  local_4 = 0;
  CWnd::GetWindowTextA(*(CWnd **)(param_1 + 0xb8),(CString *)&local_10);
  iVar2 = atoi(local_10);
  if (*(int *)(param_1 + 0xcc) == 0) {
    uVar3 = 0x96;
  }
  else {
    uVar3 = 0x95;
  }
  thunk_FUN_0043b9a0((int)pcVar1,&local_14,uVar3);
  thunk_FUN_0043ba20((int)pcVar1,&local_14,iVar2);
  thunk_FUN_004132d0(DAT_004952a0,local_14,pcVar1);
  operator_delete(pcVar1);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&local_10);
  ExceptionList = local_c;
  return;
}

