
void __thiscall FUN_00442a80(void *this,int param_1)

{
  int *piVar1;
  
  piVar1 = (int *)thunk_FUN_00442a00(this,param_1);
  if (piVar1 != (int *)0x0) {
    (**(code **)(*piVar1 + 0x44))();
  }
  return;
}

