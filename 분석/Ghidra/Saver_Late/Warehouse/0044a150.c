
void __thiscall FUN_0044a150(void *this,undefined4 param_1,int *param_2)

{
  int iVar1;
  
  if (*(int *)((int)this + 0x78) != 0) {
    thunk_FUN_00433be0(this,param_1,param_2);
    iVar1 = thunk_FUN_0044a910(this,(POINT *)((int)this + 0x10));
    if (iVar1 != 0) {
      if (*(int *)((int)this + 0x350) != 0) {
        *(undefined4 *)((int)this + 0x364) = 1;
        *(undefined4 *)((int)this + 0x368) = *(undefined4 *)((int)this + 0x34c);
        return;
      }
      *(undefined4 *)((int)this + 0x35c) = 1;
      *(undefined4 *)((int)this + 0x360) = *(undefined4 *)((int)this + 0x34c);
    }
  }
  return;
}

