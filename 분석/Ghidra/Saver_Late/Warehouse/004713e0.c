
void __thiscall FUN_004713e0(void *this,int param_1)

{
  short sVar1;
  int iVar2;
  short *psVar3;
  itemrecord *piVar4;
  void *pvVar5;
  undefined4 *puVar6;
  int iVar7;
  int iVar8;
  int iVar9;
  int local_8;
  int local_4;
  
  iVar2 = param_1;
  local_8 = 0;
  psVar3 = (short *)thunk_FUN_0043c3f0(param_1,&local_8);
  iVar8 = (int)*psVar3;
  param_1 = 0;
  iVar7 = 0;
  iVar9 = iVar8;
  if (0 < iVar8) {
    do {
      local_4 = iVar9;
      psVar3 = (short *)thunk_FUN_0043c3f0(iVar2,&local_8);
      sVar1 = *psVar3;
      thunk_FUN_0043c3f0(iVar2,&local_8);
      piVar4 = GetItemTable((int)sVar1);
      if ((piVar4 != (itemrecord *)0x0) && (*(int *)piVar4 == (int)sVar1)) {
        param_1 = param_1 + 1;
      }
      iVar9 = local_4 + -1;
    } while (local_4 + -1 != 0);
    local_4 = 0;
    iVar7 = param_1;
  }
  *(undefined4 *)((int)this + 0xb8) = 0;
  if (*(void **)((int)this + 0xbc) != (void *)0x0) {
    operator_delete(*(void **)((int)this + 0xbc));
    *(undefined4 *)((int)this + 0xbc) = 0;
  }
  if (*(void **)((int)this + 0x378) != (void *)0x0) {
    operator_delete(*(void **)((int)this + 0x378));
    *(undefined4 *)((int)this + 0x378) = 0;
  }
  *(int *)((int)this + 0xb8) = iVar7;
  pvVar5 = operator_new(iVar7 * 4);
  *(void **)((int)this + 0xbc) = pvVar5;
  pvVar5 = operator_new(*(int *)((int)this + 0xb8) << 2);
  *(void **)((int)this + 0x378) = pvVar5;
  local_8 = 2;
  if (0 < iVar8) {
    iVar7 = 0;
    param_1 = iVar8;
    do {
      psVar3 = (short *)thunk_FUN_0043c3f0(iVar2,&local_8);
      iVar9 = (int)*psVar3;
      psVar3 = (short *)thunk_FUN_0043c3f0(iVar2,&local_8);
      local_4 = (int)*psVar3;
      piVar4 = GetItemTable(iVar9);
      if ((piVar4 != (itemrecord *)0x0) && (*(int *)piVar4 == iVar9)) {
        *(int *)(*(int *)((int)this + 0xbc) + iVar7) = iVar9;
        *(int *)(*(int *)((int)this + 0x378) + iVar7) = local_4;
        iVar7 = iVar7 + 4;
      }
      param_1 = param_1 + -1;
    } while (param_1 != 0);
  }
  puVar6 = (undefined4 *)thunk_FUN_0043c4c0(iVar2,&local_8);
  *(undefined4 *)((int)this + 0x370) = *puVar6;
  *(undefined4 *)((int)this + 0x36c) = 1;
  return;
}

