
void __thiscall FUN_004499c0(void *this,undefined4 param_1,undefined4 param_2,undefined4 param_3)

{
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  puStack_8 = &LAB_0047e728;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  *(undefined4 *)((int)this + 0xb8) = param_2;
  *(undefined4 *)((int)this + 0xbc) = param_3;
  local_4 = 0;
  CString::operator=((CString *)((int)this + 0xc0),(CString *)&param_1);
  local_4 = 0xffffffff;
  CString::~CString((CString *)&param_1);
  ExceptionList = local_c;
  return;
}

