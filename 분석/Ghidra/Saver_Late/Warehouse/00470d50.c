
void __fastcall FUN_00470d50(undefined4 *param_1)

{
  *param_1 = &PTR_LAB_00486430;
  if ((void *)param_1[0xde] != (void *)0x0) {
    operator_delete((void *)param_1[0xde]);
    param_1[0xde] = 0;
  }
  thunk_FUN_00449f30(param_1);
  return;
}

