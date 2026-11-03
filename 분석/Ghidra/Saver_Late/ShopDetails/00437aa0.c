
undefined4 * __thiscall
FUN_00437aa0(void *this,undefined4 param_1,undefined4 param_2,undefined4 param_3,undefined4 param_4,
            undefined4 param_5,undefined4 param_6,undefined4 param_7,undefined4 param_8,
            undefined4 param_9)

{
  int iVar1;
  void *local_c;
  undefined1 *puStack_8;
  undefined4 local_4;
  
  local_4 = 0xffffffff;
  puStack_8 = &LAB_0047dab8;
  local_c = ExceptionList;
  ExceptionList = &local_c;
  thunk_FUN_004369f0(this,param_1,param_2,param_3);
  *(undefined ***)this = &PTR_LAB_00484674;
  *(undefined4 *)((int)this + 0x38) = param_4;
  local_4 = 0;
  *(undefined4 *)((int)this + 0x4c) = 0;
  *(undefined4 *)((int)this + 0x3c) = param_5;
  *(undefined4 *)((int)this + 0x40) = param_6;
  *(undefined4 *)((int)this + 0x44) = param_7;
  *(undefined4 *)((int)this + 0x48) = param_9;
  *(undefined4 *)((int)this + 8) = param_8;
  iVar1 = thunk_FUN_00437ce0((int)this);
  *(int *)((int)this + 0x50) = iVar1;
  ExceptionList = local_c;
  return this;
}

