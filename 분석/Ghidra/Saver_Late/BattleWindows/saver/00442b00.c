
void __thiscall FUN_00442b00(void *this,int param_1,undefined4 *param_2)

{
  int iVar1;
  
  iVar1 = thunk_FUN_00442a00(this,param_1);
  if (iVar1 != 0) {
    *(undefined4 *)(iVar1 + 0x40) = *param_2;
    *(undefined4 *)(iVar1 + 0x44) = param_2[1];
  }
  return;
}

