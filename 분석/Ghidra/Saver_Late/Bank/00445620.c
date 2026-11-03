
undefined4 __thiscall FUN_00445620(void *this,char *param_1)

{
  undefined4 uVar1;
  char *pcVar2;
  undefined4 *puVar3;
  
  pcVar2 = param_1;
  if (*param_1 == -0x6c) {
    param_1 = (char *)0x1;
    puVar3 = (undefined4 *)thunk_FUN_0043c520((int)pcVar2,(int *)&param_1);
    *(undefined4 *)((int)this + 0xb8) = *puVar3;
    uVar1 = puVar3[1];
    *(undefined4 *)((int)this + 0xc0) = 1;
    *(undefined4 *)((int)this + 0xbc) = uVar1;
    return 1;
  }
  return 0;
}

