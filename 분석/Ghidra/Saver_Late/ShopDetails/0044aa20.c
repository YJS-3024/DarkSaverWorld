
int __thiscall FUN_0044aa20(void *this,int param_1,int param_2)

{
  int iVar1;
  int iVar2;
  
  iVar1 = -1;
  if (param_1 == 0) {
    iVar2 = *(int *)((int)this + 0xc0) + param_2;
    if (iVar2 < *(int *)((int)this + 0xb8)) {
      iVar1 = iVar2;
    }
  }
  else if (param_2 < *(int *)(*(int *)((int)this + 0xb0) + 0x40)) {
    return param_2;
  }
  return iVar1;
}

