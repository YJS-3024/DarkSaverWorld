
int __thiscall FUN_0041f8f0(void *this,char *param_1,int param_2)

{
  int iVar1;
  
  if (DAT_00495320 == 0) {
    thunk_FUN_0041f400((int)this);
    return 0;
  }
  iVar1 = CDirectMusic::Play(*(CDirectMusic **)(DAT_004952a0 + 0x124),param_1,param_2);
  return iVar1;
}

