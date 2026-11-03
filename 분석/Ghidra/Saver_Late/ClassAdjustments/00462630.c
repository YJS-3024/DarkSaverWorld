
void __fastcall FUN_00462630(int param_1)

{
  CString *this;
  CString *this_00;
  int iVar1;
  abilityrecord *paVar2;
  
  this = (CString *)(param_1 + 300);
  CString::Empty(this);
  this_00 = (CString *)(param_1 + 0x128);
  CString::Empty(this_00);
  iVar1 = *(int *)(param_1 + 0xf4);
  if (iVar1 == 0) {
    param_1 = *(int *)(param_1 + 0xec);
  }
  else if (iVar1 == 1) {
    param_1 = *(int *)(param_1 + 0xe8);
  }
  else if (iVar1 == 2) {
    param_1 = *(int *)(param_1 + 0x100 + *(int *)(param_1 + 0xfc) * 4);
  }
  paVar2 = GetCharAbility(param_1);
  if (paVar2 != (abilityrecord *)0x0) {
    CString::operator=(this_00,(char *)(paVar2 + 0xa0));
    CString::operator=(this,(char *)(paVar2 + 0x1a0));
  }
  return;
}

