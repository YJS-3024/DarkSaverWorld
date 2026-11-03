
void __cdecl FUN_100053f0(int param_1,undefined4 param_2,int param_3,int param_4)

{
  int iVar1;
  int iVar2;
  int iVar3;
  int local_1c;
  int local_18;
  int local_14;
  int local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  puStack_8 = &LAB_10014f88;
  local_c = ExceptionList;
  iVar3 = 0;
  local_4 = 0;
  ExceptionList = &local_c;
  if (0 < *(int *)(param_3 + -8)) {
    iVar1 = param_4 * 7;
    ExceptionList = &local_c;
    do {
      iVar2 = *(char *)(iVar3 + param_3) + -0x30;
      if ((iVar2 < 0) || (9 < iVar2)) {
        iVar2 = -1;
      }
      local_1c = iVar2 * 5 + 0x17c;
      local_14 = iVar2 * 5 + 0x181;
      local_10 = iVar1 + 0x14f;
      local_18 = iVar1 + 0x148;
      CDDSurface::BltFast(DAT_1001c470,param_1,param_2,DAT_1006a704,&local_1c,1);
      param_1 = param_1 + 5;
      iVar3 = iVar3 + 1;
    } while (iVar3 < *(int *)(param_3 + -8));
  }
  local_4 = 0xffffffff;
  CString::~CString((CString *)&param_3);
  ExceptionList = local_c;
  return;
}

