
void __thiscall FUN_0046ec70(void *this,int param_1)

{
  int iVar1;
  
  if (param_1 == 0) {
    thunk_FUN_0046ef40((int)this);
    iVar1 = (**(code **)(*(int *)this + 4))();
    thunk_FUN_004758e0(*(void **)((int)this + 0xa0),iVar1);
  }
  else if (param_1 == 1) {
    iVar1 = (**(code **)(*(int *)this + 4))();
    thunk_FUN_004758e0(*(void **)((int)this + 0xa0),iVar1);
    return;
  }
  return;
}

