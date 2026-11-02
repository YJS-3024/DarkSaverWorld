
undefined4 FUN_00412ed0(int param_1,char *param_2)

{
  char *local_10;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047bc68;
  local_c = ExceptionList;
  if (DAT_004953b8 != (void *)0x0) {
    ExceptionList = &local_c;
    CString::CString((CString *)&local_10);
    local_4 = 0;
    CString::operator=((CString *)&local_10,param_2);
    thunk_FUN_0041f8f0(DAT_004953b8,local_10,param_1);
    local_4 = 0xffffffff;
    CString::~CString((CString *)&local_10);
  }
  ExceptionList = local_c;
  return 1;
}

