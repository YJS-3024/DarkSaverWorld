
void __fastcall FUN_0044a5d0(int param_1)

{
  itemrecord *piVar1;
  void *pvVar2;
  int iVar3;
  int iVar4;
  int iVar5;
  
  iVar3 = 0;
  iVar4 = *(int *)(param_1 + 0xb4) + 100;
  *(undefined4 *)(param_1 + 0xb8) = 0;
  if (*(void **)(param_1 + 0xbc) != (void *)0x0) {
    operator_delete(*(void **)(param_1 + 0xbc));
    *(undefined4 *)(param_1 + 0xbc) = 0;
  }
  for (iVar5 = *(int *)(param_1 + 0xb4); iVar5 < iVar4; iVar5 = iVar5 + 1) {
    piVar1 = GetItemTable(iVar5);
    if ((piVar1 != (itemrecord *)0x0) && (*(int *)piVar1 == iVar5)) {
      *(int *)(param_1 + 0xb8) = *(int *)(param_1 + 0xb8) + 1;
    }
  }
  if (*(int *)(param_1 + 0xb8) != 0) {
    pvVar2 = operator_new(*(int *)(param_1 + 0xb8) << 2);
    iVar5 = *(int *)(param_1 + 0xb4);
    *(void **)(param_1 + 0xbc) = pvVar2;
    for (; iVar5 < iVar4; iVar5 = iVar5 + 1) {
      piVar1 = GetItemTable(iVar5);
      if ((piVar1 != (itemrecord *)0x0) && (*(int *)piVar1 == iVar5)) {
        iVar3 = iVar3 + 4;
        *(int *)(*(int *)(param_1 + 0xbc) + -4 + iVar3) = iVar5;
      }
    }
  }
  return;
}

