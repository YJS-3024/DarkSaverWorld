
void __fastcall FUN_0044b060(int *param_1)

{
  int iVar1;
  int iVar2;
  char *pcVar3;
  undefined2 uVar4;
  undefined4 uVar5;
  CString aCStack_14 [4];
  void *pvStack_10;
  void *pvStack_c;
  undefined *puStack_8;
  undefined4 uStack_4;
  
  uStack_4 = 0xffffffff;
  puStack_8 = &DAT_0047e838;
  pvStack_c = ExceptionList;
  iVar2 = param_1[0x2c];
  iVar1 = param_1[0xda];
  if (((*(int *)(iVar2 + 0x44) == 0) || (*(int *)(iVar2 + 0x48) == 0)) ||
     (*(int *)(iVar2 + 0x40) <= iVar1)) {
    uVar5 = 0xffffffff;
    uVar4 = 0;
  }
  else {
    uVar5 = *(undefined4 *)(*(int *)(iVar2 + 0x44) + iVar1 * 4);
    uVar4 = (undefined2)*(undefined4 *)(*(int *)(iVar2 + 0x48) + iVar1 * 4);
  }
  ExceptionList = &pvStack_c;
  iVar2 = (**(code **)(*param_1 + 0x60))(uVar5);
  if (iVar2 == 0) {
    CString::CString(aCStack_14,&DAT_00490594);
    puStack_8 = (undefined *)0x0;
    thunk_FUN_00476870((int *)param_1[0x28]);
    puStack_8 = (undefined *)0xffffffff;
    CString::~CString(aCStack_14);
    ExceptionList = pvStack_10;
    return;
  }
  pcVar3 = operator_new(10);
  thunk_FUN_0043b9a0((int)pcVar3,(int *)&stack0xffffffe8,0x4c);
  thunk_FUN_0043b970((int)pcVar3,(int *)&stack0xffffffe8,(short)uVar5);
  thunk_FUN_0043b970((int)pcVar3,(int *)&stack0xffffffe8,uVar4);
  thunk_FUN_004132d0(DAT_004952a0,0,pcVar3);
  operator_delete(pcVar3);
  ExceptionList = pvStack_10;
  return;
}

