
void __fastcall FUN_00470070(void *param_1)

{
  int iVar1;
  char *pcVar2;
  undefined4 uVar3;
  int iVar4;
  void *local_4;
  
  iVar1 = *(int *)((int)param_1 + 0x184);
  local_4 = param_1;
  pcVar2 = operator_new(100);
  iVar4 = 0;
  local_4 = (void *)0x0;
  if (*(short *)(iVar1 + 0x9a) != -1) {
    iVar4 = thunk_FUN_00470020(param_1,(int)*(short *)(iVar1 + 0x9a));
  }
  if (*(short *)(iVar1 + 0x9a) == -1) {
    iVar4 = *(int *)((int)param_1 + 0x188);
    if ((iVar4 == -1) || (*(int *)((int)param_1 + 0x174) <= iVar4)) goto LAB_00470184;
    iVar4 = *(int *)(*(int *)((int)param_1 + 0x178) + iVar4 * 4);
    uVar3 = thunk_FUN_00470020(param_1,iVar4);
    thunk_FUN_0043b9a0((int)pcVar2,(int *)&local_4,0x4d);
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,*(undefined2 *)(iVar1 + 4));
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,(short)iVar4);
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,(short)uVar3);
  }
  else {
    if (iVar4 <= *(short *)(iVar1 + 0x9c)) goto LAB_00470184;
    *(short *)(iVar1 + 0x9c) = *(short *)(iVar1 + 0x9c) + 1;
    thunk_FUN_0043b9a0((int)pcVar2,(int *)&local_4,0x4d);
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,*(undefined2 *)(iVar1 + 4));
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,*(undefined2 *)(iVar1 + 0x9a));
    thunk_FUN_0043b970((int)pcVar2,(int *)&local_4,(short)iVar4);
  }
  thunk_FUN_004132d0(DAT_004952a0,(int)local_4,pcVar2);
LAB_00470184:
  operator_delete(pcVar2);
  return;
}

