
void __fastcall FUN_004712e0(int *param_1)

{
  int iVar1;
  int iVar2;
  char *pcVar3;
  undefined2 uVar4;
  undefined4 uVar5;
  
  iVar2 = param_1[0x2c];
  iVar1 = param_1[0xda];
  if (((*(int *)(iVar2 + 0x44) == 0) || (*(int *)(iVar2 + 0x48) == 0)) ||
     (*(int *)(iVar2 + 0x40) <= iVar1)) {
    uVar5 = 0xffffffff;
    uVar4 = 0;
  }
  else {
    uVar5 = *(undefined4 *)(*(int *)(iVar2 + 0x44) + iVar1 * 4);
    uVar4 = (undefined2)*(undefined4 *)(*(int *)(iVar2 + 0x48) + iVar1 * 4);
  }
  iVar2 = (**(code **)(*param_1 + 0x60))(uVar5);
  if (iVar2 != 0) {
    pcVar3 = operator_new(10);
    thunk_FUN_0043b9a0((int)pcVar3,(int *)&stack0xfffffff8,0x99);
    thunk_FUN_0043b970((int)pcVar3,(int *)&stack0xfffffff8,(short)param_1[0xdd]);
    thunk_FUN_0043b970((int)pcVar3,(int *)&stack0xfffffff8,(short)uVar5);
    thunk_FUN_0043b970((int)pcVar3,(int *)&stack0xfffffff8,uVar4);
    thunk_FUN_004132d0(DAT_004952a0,0,pcVar3);
    operator_delete(pcVar3);
  }
  return;
}

