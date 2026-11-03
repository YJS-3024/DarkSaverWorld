
undefined4 * __thiscall
FUN_00441d30(void *this,undefined4 param_1,undefined4 param_2,CString *param_3,undefined4 param_4)

{
  thunk_FUN_00433490(this,param_2,param_3);
  *(undefined4 *)((int)this + 0xa4) = param_4;
  *(undefined ***)this = &PTR_LAB_00484838;
  *(undefined4 *)((int)this + 0xa0) = param_1;
  return this;
}

