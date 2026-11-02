
void __thiscall FUN_0041f310(void *this,int param_1)

{
  int iVar1;
  CString local_14 [4];
  char *local_10;
  void *local_c;
  undefined1 *puStack_8;
  int local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047c600;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  CString::CString(local_14);
  local_4 = 0;
  if (DAT_00495320 == 0) {
    thunk_FUN_0041f400((int)this);
    *(int *)((int)this + 4) = param_1;
  }
  else if ((*(int *)((int)this + 4) != param_1) || (*(int *)((int)this + 8) == 0)) {
    thunk_FUN_0041f820(this,(CString *)&local_10,param_1);
    local_4._0_1_ = 1;
    if (*(int *)(local_10 + -8) == 0) {
      *(int *)((int)this + 4) = param_1;
      *(undefined4 *)((int)this + 8) = 0;
    }
    else {
      iVar1 = thunk_FUN_0041f8f0(this,local_10,1);
      *(int *)((int)this + 8) = iVar1;
      *(int *)((int)this + 4) = param_1;
    }
    local_4 = (uint)local_4._1_3_ << 8;
    CString::~CString((CString *)&local_10);
  }
  local_4 = 0xffffffff;
  CString::~CString(local_14);
  ExceptionList = local_c;
  return;
}

