
void __thiscall FUN_00453240(byte *param_1,int param_2)

{
  int *piVar1;
  byte *pbVar2;
  void *pvStack_c;
  undefined *puStack_8;
  undefined4 uStack_4;
  
  uStack_4 = 0xffffffff;
  puStack_8 = &DAT_0047ee58;
  pvStack_c = ExceptionList;
  if (param_2 == 0) {
    ExceptionList = &pvStack_c;
    pbVar2 = param_1;
    CString::CString((CString *)&stack0xffffffec,(CString *)(param_1 + 0xb8));
    uStack_4 = 0xffffffff;
    piVar1 = (int *)thunk_FUN_00453320(param_1);
    thunk_FUN_00475970(*(void **)(param_1 + 0xa0),piVar1,pbVar2);
  }
  else {
    if (param_2 == 1) {
      ExceptionList = &pvStack_c;
      thunk_FUN_00437b80(*(int *)(param_1 + 0xbc));
      ExceptionList = pvStack_c;
      return;
    }
    if (param_2 == 2) {
      ExceptionList = &pvStack_c;
      thunk_FUN_00437ba0(*(int *)(param_1 + 0xbc));
      ExceptionList = pvStack_c;
      return;
    }
  }
  ExceptionList = pvStack_c;
  return;
}

