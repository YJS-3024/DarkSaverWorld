
undefined4 __thiscall FUN_00476460(void *this,char *param_1,undefined4 param_2)

{
  int *piVar1;
  undefined4 uVar2;
  void *pvVar3;
  int iVar4;
  
  switch(*param_1) {
  case 'Q':
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x17);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00460360(pvVar3,param_1);
      return uVar2;
    }
    break;
  case 'Y':
  case -0x58:
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x3b);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00455b10(pvVar3,param_1);
      return uVar2;
    }
    break;
  case 's':
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x24);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00446fd0(pvVar3,param_1);
      return uVar2;
    }
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x34);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00446fd0(pvVar3,param_1);
      return uVar2;
    }
    break;
  case -0x79:
  case -0x75:
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x28);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00462440(pvVar3,param_1,param_2);
      return uVar2;
    }
    break;
  case -0x6f:
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x19);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00471ed0(pvVar3,param_1);
      return uVar2;
    }
    break;
  case -0x6c:
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x22);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00445620(pvVar3,param_1);
      return uVar2;
    }
    break;
  case -0x68:
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x25);
    if ((pvVar3 != (void *)0x0) && (*param_1 == -0x68)) {
      thunk_FUN_004713e0(pvVar3,(int)(param_1 + 1));
      return 1;
    }
    break;
  case -0x61:
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x35);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00445e20(pvVar3,param_1);
      return uVar2;
    }
    break;
  case -0x5f:
    piVar1 = (int *)thunk_FUN_00475f50(this,0x31);
    if (piVar1 != (int *)0x0) goto LAB_004765a4;
    iVar4 = 0x33;
    goto LAB_00476595;
  case -0x5d:
    (**(code **)(*(int *)this + 4))(0x36,1);
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x36);
    if (pvVar3 != (void *)0x0) {
      uVar2 = thunk_FUN_00472ef0(pvVar3,param_1);
      return uVar2;
    }
    break;
  case -0x5b:
    (**(code **)(*(int *)this + 4))(0x3a,1);
    pvVar3 = (void *)thunk_FUN_00475f50(this,0x3a);
    if (pvVar3 != (void *)0x0) {
      thunk_FUN_0045e570(pvVar3,(int)(param_1 + 1));
      return 1;
    }
    break;
  case -0x5a:
    thunk_FUN_004758e0(this,0x3a);
    return 1;
  case -0x46:
    iVar4 = 0x45;
LAB_00476595:
    piVar1 = (int *)thunk_FUN_00475f50(this,iVar4);
    if (piVar1 != (int *)0x0) {
LAB_004765a4:
      uVar2 = (**(code **)(*piVar1 + 0x58))(param_1,param_2);
      return uVar2;
    }
  }
  return 0;
}

