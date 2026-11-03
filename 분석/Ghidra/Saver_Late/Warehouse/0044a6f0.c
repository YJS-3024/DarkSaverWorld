
void __thiscall FUN_0044a6f0(void *this,int param_1)

{
  int iVar1;
  
  if (param_1 == 0) {
    if (0 < *(int *)((int)this + 0xc0)) {
      *(int *)((int)this + 0xc0) = *(int *)((int)this + 0xc0) + -3;
    }
    if (*(int *)((int)this + 0xc0) < 0) {
      *(undefined4 *)((int)this + 0xc0) = 0;
    }
  }
  else if (param_1 == 1) {
    iVar1 = *(int *)((int)this + 0xc0) + 3;
    if (iVar1 / 3 < (*(int *)((int)this + 0xb8) + -7) / 3) {
      *(int *)((int)this + 0xc0) = iVar1;
      return;
    }
  }
  return;
}

