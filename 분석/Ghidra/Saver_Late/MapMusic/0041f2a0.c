
void __thiscall FUN_0041f2a0(void *this,int param_1)

{
  if (param_1 != DAT_00495320) {
    DAT_00495320 = param_1;
    if (param_1 != 0) {
      thunk_FUN_0041f310(this,*(int *)((int)this + 4));
      return;
    }
    thunk_FUN_0041f400((int)this);
  }
  return;
}

