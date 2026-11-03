
void __fastcall FUN_0043b5b0(int param_1)

{
  int *piVar1;
  undefined4 *puVar2;
  undefined4 *puVar3;
  int iVar4;
  
  iVar4 = *(int *)(param_1 + 8);
  if (iVar4 != 0) {
    if (0 < *(int *)(iVar4 + 0xc)) {
      puVar3 = *(undefined4 **)(iVar4 + 4);
      while (puVar3 != (undefined4 *)0x0) {
        puVar2 = (undefined4 *)*puVar3;
        piVar1 = puVar3 + 2;
        puVar3 = puVar2;
        if ((int *)*piVar1 != (int *)0x0) {
          (**(code **)(*(int *)*piVar1 + 4))(1);
        }
      }
      CObList::RemoveAll(*(CObList **)(param_1 + 8));
    }
    if (*(int **)(param_1 + 8) != (int *)0x0) {
      (**(code **)(**(int **)(param_1 + 8) + 4))(1);
    }
    *(undefined4 *)(param_1 + 8) = 0;
  }
  *(undefined4 *)(param_1 + 4) = 0;
  if (*(void **)(param_1 + 0x1c) != (void *)0x0) {
    operator_delete(*(void **)(param_1 + 0x1c));
    *(undefined4 *)(param_1 + 0x1c) = 0;
  }
  if (*(void **)(param_1 + 0x20) != (void *)0x0) {
    operator_delete(*(void **)(param_1 + 0x20));
    *(undefined4 *)(param_1 + 0x20) = 0;
  }
  *(undefined4 *)(param_1 + 0x24) = 0;
  *(undefined4 *)(param_1 + 0x28) = 0;
  *(undefined4 *)(param_1 + 0x2c) = 0;
  *(undefined4 *)(param_1 + 0x30) = 0;
  *(undefined4 *)(param_1 + 0x38) = 0;
  if (*(int *)(param_1 + 0x10) != 0) {
    iVar4 = 0;
    do {
      iVar4 = iVar4 + 1;
      *(undefined1 *)(*(int *)(param_1 + 0x10) + -1 + iVar4) = 0;
    } while (iVar4 < 0x8dd8);
  }
  return;
}

