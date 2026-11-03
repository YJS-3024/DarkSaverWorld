
void __fastcall FUN_0046fdc0(void *param_1)

{
  undefined4 *puVar1;
  int iVar2;
  soldierlist *psVar3;
  void *pvVar4;
  short *psVar5;
  undefined4 *puVar6;
  undefined4 *puVar7;
  int local_50 [10];
  int local_28 [10];
  
  *(undefined4 *)((int)param_1 + 0x174) = 0;
  if (*(void **)((int)param_1 + 0x178) != (void *)0x0) {
    operator_delete(*(void **)((int)param_1 + 0x178));
    *(undefined4 *)((int)param_1 + 0x178) = 0;
  }
  if (*(void **)((int)param_1 + 0x17c) != (void *)0x0) {
    operator_delete(*(void **)((int)param_1 + 0x17c));
    *(undefined4 *)((int)param_1 + 0x17c) = 0;
  }
  puVar1 = (undefined4 *)thunk_FUN_00427b50(*(void **)((int)param_1 + 0xb0),0);
  *(undefined4 **)((int)param_1 + 0x184) = puVar1;
  iVar2 = 0;
  psVar5 = (short *)((int)puVar1 + 0x86);
  do {
    if (iVar2 < 0x1c) {
      *(int *)((int)local_50 + iVar2) = (int)psVar5[0xc];
    }
    else {
      *(undefined4 *)((int)local_50 + iVar2) = 0xffffffff;
    }
    *(int *)((int)local_28 + iVar2) = (int)*psVar5;
    iVar2 = iVar2 + 4;
    psVar5 = psVar5 + 1;
  } while (iVar2 < 0x28);
  psVar3 = GetSoldierList(local_50,local_28);
  if ((psVar3 != (soldierlist *)0x0) && (iVar2 = *(int *)psVar3, 0 < iVar2)) {
    *(int *)((int)param_1 + 0x174) = iVar2;
    pvVar4 = operator_new(iVar2 * 4);
    *(void **)((int)param_1 + 0x178) = pvVar4;
    pvVar4 = operator_new(*(int *)((int)param_1 + 0x174) << 2);
    *(void **)((int)param_1 + 0x17c) = pvVar4;
    iVar2 = 0;
    if (0 < *(int *)((int)param_1 + 0x174)) {
      do {
        *(undefined4 *)(*(int *)((int)param_1 + 0x178) + iVar2 * 4) = *(undefined4 *)(psVar3 + 4);
        *(undefined4 *)(*(int *)((int)param_1 + 0x17c) + iVar2 * 4) = *(undefined4 *)(psVar3 + 8);
        iVar2 = iVar2 + 1;
        psVar3 = psVar3 + 8;
      } while (iVar2 < *(int *)((int)param_1 + 0x174));
    }
  }
  puVar6 = *(undefined4 **)((int)param_1 + 0x184);
  iVar2 = 0xb5;
  puVar7 = puVar1;
  while( true ) {
    puVar7 = puVar7 + 1;
    puVar6 = puVar6 + 1;
    if (iVar2 == 0) break;
    iVar2 = iVar2 + -1;
    *puVar7 = *puVar6;
  }
  if (*(short *)((int)puVar1 + 0x9a) == -1) {
    *(undefined4 *)((int)param_1 + 0x188) = 0xffffffff;
    return;
  }
  iVar2 = thunk_FUN_004702c0(param_1,(int)*(short *)((int)puVar1 + 0x9a));
  *(int *)((int)param_1 + 0x188) = iVar2;
  return;
}

