
bool __fastcall FUN_0043b7b0(int param_1)

{
  byte bVar1;
  undefined3 extraout_var;
  
  if (*(int *)(param_1 + 0x18) == 0) {
    return true;
  }
  bVar1 = FUN_00478930(&LAB_00401226,&LAB_00401717,*(int *)(param_1 + 0x10),param_1);
  return (bool)('\x01' - (CONCAT31(extraout_var,bVar1) != 0));
}

