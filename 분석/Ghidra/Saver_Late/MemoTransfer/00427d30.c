
void __thiscall FUN_00427d30(void *this,dword arg0)

{
  short sVar1;
  short sVar2;
  int iVar3;
  int iVar4;
  bool bVar5;
  short *psVar6;
  int *piVar7;
  undefined3 extraout_var;
  byte *pbVar8;
  undefined4 *puVar9;
  int extraout_ECX;
  uint uVar10;
  uint uVar11;
  undefined4 *puVar12;
  int iStack00000008;
  undefined4 *in_stack_00002014;
  
  FUN_00479c00();
  iStack00000008 = extraout_ECX;
  thunk_FUN_0043b5b0(0x4950f8);
  psVar6 = (short *)thunk_FUN_0043c3f0((int)in_stack_00002014,(int *)&stack0x00000000);
  sVar1 = *psVar6;
  psVar6 = (short *)thunk_FUN_0043c3f0((int)in_stack_00002014,(int *)&stack0x00000000);
  sVar2 = *psVar6;
  piVar7 = (int *)thunk_FUN_0043c4c0((int)in_stack_00002014,(int *)&stack0x00000000);
  iVar3 = *piVar7;
  uVar11 = (uint)sVar1;
  puVar9 = (undefined4 *)&stack0x0000000c;
  for (uVar10 = uVar11 >> 2; uVar10 != 0; uVar10 = uVar10 - 1) {
    *puVar9 = *in_stack_00002014;
    in_stack_00002014 = in_stack_00002014 + 1;
    puVar9 = puVar9 + 1;
  }
  for (uVar10 = uVar11 & 3; uVar10 != 0; uVar10 = uVar10 - 1) {
    *(undefined1 *)puVar9 = *(undefined1 *)in_stack_00002014;
    in_stack_00002014 = (undefined4 *)((int)in_stack_00002014 + 1);
    puVar9 = (undefined4 *)((int)puVar9 + 1);
  }
  thunk_FUN_0043b6d0(&DAT_004950f8,uVar11,(int)sVar2);
  puVar9 = (undefined4 *)&stack0x0000000c;
  puVar12 = DAT_00495114;
  for (uVar10 = uVar11 >> 2; uVar10 != 0; uVar10 = uVar10 - 1) {
    *puVar12 = *puVar9;
    puVar9 = puVar9 + 1;
    puVar12 = puVar12 + 1;
  }
  for (uVar11 = uVar11 & 3; uVar11 != 0; uVar11 = uVar11 - 1) {
    *(undefined1 *)puVar12 = *(undefined1 *)puVar9;
    puVar9 = (undefined4 *)((int)puVar9 + 1);
    puVar12 = (undefined4 *)((int)puVar12 + 1);
  }
  bVar5 = thunk_FUN_0043b7b0(0x4950f8);
  iVar4 = DAT_00495118;
  if (CONCAT31(extraout_var,bVar5) == 0) {
    thunk_FUN_0043b7f0(0x4950f8);
    return;
  }
  if (DAT_00495130 != 0) {
    thunk_FUN_0043b7f0(0x4950f8);
    return;
  }
  if (DAT_0049510c != iVar3) {
    thunk_FUN_0043b7f0(0x4950f8);
    return;
  }
  if (sVar2 != DAT_00495128) {
    thunk_FUN_0043b7f0(0x4950f8);
    return;
  }
  pbVar8 = (byte *)thunk_FUN_0043c420(DAT_00495118,(int *)&stack0x00000000);
  uVar10 = (uint)*pbVar8;
  if (*pbVar8 != 0) {
    do {
      puVar9 = thunk_FUN_0043c690(iVar4,(int *)&stack0x00000000);
      CPtrArray::SetAtGrow((CPtrArray *)(iStack00000008 + 4),*(int *)(iStack00000008 + 0xc),puVar9);
      uVar10 = uVar10 - 1;
    } while (uVar10 != 0);
  }
  return;
}

