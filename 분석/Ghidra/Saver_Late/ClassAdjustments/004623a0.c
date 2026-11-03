
void __fastcall FUN_004623a0(int param_1)

{
  int iVar1;
  char *pcVar2;
  int local_4;
  
  iVar1 = *(int *)(param_1 + 0x13c);
  *(undefined4 *)(param_1 + 0xf8) = 0;
  *(undefined4 *)(param_1 + 0xf4) = 0xffffffff;
  if (*(short *)(iVar1 + 0x3e) != 0) {
    local_4 = param_1;
    pcVar2 = operator_new(10);
    local_4 = 0;
    thunk_FUN_0043b9a0((int)pcVar2,&local_4,0x86);
    thunk_FUN_0043b970((int)pcVar2,&local_4,*(undefined2 *)(iVar1 + 4));
    thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar2);
    operator_delete(pcVar2);
  }
  return;
}

