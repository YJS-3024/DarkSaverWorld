
void FUN_00458750(int param_1)

{
  if (param_1 == 0xd) {
    if (DAT_00497b84 == 0) {
      return;
    }
    thunk_FUN_004587c0(DAT_00497b84);
  }
  else if (param_1 != 0x1b) {
    return;
  }
  if (DAT_00497b84 == 0) {
    return;
  }
  thunk_FUN_00458790(DAT_00497b84);
  return;
}

