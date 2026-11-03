
undefined4 * __thiscall
FUN_004779b0(void *this,undefined4 param_1,CString *param_2,int param_3,undefined4 param_4,
            undefined4 param_5)

{
  thunk_FUN_00433180(this,param_1,param_2);
  *(undefined ***)this = &PTR_LAB_0048667c;
  *(int *)((int)this + 0xa0) = param_3;
  *(undefined4 *)((int)this + 0xa4) = param_4;
  *(undefined4 *)((int)this + 0xa8) = param_5;
  if (param_3 == 0) {
    *(undefined4 *)((int)this + 0xac) = 0;
  }
  else {
    *(undefined4 *)((int)this + 0xac) = *(undefined4 *)(param_3 + 0x54);
  }
  *(undefined4 *)((int)this + 0xb0) = *(undefined4 *)(DAT_004952a0 + 0xdc);
  return this;
}

