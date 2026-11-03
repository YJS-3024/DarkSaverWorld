
void __thiscall FUN_0044a0d0(int *param_1,int param_2)

{
  int iVar1;
  
  if (-1 < param_2) {
    if (param_2 < 2) {
      thunk_FUN_0044a6f0(param_1,param_2);
    }
    else if (param_2 == 2) {
      iVar1 = (**(code **)(*param_1 + 4))();
      thunk_FUN_004758e0((void *)param_1[0x28],iVar1);
      return;
    }
  }
  return;
}

