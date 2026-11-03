
void __fastcall FUN_00462250(void *param_1)

{
  int iVar1;
  char *pcVar2;
  void *local_4;
  
  iVar1 = *(int *)((int)param_1 + 0x13c);
  if (*(short *)(iVar1 + 0x6e) != 0) {
    local_4 = param_1;
    pcVar2 = operator_new(10);
    local_4 = (void *)0x0;
    thunk_FUN_0043b9a0((int)pcVar2,(int *)&local_4,0x85);
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,*(undefined2 *)(iVar1 + 4));
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,
                       *(undefined2 *)(iVar1 + 0x70 + *(int *)((int)param_1 + 0xb8) * 2));
    thunk_FUN_00462310(param_1,*(int *)((int)param_1 + 0xb8));
    thunk_FUN_004132d0(DAT_004952a0,(int)local_4,pcVar2);
    operator_delete(pcVar2);
  }
  return;
}

