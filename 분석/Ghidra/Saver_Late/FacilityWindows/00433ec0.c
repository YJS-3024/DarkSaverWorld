
void __thiscall FUN_00433ec0(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  
  iVar1 = param_2[1];
  *(int *)((int)this + 0x10) = *param_2 - *(int *)((int)this + 0x40);
  *(int *)((int)this + 0x14) = iVar1 - *(int *)((int)this + 0x44);
  if (((*(int *)((int)this + 0x78) != 0) && (*(int *)((int)this + 0x24) != 0)) &&
     (*(int *)((int)this + 0x7c) == 0)) {
    *(int *)((int)this + 0x40) =
         *(int *)((int)this + 0x40) + (*(int *)((int)this + 0x10) - *(int *)((int)this + 0x28));
    *(int *)((int)this + 0x44) =
         *(int *)((int)this + 0x44) + (*(int *)((int)this + 0x14) - *(int *)((int)this + 0x2c));
  }
  return;
}

