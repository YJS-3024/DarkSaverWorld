
void FUN_00449950(void)

{
  char *pcVar1;
  int local_4;
  
  pcVar1 = operator_new(0x14);
  local_4 = 0;
  thunk_FUN_0043b9a0((int)pcVar1,&local_4,0xd8);
  thunk_FUN_004132d0(DAT_004952a0,local_4,pcVar1);
  operator_delete(pcVar1);
  return;
}

