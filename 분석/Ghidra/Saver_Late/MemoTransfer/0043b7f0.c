
void __fastcall FUN_0043b7f0(int param_1)

{
  int iVar1;
  
  if (*(void **)(param_1 + 0x1c) != (void *)0x0) {
    operator_delete(*(void **)(param_1 + 0x1c));
    *(undefined4 *)(param_1 + 0x1c) = 0;
  }
  *(undefined4 *)(param_1 + 0x24) = 0;
  *(undefined4 *)(param_1 + 0x2c) = 0;
  if (*(void **)(param_1 + 0x20) != (void *)0x0) {
    operator_delete(*(void **)(param_1 + 0x20));
    *(undefined4 *)(param_1 + 0x20) = 0;
  }
  *(undefined4 *)(param_1 + 0x28) = 0;
  *(undefined4 *)(param_1 + 0x38) = 0;
  *(undefined4 *)(param_1 + 0x30) = 0;
  if (*(int *)(param_1 + 0x10) != 0) {
    iVar1 = 0;
    do {
      iVar1 = iVar1 + 1;
      *(undefined1 *)(*(int *)(param_1 + 0x10) + -1 + iVar1) = 0;
    } while (iVar1 < 0x8dd8);
  }
  return;
}

