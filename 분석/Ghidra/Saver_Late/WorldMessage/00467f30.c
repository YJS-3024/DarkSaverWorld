
undefined4 FUN_00467f30(void)

{
  int iVar1;
  
  if (DAT_00497c04 != (int *)0x0) {
    iVar1 = (**(code **)(*DAT_00497c04 + 4))();
    thunk_FUN_004758e0((void *)DAT_00497c04[0x28],iVar1);
    DAT_00497c04 = (int *)0x0;
    return 1;
  }
  return 0;
}

