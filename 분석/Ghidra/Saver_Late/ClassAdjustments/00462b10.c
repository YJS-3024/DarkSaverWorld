
void __fastcall FUN_00462b10(int param_1)

{
  char *pcVar1;
  int local_4;
  
  local_4 = param_1;
  pcVar1 = operator_new(10);
  local_4 = 0;
  thunk_FUN_0043b9a0((int)pcVar1,&local_4,0x8a);
  thunk_FUN_0043b970((int)pcVar1,&local_4,*(undefined2 *)(*(int *)(param_1 + 0x13c) + 4));
  thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar1);
  operator_delete(pcVar1);
  return;
}

