
void __fastcall FUN_0044af40(int param_1)

{
  undefined4 uVar1;
  char *pcVar2;
  int local_14;
  CString local_10 [4];
  void *local_c;
  undefined *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &DAT_0047e818;
  local_c = ExceptionList;
  if (0x1b < *(int *)(*(int *)(param_1 + 0xb0) + 0x40)) {
    ExceptionList = &local_c;
    CString::CString(local_10,&DAT_00490564);
    local_4 = 0;
    thunk_FUN_00476870(*(int **)(param_1 + 0xa0));
    local_4 = 0xffffffff;
    CString::~CString(local_10);
    ExceptionList = local_c;
    return;
  }
  ExceptionList = &local_c;
  pcVar2 = operator_new(10);
  local_14 = 0;
  uVar1 = *(undefined4 *)(*(int *)(param_1 + 0xbc) + *(int *)(param_1 + 0x360) * 4);
  thunk_FUN_0043b9a0((int)pcVar2,&local_14,0x4b);
  thunk_FUN_0043b970((int)pcVar2,&local_14,(short)uVar1);
  thunk_FUN_004132d0(DAT_004952a0,local_14,pcVar2);
  operator_delete(pcVar2);
  ExceptionList = local_c;
  return;
}

