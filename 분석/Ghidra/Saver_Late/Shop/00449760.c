
void __thiscall FUN_00449760(int *param_1,int param_2)

{
  int iVar1;
  
  if (param_2 == 0) {
    thunk_FUN_00449950();
  }
  else if (param_2 != 1) {
    return;
  }
  iVar1 = (**(code **)(*param_1 + 4))();
  thunk_FUN_004758e0((void *)param_1[0x28],iVar1);
  return;
}

