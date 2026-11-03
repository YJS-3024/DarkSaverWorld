
undefined4 __thiscall FUN_00462440(void *this,char *param_1,undefined4 param_2)

{
  char *pcVar1;
  short *psVar2;
  undefined4 uVar3;
  int *piVar4;
  int iVar5;
  
  pcVar1 = param_1;
  if (*param_1 == -0x79) {
    uVar3 = thunk_FUN_004624d0(this,param_1);
    return uVar3;
  }
  if (*param_1 != -0x75) {
    return 0;
  }
  param_1 = (char *)0x1;
  piVar4 = (int *)((int)this + 0x19c);
  iVar5 = 6;
  do {
    psVar2 = (short *)thunk_FUN_0043c3f0((int)pcVar1,(int *)&param_1);
    *piVar4 = (int)*psVar2;
    piVar4 = piVar4 + 1;
    iVar5 = iVar5 + -1;
  } while (iVar5 != 0);
  return 1;
}

