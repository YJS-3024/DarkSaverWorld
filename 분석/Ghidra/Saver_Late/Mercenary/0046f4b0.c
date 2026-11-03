
void __thiscall FUN_0046f4b0(int *param_1,undefined4 param_2)

{
  int iVar1;
  
  switch(param_2) {
  case 0:
    thunk_FUN_00470070(param_1);
    return;
  case 1:
    thunk_FUN_004701f0((int)param_1);
    return;
  case 2:
    iVar1 = (**(code **)(*param_1 + 4))();
    thunk_FUN_004758e0((void *)param_1[0x28],iVar1);
    return;
  case 3:
    if (0 < param_1[0x60]) {
      param_1[0x60] = param_1[0x60] + -3;
      return;
    }
    break;
  case 4:
    if ((param_1[0x60] + 3) / 3 < (param_1[0x5d] + -4) / 3) {
      param_1[0x60] = param_1[0x60] + 3;
    }
  }
  return;
}

