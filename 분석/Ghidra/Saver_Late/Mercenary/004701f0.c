
void __fastcall FUN_004701f0(int param_1)

{
  short sVar1;
  int iVar2;
  char *pcVar3;
  int local_4;
  
  iVar2 = *(int *)(param_1 + 0x184);
  sVar1 = *(short *)(iVar2 + 0x9a);
  if (sVar1 != -1) {
    if (*(short *)(iVar2 + 0x9c) != 0) {
      *(short *)(iVar2 + 0x9c) = *(short *)(iVar2 + 0x9c) + -1;
    }
    if (*(short *)(iVar2 + 0x9c) == 0) {
      *(undefined2 *)(iVar2 + 0x9a) = 0xffff;
    }
    local_4 = param_1;
    pcVar3 = operator_new(100);
    local_4 = 0;
    thunk_FUN_0043b9a0((int)pcVar3,&local_4,0x4e);
    thunk_FUN_0043b970((int)pcVar3,&local_4,*(undefined2 *)(iVar2 + 4));
    thunk_FUN_0043b970((int)pcVar3,&local_4,sVar1);
    thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar3);
    operator_delete(pcVar3);
  }
  return;
}

