
void __thiscall FUN_004426a0(void *this,int param_1)

{
  int iVar1;
  undefined4 *puVar2;
  int iVar3;
  int iVar4;
  
  iVar4 = 0;
  iVar1 = *(int *)((int)this + 0xc);
  if (0 < iVar1) {
    do {
      iVar3 = (**(code **)(**(int **)(*(int *)((int)this + 8) + iVar4 * 4) + 4))();
      if (iVar3 == param_1) {
        thunk_FUN_00434c20(*(int **)(*(int *)((int)this + 8) + iVar4 * 4));
        puVar2 = *(undefined4 **)(*(int *)((int)this + 8) + iVar4 * 4);
        if (puVar2 != (undefined4 *)0x0) {
          (**(code **)*puVar2)(1);
        }
        CPtrArray::RemoveAt((CPtrArray *)((int)this + 4),iVar4,1);
        break;
      }
      iVar4 = iVar4 + 1;
    } while (iVar4 < iVar1);
  }
  iVar1 = *(int *)((int)this + 0xc);
  *(int *)((int)this + 0x18) = iVar1;
  if (*(int *)((int)this + 0x1c) == iVar1) {
    *(int *)((int)this + 0x1c) = iVar1 + -1;
  }
  return;
}

