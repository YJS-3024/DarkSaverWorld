
CString * __thiscall FUN_0041f820(void *this,CString *param_1,int param_2)

{
  int iVar1;
  int *piVar2;
  CString local_14 [4];
  undefined4 local_10;
  void *local_c;
  undefined1 *puStack_8;
  uint local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047c69f;
  local_c = ExceptionList;
  local_10 = 0;
  ExceptionList = &local_c;
  CString::CString(local_14);
  iVar1 = 0;
  local_4 = 1;
  if (0 < *(int *)((int)this + 0xc)) {
    piVar2 = *(int **)((int)this + 0x10);
    do {
      if (*piVar2 == param_2) {
        CString::operator=(local_14,(CString *)(*(int *)((int)this + 0x14) + iVar1 * 4));
        break;
      }
      iVar1 = iVar1 + 1;
      piVar2 = piVar2 + 1;
    } while (iVar1 < *(int *)((int)this + 0xc));
  }
  CString::CString(param_1,local_14);
  local_10 = 1;
  local_4 = local_4 & 0xffffff00;
  CString::~CString(local_14);
  ExceptionList = local_c;
  return param_1;
}

