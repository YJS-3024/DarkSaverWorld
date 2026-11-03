
undefined4 * __thiscall
FUN_004495f0(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e6c8;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  local_4 = 0;
  CString::CString((CString *)((int)this + 0xc0));
  *(undefined4 *)((int)this + 0xb8) = 0;
  *(undefined4 *)((int)this + 0x90) = 0;
  *(undefined4 *)((int)this + 0xbc) = 0;
  *(undefined ***)this = &PTR_LAB_00484d2c;
  ExceptionList = local_c;
  return this;
}

