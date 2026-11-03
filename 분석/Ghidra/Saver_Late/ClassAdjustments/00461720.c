
void __thiscall FUN_00461720(int *param_1,undefined4 param_2)

{
  int iVar1;
  int iVar2;
  
  switch(param_2) {
  case 0:
    (**(code **)(*param_1 + 0x44))();
    return;
  case 1:
  case 0x13:
    thunk_FUN_00462900((int)param_1);
    iVar2 = param_1[0x66];
    goto joined_r0x00461762;
  case 2:
    thunk_FUN_00462000(param_1,1);
    return;
  case 3:
    thunk_FUN_00462000(param_1,2);
    return;
  case 4:
    thunk_FUN_00461dd0(param_1,0,2);
    return;
  case 5:
    thunk_FUN_00461dd0(param_1,1,2);
    return;
  case 6:
    thunk_FUN_00461dd0(param_1,2,2);
    return;
  case 7:
    thunk_FUN_00461dd0(param_1,3,2);
    return;
  case 8:
    thunk_FUN_00461dd0(param_1,4,2);
    return;
  case 9:
    thunk_FUN_00461dd0(param_1,5,2);
    return;
  case 10:
    thunk_FUN_00461dd0(param_1,0,1);
    return;
  case 0xb:
    thunk_FUN_00461dd0(param_1,1,1);
    return;
  case 0xc:
    thunk_FUN_00461dd0(param_1,2,1);
    return;
  case 0xd:
    thunk_FUN_00461dd0(param_1,3,1);
    return;
  case 0xe:
    thunk_FUN_00461dd0(param_1,4,1);
    return;
  case 0xf:
    thunk_FUN_00461dd0(param_1,5,1);
    return;
  case 0x10:
    if ((param_1[0x3e] != 0) && (iVar2 = param_1[0x3f], param_1[0x3f] = iVar2 + -1, iVar2 + -1 < 0))
    {
      param_1[0x3f] = 0;
      return;
    }
    break;
  case 0x11:
    if (((param_1[0x3e] != 0) && (iVar2 = param_1[0x3c], iVar2 != 0)) &&
       (iVar1 = param_1[0x3f], param_1[0x3f] = iVar1 + 1, iVar2 <= iVar1 + 1)) {
      param_1[0x3f] = iVar2 + -1;
      return;
    }
    break;
  case 0x12:
    thunk_FUN_00462a00((int)param_1);
    break;
  case 0x14:
    thunk_FUN_00462980((int)param_1);
    iVar2 = param_1[0x66];
joined_r0x00461762:
    if (iVar2 == 0x28) {
      iVar2 = (**(code **)(*param_1 + 4))();
      thunk_FUN_004758e0((void *)param_1[0x28],iVar2);
      return;
    }
    iVar2 = (**(code **)(*param_1 + 4))();
    thunk_FUN_004426a0((void *)param_1[0x51],iVar2);
    return;
  }
  return;
}

