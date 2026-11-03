
void __fastcall FUN_00445690(int param_1)

{
  char *pcVar1;
  int local_4;
  
  local_4 = param_1;
  pcVar1 = operator_new(10);
  local_4 = 0;
  *(undefined4 *)(param_1 + 0xc0) = 0;
  thunk_FUN_0043b9a0((int)pcVar1,&local_4,0x93);
  thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar1);
  operator_delete(pcVar1);
  return;
}

