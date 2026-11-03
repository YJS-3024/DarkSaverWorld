
undefined4 * __thiscall
FUN_0044bb30(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e8b8;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_00449b50(this,param_1,param_2,param_3,param_4,param_5);
  local_4 = 0;
  *(undefined ***)this = &PTR_LAB_00484e88;
  *(undefined4 *)((int)this + 0xb4) = 100;
  thunk_FUN_0044a5d0((int)this);
  ExceptionList = local_c;
  return this;
}

