
void __cdecl FUN_1000d870(int param_1)

{
  short sVar1;
  short *psVar2;
  int local_4;
  
  local_4 = 0;
  psVar2 = (short *)FUN_1000c540(param_1,&local_4);
  sVar1 = *psVar2;
  setmonsterai((int)sVar1);
  AIStart((int)sVar1);
  return;
}

