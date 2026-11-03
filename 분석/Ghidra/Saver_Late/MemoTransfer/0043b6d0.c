
undefined4 __thiscall FUN_0043b6d0(void *this,int param_1,int param_2)

{
  void *pvVar1;
  uint uVar2;
  
  if (*(void **)((int)this + 0x1c) != (void *)0x0) {
    operator_delete(*(void **)((int)this + 0x1c));
  }
  pvVar1 = operator_new(param_1 + 1U);
  *(void **)((int)this + 0x1c) = pvVar1;
  *(uint *)((int)this + 0x24) = param_1 + 1U;
  *(undefined4 *)((int)this + 0x2c) = 0;
  if (*(void **)((int)this + 0x20) != (void *)0x0) {
    operator_delete(*(void **)((int)this + 0x20));
  }
  if (param_2 == 0) {
    if (DAT_00497a98 == 1) {
      uVar2 = param_1 * 100;
    }
    else {
      uVar2 = param_1 * 10;
    }
  }
  else {
    uVar2 = param_2 + 1;
  }
  pvVar1 = operator_new(uVar2);
  *(uint *)((int)this + 0x28) = uVar2;
  *(void **)((int)this + 0x20) = pvVar1;
  *(int *)((int)this + 0x18) = param_2;
  *(undefined4 *)((int)this + 0x14) = 0xffffffff;
  *(undefined4 *)((int)this + 0x34) = 2;
  *(undefined4 *)((int)this + 0x38) = 0;
  *(undefined4 *)((int)this + 0x30) = 0;
  return 1;
}

