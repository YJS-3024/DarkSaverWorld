
undefined4 __thiscall FUN_004624d0(void *this,char *param_1)

{
  short *psVar1;
  int iVar2;
  int *piVar3;
  char *pcVar4;
  
  if (*param_1 != -0x79) {
    return 0;
  }
  pcVar4 = param_1 + 1;
  iVar2 = 0;
  param_1 = (char *)0x0;
  thunk_FUN_0043c3f0((int)pcVar4,(int *)&param_1);
  psVar1 = (short *)thunk_FUN_0043c3f0((int)pcVar4,(int *)&param_1);
  *(int *)((int)this + 0xe8) = (int)*psVar1;
  psVar1 = (short *)thunk_FUN_0043c3f0((int)pcVar4,(int *)&param_1);
  *(int *)((int)this + 0xec) = (int)*psVar1;
  psVar1 = (short *)thunk_FUN_0043c3f0((int)pcVar4,(int *)&param_1);
  *(int *)((int)this + 0xf0) = (int)*psVar1;
  piVar3 = (int *)((int)this + 0x100);
  do {
    if (iVar2 < *(int *)((int)this + 0xf0)) {
      psVar1 = (short *)thunk_FUN_0043c3f0((int)pcVar4,(int *)&param_1);
      *piVar3 = (int)*psVar1;
    }
    else {
      *piVar3 = -1;
    }
    iVar2 = iVar2 + 1;
    piVar3 = piVar3 + 1;
  } while (iVar2 < 10);
  *(undefined4 *)((int)this + 0xf8) = 1;
  if (*(int *)((int)this + 0xe8) != -1) {
    *(undefined4 *)((int)this + 0xf4) = 1;
    thunk_FUN_00462630((int)this);
    return 1;
  }
  if (*(int *)((int)this + 0xf0) != 0) {
    *(undefined4 *)((int)this + 0xf4) = 2;
    thunk_FUN_00462630((int)this);
    return 1;
  }
  if (*(int *)((int)this + 0xec) != -1) {
    *(undefined4 *)((int)this + 0xf4) = 0;
    thunk_FUN_00462630((int)this);
  }
  return 1;
}

