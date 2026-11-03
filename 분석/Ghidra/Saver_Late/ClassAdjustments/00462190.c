
void __fastcall FUN_00462190(int param_1)

{
  int iVar1;
  char *pcVar2;
  int local_4;
  
  iVar1 = *(int *)(param_1 + 0x13c);
  if (*(short *)(iVar1 + 0x44) != 0) {
    local_4 = param_1;
    pcVar2 = operator_new(10);
    local_4 = 0;
    thunk_FUN_0043b9a0((int)pcVar2,&local_4,0x83);
    thunk_FUN_0043b970((int)pcVar2,&local_4,*(undefined2 *)(iVar1 + 4));
    thunk_FUN_0043b970((int)pcVar2,&local_4,
                       *(undefined2 *)(iVar1 + 0x46 + *(int *)(param_1 + 0xb8) * 2));
    *(short *)(iVar1 + 0x44) = *(short *)(iVar1 + 0x44) + -1;
    if (*(short *)(iVar1 + 0x44) < 0) {
      *(undefined2 *)(iVar1 + 0x44) = 0;
    }
    thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar2);
    operator_delete(pcVar2);
  }
  return;
}

