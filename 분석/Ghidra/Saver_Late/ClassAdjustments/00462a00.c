
void __fastcall FUN_00462a00(int param_1)

{
  int iVar1;
  int iVar2;
  char *pcVar3;
  int iVar4;
  int local_4;
  
  iVar1 = *(int *)(param_1 + 0x13c);
  if ((*(short *)(iVar1 + 0x3e) != 0) && (*(int *)(param_1 + 0xf8) != 0)) {
    local_4 = param_1;
    pcVar3 = operator_new(10);
    local_4 = 0;
    thunk_FUN_0043b9a0((int)pcVar3,&local_4,0x81);
    thunk_FUN_0043b970((int)pcVar3,&local_4,*(undefined2 *)(iVar1 + 4));
    iVar2 = *(int *)(param_1 + 0xf4);
    if (iVar2 == 0) {
      iVar4 = *(int *)(param_1 + 0xec);
    }
    else if (iVar2 == 1) {
      iVar4 = *(int *)(param_1 + 0xe8);
    }
    else {
      iVar4 = local_4;
      if (iVar2 == 2) {
        iVar4 = *(int *)(param_1 + 0x100 + *(int *)(param_1 + 0xfc) * 4);
      }
    }
    thunk_FUN_0043b970((int)pcVar3,&local_4,(short)iVar4);
    *(short *)(iVar1 + 0x3e) = *(short *)(iVar1 + 0x3e) + -1;
    thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar3);
    operator_delete(pcVar3);
    thunk_FUN_004623a0(param_1);
  }
  return;
}

