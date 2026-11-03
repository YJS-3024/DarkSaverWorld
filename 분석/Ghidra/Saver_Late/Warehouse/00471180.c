
void __fastcall FUN_00471180(int param_1)

{
  undefined4 uVar1;
  undefined4 uVar2;
  char *pcVar3;
  int iVar4;
  int iStack_14;
  CString local_10 [4];
  void *local_c;
  undefined *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &DAT_00480a48;
  local_c = ExceptionList;
  if (0x1b < *(int *)(*(int *)(param_1 + 0xb0) + 0x40)) {
    ExceptionList = &local_c;
    CString::CString(local_10,&DAT_00490efc);
    local_4 = 0;
    thunk_FUN_00476870(*(int **)(param_1 + 0xa0));
    local_4 = 0xffffffff;
    CString::~CString(local_10);
    ExceptionList = local_c;
    return;
  }
  ExceptionList = &local_c;
  pcVar3 = operator_new(10);
  iVar4 = *(int *)(param_1 + 0x360) * 4;
  iStack_14 = 0;
  uVar1 = *(undefined4 *)(*(int *)(param_1 + 0xbc) + iVar4);
  uVar2 = *(undefined4 *)(*(int *)(param_1 + 0x378) + iVar4);
  thunk_FUN_0043b9a0((int)pcVar3,&iStack_14,0x9a);
  thunk_FUN_0043b970((int)pcVar3,&iStack_14,*(undefined2 *)(param_1 + 0x374));
  thunk_FUN_0043b970((int)pcVar3,&iStack_14,(short)uVar1);
  thunk_FUN_0043b970((int)pcVar3,&iStack_14,(short)uVar2);
  thunk_FUN_004132d0(DAT_004952a0,iStack_14,pcVar3);
  operator_delete(pcVar3);
  ExceptionList = local_c;
  return;
}

