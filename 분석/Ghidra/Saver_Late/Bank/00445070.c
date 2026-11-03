
undefined4 * __thiscall
FUN_00445070(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047e288;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004779b0(this,param_1,param_2,param_3,param_4,param_5);
  *(undefined4 *)((int)this + 0xc4) = 0x73;
  *(undefined4 *)((int)this + 200) = 0x2f;
  *(undefined4 *)((int)this + 0xcc) = 0x137;
  *(undefined4 *)((int)this + 0xd0) = 0x13;
  *(undefined4 *)((int)this + 0xb8) = 0;
  local_4 = 0;
  *(undefined ***)this = &PTR_LAB_00484a90;
  *(undefined4 *)((int)this + 0xbc) = 0;
  *(undefined4 *)((int)this + 0xc0) = 0;
  *(undefined4 *)((int)this + 0x90) = 0;
  thunk_FUN_00445690((int)this);
  ExceptionList = local_c;
  return this;
}

