
void FUN_0046ef00(int param_1)

{
  if (DAT_00497c40 != (int *)0x0) {
    if (param_1 == 0xd) {
      (**(code **)(*DAT_00497c40 + 0x10))(0);
    }
    else if (param_1 == 0x1b) {
      (**(code **)(*DAT_00497c40 + 0x10))(1);
      return;
    }
  }
  return;
}

